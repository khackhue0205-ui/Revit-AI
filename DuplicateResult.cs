using System.Collections.Generic;

namespace BTVN1_Application.Models
{
    public class DuplicateResult
    {
        public string  OriginalSheetNumber { get; set; } = string.Empty;
        public string  OriginalSheetName   { get; set; } = string.Empty;
        public string? NewSheetNumber      { get; set; }
        public string? NewSheetName        { get; set; }
        public bool    Success             { get; set; }
        public string? Error               { get; set; }
        public List<string> Warnings       { get; } = new();
    }
}
