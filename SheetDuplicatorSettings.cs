using Autodesk.Revit.DB;

namespace BTVN1_Application.Models
{
    public class SheetDuplicatorSettings
    {
        public string SheetNamePrefix          { get; set; } = "Copy of ";
        public string SheetNumberSuffix        { get; set; } = "_COPY";
        public ViewDuplicateOption DuplicateOption { get; set; } = ViewDuplicateOption.WithDetailing;
        // ViewDuplicateOption values: Duplicate (copy geometry only), WithDetailing (copy annotations too), AsDependent
    }
}
