using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models.Rates
{
    [Table("PropertyRateTariffs")]
    public sealed class PropertyRateTariff
    {
        public int Id { get; set; }

        public int FinancialYearId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CategoryCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Ratio { get; set; }

        [Column(TypeName = "decimal(18,9)")]
        public decimal AnnualTariff { get; set; }

        public bool IsZeroRated { get; set; }

        public bool IsMultipurpose { get; set; }

        public bool IsPenaltyTariff { get; set; }

        public bool IsActive { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime EffectiveTo { get; set; }

        [MaxLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        [MaxLength(150)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public RateFinancialYear FinancialYear { get; set; } = null!;
    }
}
