using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BTVN1_Application.Models;
using BTVN1_Application.Services;
using BTVN1_Application.ViewModels;
using BTVN1_Application.Views;
using Nice3point.Revit.Toolkit.External;
using Serilog;

namespace BTVN1_Application.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class SheetDuplicatorCommand : ExternalCommand
    {
        private Document Doc => Application.ActiveUIDocument.Document;

        public override void Execute()
        {
            // ── Load all sheets ────────────────────────────────────────────
            var sheets = LoadSheets(Doc);

            if (!sheets.Any())
            {
                TaskDialog.Show("Nhân Bản Sheet", "Không tìm thấy sheet nào trong project.");
                return;
            }

            // ── Build ViewModel & open dialog ─────────────────────────────
            var vm = new SheetDuplicatorViewModel();
            foreach (var s in sheets)
                vm.AddSheet(s);
            vm.RefreshStatus();

            var dialog = new SheetDuplicatorWindow(vm);
            dialog.ShowDialog();

            if (!dialog.IsConfirmed) return;

            var settings      = dialog.GetSettings();
            var selectedSheets = vm.SelectedSheets.ToList();

            // ── Run inside a single Transaction ──────────────────────────
            try
            {
                using var tx = new Transaction(Doc, "Nhân Bản Sheet");
                tx.Start();

                var svc     = new SheetDuplicatorService(Doc, settings);
                var results = svc.Run(selectedSheets);

                tx.Commit();

                ShowSummary(results);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SheetDuplicatorCommand failed");
                TaskDialog.Show("Lỗi", $"Có lỗi xảy ra:\n\n{ex.Message}");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static List<SheetInfo> LoadSheets(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(s => !s.IsPlaceholder)
                .OrderBy(s => s.SheetNumber)
                .Select(s => new SheetInfo
                {
                    Id           = s.Id,
                    SheetNumber  = s.SheetNumber,
                    SheetName    = s.Name,
                    ViewportCount = CountViewports(doc, s),
                })
                .ToList();
        }

        private static int CountViewports(Document doc, ViewSheet sheet)
        {
            var vpCount = sheet.GetAllViewports().Count;
            var scheduleCount = new FilteredElementCollector(doc, sheet.Id)
                .OfClass(typeof(ScheduleSheetInstance))
                .GetElementCount();
            return vpCount + scheduleCount;
        }

        private static void ShowSummary(IReadOnlyList<DuplicateResult> results)
        {
            var success  = results.Count(r => r.Success);
            var failed   = results.Count(r => !r.Success);
            var warnings = results.Sum(r => r.Warnings.Count);

            var sb = new StringBuilder();
            sb.AppendLine($"Hoàn thành nhân bản {success}/{results.Count} sheet.");

            if (failed > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Sheet thất bại ({failed}):");
                foreach (var r in results.Where(r => !r.Success))
                    sb.AppendLine($"  • {r.OriginalSheetNumber} – {r.Error}");
            }

            if (warnings > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Cảnh báo ({warnings} view bị bỏ qua):");
                foreach (var r in results.Where(r => r.Warnings.Count > 0))
                foreach (var w in r.Warnings)
                    sb.AppendLine($"  [{r.OriginalSheetNumber}] {w}");
            }

            TaskDialog.Show("Nhân Bản Sheet", sb.ToString());
        }
    }
}
