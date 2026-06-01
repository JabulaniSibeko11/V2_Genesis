using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace V2_Genesis.Models
{
    public class Rebate_View_Model
    {
        [Key]
        public long Rebate_ID { get; set; }
        public long Id { get; set; }

        [BindNever]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Rebate_No { get; set; }

        public string? Rebate_Type { get; set; }


        //Section1 Personal Details
        [MinLength(9)]
        [DisplayName("Account Number")]
        public string? AccountNumber { get; set; }
        public string? Gender { get; set; }

        [DisplayName("Marital Status")]
        public string? MaritalStatus { get; set; }

        [Required]
        [StringLength(20)]
        public string? Surname { get; set; }

        //[Required]
        [StringLength(20)]
        [Display(Name = "First Names")]
        public string? FirstNames { get; set; }

        [Display(Name ="Date Of Birth")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public string? DOB { get; set; }

        //[Required]
        [DisplayName("Identity Number")]
        public string? IDNumber { get; set; }

        [DisplayName("Spouse Surname")]
        public string? SpouseSurname { get; set; }

        [DisplayName("Spouse First Names")]
        public string? SpouseFirstNames { get; set; }

        [DisplayName("Spouse Date of birth")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public string? SpouseDOB { get; set; }

        [DisplayName("Spouse Identity Number")]
        public string? SpouseIDNumber { get; set; }

        //[Required]
        [DisplayName("Do you occupy the above mentioned property")]
        public string? OccupyMentionedProperty { get; set; }

        [DisplayName("Passport")]
        public string? PassportNumber { get; set; }

        public string? Status { get; set; }


        //Section2 Addresses

        //[Required]
        [DisplayName("Street Address")]
        public string? StreetAddress { get; set; }

        //[Required]
        [DisplayName("City/Suburb")]
        public string? CitySuburb { get; set; } 

        //[Required]
        [DisplayName("Postal Code")]
        public string? PostalCode { get; set; }


        [DisplayName("Postal Address")]
        public string? PostalAddress { get; set; }

        [DisplayName("City/Suburb")]
        public string? PostalAddressCitySuburb { get; set; }

        [DisplayName("Postal Code")]
        public string? PostalAddressPostalCode { get; set; }


        //Section3 ContactDetails

        [DisplayName("Home Tel")]
        public string? HomeTel { get; set; }

        //[Required]
        [DisplayName("Cell Number")]
        public string? CellNo { get; set; }

        [DisplayName("Work Tel")]
        public string? WorkTel { get; set; }

        public string? FaxNo { get; set; }

        //[Required]
        [EmailAddress]
        [DisplayName("Email")]
        public string? Email { get; set; }

        //Section4 Ownership

        [DisplayName("Stand Number")]
        public string? StandNumber { get; set; }

        [DisplayName("Portion Number")]
        public string? PortionNumber { get; set; }


        public string? Suburb { get; set; }
        //public string? OccupyProp { get; set; }
        //
        [DisplayName("Name Of Body Corporate")]
        public string? NameOfBodyCorporate { get; set; }

        [DisplayName("Unit Number: Door Number")]
        public string? UnitNumberDoorNumber { get; set; }

        [DisplayName("Door Number")]
        public string? DoorNumber { get; set; }

        //Section5 Declaration
        public string? Place { get; set; }

        //[Required]
        [DisplayName("Declaration Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public string? DeclarationDate { get; set; }

        public string? Signature { get; set; }
        public string? DateOfSubmission { get; set; }
        public string? FileName { get; set; }

        //Sectio6 FinancialInformation
        //ApplicantFI
        //[DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
        [DisplayName("Salary Income of Applicant")]
        public string? SalaryIncomeApplicant { get; set; }

        [DisplayName("Interest On Investments of Applicant")]
        public string? InterestOnInvestmentsApplicant { get; set; }

        [DisplayName("Monthly Pension of Applicant")]
        public string? MonthlyPensionApplicant { get; set; }

        [DisplayName("State Disability Allowance of Applicant")]
        public string? StateDisabilityAllowanceApplicant { get; set; } 

        [DisplayName("Other Income of Applicant")]
        public string? OtherIncomeApplicant { get; set; }


        //SpouseFI
        //[DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
        [DisplayName("Salary Income of Spouse")]
        public string? SalaryIncomeSpouse { get; set; }

        [DisplayName("Interest On Investments of Spouse")]
        public string? InterestOnInvestmentsSpouse { get; set; }

        [DisplayName("Monthly Pension of Spouse")]
        public string? MonthlyPensionSpouse { get; set; }

        [DisplayName("State Disability Allowance of Spouse")]
        public string? StateDisabilityAllowanceSpouse { get; set; }

        [DisplayName("Other Income of Spouse")]
        public string? OtherIncomeSpouse { get; set; }


        //Section7 ChildHeaded HouseHold
        [DisplayName("Name & Surname Occupant 1")]
        public string? NameSurname1 { get; set; }

        [DisplayName("Name & Surname Occupant 2")]
        public string? NameSurname2 { get; set; }

        [DisplayName("Name & Surname Occupant 3")]
        public string? NameSurname3 { get; set; }

        [DisplayName("Name & Surname Occupant 4")]
        public string? NameSurname4 { get; set; }

        [DisplayName("ID No Occupant 1")]
        public string? IDNo1 { get; set; }

        [DisplayName("ID No Occupant 2")]
        public string? IDNo2 { get; set; }

        [DisplayName("ID No Occupant 3")]
        public string? IDNo3 { get; set; }

        [DisplayName("ID No Occupant 4")]
        public string? IDNo4 { get; set; }
        public string? HouseUnits { get; set; }


        //Section8 ACS
        [DisplayName("Account Number(s) - Multiple Units")]
        public string? AccountNo_MultipleUnits { get; set; }

        [DisplayName("Contact -Tel/Cell Number")]
        public string? Contact_CellNo { get; set; }

        [DisplayName("Scheme/Building Name")]
        public string? SchemeBuilding { get; set; }


        //Section9 HritageSite
        public string? NameOfApplicant { get; set; } 
        public string? NameOfHeritageProperty { get; set; }
        public string? BriefDescriptionHS { get; set; } 
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


        //Section10 Organisation
        [DisplayName("Name of the Organisation")]
        public string? NameOfTheOrganisation { get; set; }

        [DisplayName("Registration Details")]
        public string? RegistrationNumber { get; set; }

        [DisplayName("Name, Surname and Designation of the person completing application")]
        public string? NameSurnameDesignation { get; set; }

        [DisplayName("How long has the school, college or university been in operation?")]
        public string? OperationTime { get; set; }

        [DisplayName("Is the organisation registered with SARS in terms of Section 30 of the Income Tax Act?")]
        public string? RegisteredWithSARS { get; set; }
        

        [DisplayName("Contact person who receives City of Johannesburg statements and Valuation information")]
        public string? ContactPersonWithCOJStatements { get; set; }

        [DisplayName("Is the school, college, or university an affiliate to a larger organisation")]
        public string? LargeOrganisation { get; set; }

        [DisplayName("Please give details regarding the affiliation")]
        public string? LargeOrganisationDescription { get; set; }

        [DisplayName("No. of permanent  staff employed")]
        public string? NoPermantStaff { get; set; }

        [DisplayName("No. of full-time enrolled scholars/students")]
        public string? FulltimeScholars { get; set; }

        [DisplayName("No. of scholars/students paying full school, college, or university fees")]
        public string? NoOfScholarsPayingFull { get; set; }

        [DisplayName("No. of scholars/students not able to pay school fees")]
        public string? NoOfScholarsNotPaying { get; set; }

        [DisplayName("Estimated Revenue forfeiture - scholars/students paying partial fees")]
        public string? EstimatedRevenuePayingFees { get; set; }

        [DisplayName("Estimated Revenue forfeiture - scholars/students not able to pay school, college, or university fees")]
        public string? EstimatedRevenueNotPayingFees { get; set; }



        //Section for Private learning (Summary of projected incime and expediture scholar/student)
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
