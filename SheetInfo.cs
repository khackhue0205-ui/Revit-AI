using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace BTVN1_Application.Models
{
    public class SheetInfo : INotifyPropertyChanged
    {
        public ElementId Id          { get; set; } = ElementId.InvalidElementId;
        public string SheetNumber    { get; set; } = string.Empty;
        public string SheetName      { get; set; } = string.Empty;
        public int    ViewportCount  { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string DisplayText       => $"{SheetNumber} - {SheetName}";
        public string ViewportCountText => ViewportCount == 1 ? "1 view" : $"{ViewportCount} views";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
