using System;
using System.Collections.Generic;

namespace V2_Genesis.Models
{
    public class ObjectionTB
    {
        //Valuers

        public string? Revise_MVD { get; set; }    
        public string? SapNumberOld { get; set; }

        public string? SapNumber { get; set; }

        public string? FirstName { get; set; }

        public string? SecondName { get; set; }

        public string? Surname { get; set; }

        public string? EmailAddress { get; set; }

        public string? Role { get; set; }

        public string? Username { get; set; }
        public string? Username_old { get; set; }

        public string? PhoneNo { get; set; }

        //Property info table
        public long ObjectionId { get; set; }

        public string? ObjectionNo { get; set; }

        public string? ObjectorType { get; set; }

        public string? PropertyType { get; set; }

        public string? PropertyDesc { get; set; }

        public string? PremiseId { get; set; }

        public string? UnitKey { get; set; }

        public string? PropertyId { get; set; }

        public string? ValuationKey { get; set; }

        public string? Sector { get; set; }
        public string? Sector1 { get; set; }
        public string? objectionStatus { get; set; }

        public string? TaskAsigner { get; set; }
        public string? TaskAsignercomment { get; set; }
        public string? TaskAssignerComment { get; set; }
        public string? TaskAsignedTo { get; set; }

        public string? New3_Market_Value_MVD { get; set; }

        public string? New3_Extent_MVD { get; set; }

        public string? New3_Category_MVD { get; set; }

        public string? New2_Extent_MVD { get; set; }

        public string? New2_Market_Value_MVD { get; set; }

        public string? New2_Category_MVD { get; set; }

        public string? New_Owner_MVD { get; set; }

        public string? New_Market_Value_MVD { get; set; }

        public string? New_Extent_MVD { get; set; }
        public string? New_Address_MVD { get; set; }
        public string? New_Category_MVD { get; set; }
        public string? New_Property_Description_MVD { get; set; }
        public string? valuerComment { get; set; }
        public string? valuerEvidence { get; set; }
        public string? MVDapproverComment { get; set; }
        public string? Objection_Start_DateTime { get; set; }
        public string? Section52Review { get; set; }
        public string? wefDateMVD { get; set; }
        public string? WefDate { get; set; }
        public string? MVD_Date { get; set; }
        public string? Appeal_Start_Date { get; set; }
        public string? Appeal_Close_Date { get; set; }
        public string? Reason { get; set; }
        public string? Batch_Date { get; set; }
        public int? total { get; set; }
        public string? GV_Category { get; set; }
        public string? GV_Category2 { get; set; }
        public string? GV_Category3 { get; set; }
        public string? GV_Market_Value { get; set; }
        public string? GV_Market_Value2 { get; set; }
        public string? GV_Market_Value3 { get; set; }
        public string? GV_Extent { get; set; }
        public string? GV_Extent2 { get; set; }
        public string? GV_Extent3 { get; set; }
        public string? GV_Remarks { get; set; }
        public string? GV_Remarks2 { get; set; }
        public string? GV_Remarks3 { get; set; }




        //Section 1 table

        public long? Ref { get; set; }

        public string? ObjectionRefS1 { get; set; }

        public string? OwnerName { get; set; }

        public string? OwnerIdentity { get; set; }

        public string? OwnerCompany { get; set; }

        public string? OwnerAddress1 { get; set; }

        public string? OwnerAddress2 { get; set; }

        public string? OwnerAddress3 { get; set; }

        public string? OwnerAddress4 { get; set; }

        public string? OwnerAddress5 { get; set; }

        public string? OwnerPostal1 { get; set; }

        public string? OwnerPostal2 { get; set; }

        public string? OwnerPostal3 { get; set; }

        public string? OwnerPostal4 { get; set; }

        public string? OwnerPostal5 { get; set; }

        public string? OwnerHomePhone { get; set; }

        public string? OwnerCellPhone { get; set; }

        public string? OwnerWorkPhone { get; set; }

        public string? OwnerFaxPhone { get; set; }

        public string? OwnerEmail { get; set; }

        public string? ObjectorName { get; set; }

        public string? ObjectorIdentity { get; set; }

        public string? ObjectorCompany { get; set; }

        public string? ObjectorPostal1 { get; set; }

