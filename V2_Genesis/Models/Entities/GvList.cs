using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models;

[Table("GV_LIST")]
public class GvList
{
    [Key]
    public long ID { get; set; }
    public string Roll_Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Short { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Main_Roll { get; set; } = string.Empty;
    public bool? Objection { get; set; }
    public bool? Appeal { get; set; }
    public string? SAP_Address { get; set; }

    // ── Computed helpers ──────────────────────────────────────────────────

    /// <summary>Maps Source column → MVC controller name prefix.</summary>
    [NotMapped]
    public string ControllerName => Source switch
    {
        "Objection" => "Objection",
        "Objection_Supp1" => "Sup1",
        "Objection_Supp2" => "Sup2",
        "Objection_Supp3" => "Sup3",
        "Objection_Query" => "Query",
        _ => "Home"
    };

    /// <summary>Dashboard action name for this roll.</summary>
    [NotMapped]
    public string DashboardAction => Source switch
    {
        "Objection" => "DashBoard",
        "Objection_Supp1" => "DashBoardSup1",
        "Objection_Supp2" => "DashboardSup2",
        "Objection_Supp3" => "DashboardSup3",
        _ => "Index"
    };

    [NotMapped]
    public string SourceTableCode => Source switch
    {
        "Objection" => "GV23",
        "Objection_Supp1" => "GV23-SUP1",
        "Objection_Supp2" => "GV23-SUP2",
        "Objection_Supp3" => "GV23-SUP3",
        "Objection_Query" => "Query",
        _ => Source
    };
    [NotMapped] public bool HasObjection => Objection == true;
    [NotMapped] public bool HasAppeal => Appeal == true;
    [NotMapped] public bool IsQuery => Roll_Type == "Query";
    [NotMapped] public bool IsGv => Roll_Type == "GV";
    [NotMapped] public bool IsSupp => Roll_Type == "Supp";
}