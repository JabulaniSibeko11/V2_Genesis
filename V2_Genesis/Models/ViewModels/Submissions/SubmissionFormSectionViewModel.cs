namespace V2_Genesis.Models.ViewModels.Submissions
{
    public sealed class SubmissionFormSectionViewModel
    {
        public string Key { get; set; } = string.Empty;

        public string TabText { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Order { get; set; }

        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// One displayed form section may be populated by more than one
        /// stored-procedure result set.
        /// </summary>
        public List<string> SourceKeys { get; set; } = new();

        public List<SubmissionSectionViewModel> DataSections { get; set; } = new();

        public bool HasFields =>
            DataSections.Any(section => section.Fields.Count > 0);
    }
}
