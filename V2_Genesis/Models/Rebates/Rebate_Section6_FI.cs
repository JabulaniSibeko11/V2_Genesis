using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section6_FI
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Reb_Info")]
        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_S6 { get; set; }


        //ApplicantFI
        public string? SalaryIncomeApplicant { get; set; }   
        public string? InterestOnInvestmentsApplicant { get; set; }
        public string? MonthlyPensionApplicant { get; set; }  
        public string? StateDisabilityAllowanceApplicant { get; set; }
        public string? OtherIncomeApplicant { get; set; }
        //SpouseFI
        public string? SalaryIncomeSpouse { get; set; } 
        public string? InterestOnInvestmentsSpouse { get; set; }
        public string? MonthlyPensionSpouse { get; set; }
        public string? StateDisabilityAllowanceSpouse { get; set; }
        public string? OtherIncomeSpouse { get; set; }

    }
} 
