namespace V2_Genesis.Helpers
{
    public static class AttributeStatusDisplayHelper
    {
        public static string GetDisplayStatus(string? backendStatus)
        {
            var status = Clean(backendStatus);

            return status switch
            {
                "EVIDENCEOPEN" => "Evidence Upload Open",
                "EVIDENCELOCKED" => "Evidence Locked",
                "SECTORINBOX" => "Submitted to Sector",
                "SECTORROUTINGEXCEPTION" => "Routing Issue",

                "CLAIMED" => "Assigned to Valuer",
                "VALUERREVIEW" => "Under Valuer Review",

                "INSPECTIONREQUIRED" => "Inspection Required",
                "PENDINGCLIENTRESPONSE" => "Select Inspection Date",
                "CONFIRMED" => "Inspection Date Confirmed",
                "INSPECTIONCONFIRMED" => "Inspection Date Confirmed",
                "INSPECTIONDETAILSSENT" => "Enter PIN to View Valuer Details",
                "INSPECTIONEXPIRED" => "Inspection Date Options Expired",
                "EXPIRED" => "Inspection Date Options Expired",

                "RETURNEDTOCLIENT" => "Returned for Correction",
                "RESUBMITTED" => "Resubmitted",

                "READYFOROVVIOEXTRACT" => "Accepted / Processing",
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
                "EVIDENCEOPEN" => "status-open",
                "EVIDENCELOCKED" => "status-locked",
                "SECTORINBOX" => "status-submitted",
                "SECTORROUTINGEXCEPTION" => "status-error",

                "CLAIMED" => "status-assigned",
                "VALUERREVIEW" => "status-review",

                "INSPECTIONREQUIRED" => "status-inspection",
                "PENDINGCLIENTRESPONSE" => "status-action",
                "CONFIRMED" => "status-confirmed",
                "INSPECTIONCONFIRMED" => "status-confirmed",
                "INSPECTIONDETAILSSENT" => "status-pin",
                "INSPECTIONEXPIRED" => "status-expired",
                "EXPIRED" => "status-expired",

                "RETURNEDTOCLIENT" => "status-returned",
                "RESUBMITTED" => "status-resubmitted",

                "READYFOROVVIOEXTRACT" => "status-accepted",
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
                "PENDINGCLIENTRESPONSE" => "Select Date",
                "CONFIRMED" => "Date Confirmed",
                "INSPECTIONDETAILSSENT" => "Enter PIN",
                "EXPIRED" => "Expired",
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