        public string? ObjectorPostal2 { get; set; }

        public string? ObjectorPostal3 { get; set; }

        public string? ObjectorPostal4 { get; set; }

        public string? ObjectorPostal5 { get; set; }

        public string? ObjectorHome { get; set; }

        public string? ObjectorCell { get; set; }

        public string? ObjectorWork { get; set; }

        public string? ObjectorFax { get; set; }

        public string? ObjectorEmail { get; set; }

        public string? ObjectorStatus { get; set; }

        public string? RepresentativeName { get; set; }

        public string? RepPostal1 { get; set; }

        public string? RepPostal2 { get; set; }

        public string? RepPostal3 { get; set; }

        public string? RepPostal4 { get; set; }

        public string? RepPostal5 { get; set; }

        public string? RepHomePhone { get; set; }

        public string? RepCellPhone { get; set; }

        public string? RepWorkPhone { get; set; }

        public string? RepFaxPhone { get; set; }

        public string? RepEmail { get; set; }

        //Section 2 table

        public string? ObjectionRefS2 { get; set; }

        public string? PhysicalAddress { get; set; }

        public string? TownName { get; set; }

        public string? Code { get; set; }

        public string? Extent { get; set; }

        public string? MunicipalAccountNo { get; set; }

        public string? BondHolderName { get; set; }

        public string? RegisteredAmount { get; set; }

        public string? FullDetails { get; set; }

        public string? ServitudeNo { get; set; }

        public string? AffectedArea { get; set; }

        public string? PropertyFavourOf { get; set; }

        public string? PropertyPurpose { get; set; }

        public string? CompensationPaid { get; set; }

        public string? PaymentDate { get; set; }

        public string? CompensationAmount { get; set; }

        //Section 3 res table
        public string? ObjectionRefSr3 { get; set; }

        public string? ResNoOfBedroom { get; set; }

        public string? ResNoOfBathRoom { get; set; }

        public string? ResKitchen { get; set; }

        public string? ResLounge { get; set; }

        public string? ResDinningRoom { get; set; }

        public string? ResLoungeDiningRoom { get; set; }

        public string? ResStudy { get; set; }

        public string? ResPlayRoom { get; set; }

        public string? ResTelevision { get; set; }

        public string? ResLaundry { get; set; }

        public string? ResSeperateToilet { get; set; }

        public string? ResDwellOther1 { get; set; }

        public string? ResDwellOther2 { get; set; }

        public string? ResDwellOther3 { get; set; }

        public string? ResDwellOther4 { get; set; }

        public string? ResNoOfGarages { get; set; }

        public string? ResGrannyRoom { get; set; }

        public string? ResOutbuildOther { get; set; }

        public string? ResMainDwellingSize { get; set; }

        public string? ResOutsideBuildingSize { get; set; }

        public string? ResOtherBuildingSize { get; set; }

        public string? ResTotalBuildingSize { get; set; }

        public string? ResSwimmingPool { get; set; }

        public string? ResBoreHole { get; set; }

        public string? ResTennisCourt { get; set; }

        public string? ResGarden { get; set; }

        public string? ResOtherDwell1 { get; set; }

        public string? ResOtherDwell2 { get; set; }

        public string? ResFence { get; set; }

        public string? ResFenceFront { get; set; }

        public string? ResFenceBack { get; set; }

        public string? ResFenceSide1 { get; set; }

        public string? ResFenceSide2 { get; set; }

        public string? ResFenceHeightFront { get; set; }

        public string? ResFenceHeightBack { get; set; }

        public string? ResFenceHeightSide1 { get; set; }

        public string? ResFenceHeightSide2 { get; set; }

        public string? ResDriveWay { get; set; }

        public string? ResSecurityBoomedArea { get; set; }

        public string? ResOtherFeatures { get; set; }

        public string? ResOtherFeaturesCondition { get; set; }

        public string? ResGeneralCondition { get; set; }

        //Section 3 Agric table
        public string? ObjectionRefSa3 { get; set; }

        public string? AgriNoOfBedroom { get; set; }

        public string? AgriNoOfBathRoom { get; set; }

        public string? AgriKitchen { get; set; }

        public string? AgriLounge { get; set; }

        public string? AgriDinningRoom { get; set; }

