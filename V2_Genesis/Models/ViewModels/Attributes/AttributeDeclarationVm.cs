using System.ComponentModel.DataAnnotations;

namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributeDeclarationVm
    {
        public bool DeclarationAccepted { get; set; }

        [Display(Name = "Signature Name")]
        public string? SignatureName { get; set; }

        public string? SignaturePicture { get; set; }

        public string? DeclarationText { get; set; }
    }
}
