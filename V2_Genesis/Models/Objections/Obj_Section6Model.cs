using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models;

public class Obj_Section6Model
{
    [Key]
    public long ID { get; set; }

    // Ref is text in the database.
    [StringLength(100)]
    public string? Ref { get; set; }

    [StringLength(100)]
    public string? Objection_Ref_S6 { get; set; }

    [StringLength(100)]
    public string? Old_Property_Description { get; set; }

    [StringLength(100)]
    public string? Old_Category { get; set; }

    [StringLength(250)]
    public string? Old_Address { get; set; }

    public string? Old_Extent { get; set; }

    [StringLength(100)]
    public string? Old_Market_Value { get; set; }

    [StringLength(100)]
    public string? Old_Owner { get; set; }

    [StringLength(100)]
    public string? New_Property_Description { get; set; }

    [StringLength(100)]
    public string? New_Category { get; set; }

    [StringLength(250)]
    public string? New_Address { get; set; }

    public string? New_Extent { get; set; }

    [StringLength(25)]
    public string? New_Market_Value { get; set; }

    [StringLength(100)]
    public string? New_Owner { get; set; }

    [StringLength(550)]
    public string? Objection_Reasons { get; set; }

    [StringLength(100)]
    public string? Old2_Category { get; set; }

    public string? Old2_Extent { get; set; }

    public string? Old2_Market_Value { get; set; }

    [StringLength(100)]
    public string? New2_Category { get; set; }

    public string? New2_Extent { get; set; }

    [StringLength(25)]
    public string? New2_Market_Value { get; set; }

    [StringLength(100)]
    public string? Old3_Category { get; set; }

    public string? Old3_Extent { get; set; }

    public string? Old3_Market_Value { get; set; }

    [StringLength(100)]
    public string? New3_Category { get; set; }

    public string? New3_Extent { get; set; }

    [StringLength(25)]
    public string? New3_Market_Value { get; set; }

    public long? Appeal_Ref_S6 { get; set; }
}