        public string? AgriLoungeDiningRoom { get; set; }

        public string? AgriStudy { get; set; }

        public string? AgriPlayRoom { get; set; }

        public string? AgriTelevision { get; set; }

        public string? AgriLaundry { get; set; }

        public string? AgriSeperateToilet { get; set; }

        public string? AgriDwellOther1 { get; set; }

        public string? AgriMainDwellingSize { get; set; }

        public string? AgriBuildingNo { get; set; }

        public string? AgriBuildingDescription { get; set; }

        public string? AgriBuildingSize { get; set; }

        public string? AgriBuildingCondition { get; set; }

        public string? AgriBuildingFunctional { get; set; }

        public string? AgriAnotherPurposeNotAgriculture { get; set; }

        public string? AgriAnotherPurposeNotAgricultureDesc { get; set; }

        public string? AgriNonAgricultural { get; set; }

        public string? AgriGrazing { get; set; }

        public string? AgriUnderIrrigation { get; set; }

        public string? AgriDryLand { get; set; }

        public string? AgriPermanentCrop { get; set; }

        public string? AgriOtherHa1 { get; set; }

        public string? AgriOtherHa2 { get; set; }

        public string? AgriOtherHa3 { get; set; }

        public string? AgriTotalHa { get; set; }

        public string? AgriFenceCondition { get; set; }

        public string? AgriGameAreaFenced { get; set; }

        public string? AgriNumOfBoreholes { get; set; }

        public string? AgriOutputLitresHours { get; set; }

        public string? AgriDams { get; set; }

        public string? AgriCapacity { get; set; }

        public string? AgriExposedToRiver { get; set; }

        public string? AgriLandClaim { get; set; }

        public string? AgriClaimDate { get; set; }

        public string? AgriGazetteNo { get; set; }

        public string? AgriWaterRights { get; set; }

        public string? AgriWaterRightsDetails { get; set; }

        public string? AgriRezoningConsentUse { get; set; }

        public string? AgriConsentUseDetails { get; set; }

        public string? AgriLandExcised { get; set; }

        public string? AgriNewFarmDesc { get; set; }

        public string? AgriTownshipApplied { get; set; }

        public string? AgriTownshipAppliedDetail { get; set; }

        public string? AgriTenantName { get; set; }

        public string? AgriRentalLandSize { get; set; }

        public string? AgriRental { get; set; }

        public string? AgriEscalation { get; set; }

        public string? AgriOtherContribution { get; set; }

        public string? AgriLeaseTerm { get; set; }

        public string? AgriStartDate { get; set; }

        public string? AgriUse { get; set; }

        //Section 3 bus table
        public string? ObjectionRefSb3 { get; set; }

        public string? BusTenantName { get; set; }

        public string? BusRentalLandSize { get; set; }

        public string? BusRental { get; set; }

        public string? BusEscalation { get; set; }

        public string? BusOtherContribution { get; set; }

        public string? BusLeaseTerm { get; set; }

        public string? BusStartDate { get; set; }

        public string? BusBuildingNo { get; set; }

        public string? BusBuildingSize { get; set; }

        public string? BusShops { get; set; }

        public string? BusBuildingCondition { get; set; }

        public string? BusExtentLandFurtherDev { get; set; }

        public string? BusOtherFeaturesCondition { get; set; }

        //Section 4 res table
        public string? ObjectionRefSr4 { get; set; }

        public string? Res4SchemeName { get; set; }

        public string Res4SchemeNo { get; set; }

        public string Res4FlatNo { get; set; }

        public string? Res4UnitSize { get; set; }

        public string? Res4ManagingAgentName { get; set; }

        public string? Res4ManagingAgentTelNo { get; set; }

        public string Res4NoOfBedroom { get; set; }

        public string Res4NoOfBathRoom { get; set; }

        public string? Res4MonthlyLevyRes { get; set; }

        public string? Res4Kitchen { get; set; }

        public string? Res4Lounge { get; set; }

        public string? Res4DinningRoom { get; set; }

        public string? Res4LoungeDiningRoom { get; set; }

        public string? Res4Study { get; set; }

        public string? Res4PlayRoom { get; set; }

        public string? Res4Television { get; set; }

        public string? Res4Laundry { get; set; }

