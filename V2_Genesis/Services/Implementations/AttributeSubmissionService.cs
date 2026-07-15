using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class AttributeSubmissionService : IAttributeSubmissionService
    {
        private readonly AttributesDbContext _context;

        private readonly IAttributeDocumentService _documentService;
        public AttributeSubmissionService(
            AttributesDbContext context,
           IAttributeDocumentService documentService)
        {
            _context = context;

            _documentService = documentService;
        }

        public AttributeSubmissionViewModel CreateNew(string formType)
        {
            formType = NormalizeFormType(formType);

            var model = new AttributeSubmissionViewModel
            {
                FormType = formType,
                ContactInfos = new List<AttributeContactInfoVm>
        {
            new AttributeContactInfoVm
            {
                ContactType = "Owner",
                IsCompany = false
            }
        },
                Access = new AttributeAccessVm
                {
                    AccessType = null,
                    PermissionStatus = null,
                    Comments = null
                },
                ValuationDetails = new AttributeValuationDetailsVm
                {
                    IsMixedUse = false
                }
            };

            if (formType == "BusinessCommercial")
            {
                for (int i = 0; i < 5; i++)
                {
                    model.BusinessBuildings.Add(new AttributeBusinessBuildingVm());
                    model.BusinessSections.Add(new AttributeBusinessSectionVm());
                }
            }

            if (formType == "DRCMethod")
            {
                for (int i = 0; i < 5; i++)
                {
                    model.DrcBuildings.Add(new AttributeDrcBuildingVm());
                    model.DrcImprovements.Add(new AttributeDrcImprovementVm());
                    model.DrcVacantLands.Add(new AttributeDrcVacantLandVm());
                }
            }

            return model;
        }

        private static string NormalizeFormType(string? formType)
        {
            return formType?.Trim() switch
            {
                "Residential" => "Residential",
                "ResidentialST" => "ResidentialST",
                "BusinessCommercial" => "BusinessCommercial",
                "DRCMethod" => "DRCMethod",
                "Business" => "BusinessCommercial",
                "DRC" => "DRCMethod",
                "Residential-ST" => "ResidentialST",
                _ => "Residential"
            };
        }

        public async Task<long> SubmitAsync(AttributeSubmissionViewModel model, string userId, string userName)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var propertyDetails = new AttrPropertyDetails
            {
                FormType = model.FormType,

                HArea = model.PropertyDetails.HArea,
                DataController = model.PropertyDetails.DataController,
                CollectionBlock = model.PropertyDetails.CollectionBlock,
                DataCollector = model.PropertyDetails.DataCollector,
                SGNumber = model.PropertyDetails.SGNumber,
                Centroid = model.PropertyDetails.Centroid,
                Erf = model.PropertyDetails.Erf,
                Extent = model.PropertyDetails.Extent,
                SectionalTitle = model.PropertyDetails.SectionalTitle,
                LandUseFinancials = model.PropertyDetails.LandUseFinancials,
                Municipality = model.PropertyDetails.Municipality,
                Ward = model.PropertyDetails.Ward,
                Township = model.PropertyDetails.Township,
                Zoning = model.PropertyDetails.Zoning,
                Sources = model.PropertyDetails.Sources,
                Address = model.PropertyDetails.Address,

                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            _context.AttrPropertyDetails.Add(propertyDetails);
            await _context.SaveChangesAsync();

            var propertyInfo = new AttrPropertyInfo
            {
                Attr_PropertyDetailsId = propertyDetails.Id,

                Objector_Type = model.ContactInfos.Count > 0 ? model.ContactInfos[0].ContactType : null,
                Property_Type = model.FormType,
                Property_Desc = model.PropertyDetails.PropertyDesc,
                Premise_id = model.PropertyDetails.PremiseId,
                Unit_key = model.PropertyDetails.UnitKey,
                Property_id = model.PropertyDetails.PropertyId,
                Valuation_Key = model.PropertyDetails.ValuationKey,
                Sector = model.PropertyDetails.Sector,
                RollType = model.PropertyDetails.RollType,
                RollDescription = model.PropertyDetails.RollDescription,

                SubmittedByUserId = userId,
                SubmittedByName = userName,
                SubmissionSource = "Genesis",
                SubmissionDateTime = DateTime.Now,
                ClientComment = model.ClientComment,

                Attr_Status = "Submitted",
                IsActive = true,

                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            _context.AttrPropertyInfo.Add(propertyInfo);
            await _context.SaveChangesAsync();

            // Required so computed Attr_No is available immediately
            await _context.Entry(propertyInfo).ReloadAsync();

            await SaveCommonSectionsAsync(model, propertyDetails.Id, userId);
            await SaveFormSpecificSectionsAsync(model, propertyDetails.Id, userId);

            // Creates:
            // C:\Attributes\ATTR-GV23-1
            // C:\Attributes\ATTR-GV23-1\Representative Documentations
            // C:\Attributes\ATTR-GV23-1\Attribute Lodged Evidence
            // and generates the PDF form.
            var documentResult = await _documentService.CreateSubmissionPackageAsync(model, propertyInfo);

            var evidencePin = GenerateEvidencePin();
            var evidenceDeadline = DateTime.Now.AddHours(48);

            model.GeneratedEvidencePin = evidencePin;
            model.GeneratedEvidenceDeadline = evidenceDeadline;



            _context.AttrDeclarations.Add(new AttrDeclaration
            {
                Attr_ID = propertyInfo.Attr_ID,
                Attr_No = propertyInfo.Attr_No,
                Attr_Ref_Signature = propertyInfo.Attr_No,

                Declaration_Text = model.Declaration.DeclarationText,
                Declaration_Accepted = model.Declaration.DeclarationAccepted,
                Declaration_Date = DateTime.Now,

                Signature_Picture = model.Declaration.SignaturePicture,
                Signature_Name = model.Declaration.SignatureName,

                RandomPin = evidencePin,
                EvidencePin = evidencePin,

                PinGeneratedDateTime = DateTime.Now,
                PinExpiryDateTime = evidenceDeadline,
                PinIsActive = true,

                AdditionalEvidenceAllowed = true,
                AdditionalEvidenceDeadline = evidenceDeadline,

                DeclaredByUserId = userId,
                DeclaredByName = userName,
                DeclaredByRole = model.RepresentativeDetails?.IsRepresentative == true ? "Representative" : "Client",

                CreatedBy = userId,
                CreatedDate = DateTime.Now
            });
            _context.AttrFiles.Add(new AttrFiles
            {
                Attr_ID = propertyInfo.Attr_ID,
                Attr_No = propertyInfo.Attr_No,
                Attr_Ref_Files = propertyInfo.Attr_No,

                Files1 = documentResult.Files1,
                Files2 = documentResult.Files2,
                Files3 = documentResult.Files3,
                Files4 = documentResult.Files4,
                Files5 = documentResult.Files5,
                Files6 = documentResult.Files6,
                Files7 = documentResult.Files7,
                Files8 = documentResult.Files8,
                Files9 = documentResult.Files9,
                Files10 = documentResult.Files10,

                Rep_Letter = documentResult.RepLetterFileName,

                Bulk_File_Name = documentResult.PdfFileName,

                // Add this if your AttrFiles model/table has this column
                Acknowledgement_FileName = documentResult.AcknowledgementFileName,

                Evidence_Count = documentResult.EvidenceCount,
                RootFolder = documentResult.AttrFolderPath,

                UploadedByUserId = userId,
                UploadedByName = userName,
                UploadedByRole = "Client",

                CreatedBy = userId,
                CreatedDate = DateTime.Now
            });

            propertyInfo.Evidence_Count = documentResult.EvidenceCount;
            propertyInfo.Has_Client_Evidence = documentResult.EvidenceCount > 0;
            propertyInfo.Last_Evidence_Uploaded_DateTime =
                documentResult.EvidenceCount > 0 ? DateTime.Now : null;

            propertyInfo.ClientEvidencePath = documentResult.AttrFolderPath;

            if (model.RepresentativeDetails?.IsRepresentative == true &&
                !string.IsNullOrWhiteSpace(model.RepresentativeDetails.Representative_Name))
            {
                _context.AttrRepresentatives.Add(new AttrRepresentative
                {
                    Attr_ID = propertyInfo.Attr_ID,
                    Attr_No = propertyInfo.Attr_No,
                    IDProperty = model.PropertyDetails.UnitKey ?? model.PropertyDetails.PropertyId ?? model.PropertyDetails.PremiseId,
                    UserID = userId,

                    Representative_Name = model.RepresentativeDetails.Representative_Name,
                    Rep_Postal_1 = model.RepresentativeDetails.Rep_Postal_1,
                    Rep_Postal_2 = model.RepresentativeDetails.Rep_Postal_2,
                    Rep_Postal_3 = model.RepresentativeDetails.Rep_Postal_3,
                    Rep_Postal_4 = model.RepresentativeDetails.Rep_Postal_4,
                    Rep_Postal_5 = model.RepresentativeDetails.Rep_Postal_5,

                    Rep_Home_Phone = model.RepresentativeDetails.Rep_Home_Phone,
                    Rep_Cell_Phone = model.RepresentativeDetails.Rep_Cell_Phone,
                    Rep_Work_Phone = model.RepresentativeDetails.Rep_Work_Phone,
                    Rep_Fax_Phone = model.RepresentativeDetails.Rep_Fax_Phone,
                    Rep_Email = model.RepresentativeDetails.Rep_Email,

                    Auth_Letter_FileName = documentResult.RepLetterFileName,

                    CreatedBy = userId,
                    CreatedDate = DateTime.Now
                });
            }

            await AddAuditAsync(
                propertyInfo.Attr_ID,
                propertyInfo.Attr_No,
                "Submitted",
                null,
                "Submitted",
                userId,
                userName,
                "Client",
                "Client submitted attribute property information.");

            await AddAuditAsync(
                propertyInfo.Attr_ID,
                propertyInfo.Attr_No,
                "PDF and Evidence Saved",
                "Submitted",
                "Submitted",
                userId,
                userName,
                "Client",
                $"PDF saved as {documentResult.PdfFileName}. Evidence files uploaded: {documentResult.EvidenceCount}.");

            await AddAuditAsync(
    propertyInfo.Attr_ID,
    propertyInfo.Attr_No,
    "Declaration Submitted",
    "Submitted",
    "Submitted",
    userId,
    userName,
    "Client",
    "Client accepted declaration and signature was captured. Evidence PIN generated for 48 hours.");

            var unitKey = model.PropertyDetails.UnitKey
              ?? model.PropertyDetails.PropertyId
              ?? model.PropertyDetails.PremiseId;

            if (!string.IsNullOrWhiteSpace(unitKey))
            {
                var linkedRecord = await _context.LinkedProperties
                    .FirstOrDefaultAsync(lp => lp.IDProperty == unitKey
                                             && lp.UserID == userId);

                if (linkedRecord != null)
                    _context.LinkedProperties.Remove(linkedRecord);
            }
            await _context.SaveChangesAsync();


            await transaction.CommitAsync();

            return propertyInfo.Attr_ID;
        }

        private static string GenerateEvidencePin()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var random = new Random();

            return new string(Enumerable
                .Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }
        private async Task SaveCommonSectionsAsync(AttributeSubmissionViewModel model, int propertyDetailsId, string userId)
        {
            _context.AttrValuationDetails.Add(new AttrValuationDetails
            {
                PropertyDetailsId = propertyDetailsId,
                ValuationCategoryOnRoll = model.ValuationDetails.ValuationCategoryOnRoll,
                ActualUse = model.ValuationDetails.ActualUse,
                IsMixedUse = model.ValuationDetails.IsMixedUse,
                AlternateUsages = model.ValuationDetails.AlternateUsages,
                OwnersTitleDeeds = model.ValuationDetails.OwnersTitleDeeds,
                OwnersFinancials = model.ValuationDetails.OwnersFinancials,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            });

            _context.AttrAccess.Add(new AttrAccess
            {
                PropertyDetailsId = propertyDetailsId,
                AccessType = model.Access.AccessType,
                PermissionStatus = model.Access.PermissionStatus,
                Comments = model.Access.Comments,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            });

            foreach (var contact in model.ContactInfos)
            {
                if (IsEmptyContact(contact))
                    continue;

                _context.AttrContactInfo.Add(new AttrContactInfo
                {
                    PropertyDetailsId = propertyDetailsId,
                    ContactType = contact.ContactType,
                    IsCompany = contact.IsCompany,
                    CompanyName = contact.CompanyName,
                    CompanyRegistrationNumber = contact.CompanyRegistrationNumber,
                    FirstNames = contact.FirstNames,
                    LastName = contact.LastName,
                    MaidenName = contact.MaidenName,
                    IDNumber = contact.IDNumber,
                    DateOfBirth = contact.DateOfBirth,
                    Gender = contact.Gender,
                    MaritalStatus = contact.MaritalStatus,
                    Citizenship = contact.Citizenship,
                    PhysicalAddress = contact.PhysicalAddress,
                    PostalAddress = contact.PostalAddress,
                    Email = contact.Email,
                    HomePhoneNo = contact.HomePhoneNo,
                    WorkPhoneNo = contact.WorkPhoneNo,
                    CellNo = contact.CellNo,
                    FaxNo = contact.FaxNo,
                    Interviewed = contact.Interviewed,
                    Comments = contact.Comments,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now
                });
            }

            _context.AttrPrimaryAttributes.Add(new AttrPrimaryAttributes
            {
                PropertyDetailsId = propertyDetailsId,
                Tla1 = model.PrimaryAttributes.Tla1,
                Tla2 = model.PrimaryAttributes.Tla2,
                Tla3 = model.PrimaryAttributes.Tla3,
                Garage = model.PrimaryAttributes.Garage,
                CarportCp = model.PrimaryAttributes.CarportCp,
                GrannyFlatGf = model.PrimaryAttributes.GrannyFlatGf,
                StaffQuartersSq = model.PrimaryAttributes.StaffQuartersSq,
                Storage = model.PrimaryAttributes.Storage,
                AdjustmentFactor = model.PrimaryAttributes.AdjustmentFactor,
                STMain = model.PrimaryAttributes.STMain,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            });

            _context.AttrSecondaryAttributes.Add(new AttrSecondaryAttributes
            {
                PropertyDetailsId = propertyDetailsId,
                Storeys = model.SecondaryAttributes.Storeys,
                Security = model.SecondaryAttributes.Security,
                Noise = model.SecondaryAttributes.Noise,
                Topography = model.SecondaryAttributes.Topography,
                Quality = model.SecondaryAttributes.Quality,
                Condition = model.SecondaryAttributes.Condition,
                SwimmingPool = model.SecondaryAttributes.SwimmingPool,
                TennisCourt = model.SecondaryAttributes.TennisCourt,
                STCondition = model.SecondaryAttributes.STCondition,
                STFloor = model.SecondaryAttributes.STFloor,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            });

            _context.AttrCalculations.Add(new AttrCalculations
            {
                PropertyDetailsId = propertyDetailsId,
                CalcUpdateTla = model.Calculations.CalcUpdateTla,
                Tla = model.Calculations.Tla,
                CalcUpdateWgba = model.Calculations.CalcUpdateWgba,
                AdjustedWgba = model.Calculations.AdjustedWgba,
                TotalValueNonRes = model.Calculations.TotalValueNonRes,
                TotalValueUnutilisedLand = model.Calculations.TotalValueUnutilisedLand,
                DRCFinalValue = model.Calculations.DRCFinalValue,
                CalculationStatus = model.Calculations.CalculationStatus,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private async Task SaveFormSpecificSectionsAsync(AttributeSubmissionViewModel model, int propertyDetailsId, string userId)
        {
            if (model.FormType == "BusinessCommercial")
            {
                foreach (var item in model.BusinessBuildings)
                {
                    if (IsEmptyBusinessBuilding(item))
                        continue;

                    _context.AttrBusinessBuildings.Add(new AttrBusinessBuildings
                    {
                        PropertyDetailsId = propertyDetailsId,
                        BuildingNr = item.BuildingNr,
                        Quality = item.Quality,
                        Condition = item.Condition,
                        YearBuilt = item.YearBuilt,
                        Storeys = item.Storeys,
                        Depreciation = item.Depreciation,
                        GBA = item.GBA,
                        Cost = item.Cost,
                        DRC = item.DRC,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    });
                }

                foreach (var item in model.BusinessSections)
                {
                    if (IsEmptyBusinessSection(item))
                        continue;

                    _context.AttrBusinessSections.Add(new AttrBusinessSections
                    {
                        PropertyDetailsId = propertyDetailsId,
                        BuildingNr = item.BuildingNr,
                        Usage = item.Usage,
                        MarketGroup = item.MarketGroup,
                        Quality = item.Quality,
                        GBA = item.GBA,
                        NLA = item.NLA,
                        CostRate = item.CostRate,
                        Cost = item.Cost,
                        Rental = item.Rental,
                        Vac = item.Vac,
                        Exp = item.Exp,
                        Cap = item.Cap,
                        Gross = item.Gross,
                        Normalised = item.Normalised,
                        Nett = item.Nett,
                        Value = item.Value,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    });
                }

                _context.AttrBusinessGeneral.Add(new AttrBusinessGeneral
                {
                    PropertyDetailsId = propertyDetailsId,
                    UnutilisedLandExtent = model.BusinessGeneral.UnutilisedLandExtent,
                    UnutilisedLandRate = model.BusinessGeneral.UnutilisedLandRate,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now
                });
            }

            if (model.FormType == "DRCMethod")
            {
                foreach (var item in model.DrcBuildings)
                {
                    if (IsEmptyDrcBuilding(item))
                        continue;

                    _context.AttrDrcBuildings.Add(new AttrDrcBuildings
                    {
                        PropertyDetailsId = propertyDetailsId,
                        BuildingDescription = item.BuildingDescription,
                        Quality = item.Quality,
                        GrossBuildingArea = item.GrossBuildingArea,
                        Condition = item.Condition,
                        DepreciationPercentage = item.DepreciationPercentage,
                        RatePerSQM = item.RatePerSQM,
                        DepreciatedRate = item.DepreciatedRate,
                        ReplacementCost = item.ReplacementCost,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    });
                }

                foreach (var item in model.DrcImprovements)
                {
                    if (IsEmptyDrcImprovement(item))
                        continue;

                    _context.AttrDrcImprovements.Add(new AttrDrcImprovements
                    {
                        PropertyDetailsId = propertyDetailsId,
                        ImprovementDescription = item.ImprovementDescription,
                        Quality = item.Quality,
                        AreaUnit = item.AreaUnit,
                        Condition = item.Condition,
                        DepreciationPercentage = item.DepreciationPercentage,
                        RatePerSQM = item.RatePerSQM,
                        DepreciatedRate = item.DepreciatedRate,
                        ReplacementCost = item.ReplacementCost,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    });
                }

                foreach (var item in model.DrcVacantLands)
                {
                    if (IsEmptyDrcVacantLand(item))
                        continue;

                    _context.AttrDrcVacantLand.Add(new AttrDrcVacantLand
                    {
                        PropertyDetailsId = propertyDetailsId,
                        Region = item.Region,
                        MinRatePerSQM = item.MinRatePerSQM,
                        MidRatePerSQM = item.MidRatePerSQM,
                        MaxRatePerSQM = item.MaxRatePerSQM,
                        Area = item.Area,
                        Rate = item.Rate,
                        VacantLandCost = item.VacantLandCost,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    });
                }

                _context.AttrDrcMarketValueDemolition.Add(new AttrDrcMarketValueDemolition
                {
                    PropertyDetailsId = propertyDetailsId,
                    DemolitionRate = model.DrcMarketValueDemolition.DemolitionRate,
                    MarketValue = model.DrcMarketValueDemolition.MarketValue,
                    MarketValueAfterDemolition = model.DrcMarketValueDemolition.MarketValueAfterDemolition,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }




        public async Task<AttributeSubmissionViewModel?> GetForReviewAsync(long attrId)
        {
            var info = await _context.AttrPropertyInfo
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.ContactInfos)
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.BusinessBuildings)
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.BusinessSections)
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.DrcBuildings)
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.DrcImprovements)
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.DrcVacantLands)
                .FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            if (info?.PropertyDetails == null)
                return null;

            var pd = info.PropertyDetails;

            var vm = new AttributeSubmissionViewModel
            {
                AttrId = info.Attr_ID,
                AttrNo = info.Attr_No,
                FormType = pd.FormType,
                ClientComment = info.ClientComment,

                PropertyDetails = new AttributePropertyDetailsVm
                {
                    HArea = pd.HArea,
                    DataController = pd.DataController,
                    CollectionBlock = pd.CollectionBlock,
                    DataCollector = pd.DataCollector,
                    SGNumber = pd.SGNumber,
                    Centroid = pd.Centroid,
                    Erf = pd.Erf,
                    Extent = pd.Extent,
                    SectionalTitle = pd.SectionalTitle,
                    LandUseFinancials = pd.LandUseFinancials,
                    Municipality = pd.Municipality,
                    Ward = pd.Ward,
                    Township = pd.Township,
                    Zoning = pd.Zoning,
                    Sources = pd.Sources,
                    Address = pd.Address,
                    PropertyDesc = info.Property_Desc,
                    PremiseId = info.Premise_id,
                    UnitKey = info.Unit_key,
                    PropertyId = info.Property_id,
                    ValuationKey = info.Valuation_Key,
                    Sector = info.Sector,
                    RollType = info.RollType,
                    RollDescription = info.RollDescription
                }
            };

            return vm;
        }

        public async Task AssignToValuerAsync(long attrId, string valuerUserId, string valuerName, string assignedBy, string? comment)
        {
            var item = await _context.AttrPropertyInfo.FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            if (item == null)
                throw new InvalidOperationException("Attribute submission was not found.");

            var oldStatus = item.Attr_Status;

            item.Task_Assigned_To_UserId = valuerUserId;
            item.Task_Assigned_To = valuerName;
            item.Task_Assigned_DateTime = DateTime.Now;
            item.Task_Assigner = assignedBy;
            item.TaskAssignerComment = comment;
            item.Attr_Status = "Assigned";
            item.UpdatedBy = assignedBy;
            item.UpdatedDate = DateTime.Now;

            await AddAuditAsync(
                item.Attr_ID,
                item.Attr_No,
                "Assigned",
                oldStatus,
                "Assigned",
                assignedBy,
                assignedBy,
                "Manager",
                comment ?? $"Assigned to {valuerName}");

            await _context.SaveChangesAsync();
        }

        public async Task ValuerDecisionAsync(
            long attrId,
            string decision,
            string valuerUserId,
            string valuerName,
            string? comment,
            string? rejectionReason)
        {
            var item = await _context.AttrPropertyInfo.FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            if (item == null)
                throw new InvalidOperationException("Attribute submission was not found.");

            if (decision == "Rejected" && string.IsNullOrWhiteSpace(rejectionReason))
                throw new InvalidOperationException("Rejection reason is required.");

            var oldStatus = item.Attr_Status;

            item.Valuer = valuerName;
            item.ValuerUserId = valuerUserId;
            item.ValuerComment = comment;
            item.ValuerDecision = decision;
            item.RejectionReason = rejectionReason;
            item.ValuerDecisionDateTime = DateTime.Now;
            item.Attr_Status = decision;

            if (decision == "Approved")
            {
                item.ReadyForOvvioExtract = true;
                item.OvvioExtractStatus = "Pending";
            }

            item.UpdatedBy = valuerUserId;
            item.UpdatedDate = DateTime.Now;

            await AddAuditAsync(
                item.Attr_ID,
                item.Attr_No,
                decision,
                oldStatus,
                decision,
                valuerUserId,
                valuerName,
                "Valuer",
                decision == "Rejected" ? rejectionReason : comment);

            await _context.SaveChangesAsync();
        }

        public async Task WithdrawAsync(long attrId, string userId, string userName, string reason)
        {
            var item = await _context.AttrPropertyInfo.FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            if (item == null)
                throw new InvalidOperationException("Attribute submission was not found.");

            var oldStatus = item.Attr_Status;

            item.IsWithdrawn = true;
            item.WithdrawnByUserId = userId;
            item.WithdrawnByName = userName;
            item.WithdrawnDateTime = DateTime.Now;
            item.WithdrawalReason = reason;
            item.Attr_Status = "Withdrawn";
            item.UpdatedBy = userId;
            item.UpdatedDate = DateTime.Now;

            _context.AttrWithdrawals.Add(new AttrWithdrawals
            {
                Attr_ID = item.Attr_ID,
                Attr_No = item.Attr_No,
                Attribute_Withdrawn = "Full Submission",
                WithdrawalReason = reason,
                WithdrawnByUserId = userId,
                WithdrawnByName = userName,
                WithdrawnByRole = "Client",
                WithdrawalStatus = "Withdrawn",
                DateWithdrawn = DateTime.Now
            });

            await AddAuditAsync(
                item.Attr_ID,
                item.Attr_No,
                "Withdrawn",
                oldStatus,
                "Withdrawn",
                userId,
                userName,
                "Client",
                reason);

            await _context.SaveChangesAsync();
        }

        private async Task AddAuditAsync(
            long attrId,
            string? attrNo,
            string action,
            string? oldStatus,
            string? newStatus,
            string? userId,
            string? userName,
            string role,
            string? comment)
        {
            _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
            {
                Attr_ID = attrId,
                Attr_No = attrNo,
                Action = action,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ActionByUserId = userId,
                ActionByName = userName,
                ActionRole = role,
                Comment = comment,
                ActionDateTime = DateTime.Now
            });

            await Task.CompletedTask;
        }

        private static bool IsEmptyContact(AttributeContactInfoVm x)
        {
            return string.IsNullOrWhiteSpace(x.FirstNames)
                   && string.IsNullOrWhiteSpace(x.LastName)
                   && string.IsNullOrWhiteSpace(x.CompanyName)
                   && string.IsNullOrWhiteSpace(x.IDNumber)
                   && string.IsNullOrWhiteSpace(x.Email);
        }

        private static bool IsEmptyBusinessBuilding(AttributeBusinessBuildingVm x)
        {
            return string.IsNullOrWhiteSpace(x.BuildingNr)
                   && string.IsNullOrWhiteSpace(x.Quality)
                   && string.IsNullOrWhiteSpace(x.Condition)
                   && x.GBA == null
                   && x.Cost == null;
        }

        private static bool IsEmptyBusinessSection(AttributeBusinessSectionVm x)
        {
            return string.IsNullOrWhiteSpace(x.BuildingNr)
                   && string.IsNullOrWhiteSpace(x.Usage)
                   && string.IsNullOrWhiteSpace(x.MarketGroup)
                   && x.GBA == null
                   && x.NLA == null
                   && x.Value == null;
        }

        private static bool IsEmptyDrcBuilding(AttributeDrcBuildingVm x)
        {
            return string.IsNullOrWhiteSpace(x.BuildingDescription)
                   && x.GrossBuildingArea == null
                   && x.ReplacementCost == null;
        }

        private static bool IsEmptyDrcImprovement(AttributeDrcImprovementVm x)
        {
            return string.IsNullOrWhiteSpace(x.ImprovementDescription)
                   && x.AreaUnit == null
                   && x.ReplacementCost == null;
        }

        private static bool IsEmptyDrcVacantLand(AttributeDrcVacantLandVm x)
        {
            return string.IsNullOrWhiteSpace(x.Region)
                   && x.Area == null
                   && x.Rate == null
                   && x.VacantLandCost == null;
        }


        public async Task<AttributeAcknowledgementVm?> GetAcknowledgementAsync(long attrId)
        {
            var info = await _context.AttrPropertyInfo
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.ValuationDetails)
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.Calculations)
                .FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            if (info == null)
                return null;

            var declaration = await _context.AttrDeclarations
                .FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            var files = await _context.AttrFiles
                .FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            var uploadedDocs = new List<string>();

            if (!string.IsNullOrWhiteSpace(files?.Files1)) uploadedDocs.Add(files.Files1);
            if (!string.IsNullOrWhiteSpace(files?.Files2)) uploadedDocs.Add(files.Files2);
            if (!string.IsNullOrWhiteSpace(files?.Files3)) uploadedDocs.Add(files.Files3);
            if (!string.IsNullOrWhiteSpace(files?.Files4)) uploadedDocs.Add(files.Files4);
            if (!string.IsNullOrWhiteSpace(files?.Files5)) uploadedDocs.Add(files.Files5);
            if (!string.IsNullOrWhiteSpace(files?.Files6)) uploadedDocs.Add(files.Files6);
            if (!string.IsNullOrWhiteSpace(files?.Files7)) uploadedDocs.Add(files.Files7);
            if (!string.IsNullOrWhiteSpace(files?.Files8)) uploadedDocs.Add(files.Files8);
            if (!string.IsNullOrWhiteSpace(files?.Files9)) uploadedDocs.Add(files.Files9);
            if (!string.IsNullOrWhiteSpace(files?.Files10)) uploadedDocs.Add(files.Files10);

            var submission = await BuildSubmittedAttributeViewModelAsync(attrId);

            return new AttributeAcknowledgementVm
            {
                AttrId = info.Attr_ID,
                AttrNo = info.Attr_No,

                PropertyDescription = info.Property_Desc,
                PropertyCategory = info.PropertyDetails?.ValuationDetails?.ValuationCategoryOnRoll,
                PhysicalAddress = info.PropertyDetails?.Address,

                MarketValue = info.PropertyDetails?.Calculations?.Tla?.ToString(),
                Extent = info.PropertyDetails?.Extent,

                OwnerName = info.PropertyDetails?.ValuationDetails?.OwnersFinancials
                            ?? info.PropertyDetails?.ValuationDetails?.OwnersTitleDeeds,

                Pin = declaration?.EvidencePin ?? declaration?.RandomPin,

                SubmissionDate = info.SubmissionDateTime,

                EvidenceDeadline = declaration?.AdditionalEvidenceDeadline
                                   ?? info.SubmissionDateTime.AddHours(48),

                EvidenceCount = files?.Evidence_Count ?? 0,

                AcknowledgementFileName = files?.Acknowledgement_FileName,

                AcknowledgementPath = files == null || string.IsNullOrWhiteSpace(files.Acknowledgement_FileName)
                    ? null
                    : Path.Combine(files.RootFolder ?? "", files.Acknowledgement_FileName),

                UploadedDocuments = uploadedDocs,

                // New: full submitted form data for the HTML acknowledgement display
                Submission = submission
            };

        }
        private async Task<AttributeSubmissionViewModel?> BuildSubmittedAttributeViewModelAsync(long attrId)
        {
            var info = await _context.AttrPropertyInfo
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.ValuationDetails)
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.Calculations)
                .FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            if (info?.PropertyDetails == null)
                return null;

            var property = info.PropertyDetails;
            var propertyDetailsId = property.Id;

            var valuation = property.ValuationDetails;
            var calculations = property.Calculations;

            var declaration = await _context.AttrDeclarations
                .FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            var contacts = await _context.AttrContactInfo
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var primary = await _context.AttrPrimaryAttributes
                .FirstOrDefaultAsync(x => x.PropertyDetailsId == propertyDetailsId);

            var secondary = await _context.AttrSecondaryAttributes
                .FirstOrDefaultAsync(x => x.PropertyDetailsId == propertyDetailsId);

            var businessBuildings = await _context.AttrBusinessBuildings
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var businessSections = await _context.AttrBusinessSections
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var businessGeneral = await _context.AttrBusinessGeneral
                .FirstOrDefaultAsync(x => x.PropertyDetailsId == propertyDetailsId);

            var drcBuildings = await _context.AttrDrcBuildings
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var drcImprovements = await _context.AttrDrcImprovements
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var drcVacantLands = await _context.AttrDrcVacantLand
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var drcMarketValue = await _context.AttrDrcMarketValueDemolition
                .FirstOrDefaultAsync(x => x.PropertyDetailsId == propertyDetailsId);

            var model = new AttributeSubmissionViewModel
            {
                AttrId = info.Attr_ID,
                AttrNo = info.Attr_No,

                // In your SubmitAsync, FormType is saved on AttrPropertyDetails.FormType
                // and also Property_Type on AttrPropertyInfo.
                FormType = property.FormType ?? info.Property_Type,

                ClientComment = info.ClientComment,

                PropertyDetails = new AttributePropertyDetailsVm
                {
                    PropertyId = info.Property_id,
                    PremiseId = info.Premise_id,
                    UnitKey = info.Unit_key,
                    ValuationKey = info.Valuation_Key,
                    Sector = info.Sector,
                    RollType = info.RollType,
                    RollDescription = info.RollDescription,

                    HArea = property.HArea,
                    CollectionBlock = property.CollectionBlock,
                    DataController = property.DataController,
                    DataCollector = property.DataCollector,
                    SGNumber = property.SGNumber,
                    Centroid = property.Centroid,
                    Erf = property.Erf,
                    Extent = property.Extent,
                    SectionalTitle = property.SectionalTitle,
                    LandUseFinancials = property.LandUseFinancials,
                    Municipality = property.Municipality,
                    Ward = property.Ward,
                    Township = property.Township,
                    Zoning = property.Zoning,
                    Sources = property.Sources,
                    Address = property.Address,
                    PropertyDesc = info.Property_Desc
                },

                ValuationDetails = new AttributeValuationDetailsVm
                {
                    ValuationCategoryOnRoll = valuation?.ValuationCategoryOnRoll,
                    ActualUse = valuation?.ActualUse,
                    IsMixedUse = valuation?.IsMixedUse ?? false,
                    AlternateUsages = valuation?.AlternateUsages,
                    OwnersTitleDeeds = valuation?.OwnersTitleDeeds,
                    OwnersFinancials = valuation?.OwnersFinancials
                },

                Access = new AttributeAccessVm
                {
                    AccessType = null,
                    PermissionStatus = null,
                    Comments = null
                },

                PrimaryAttributes = new AttributePrimaryAttributesVm
                {
                    Tla1 = primary?.Tla1,
                    Tla2 = primary?.Tla2,
                    Tla3 = primary?.Tla3,
                    Garage = primary?.Garage,
                    CarportCp = primary?.CarportCp,
                    GrannyFlatGf = primary?.GrannyFlatGf,
                    StaffQuartersSq = primary?.StaffQuartersSq,
                    Storage = primary?.Storage,
                    AdjustmentFactor = primary?.AdjustmentFactor,
                    STMain = primary?.STMain
                },

                SecondaryAttributes = new AttributeSecondaryAttributesVm
                {
                    Storeys = secondary?.Storeys,
                    Security = secondary?.Security,
                    Noise = secondary?.Noise,
                    Topography = secondary?.Topography,
                    Quality = secondary?.Quality,
                    Condition = secondary?.Condition,
                    SwimmingPool = secondary?.SwimmingPool,
                    TennisCourt = secondary?.TennisCourt,
                    STCondition = secondary?.STCondition,
                    STFloor = secondary?.STFloor
                },

                Calculations = new AttributeCalculationsVm
                {
                    CalcUpdateTla = calculations?.CalcUpdateTla,
                    Tla = calculations?.Tla,
                    CalcUpdateWgba = calculations?.CalcUpdateWgba,
                    AdjustedWgba = calculations?.AdjustedWgba,
                    TotalValueNonRes = calculations?.TotalValueNonRes,
                    TotalValueUnutilisedLand = calculations?.TotalValueUnutilisedLand,
                    DRCFinalValue = calculations?.DRCFinalValue,
                    CalculationStatus = calculations?.CalculationStatus
                },

                BusinessGeneral = new AttributeBusinessGeneralVm
                {
                    UnutilisedLandExtent = businessGeneral?.UnutilisedLandExtent,
                    UnutilisedLandRate = businessGeneral?.UnutilisedLandRate
                },

                DrcMarketValueDemolition = new AttributeDrcMarketValueDemolitionVm
                {
                    DemolitionRate = drcMarketValue?.DemolitionRate,
                    MarketValue = drcMarketValue?.MarketValue,
                    MarketValueAfterDemolition = drcMarketValue?.MarketValueAfterDemolition
                },

                Declaration = new AttributeDeclarationVm
                {
                    DeclarationAccepted = declaration?.Declaration_Accepted ?? false,
                    SignatureName = declaration?.Signature_Name,
                    SignaturePicture = declaration?.Signature_Picture,
                    DeclarationText = declaration?.Declaration_Text
                },

                ContactInfos = contacts.Select(c => new AttributeContactInfoVm
                {
                    ContactType = c.ContactType,
                    IsCompany = c.IsCompany,
                    CompanyName = c.CompanyName,
                    CompanyRegistrationNumber = c.CompanyRegistrationNumber,
                    FirstNames = c.FirstNames,
                    LastName = c.LastName,
                    PhysicalAddress = c.PhysicalAddress,
                    PostalAddress = c.PostalAddress,
                    Email = c.Email,
                    HomePhoneNo = c.HomePhoneNo,
                    WorkPhoneNo = c.WorkPhoneNo,
                    CellNo = c.CellNo
                }).ToList(),

                BusinessBuildings = businessBuildings.Select(b => new AttributeBusinessBuildingVm
                {
                    BuildingNr = b.BuildingNr,
                    Quality = b.Quality,
                    Condition = b.Condition,
                    YearBuilt = b.YearBuilt,
                    Storeys = b.Storeys,
                    GBA = b.GBA
                }).ToList(),

                BusinessSections = businessSections.Select(s => new AttributeBusinessSectionVm
                {
                    BuildingNr = s.BuildingNr,
                    Usage = s.Usage,
                    GBA = s.GBA,
                    NLA = s.NLA,
                    Rental = s.Rental
                }).ToList(),

                DrcBuildings = drcBuildings.Select(b => new AttributeDrcBuildingVm
                {
                    BuildingDescription = b.BuildingDescription,
                    Quality = b.Quality,
                    GrossBuildingArea = b.GrossBuildingArea,
                    Condition = b.Condition
                }).ToList(),

                DrcImprovements = drcImprovements.Select(i => new AttributeDrcImprovementVm
                {
                    ImprovementDescription = i.ImprovementDescription,
                    Quality = i.Quality,
                    AreaUnit = i.AreaUnit,
                    Condition = i.Condition
                }).ToList(),

                DrcVacantLands = drcVacantLands.Select(v => new AttributeDrcVacantLandVm
                {
                    Region = v.Region,
                    Area = v.Area
                }).ToList()
            };

            return model;
        }
    }
}