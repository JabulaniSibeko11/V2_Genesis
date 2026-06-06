// ═══════════════════════════════════════════════════════════════
//  Models/Results/Admin/AdminSearchModels.cs  — new file
// ═══════════════════════════════════════════════════════════════
namespace V2_Genesis.Models.Results.Admin;

/// <summary>Holds results from either search tab.</summary>
public class AdminSearchResult
{
    public string SearchType { get; set; } = ""; // "Reference" | "Property"
    public string SearchInput { get; set; } = ""; // what was typed
    public string? RollFilter { get; set; }       // null = all rolls

    // Reference search — one result per roll that matched
    public List<AdminRefMatch> RefMatches { get; set; } = new();

    // Property search — one row per matching objection/appeal across rolls
    public List<AdminPropMatch> PropMatches { get; set; } = new();

    public bool HasResults =>
        RefMatches.Any() || PropMatches.Any();
}

public class AdminRefMatch
{
    public string RollSource { get; set; } = "";
    public string RollName { get; set; } = "";
    public string RefType { get; set; } = ""; // "Objection" | "Appeal" | "Query"

    // Objection fields
    public string? Objection_No { get; set; }
    public string? Property_Desc { get; set; }
    public string? Property_Type { get; set; }
    public string? Town_Name { get; set; }
    public string? Old_Market_Value { get; set; }
    public string? Old_Category { get; set; }
    public string? objection_Status { get; set; }
    public string? Unit_key { get; set; }
    public string? Valuation_Key { get; set; }
    public string? PropertyFrom { get; set; }

    // Appeal-specific
    public string? Appeal_No { get; set; }
    public string? Appeal_Status { get; set; }

    // Query-specific
    public string? Query_No { get; set; }
    public string? Query_Status { get; set; }
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
    public int Sub_typ { get; set; }
}