        public string? Res4SeperateToilet { get; set; }

        public string? Res4DwellOther1 { get; set; }

        public string? Res4DwellOther2 { get; set; }

        public string? Res4DwellOther3 { get; set; }

        public string? Res4DwellOther4 { get; set; }

        public string? Res4CommonPropertyOther1 { get; set; }

        public string? Res4CommonPropertyOther2 { get; set; }

        public string? Res4CommonPropertyOther3 { get; set; }

        public string? Res4PoolSize { get; set; }

        public string? Res4TennisCourtSize { get; set; }

        public string? Res4GarageSize { get; set; }

        public string? Res4CarportSize { get; set; }

        public string? Res4OpenParkingSize { get; set; }

        public string? Res4StoreRoomSize { get; set; }

        public string? Res4GardenSize { get; set; }

        public string? Res4ExclusiveOther { get; set; }

        //Section 4 bus table
        public string? ObjectionRefSb4 { get; set; }

        public string? Bus4SchemeName { get; set; }

        public string? Bus4SchemeNo { get; set; }

        public string? Bus4FlatNo { get; set; }

        public string? Bus4UnitSize { get; set; }

        public string? Bus4ManagingAgentName { get; set; }

        public string? Bus4ManagingAgentTelNo { get; set; }

        public string? Bus4Shops { get; set; }

        public string? Bus4Offices { get; set; }

        public string? Bus4Factories { get; set; }

        public string? Bus4BusSectTitleOther1Name { get; set; }

        public string? Bus4BusSectTitleOther2Name { get; set; }

        public string? Bus4BusSectTitleOther3Name { get; set; }

        public string? Bus4BusSectTitleOther1 { get; set; }

        public string? Bus4BusSectTitleOther2 { get; set; }

        public string? Bus4BusSectTitleOther3 { get; set; }

        public string? Bus4TenantName { get; set; }

        public string? Bus4Rental { get; set; }

        public string? Bus4OtherContribution { get; set; }

        public string? Bus4MonthlyLevy { get; set; }

        public string? Bus4RentalLandSize { get; set; }

        public string? Bus4Escalation { get; set; }

        public string? Bus4LeaseTerm { get; set; }

        public string? Bus4StartDate { get; set; }

        public string? Bus4PoolSize { get; set; }

        public string? Bus4TennisCourtSize { get; set; }

        public string? Bus4CommonPropertyOther1 { get; set; }

        public string? Bus4CommonPropertyOther2 { get; set; }

        public string? Bus4CommonPropertyOther3 { get; set; }

        public string? Bus4GarageSize { get; set; }

        public string? Bus4CarportSize { get; set; }

        public string? Bus4OpenParkingSize { get; set; }

        public string? Bus4StoreRoomSize { get; set; }

        public string? Bus4GardenSize { get; set; }

        public string? Bus4ExclusiveOther { get; set; }

        //Section 5 table
        public string? ObjectionRefS5 { get; set; }

        public string? CurrentAskingPrice { get; set; }

        public string? PreviousAskingPrice { get; set; }

        public string? AgentName { get; set; }

        public string? UnitNo { get; set; }

        public string? OtherNearbySales { get; set; }

        public string? SaleDate { get; set; }

        public string? CurrentRecievedOffer { get; set; }

        public string? PreviousRecievedOffer { get; set; }

        public string? AgentTelNo { get; set; }

        public string? SuburbName { get; set; }

        public string? SellingPrice { get; set; }

        //Section 6 table
        public string? ObjectionRefS6 { get; set; }

        public string? OldPropertyDescription { get; set; }

        public string? OldCategory { get; set; }

        public string? OldAddress { get; set; }

        public string? OldExtent { get; set; }

        public string? OldMarketValue { get; set; }

        public string? OldOwner { get; set; }

        public string? NewPropertyDescription { get; set; }

        public string? NewCategory { get; set; }

        public string? NewAddress { get; set; }

        public string? NewExtent { get; set; }

        public string? NewMarketValue { get; set; }

        public string? NewOwner { get; set; }

        public string? ObjectionReasons { get; set; }

        public string? Old2Category { get; set; }

        public string? Old2Extent { get; set; }

        public string? Old2MarketValue { get; set; }

        public string? New2Category { get; set; }

