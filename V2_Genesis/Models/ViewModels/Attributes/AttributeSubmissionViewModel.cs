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
    }
}
