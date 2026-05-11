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
        private readonly IWebHostEnvironment _environment;

        public AttributeSubmissionService(
            AttributesDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public AttributeSubmissionViewModel CreateNew(string formType)
        {
            var model = new AttributeSubmissionViewModel
            {
                FormType = formType
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

            await SaveCommonSectionsAsync(model, propertyDetails.Id, userId);

            await SaveFormSpecificSectionsAsync(model, propertyDetails.Id, userId);

            var fileRecord = await SaveFilesAsync(model, propertyInfo, userId, userName);

            if (fileRecord != null)
            {
                propertyInfo.Evidence_Count = fileRecord.Evidence_Count ?? 0;
                propertyInfo.Has_Client_Evidence = propertyInfo.Evidence_Count > 0;
                propertyInfo.Last_Evidence_Uploaded_DateTime = DateTime.Now;
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

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return propertyInfo.Attr_ID;
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

        private async Task<AttrFiles?> SaveFilesAsync(
            AttributeSubmissionViewModel model,
            AttrPropertyInfo propertyInfo,
            string userId,
            string userName)
        {
            var attrNo = propertyInfo.Attr_No ?? $"ATTR-GV23-{propertyInfo.Attr_ID}";
            var root = Path.Combine(_environment.WebRootPath, "AttributeEvidence", attrNo);

            Directory.CreateDirectory(root);

            var fileRecord = new AttrFiles
            {
                Attr_ID = propertyInfo.Attr_ID,
                Attr_No = attrNo,
                Attr_Ref_Files = attrNo,
                RootFolder = root,
                UploadedByUserId = userId,
                UploadedByName = userName,
                UploadedByRole = "Client",
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            int count = 0;

            fileRecord.Files1 = await SaveOneFileAsync(model.Files.Files1, root, attrNo, "Evidence_1", () => count++);
            fileRecord.Files2 = await SaveOneFileAsync(model.Files.Files2, root, attrNo, "Evidence_2", () => count++);
            fileRecord.Files3 = await SaveOneFileAsync(model.Files.Files3, root, attrNo, "Evidence_3", () => count++);
            fileRecord.Files4 = await SaveOneFileAsync(model.Files.Files4, root, attrNo, "Evidence_4", () => count++);
            fileRecord.Files5 = await SaveOneFileAsync(model.Files.Files5, root, attrNo, "Evidence_5", () => count++);
            fileRecord.Files6 = await SaveOneFileAsync(model.Files.Files6, root, attrNo, "Evidence_6", () => count++);
            fileRecord.Files7 = await SaveOneFileAsync(model.Files.Files7, root, attrNo, "Evidence_7", () => count++);
            fileRecord.Files8 = await SaveOneFileAsync(model.Files.Files8, root, attrNo, "Evidence_8", () => count++);
            fileRecord.Files9 = await SaveOneFileAsync(model.Files.Files9, root, attrNo, "Evidence_9", () => count++);
            fileRecord.Files10 = await SaveOneFileAsync(model.Files.Files10, root, attrNo, "Evidence_10", () => count++);

            fileRecord.Rep_Letter = await SaveOneFileAsync(model.Files.RepLetter, root, attrNo, "Representative_Letter", () => count++);

            fileRecord.Evidence_Count = count;

            if (count == 0)
                return null;

            _context.AttrFiles.Add(fileRecord);

            await AddAuditAsync(
                propertyInfo.Attr_ID,
                attrNo,
                "Evidence Uploaded",
                "Submitted",
                "Submitted",
                userId,
                userName,
                "Client",
                $"Client uploaded {count} file(s).");

            return fileRecord;
        }

        private static async Task<string?> SaveOneFileAsync(
            Microsoft.AspNetCore.Http.IFormFile? file,
            string folder,
            string attrNo,
            string label,
            Action incrementCount)
        {
            if (file == null || file.Length == 0)
                return null;

            var extension = Path.GetExtension(file.FileName);
            var safeFileName = $"{attrNo}_{label}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
            var path = Path.Combine(folder, safeFileName);

            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            incrementCount();

            return safeFileName;
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
    }
}
