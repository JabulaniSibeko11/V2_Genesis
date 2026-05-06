namespace V2_Genesis.Models.ViewModels.Account
{
    public class RegisterPageViewModel
    {
        public IndividualRegisterViewModel Individual { get; set; } = new();
        public CompanyRegisterViewModel Company { get; set; } = new();

        /// <summary>"individual" or "company" — controls which tab is active on render.</summary>
        public string ActiveTab { get; set; } = "individual";
    }

}
