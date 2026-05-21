using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BTVN1_Application.ViewModels;
using BTVN1_Application.Views;
using Nice3point.Revit.Toolkit.External;
using Serilog;

namespace BTVN1_Application.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CreateSheetCommand : ExternalCommand
    {
        public override void Execute()
        {
            try
            {
                var doc = Application.ActiveUIDocument.Document;

                // Chỉ chạy trong Document context (không phải Family Editor)
                if (doc.IsFamilyDocument)
                {
                    TaskDialog.Show("Thông báo",
                        "Tool này chỉ hoạt động trong môi trường Project, không phải Family Editor.");
                    return;
                }

                var vm  = new CreateSheetViewModel(doc);
                var wnd = new CreateSheetWindow(vm);

                // Gắn vào Revit window để không bị che khuất
                new System.Windows.Interop.WindowInteropHelper(wnd)
                {
                    Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle
                };

                wnd.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CreateSheetCommand failed");
                TaskDialog.Show("Lỗi", ex.Message);
            }
        }
    }
}
