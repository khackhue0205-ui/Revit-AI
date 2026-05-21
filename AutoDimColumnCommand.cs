using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BTVN1_Application.Services;
using BTVN1_Application.ViewModels;
using BTVN1_Application.Views;
using Nice3point.Revit.Toolkit.External;
using Serilog;

namespace BTVN1_Application.Commands
{
    /// <summary>
    /// Lệnh Tự Động Đim Mặt Bằng Định Vị Cột.
    /// - Kiểm tra view hiện tại là Floor Plan / Structural Plan
    /// - Mở dialog cài đặt
    /// - Chạy ColumnDimensioner
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class AutoDimColumnCommand : ExternalCommand
    {
        // ── Shortcut properties ───────────────────────────────────────────────
        private UIDocument UiDoc  => Application.ActiveUIDocument;
        private Document   Doc    => Application.ActiveUIDocument.Document;

        public override void Execute()
        {
            // ── Validate view ──────────────────────────────────────────────
            var view = Doc.ActiveView;
            if (view.ViewType is not (ViewType.FloorPlan or ViewType.EngineeringPlan))
            {
                TaskDialog.Show(
                    "Sai loại view",
                    "Vui lòng mở một Floor Plan hoặc Structural Plan trước khi chạy lệnh này.");
                return;
            }

            // ── Load DimensionTypes ────────────────────────────────────────
            var dimTypes = LoadLinearDimTypes(Doc);
            if (!dimTypes.Any())
            {
                TaskDialog.Show("Lỗi", "Không tìm thấy Linear Dimension Type nào trong project.");
                return;
            }

            // ── Pre-selection: columns đã chọn trước khi mở dialog ────────
            var preSelected = GetPreSelectedColumns(UiDoc, Doc);

            // ── Build ViewModel & open dialog ─────────────────────────────
            var vm = new AutoDimViewModel();
            foreach (var dt in dimTypes)
                vm.DimensionTypes.Add(dt);

            vm.SelectedDimensionType = vm.DimensionTypes.First();

            if (preSelected.Count > 0)
            {
                vm.ColumnCount   = preSelected.Count;
                vm.UseActiveView = false;
            }
            else
            {
                vm.ColumnCount   = CountColumnsInView(Doc, view);
                vm.UseActiveView = true;
            }

            var dialog = new AutoDimWindow(vm);
            dialog.ShowDialog();

            if (!dialog.IsConfirmed) return;

            var settings = dialog.GetSettings();

            // ── Gather columns ─────────────────────────────────────────────
            List<FamilyInstance> columns;
            if (!settings.UseActiveView && preSelected.Count > 0)
                columns = preSelected;
            else
                columns = GetColumnsInView(Doc, view);

            if (!columns.Any())
            {
                TaskDialog.Show("Thông báo", "Không tìm thấy cột kết cấu nào trong phạm vi đã chọn.");
                return;
            }

            // ── Run ────────────────────────────────────────────────────────
            try
            {
                var svc = new ColumnDimensioner(Doc, view, settings);
                var (success, skipped) = svc.Run(columns);

                var msg = "✅ Hoàn thành!\n\n" +
                          $"Cột được dim:  {success}\n" +
                          (skipped > 0
                              ? $"Cột bị bỏ qua: {skipped}  (xem Output Window để biết chi tiết)"
                              : "");

                TaskDialog.Show("Tự Động Đim Định Vị Cột", msg);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AutoDimColumnCommand failed");
                TaskDialog.Show("Lỗi", $"Có lỗi xảy ra:\n\n{ex.Message}");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static List<DimensionTypeItem> LoadLinearDimTypes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .Where(dt => dt.StyleType == DimensionStyleType.Linear)
                .OrderBy(dt => dt.Name)
                .Select(dt => new DimensionTypeItem { Id = dt.Id, Name = dt.Name })
                .ToList();
        }

        private static List<FamilyInstance> GetPreSelectedColumns(UIDocument uiDoc, Document doc)
        {
            try
            {
                return uiDoc.Selection.GetElementIds()
                    .Select(id => doc.GetElement(id))
                    .OfType<FamilyInstance>()
                    .Where(IsStructuralColumn)
                    .ToList();
            }
            catch
            {
                return new List<FamilyInstance>();
            }
        }

        private static List<FamilyInstance> GetColumnsInView(Document doc, View view)
        {
            return new FilteredElementCollector(doc, view.Id)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .ToList();
        }

        private static int CountColumnsInView(Document doc, View view)
        {
            return new FilteredElementCollector(doc, view.Id)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .OfClass(typeof(FamilyInstance))
                .GetElementCount();
        }

        private static bool IsStructuralColumn(FamilyInstance fi)
        {
            return fi.Category?.Id == new ElementId(BuiltInCategory.OST_StructuralColumns);
        }
    }
}
