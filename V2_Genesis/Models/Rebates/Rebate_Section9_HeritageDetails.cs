using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section9_HeritageDetails
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Rebate_Info")]
        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_S9 { get; set; }


        public string? NameOfApplicant { get; set; }
        public string? NameOfHeritageProperty { get; set; } 
        public string? BriefDescriptionHS   {get; set; }
        public string? ErfandTownship { get; set; }
        public string? NationalHeritageSite { get; set; }
        public string? ProvincialHeritageSite { get; set; }
        public string? HeritageArea { get; set; } 
        public string? ProvisionalProtection { get; set; }
        public string? Other { get; set; }
        public string? SpecifyOther { get; set; }
        public string? NumberDateGovernmentNotice { get; set; }
        public string? DetailsGazette { get; set; }
        public string? FormerProtection { get; set; } 
    }
}
