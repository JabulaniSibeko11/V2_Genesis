namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributeFormSelectionViewModel
    {
        public string UnitKey { get; set; } = "";

        public string? PropertyDescription { get; set; }

        public string? Category { get; set; }

        public string? Town { get; set; }

        public string? MarketValue { get; set; }

        // This is only a suggestion shown to the client.
        // The client must still select the form manually.
        public string? SuggestedFormType { get; set; }

        // This is the radio button value selected by the client.
        public string? SelectedFormType { get; set; }
    }
}
