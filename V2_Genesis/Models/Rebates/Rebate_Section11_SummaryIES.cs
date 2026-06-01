using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section11_SummaryIES
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Rebate_Info")]
        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_S11 { get; set; }


        //Summary of projected income and expenditure scholar/student
        public string? ExpectedIncome1 { get; set; }
        public string? ExpectedIncome2 { get; set; }
        public string? ExpectedIncome3 { get; set; }
        public string? ExpectedIncome4 { get; set; }

        //Past Year
        public string? EIPastYear1 { get; set; }   
        public string? EIPastYear2 { get; set; }
        public string? EIPastYear3 { get; set; }
        public string? EIPastYear4 { get; set; }

        //Next Year
        public string? EICurrentYear1 { get; set; }
        public string? EICurrentYear2 { get; set; }
        public string? EICurrentYear3 { get; set; }
        public string? EICurrentYear4 { get; set; }

        //NextYear
        public string? EINextYear1 { get; set; }
        public string? EINextYear2 { get; set; }
        public string? EINextYear3 { get; set; }
        public string? EINextYear4 { get; set; }


        //Expenditure
        public string? Expediture1 { get; set; }
        public string? Expediture2 { get; set; }
        public string? Expediture3 { get; set; }

        //Expenditure PastYear
        public string? ExpediturePastYear1 { get; set; }
        public string? ExpediturePastYear2 { get; set; }
        public string? ExpediturePastYear3 { get; set; }

        //Expediture CurrentYear
        public string? ExpeditureCurrentYear1 { get; set; }
        public string? ExpeditureCurrentYear2 { get; set; }
        public string? ExpeditureCurrentYear3 { get; set; }

        //Expediture NextYear
        public string? ExpeditureNextYear1 { get; set; } 
        public string? ExpeditureNextYear2 { get; set; }
        public string? ExpeditureNextYear3 { get; set; }

        //Pesonnel
        public string? SalaryPastYear { get; set; }
        public string? SalaryCurrentYear { get; set; } 
        public string? SalaryNextYear { get; set; }

        //Bonus
        public string? BonusPastYear { get; set; }
        public string? BonusCurrentYear { get; set; }
        public string? BonusNextYear { get; set; } 
    }
}
