namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributeSubmissionViewModel
    {
        public long? AttrId { get; set; }

        public string? AttrNo { get; set; }

        /*
            Residential
            BusinessCommercial
            DRCMethod
            ResidentialST
        */
        public string FormType { get; set; } = "Residential";

        public AttributePropertyDetailsVm PropertyDetails { get; set; } = new();

        public AttributeValuationDetailsVm ValuationDetails { get; set; } = new();

        public AttributeAccessVm Access { get; set; } = new();

        public List<AttributeContactInfoVm> ContactInfos { get; set; } = new()
        {
            new AttributeContactInfoVm()
        };

        public AttributePrimaryAttributesVm PrimaryAttributes { get; set; } = new();

        public AttributeSecondaryAttributesVm SecondaryAttributes { get; set; } = new();

        public AttributeCalculationsVm Calculations { get; set; } = new();

        public List<AttributeBusinessBuildingVm> BusinessBuildings { get; set; } = new();

        public List<AttributeBusinessSectionVm> BusinessSections { get; set; } = new();

        public AttributeBusinessGeneralVm BusinessGeneral { get; set; } = new();

        public List<AttributeDrcBuildingVm> DrcBuildings { get; set; } = new();

        public List<AttributeDrcImprovementVm> DrcImprovements { get; set; } = new();

        public List<AttributeDrcVacantLandVm> DrcVacantLands { get; set; } = new();

        public AttributeDrcMarketValueDemolitionVm DrcMarketValueDemolition { get; set; } = new();

        public AttributeFilesVm Files { get; set; } = new();

        public string? ClientComment { get; set; }

        public RepresentativeDetailsVm RepresentativeDetails { get; set; } = new();
        public AttributeDeclarationVm Declaration { get; set; } = new();
        public string? GeneratedEvidencePin { get; set; }

        public DateTime? GeneratedEvidenceDeadline { get; set; }
    }


    public class RepresentativeDetailsVm
    {
        public bool IsRepresentative { get; set; }
        public string? Representative_Name { get; set; }
        public string? Rep_Postal_1 { get; set; }
        public string? Rep_Postal_2 { get; set; }
        public string? Rep_Postal_3 { get; set; }
        public string? Rep_Postal_4 { get; set; }
        public string? Rep_Postal_5 { get; set; }
        public string? Rep_Home_Phone { get; set; }
        public string? Rep_Cell_Phone { get; set; }
        public string? Rep_Work_Phone { get; set; }
        public string? Rep_Fax_Phone { get; set; }
        public string? Rep_Email { get; set; }
    }

}
