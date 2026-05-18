namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributeFilesVm
    {
     

        public IFormFile? RepLetter { get; set; }

        public List<IFormFile> EvidenceFiles { get; set; } = new();
      
    }
}
