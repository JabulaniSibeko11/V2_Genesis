using System.ComponentModel.DataAnnotations;

namespace V2_Genesis.Models.ViewModels.Attributes;

public sealed class AttributeWithdrawalViewModel
{
    public long AttrId { get; set; }

    public string AttrNo { get; set; } = string.Empty;

    public string PropertyDescription { get; set; } = string.Empty;

    public string? CurrentStatus { get; set; }

    [Required(ErrorMessage = "Please provide a reason for the withdrawal.")]
    [StringLength(
        1000,
        MinimumLength = 10,
        ErrorMessage = "The withdrawal reason must be between 10 and 1,000 characters.")]
    public string Reason { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
