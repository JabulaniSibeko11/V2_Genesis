using System.ComponentModel.DataAnnotations;

namespace V2_Genesis.Models.ViewModels.Attributes;

public class AttributePropertyLinkVerificationViewModel
{
    [Required]
    public string IdProperty { get; set; } = string.Empty;

    [Required]
    public string PropertyFrom { get; set; } = "Attributes";

    [Required(ErrorMessage = "Account Number is required.")]
    [StringLength(50)]
    [Display(Name = "Account Number")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Statement PIN is required.")]
    [StringLength(100)]
    [Display(Name = "Statement PIN")]
    public string StatementPin { get; set; } = string.Empty;
}
