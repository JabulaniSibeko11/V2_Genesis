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
                "notice-sent" => "Notice Sent",
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
                    "A notice has been generated and sent for this case. You can preview or download the notice from your dashboard where available.",

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
                "notice-sent" => "status-success",

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