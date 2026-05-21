using BTVN1_Application.Commands;
using Nice3point.Revit.Toolkit.External;
using Serilog;
using Serilog.Events;

namespace BTVN1_Application
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateLogger();
            CreateRibbon();
        }

        public override void OnShutdown()
        {
            Log.CloseAndFlush();
        }

        private void CreateRibbon()
        {
            var panel = Application.CreatePanel("Commands", "BTVN1 Application");

            panel.AddPushButton<StartupCommand>("Execute")
                .SetImage("/BTVN1 Application;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/BTVN1 Application;component/Resources/Icons/RibbonIcon32.png");

            panel.AddPushButton<AutoDimColumnCommand>("Đim\nĐịnh Vị Cột")
                .SetImage("/BTVN1 Application;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/BTVN1 Application;component/Resources/Icons/RibbonIcon32.png")
                .SetToolTip("Tự động tạo dimension cho cột kết cấu trên mặt bằng định vị")
                .SetLongDescription(
                    "Đim 2 chiều kích thước thân cột (Rộng × Sâu) và đim khoảng cách " +
                    "từ mặt cột đến grid gần nhất theo cả hai phương X và Y.");

            panel.AddPushButton<SheetDuplicatorCommand>("Nhân Bản\nSheet")
                .SetImage("/BTVN1 Application;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/BTVN1 Application;component/Resources/Icons/RibbonIcon32.png")
                .SetToolTip("Nhân bản sheet và toàn bộ nội dung (view, legend, schedule)")
                .SetLongDescription(
                    "Chọn một hoặc nhiều sheet, add-in sẽ tạo sheet mới kèm đầy đủ " +
                    "viewport, legend và schedule với vị trí giống hệt bản gốc.");

            panel.AddPushButton<CreateSheetCommand>("Tạo Sheet\ntừ File")
                .SetImage("/BTVN1 Application;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/BTVN1 Application;component/Resources/Icons/RibbonIcon32.png")
                .SetToolTip("Tạo hàng loạt Sheet từ file Excel (.xlsx) hoặc CSV (.csv)")
                .SetLongDescription(
                    "Import danh sách Sheet Number và Sheet Name từ file Excel hoặc CSV. " +
                    "Hỗ trợ chọn Title Block, preview trước khi tạo, và tự động xử lý " +
                    "số tờ bị trùng bằng cách thêm 'A' vào cuối.");
        }

        private static void CreateLogger()
        {
            const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug(LogEventLevel.Debug, outputTemplate)
                .MinimumLevel.Debug()
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var exception = (Exception)args.ExceptionObject;
                Log.Fatal(exception, "Domain unhandled exception");
            };
        }
    }
}