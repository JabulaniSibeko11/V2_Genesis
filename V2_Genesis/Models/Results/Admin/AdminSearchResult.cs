namespace V2_Genesis.Models.Results.Admin;

public class AdminSearchResult
{
    public string SearchType { get; set; } = "";
    public string SearchInput { get; set; } = "";
    public string? RollFilter { get; set; }

    public List<AdminRefMatch> RefMatches { get; set; } = new();
    public List<AdminPropMatch> PropMatches { get; set; } = new();

    public bool HasResults =>
        RefMatches.Any() || PropMatches.Any();
}

public class AdminRefMatch
{
    public string RollSource { get; set; } = "";
    public string RollName { get; set; } = "";
    public string SourceTable { get; set; } = "";

    public string RefType { get; set; } = ""; // Objection, Appeal, Query, Review

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