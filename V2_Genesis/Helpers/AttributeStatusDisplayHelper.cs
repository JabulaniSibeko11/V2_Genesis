namespace V2_Genesis.Helpers
{
    public static class AttributeStatusDisplayHelper
    {
        public static string GetDisplayStatus(string? backendStatus)
        {
            var status = Clean(backendStatus);

            return status switch
            {
                "SUBMITTED" => "Submitted",
                "PENDING" => "Submission Being Processed",
                "EVIDENCEOPEN" => "Submitted – Evidence Upload Open",
                "EVIDENCELOCKED" => "Submitted – Evidence Upload Closed",
                "SECTORINBOX" => "Awaiting Valuer Assignment",
                "SECTORROUTINGEXCEPTION" or "ROUTINGEXCEPTION" => "Processing Delay",

                "ASSIGNED" or "CLAIMED" or "MANAGERASSIGNED" => "Assigned to Valuer",
                "VALUERREVIEW" => "Under Valuer Review",

                "INSPECTIONREQUIRED" => "Inspection Required",
                "PENDINGCLIENTRESPONSE" or "INSPECTIONDATEOPTIONSSENT" => "Select Inspection Date",
                "CONFIRMED" => "Inspection Date Confirmed",
                "INSPECTIONCONFIRMED" => "Inspection Date Confirmed",
                "INSPECTIONDETAILSSENT" => "Enter PIN to View Valuer Details",
                "INSPECTIONEXPIRED" => "Inspection Date Options Expired",
                "EXPIRED" => "Inspection Date Options Expired",
                "INSPECTIONCOMPLETED" => "Inspection Completed – Under Review",

                "RETURNEDTOCLIENT" => "Action Required – Correct Submission",
                "RESUBMITTED" => "Corrections Resubmitted",
                "RETURNEDTOVALUER" => "Under Valuer Rework",
                "SECTORMANAGERQA" or "SUBMITTEDFORSECTORMANAGERQA" => "Quality Assurance Review",

                "READYFOROVVIOEXTRACT" => "Accepted – Final Processing",
                "OVVIOEXTRACTED" or "COMPLETED" or "APPROVED" => "Completed",
                "ACCEPTED" => "Accepted",
                "REJECTED" => "Rejected",
                "WITHDRAWN" => "Withdrawn",

                _ => string.IsNullOrWhiteSpace(backendStatus)
                    ? "Pending"
                    : backendStatus
            };
        }

        public static string GetStatusCssClass(string? backendStatus)
        {
            var status = Clean(backendStatus);

            return status switch
            {
                "SUBMITTED" or "PENDING" => "status-submitted",
                "EVIDENCEOPEN" => "status-open",
                "EVIDENCELOCKED" => "status-locked",
                "SECTORINBOX" => "status-submitted",
                "SECTORROUTINGEXCEPTION" or "ROUTINGEXCEPTION" => "status-error",

                "ASSIGNED" or "CLAIMED" or "MANAGERASSIGNED" => "status-assigned",
                "VALUERREVIEW" or "RETURNEDTOVALUER" => "status-review",

                "INSPECTIONREQUIRED" => "status-inspection",
                "PENDINGCLIENTRESPONSE" or "INSPECTIONDATEOPTIONSSENT" => "status-action",
                "CONFIRMED" => "status-confirmed",
                "INSPECTIONCONFIRMED" => "status-confirmed",
                "INSPECTIONDETAILSSENT" => "status-pin",
                "INSPECTIONEXPIRED" => "status-expired",
                "EXPIRED" => "status-expired",
                "INSPECTIONCOMPLETED" => "status-review",

                "RETURNEDTOCLIENT" => "status-returned",
                "RESUBMITTED" => "status-resubmitted",
                "SECTORMANAGERQA" or "SUBMITTEDFORSECTORMANAGERQA" => "status-qa",

                "READYFOROVVIOEXTRACT" => "status-accepted",
                "OVVIOEXTRACTED" or "COMPLETED" or "APPROVED" or "ACCEPTED" => "status-completed",
                "REJECTED" => "status-rejected",
                "WITHDRAWN" => "status-withdrawn",

                _ => "status-default"
            };
        }

        public static string GetAppointmentActionText(string? backendStatus)
        {
            var status = Clean(backendStatus);

            return status switch
            {
                "PENDINGCLIENTRESPONSE" or "INSPECTIONDATEOPTIONSSENT" => "Select Date",
                "CONFIRMED" or "INSPECTIONCONFIRMED" => "Date Confirmed",
                "INSPECTIONDETAILSSENT" => "Enter PIN",
                "EXPIRED" or "INSPECTIONEXPIRED" => "Expired",
                _ => GetDisplayStatus(backendStatus)
            };
        }

        private static string Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }
    }
}
