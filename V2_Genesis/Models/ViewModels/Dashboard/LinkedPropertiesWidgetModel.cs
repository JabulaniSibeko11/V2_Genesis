using System.Dynamic;
using System.Globalization;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.ViewModels.Dashboard;

namespace V2_Genesis.Models.ViewModels.Dashboard
{
    public class LinkedPropertiesWidgetModel
    {
        public string RollKey { get; set; } = "";
        public RollData Roll { get; set; } = default!;
        public int LinkedCount { get; set; }
        public List<LinkedPropertyResult> Items { get; set; } = new();
        public bool IsQuery { get; set; }
        public bool HasObjection { get; set; }
        public string ReturnUrl { get; set; } = "/dashboard";

        public string FormatZAR(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return "–";
            var clean = val.Replace("R", "").Replace(",", "").Trim();
            if (!decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) || num <= 0)
                return "–";
            return "R " + num.ToString("N0", new CultureInfo("en-ZA"));
        }
    }
}
