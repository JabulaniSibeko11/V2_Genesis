namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributeFilesVm
    {


        public IFormFile? RepLetter { get; set; }

        public List<IFormFile> EvidenceFiles { get; set; } = new();

        // Populated when an existing submission is loaded so regenerated
        // acknowledgements can list the documents stored in Attr_Files.
        public List<string> UploadedFileNames { get; set; } = new();

    }
}
