namespace V2_Genesis.Models.ViewModels
{
     public class SupportingDocumentViewModel
        {
            public string FileName { get; set; } = "";
            public string DownloadUrl { get; set; } = "";
            public long SizeBytes { get; set; }
            public DateTime UploadedDate { get; set; }

            public string SizeDisplay
            {
                get
                {
                    if (SizeBytes <= 0) return "";
                    var kb = SizeBytes / 1024m;
                    if (kb < 1024) return $"{kb:0.#} KB";
                    return $"{kb / 1024m:0.#} MB";
                }
            }
        }
    }

