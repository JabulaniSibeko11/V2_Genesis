namespace V2_Genesis.Models.Results
{
    public class DuplicateLodgementResult
    {
        public bool Exists { get; set; }
        public bool IsAppeal { get; set; }

        public string? ReferenceNo { get; set; }
        public string? Status { get; set; }
        public string? PropertyDescription { get; set; }

        public string Message
        {
            get
            {
                var type = IsAppeal ? "appeal" : "objection";

                return $"This property already has an {type} being lodged. " +
                       $"You cannot lodge it again. Please contact the Valuation team.";
            }
        }
    }
}
