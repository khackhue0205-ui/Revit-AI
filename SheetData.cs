using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BTVN1_Application.Models
{
    public enum SheetImportStatus
    {
        New,           // Sheet number chưa tồn tại → tạo bình thường
        NumberChanged, // Số bị trùng → đã thêm "A" vào cuối
    }

    /// <summary>Đại diện cho một dòng dữ liệu sheet từ file import</summary>
    public class SheetData : INotifyPropertyChanged
    {
        // ── Dữ liệu từ file ──────────────────────────────────────────────────
        public string OriginalNumber { get; set; } = "";
        public string Name           { get; set; } = "";

        // ── Sau khi resolve duplicate ─────────────────────────────────────────
        public string           FinalNumber { get; set; } = "";
        public SheetImportStatus Status     { get; set; } = SheetImportStatus.New;

        // ── User chọn/bỏ chọn ────────────────────────────────────────────────
        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        // ── Display helpers ───────────────────────────────────────────────────
        public string StatusLabel => Status switch
        {
            SheetImportStatus.NumberChanged => "CẬP NHẬT",
            _                              => "MỚI",
        };

        public bool IsNumberChanged => Status == SheetImportStatus.NumberChanged;

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
