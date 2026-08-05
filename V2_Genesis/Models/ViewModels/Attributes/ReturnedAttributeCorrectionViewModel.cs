using System.ComponentModel.DataAnnotations;

namespace V2_Genesis.Models.ViewModels.Attributes
{
    public sealed class ReturnedAttributeCorrectionViewModel
    {
        public long AttrId { get; set; }
        public string AttrNo { get; set; } = string.Empty;
        public string PropertyDescription { get; set; } = string.Empty;
        public string FormType { get; set; } = string.Empty;
        public string RevisionReason { get; set; } = string.Empty;
        public DateTime? RequestedAt { get; set; }
        public string RequestedBy { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please explain what you corrected.")]
        [StringLength(2000)]
        public string RevisionComment { get; set; } = string.Empty;

        public AttributeSubmissionViewModel Submission { get; set; } = new();
        public List<ReturnedAttributeCorrectionSectionVm> Sections { get; set; } = new();
        public List<ReturnedAttributeCorrectionFieldVm> Fields { get; set; } = new();
    }

    public sealed class ReturnedAttributeCorrectionFieldVm
    {
        public string SectionCode { get; set; } = string.Empty;
        public string FieldCode { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string? CityValue { get; set; }
        public string? ClientValue { get; set; }
    }

    public sealed class ReturnedAttributeCorrectionSectionVm
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}
