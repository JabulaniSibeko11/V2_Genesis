using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section10_Organisation
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Rebate_Info")]
        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_S10 { get; set; }



        [DisplayName("Name of the Organisation")]
        public string? NameOfTheOrganisation { get; set; }

        [DisplayName("Registration Details")]
        public string? RegistrationNumber { get; set; }

        [DisplayName("Name, Surname and Designation of the person completing application")]
        public string? NameSurnameDesignation { get; set; }
        public string? OperationTime { get; set; }
        public string? RegisteredWithSARS { get; set; }   
        public string? ContactPersonWithCOJStatements { get; set; } 
        public string? LargeOrganisation { get; set; } 
        public string? LargeOrganisationDescription { get; set; }
        public string? FulltimeScholars { get; set; } 
        public string? NoPermantStaff { get; set; }
        public string? NoOfScholarsPayingFull { get; set; }  
        public string? NoOfScholarsNotPaying { get; set; }
        public string? EstimatedRevenuePayingFees { get; set; }  
        public string? EstimatedRevenueNotPayingFees { get; set; }
    }
}
