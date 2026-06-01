using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section2_Addresses 
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Reb_Info")]
        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_S2 { get; set; }


         
        public string? StreetAddress { get; set; }
        public string? CitySuburb { get; set; } 
        public string? PostalCode { get; set; } 
        public string? PostalAddress { get; set; }
        public string? PostalAddressCitySuburb { get; set; }
        public string? PostalAddressPostalCode { get; set; } 

    }
}