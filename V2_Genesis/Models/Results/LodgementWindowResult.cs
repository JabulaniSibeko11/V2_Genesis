namespace V2_Genesis.Models.Results
{
    public class LodgementWindowResult
    {
        public bool Exists { get; set; }
        public bool IsOpen { get; set; }

        public string Type { get; set; } = ""; // Objection / Appeal

        public DateTime? StartDate { get; set; }
        public DateTime? CloseDate { get; set; }

        public string? ReferenceNo { get; set; }

        public string Message
        {
            get
            {
                var today = DateTime.Today;

                if (Type.Equals("Appeal", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Exists)
                        return "Appeal period information could not be found for this property. Please contact the Valuation team.";

                    if (StartDate.HasValue && today < StartDate.Value.Date)
                        return $"The appeal period has not opened yet. It opens on {StartDate.Value:dd MMMM yyyy}.";

                    if (CloseDate.HasValue && today > CloseDate.Value.Date)
                        return $"The appeal period closed on {CloseDate.Value:dd MMMM yyyy}. You cannot lodge an appeal. Please contact the Valuation team.";

                    return "The appeal period is not open. Please contact the Valuation team.";
                }

                if (!Exists)
                    return "Objection period information could not be found. Please contact the Valuation team.";

                if (StartDate.HasValue && today < StartDate.Value.Date)
                    return $"The objection period has not opened yet. It opens on {StartDate.Value:dd MMMM yyyy}.";

                if (CloseDate.HasValue && today > CloseDate.Value.Date)
                    return $"The objection period closed on {CloseDate.Value:dd MMMM yyyy}. You cannot lodge an objection. Please contact the Valuation team.";

                return "The objection period is not open. Please contact the Valuation team.";
            }
        }
    }
}