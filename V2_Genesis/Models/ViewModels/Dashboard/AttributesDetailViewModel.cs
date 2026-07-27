using V2_Genesis.Models.Results.Atrributes;
using V2_Genesis.Services.Attributes;

namespace V2_Genesis.Models.ViewModels.Dashboard
{
    public class AttributesDetailViewModel
    {
        public AttributesDashboardData AttrData { get; set; } = new();
        public List<AttrLinkedPropertyResult> AttributesLinked { get; set; } = new();
    }
}
