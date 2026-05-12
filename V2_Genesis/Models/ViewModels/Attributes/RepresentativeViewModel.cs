namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class RepresentativeViewModel
    {
        // Hidden — carried through from Check page
        public string IDProperty { get; set; } = string.Empty;
        public string FormType { get; set; } = "Residential";

        // Representative fields
        [System.ComponentModel.DataAnnotations.Required(
            ErrorMessage = "Representative name is required")]
        public string? Representative_Name { get; set; }

        public string? Rep_Postal_1 { get; set; }
        public string? Rep_Postal_2 { get; set; }
        public string? Rep_Postal_3 { get; set; }
        public string? Rep_Postal_4 { get; set; }
        public string? Rep_Postal_5 { get; set; }

        public string? Rep_Home_Phone { get; set; }
        public string? Rep_Cell_Phone { get; set; }
        public string? Rep_Work_Phone { get; set; }
        public string? Rep_Fax_Phone { get; set; }

        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string? Rep_Email { get; set; }

        // Authorisation letter — required for representatives
        [System.ComponentModel.DataAnnotations.Required(
            ErrorMessage = "Authorisation letter is required for representatives")]
        public IFormFile? AuthLetter { get; set; }

        // Property summary — shown on the page for context
        public string? PropertyDesc { get; set; }
        public string? TownNameDesc { get; set; }
        public string? LisStreetAddress { get; set; }
        public string? CatDesc { get; set; }
    }
}
