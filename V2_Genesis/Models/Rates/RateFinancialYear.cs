using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models.Rates
{
    [Table("RateFinancialYears")]
    public sealed class RateFinancialYear
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string FinancialYear { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ResidentialExclusion { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalPropertyReduction { get; set; }

        public bool IsActive { get; set; }

        [MaxLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        [MaxLength(150)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<PropertyRateTariff> Tariffs { get; set; }
            = new List<PropertyRateTariff>();
    }
}