        public string? New2Extent { get; set; }

        public string? New2MarketValue { get; set; }

        public string? Old3Category { get; set; }

        public string? Old3Extent { get; set; }

        public string? Old3MarketValue { get; set; }

        public string? New3Category { get; set; }

        public string? New3Extent { get; set; }

        public string? New3MarketValue { get; set; }

        //Section 7 table
        public string? ObjectionRefS7 { get; set; }

        public string? SignaturePicture { get; set; }

        public string? SignatureName { get; set; }

        public string? RandomPin { get; set; }

        public string? FileName { get; set; }

        public string? FileType { get; set; }

        public string? FilePath { get; set; }

        public string? DeclarationDate { get; set; }

        //File info table
        public string? ObjectionRefFiles { get; set; }

        public string? Files1 { get; set; }

        public string? Files2 { get; set; }

        public string? Files3 { get; set; }

        public string? Files4 { get; set; }

        public string? Files5 { get; set; }

        public string? Files6 { get; set; }

        public string? Files7 { get; set; }

        public string? Files8 { get; set; }

        public string? Files9 { get; set; }

        public string? Files10 { get; set; }

        public string? RepLetter { get; set; }

        public double? EvidenceCount { get; set; }

        ///////////////

        public string? Files1_path { get; set; }

        public string? Files2_path { get; set; }

        public string? Files3_path { get; set; }

        public string? Files4_path { get; set; }

        public string? Files5_path { get; set; }

        public string? Files6_path { get; set; }

        public string? Files7_path { get; set; }

        public string? Files8_path { get; set; }

        public string? Files9_path { get; set; }

        public string? Files10_path { get; set; }

        public string? Acknowledgement_path { get; set; }

        public string? Acknowledgement { get; set; }

        public string? RepLetter_Path { get; set; }


        //Stats
        public int? SectorCount { get; set; }
        public int? statusCount { get; set; }
        public int? MVCount { get; set; }
        public int? CatCount { get; set; }
        public int? ExtCount { get; set; }
        public int? DescCount { get; set; }
        public int? NameCount { get; set; }
        public int? AddrCount { get; set; }
        public int? Total_ObjStats { get; set; }
        public int? TotalObjStatsPerItems { get; set; }
        public int? NumberOfUsers { get; set; }
        public int? LinkedProperties { get; set; }
        public int? TotalObjectionsStats { get; set; }
        public int? NoOfObjectionsPublicStats { get; set; }
        public int? NoOfObjectionsCenterStats { get; set; }
        public int? NoOfObjections_PublicStas { get; set; }
        public int? NoOfObjections_CenterStas { get; set; }
        public int? NoOfObjectionsFormPublicStas { get; set; }
        public int? NoOfObjectionsFormCenterStas { get; set; }
        public int? NumberOfObjectionsPerCenter { get; set; }
        public string? UserID { get; set; }

        //Township table

        public string? TC { get; set; }
        public string? TOWN_NAME_DESC { get; set; }
        public string? REGION_NAME { get; set; }
        public string? Snr_Manager { get; set; }
        public string? Dept_Dir { get; set; }
        public string? Area_Manager { get; set; }
        public string? Candidate_DC { get; set; }
        public string? Email_Address { get; set; }

        //Audit trail

        public string? Active_Username { get; set; }

        public string? Active_SapNo { get; set; }
        public string? Activity { get; set; }
        public string? AllocateTaskTo { get; set; }
        public string? ReAllocateTaskTo { get; set; }
        public string? MarketValue_MVD { get; set; }
        public string? Category_MVD { get; set; }
        public string? PhysicalAddress_MVD { get; set; }
        public string? Extent_MVD { get; set; }
        public string? PropertyDescription_MVD { get; set; }
        public string? OwnerName_MVD { get; set; }
        public string? Multi_MarketValue_MVD2 { get; set; }
        public string? Multi_Category_MVD2 { get; set; }
        public string? Multi_Extent_MVD2 { get; set; }
        public string? Multi_MarketValue_MVD3 { get; set; }
        public string? Multi_Category_MVD3 { get; set; }
        public string? Multi_Extent_MVD3 { get; set; }
        public string? Activity_DateTime { get; set; }
        public string? Declaration_Date { get; set; }

    }
}
