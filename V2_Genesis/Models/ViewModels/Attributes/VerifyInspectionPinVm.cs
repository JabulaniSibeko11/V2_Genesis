namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class VerifyInspectionPinVm
    {
        public long InspectionRequestId { get; set; }

        public string Pin { get; set; } = string.Empty;
    }
}
