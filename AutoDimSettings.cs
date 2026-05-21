using Autodesk.Revit.DB;

namespace BTVN1_Application.Models
{
    /// <summary>
    /// Lưu trữ các cài đặt người dùng cho lệnh Tự Động Đim Định Vị Cột
    /// </summary>
    public class AutoDimSettings
    {
        /// <summary>Id của DimensionType được chọn</summary>
        public ElementId DimensionTypeId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Khoảng cách từ face cột đến đường dim (mm)</summary>
        public double DimOffsetMm { get; set; } = 200;

        /// <summary>Tạo dim chain ngang (Rộng cột + khoảng tới grid dọc)</summary>
        public bool DimChainX { get; set; } = true;

        /// <summary>Tạo dim chain dọc (Sâu cột + khoảng tới grid ngang)</summary>
        public bool DimChainY { get; set; } = true;

        /// <summary>Xóa dim cũ trước khi dim lại</summary>
        public bool DeleteExistingDims { get; set; } = false;

        /// <summary>true = dùng view hiện tại; false = dùng selection</summary>
        public bool UseActiveView { get; set; } = true;
    }
}
