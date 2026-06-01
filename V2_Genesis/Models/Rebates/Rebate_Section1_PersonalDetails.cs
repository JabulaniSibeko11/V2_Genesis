using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section1_PersonalDetails
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Rebate_Info")]
        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] 
        public string? Rebate_Ref_S1 { get; set; }



        public string? AccountNumber { get; set; }
        public string? Gender { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Surname { get; set; }
        public string? FirstNames { get; set; }
        public string? DOB { get; set; }
        public string? IDNumber { get; set; }
        public string? SpouseSurname { get; set; } 
        public string? SpouseFirstNames { get; set; } 
        public string? SpouseDOB { get; set; } 
        public string? SpouseIDNumber { get; set; }
        public string? OccupyMentionedProperty { get; set; }
        public string? PassportNumber { get; set; }

    }
}
