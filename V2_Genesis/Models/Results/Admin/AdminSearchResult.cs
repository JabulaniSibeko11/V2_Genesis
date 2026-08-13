namespace V2_Genesis.Models.Results.Admin;

public class AdminSearchResult
{
    public string SearchType { get; set; } = "";
    public string SearchInput { get; set; } = "";
    public string? RollFilter { get; set; }

    public List<AdminRefMatch> RefMatches { get; set; } = new();
    public List<AdminPropMatch> PropMatches { get; set; } = new();
    public List<AdminPropertyCandidate> PropertyCandidates { get; set; } = new();
    public List<AdminOmissionCandidate> PropertyOmissionCandidates { get; set; } = new();

    // Phase 1: property-centred foundation used by the new Admin Enquiry
    // workspace. Existing result collections remain populated while the
    // remaining tabs are migrated.
    public AdminEnquiryFoundation? Foundation { get; set; }
    public List<string> Warnings { get; set; } = new();

    public bool HasResults =>
        Foundation is not null
        || RefMatches.Any()
        || PropMatches.Any()
        || PropertyCandidates.Any()
        || PropertyOmissionCandidates.Any();
}

public sealed class AdminPropertyCandidate
{
    public string RollSource { get; set; } = string.Empty;
    public string RollName { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public string PropertyFrom { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string Town { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MarketValue { get; set; } = string.Empty;
    public string UnitKey { get; set; } = string.Empty;
    public string ValuationKey { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public int Erf { get; set; }
    public int Portion { get; set; }
    public int UnitNumber { get; set; }
    public bool IsLis { get; set; }
    public int MatchScore { get; set; }
}

public class AdminRefMatch
{
    public string RollSource { get; set; } = "";
    public string RollName { get; set; } = "";
    public string SourceTable { get; set; } = "";

    public string RefType { get; set; } = ""; // Objection, Appeal, Query, Review, Attributes

    public string? ReferenceNo { get; set; }
    public string? Objection_No { get; set; }
    public string? Appeal_No { get; set; }
    public string? Query_No { get; set; }
    public string? Review_No { get; set; }

    public string? CurrentStatus { get; set; }

    public string? Property_Desc { get; set; }
    public string? Property_Type { get; set; }
    public string? Town_Name { get; set; }
    public string? Old_Market_Value { get; set; }
    public string? Old_Category { get; set; }

    public string? Unit_key { get; set; }
    public string? Valuation_Key { get; set; }
    public string? PremiseId { get; set; }
    public string? PropertyFrom { get; set; }

    public string? UserId { get; set; }
    public string? ClientDisplayName { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientPhoneNumber { get; set; }
    public string? ClientAccountType { get; set; }
    public bool ClientAccountResolved { get; set; }

    public bool IsThirdParty { get; set; }
    public bool IsRepresentative { get; set; }

    public List<AdminNoticeOption> Notices { get; set; } = new();
}

public class AdminNoticeOption
{
    public string NoticeName { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsAvailable { get; set; }
    public string? ReasonUnavailable { get; set; }
    public string Icon { get; set; } = "fa-file-pdf";
}

public class AdminPropMatch
{
    public string RollSource { get; set; } = "";
    public string RollName { get; set; } = "";

    public string? Objection_No { get; set; }
    public string? Property_Desc { get; set; }
    public string? Town_Name { get; set; }
    public string? Old_Category { get; set; }
    public string? Old_Market_Value { get; set; }
    public string? objection_Status { get; set; }
    public string? Unit_key { get; set; }
    public string? Valuation_Key { get; set; }
    public string? PropertyFrom { get; set; }

    public string? UserId { get; set; }
    public string? ClientDisplayName { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientPhoneNumber { get; set; }
    public string? ClientAccountType { get; set; }
    public bool ClientAccountResolved { get; set; }

    public int Sub_typ { get; set; }
}

public sealed class AdminEnquiryFoundation
{
    public AdminResolvedReference Reference { get; set; } = new();
    public AdminCanonicalProperty Property { get; set; } = new();
    public List<AdminRollOccurrence> RollOccurrences { get; set; } = new();
    public AdminAccountInformation AccountInformation { get; set; } = new();
    public AdminRollInformation RollInformation { get; set; } = new();
    public AdminCaseHistory CaseHistory { get; set; } = new();
    public AdminEnquiryNotices Notices { get; set; } = new();
    public long ElapsedMilliseconds { get; set; }
}

public sealed class AdminCaseHistory
{
    public List<AdminCaseHistoryItem> Cases { get; set; } = new();
    public int ObjectionCount => Cases.Count(x => x.CaseType == "Objection");
    public int AppealCount => Cases.Count(x => x.CaseType == "Appeal");
    public int QueryCount => Cases.Count(x => x.CaseType == "Query");
    public int ReviewCount => Cases.Count(x => x.CaseType == "Review");
    public int AttributeCount => Cases.Count(x => x.CaseType == "Attributes");
}

public sealed class AdminCaseHistoryItem
{
    public string CaseType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string RelatedReferenceNumber { get; set; } = string.Empty;
    public string RollSource { get; set; } = string.Empty;
    public string RollName { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string PremiseId { get; set; } = string.Empty;
    public string UnitKey { get; set; } = string.Empty;
    public string ValuationKey { get; set; } = string.Empty;
    public string PropertyFrom { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ObjectorType { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public bool CanLodgeAppeal { get; set; }
    public string AppealUnavailableReason { get; set; } = string.Empty;
    public string ViewUrl { get; set; } = string.Empty;
}

public sealed class AdminEnquiryNotices
{
    public List<AdminEnquiryNoticeItem> Items { get; set; } = new();
    public int AvailableCount => Items.Count(x => x.IsAvailable);
}

public sealed class AdminEnquiryNoticeItem
{
    public string Group { get; set; } = string.Empty;
    public string NoticeName { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CaseType { get; set; } = string.Empty;
    public string RollSource { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-file-pdf";
    public bool IsAvailable { get; set; }
    public string ReasonUnavailable { get; set; } = string.Empty;
}

public sealed class AdminResolvedReference
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public string RollSource { get; set; } = string.Empty;
    public string RollName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ObjectorType { get; set; } = string.Empty;
    public string RelatedReferenceNumber { get; set; } = string.Empty;
    public string CapturerSapNumber { get; set; } = string.Empty;
    public string SubmittedByName { get; set; } = string.Empty;
    public string SubmittedByEmail { get; set; } = string.Empty;
    public string SubmittedByPhone { get; set; } = string.Empty;
    public string SubmissionSource { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }

    public bool IsAdminCaptured =>
        !string.IsNullOrWhiteSpace(CapturerSapNumber);
}

public sealed class AdminAccountInformation
{
    public AdminSubmittingAccount SubmittingAccount { get; set; } = new();
    public List<AdminSubmissionParty> Parties { get; set; } = new();
    public bool PartyInformationFound => Parties.Count > 0;
}

public sealed class AdminSubmittingAccount
{
    public bool Resolved { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string CompanyRegistration { get; set; } = string.Empty;
    public string SapNumber { get; set; } = string.Empty;
    public DateTime? AccountCreatedAt { get; set; }
    public bool EmailConfirmed { get; set; }

    // Snapshot stored with the actual submission. This is particularly
    // important for Admin-captured Attributes records.
    public string SubmittedByName { get; set; } = string.Empty;
    public string SubmittedByEmail { get; set; } = string.Empty;
    public string SubmittedByPhone { get; set; } = string.Empty;
    public string SubmissionSource { get; set; } = string.Empty;
    public string CapturerSapNumber { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public bool IsAdminCaptured { get; set; }
    public bool IsAdministrativeAccount { get; set; }

    public bool CanOwnLinkedProperties =>
        Resolved && !IsAdministrativeAccount;
}

public sealed class AdminSubmissionParty
{
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string IdentityNumber { get; set; } = string.Empty;
    public string CompanyRegistrationNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CellPhone { get; set; } = string.Empty;
    public string HomePhone { get; set; } = string.Empty;
    public string WorkPhone { get; set; } = string.Empty;
    public string PhysicalAddress { get; set; } = string.Empty;
    public string PostalAddress { get; set; } = string.Empty;

    public bool HasContactDetails =>
        !string.IsNullOrWhiteSpace(Email)
        || !string.IsNullOrWhiteSpace(CellPhone)
        || !string.IsNullOrWhiteSpace(HomePhone)
        || !string.IsNullOrWhiteSpace(WorkPhone);
}

public sealed class AdminCanonicalProperty
{
    public string PremiseId { get; set; } = string.Empty;
    public string UnitKey { get; set; } = string.Empty;
    public string ValuationKey { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string PropertyFrom { get; set; } = string.Empty;

    public bool HasStableIdentity =>
        !string.IsNullOrWhiteSpace(PremiseId)
        || (!string.IsNullOrWhiteSpace(UnitKey)
            && !string.IsNullOrWhiteSpace(ValuationKey));
}

public sealed class AdminRollOccurrence
{
    public string RollSource { get; set; } = string.Empty;
    public string RollName { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string PremiseId { get; set; } = string.Empty;
    public string UnitKey { get; set; } = string.Empty;
    public string ValuationKey { get; set; } = string.Empty;
    public string PropertyFrom { get; set; } = string.Empty;
    public string ExistingReference { get; set; } = string.Empty;
    public string ExistingStatus { get; set; } = string.Empty;
}

public sealed class AdminRollInformation
{
    public List<AdminRollPropertyItem> Properties { get; set; } = new();
    public List<AdminOmissionCandidate> OmissionCandidates { get; set; } = new();
    public bool UsedLisFallback { get; set; }
    public bool HasRollProperties => Properties.Any(x => !x.IsLis);
    public bool HasProperties => Properties.Count > 0;
}

public sealed class AdminRollPropertyItem
{
    public string RollSource { get; set; } = string.Empty;
    public string RollName { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public string PropertyFrom { get; set; } = string.Empty;
    public string IdProperty { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string PremiseId { get; set; } = string.Empty;
    public string UnitKey { get; set; } = string.Empty;
    public string ValuationKey { get; set; } = string.Empty;
    public string Town { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MarketValue { get; set; } = string.Empty;
    public bool IsLis { get; set; }
    public bool IsLinked { get; set; }
    public bool CanLink { get; set; }
    public bool CanLodgeObjection { get; set; }
    public bool CanStartQuery { get; set; }
    public bool CanViewSection49 { get; set; }
    public DateTime? ObjectionOpenDate { get; set; }
    public DateTime? ObjectionCloseDate { get; set; }
    public string LinkUnavailableReason { get; set; } = string.Empty;
    public string ObjectionUnavailableReason { get; set; } = string.Empty;
}

public sealed class AdminOmissionCandidate
{
    public string RollSource { get; set; } = string.Empty;
    public string RollName { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public bool CanLodge { get; set; }
    public DateTime? OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public string UnavailableReason { get; set; } = string.Empty;
}
