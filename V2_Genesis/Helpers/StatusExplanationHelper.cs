namespace V2_Genesis.Helpers
{
    public static class StatusExplanationHelper
    {
        public static string GetTitle(string? status)
        {
            status = Normalize(status);

            return status switch
            {
                "obj-lodging" => "Objection Lodged",
                "obj-inprogress" => "Objection In Progress",
                "obj-finalized" => "Objection Finalised",
                "notice-sent" => "Objection Finalised",
                "notice-sent-dear-johnny" => "Objection Outcome Available",
                "notice-sent-invalid-objection" => "Objection Not Valid",
                "notice-sent-invalid-omission" => "Omission Objection Not Valid",
                "app-lodging" => "Appeal Lodged",
                "app-scheduling" => "Appeal Being Scheduled",
                "app-scheduled" => "Appeal Scheduled",
                "app-pending-approval" => "Appeal Pending Approval",
                "app-finalized" => "Appeal Finalised",
                "query-lodging" => "Query Lodged",
                "review-lodging" => "Review Lodged",
                "withdrawn" => "Withdrawn",
                _ => "Status Information"
            };
        }

        public static string GetDescription(string? status)
        {
            status = Normalize(status);

            return status switch
            {
                "obj-lodging" =>
                    "Your objection has been received by the Valuation Department. The system has recorded your submission and it is waiting to be reviewed.",

                "obj-inprogress" =>
                    "Your objection is currently being reviewed by the Valuation Department. You may be contacted if more information or evidence is required.",

                "obj-finalized" =>
                    "Your objection has been finalised. Please check your notices or outcome documents for the decision.",

                "notice-sent" =>
                    "Your objection has been finalised. You can download the Section 53 Valuer Decision from your dashboard.",
                "notice-sent-dear-johnny" =>
                    "An outcome notice is available for this objection. Download it from the dashboard for the full explanation and available next steps.",
                "notice-sent-invalid-objection" =>
                    "This objection could not be considered because the property was not found on the applicable official property register. Download the outcome notice for details.",
                "notice-sent-invalid-omission" =>
                    "This omission objection could not be considered because it used an incorrect property description. Download the outcome notice for details.",

                "app-lodging" =>
                    "Your appeal has been received. The appeal process has started and the matter will be prepared for further handling.",

                "app-scheduling" =>
                    "Your appeal is being prepared for scheduling. The Valuation Appeal Board or admin team may still need to confirm the hearing details.",

                "app-scheduled" =>
                    "Your appeal has been scheduled. Please check the hearing date, time, and any documents required before the hearing.",

                "app-pending-approval" =>
                    "Your appeal outcome is being reviewed for approval. The final result has not yet been released.",

                "app-finalized" =>
                    "Your appeal has been finalised. Please check the final decision notice or outcome document.",

                "query-lodging" =>
                    "Your Section 78 query has been received and is waiting to be reviewed.",

                "review-lodging" =>
                    "Your Section 78 review has been received and is waiting to be reviewed.",

                "withdrawn" =>
                    "This case has been withdrawn. No further processing will continue unless a new submission is lodged.",

                _ =>
                    "This is the current status of your case. Please contact support if you need more information."
            };
        }

        public static string GetBadgeClass(string? status)
        {
            status = Normalize(status);

            return status switch
            {
                "obj-lodging" => "status-info",
                "obj-inprogress" => "status-warning",
                "obj-finalized" => "status-success",
                "notice-sent" or "notice-sent-dear-johnny" => "status-success",
                "notice-sent-invalid-objection" or "notice-sent-invalid-omission" => "status-danger",

                "app-lodging" => "status-info",
                "app-scheduling" => "status-warning",
                "app-scheduled" => "status-primary",
                "app-pending-approval" => "status-warning",
                "app-finalized" => "status-success",

                "query-lodging" => "status-info",
                "review-lodging" => "status-info",
                "withdrawn" => "status-danger",

                _ => "status-muted"
            };
        }

        private static string Normalize(string? status)
        {
            return string.IsNullOrWhiteSpace(status)
                ? ""
                : status.Trim().ToLowerInvariant();
        }
    }
}
