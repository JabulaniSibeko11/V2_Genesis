using System.ComponentModel.DataAnnotations;

namespace V2_Genesis.Models.ViewModels.Account
{
    public sealed class CompanyRegisterViewModel
    {
        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(255)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "CIPC registration number is required.")]
        [StringLength(100)]
        [Display(Name = "CIPC Registration Number")]
        public string CompanyRegistration { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Company Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Company Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the Terms of Use and POPIA Notice.")]
        [Display(Name = "Accept Terms")]
        public bool AcceptTerms { get; set; }
    }

}
