using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.Configuration;
using V2_Genesis.Models.Results.Atrributes;
using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Attributes
{
    public class AttributeDocumentService : IAttributeDocumentService
    {
        private readonly AttributeStorageOptions _options;
        private readonly IWebHostEnvironment _env;

        private const string HEADER_IMAGE = "Images/Obj_Header.PNG";

        public AttributeDocumentService(
            IOptions<AttributeStorageOptions> options,
            IWebHostEnvironment env)
        {
            _options = options.Value;
            _env = env;
        }

        public async Task<AttributeDocumentSaveResult> CreateSubmissionPackageAsync(
            AttributeSubmissionViewModel model,
            AttrPropertyInfo propertyInfo)
        {
            if (string.IsNullOrWhiteSpace(_options.BasePath))
                throw new InvalidOperationException("AttributeStorage:BasePath is not configured in appsettings.json.");

            var attrNo = propertyInfo.Attr_No ?? $"ATTR-GV23-{propertyInfo.Attr_ID}";
            var formName = GetFormName(model.FormType);
            var propertyDesc = model.PropertyDetails.PropertyDesc ?? propertyInfo.Property_Desc ?? "Property";
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var safeAttrNo = MakeSafeFileName(attrNo);
            var safePropertyDesc = MakeSafeFileName(propertyDesc);
            var safeFormName = MakeSafeFileName(formName);

            var attrFolder = Path.Combine(_options.BasePath, safeAttrNo);
            var repFolder = Path.Combine(attrFolder, "Representative Documentations");
            var evidenceFolder = Path.Combine(attrFolder, "Attribute Lodged Evidence");

            Directory.CreateDirectory(attrFolder);
            Directory.CreateDirectory(repFolder);
            Directory.CreateDirectory(evidenceFolder);

            var pdfFileName = $"{safeAttrNo}_{safePropertyDesc}_{safeFormName}_{timestamp}.pdf";
            var pdfPath = Path.Combine(attrFolder, pdfFileName);

            GeneratePdf(model, propertyInfo, pdfPath, formName);
            var acknowledgementFileName = $"{safeAttrNo}_{safePropertyDesc}_{timestamp}_Acknowledgement.pdf";
            var acknowledgementPath = Path.Combine(attrFolder, acknowledgementFileName);

            GenerateAcknowledgementReplicaPdf(model, propertyInfo, acknowledgementPath);

            var result = new AttributeDocumentSaveResult
            {
                AttrFolderPath = attrFolder,
                PdfFileName = pdfFileName,
                PdfFullPath = pdfPath,
                AcknowledgementFileName = acknowledgementFileName,
                AcknowledgementFullPath = acknowledgementPath
            };

            result.RepLetterFileName = await SaveOneFileAsync(
                model.Files.RepLetter,
                repFolder,
                safeAttrNo,
                "Representative_Letter");
            var evidenceFiles = model.Files.EvidenceFiles
                .Where(f => f is { Length: > 0 })
                .Take(10)
                .ToList();

            var fileProps = new string?[10];

            for (int i = 0; i < evidenceFiles.Count; i++)
            {
                fileProps[i] = await SaveEvidenceFileAsync(
                    evidenceFiles[i], evidenceFolder, safeAttrNo, $"Evidence_{i + 1}", result);
            }

            result.Files1 = fileProps[0];
            result.Files2 = fileProps[1];
            result.Files3 = fileProps[2];
            result.Files4 = fileProps[3];
            result.Files5 = fileProps[4];
            result.Files6 = fileProps[5];
            result.Files7 = fileProps[6];
            result.Files8 = fileProps[7];
            result.Files9 = fileProps[8];
            result.Files10 = fileProps[9];
            return result;
        }

        private static async Task<string?> SaveEvidenceFileAsync(
            IFormFile? file,
            string folder,
            string attrNo,
            string label,
            AttributeDocumentSaveResult result)
        {
            var saved = await SaveOneFileAsync(file, folder, attrNo, label);

            if (!string.IsNullOrWhiteSpace(saved))
                result.EvidenceCount++;

            return saved;
        }

        private static async Task<string?> SaveOneFileAsync(
            IFormFile? file,
            string folder,
            string attrNo,
            string label)
        {
            if (file == null || file.Length == 0)
                return null;

            var originalExtension = Path.GetExtension(file.FileName);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");

            var safeLabel = MakeSafeFileName(label);
            var safeOriginalName = MakeSafeFileName(Path.GetFileNameWithoutExtension(file.FileName));

            var storedFileName = $"{attrNo}_{safeLabel}_{safeOriginalName}_{timestamp}{originalExtension}";
            var fullPath = Path.Combine(folder, storedFileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return storedFileName;
        }

        private void GeneratePdf(
    AttributeSubmissionViewModel model,
    AttrPropertyInfo propertyInfo,
    string pdfPath,
    string formName)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    var headerPath = Path.Combine(_env.WebRootPath, HEADER_IMAGE);

                    page.Header().Column(header =>
                    {
                        AddCojHeaderImage(header, headerPath);

                        header.Item()
                            .PaddingTop(4)
                            .AlignCenter()
                            .Text(formName)
                            .Bold()
                            .FontSize(13);

                        header.Item().PaddingTop(4).LineHorizontal(0.5f);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        AddReferenceSection(col, model, propertyInfo);
                        AddPropertyDetailsSection(col, model);
                        AddValuationSection(col, model);
                        AddAccessSection(col, model);
                        AddContactSection(col, model);

                        if (model.FormType == "Residential")
                        {
                            AddResidentialPrimarySection(col, model);
                            AddResidentialSecondarySection(col, model);
                        }

                        if (model.FormType == "ResidentialST")
                        {
                            AddStPrimarySection(col, model);
                            AddStSecondarySection(col, model);
                        }

                        if (model.FormType == "BusinessCommercial")
                        {
                            AddBusinessBuildingsSection(col, model);
                            AddBusinessSectionsSection(col, model);
                            AddBusinessGeneralSection(col, model);
                        }

                        if (model.FormType == "DRCMethod")
                        {
                            AddDrcBuildingsSection(col, model);
                            AddDrcImprovementsSection(col, model);
                            AddDrcVacantLandSection(col, model);
                            AddDrcMarketValueSection(col, model);
                        }

                        AddCalculationsSection(col, model);

                        if (!string.IsNullOrWhiteSpace(model.ClientComment))
                        {
                            AddSectionTitle(col, "Client Comment");
                            col.Item().Border(1).Padding(5).Text(model.ClientComment);
                        }
                        AddDeclarationSection(col, model, propertyInfo);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated on ");
                        text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).SemiBold();
                        text.Span(" | Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });

                });

            }).GeneratePdf(pdfPath);
        }
        private static void AddCojHeaderImage(ColumnDescriptor col, string headerPath)
        {
            if (File.Exists(headerPath))
            {
                col.Item()
                    .AlignCenter()
                    .Width(500)
                    .Height(90)
                    .Image(headerPath, ImageScaling.FitArea);

                return;
            }

            // Fallback only if image is missing.
            col.Item()
                .AlignCenter()
                .Text("City of Johannesburg")
                .Bold()
                .FontSize(15);
        }
        private static void AddReferenceSection(ColumnDescriptor col, AttributeSubmissionViewModel model, AttrPropertyInfo info)
        {
            AddSectionTitle(col, "Submission Reference");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("Attribute Number", info.Attr_No),
                ("Status", info.Attr_Status),
                ("Form Type", model.FormType),
                ("Submitted Date", info.SubmissionDateTime.ToString("yyyy-MM-dd HH:mm")),
                ("Submitted By", info.SubmittedByName),
                ("Submission Source", info.SubmissionSource)
            });
        }

        private static void AddPropertyDetailsSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            var p = model.PropertyDetails;

            AddSectionTitle(col, "Property Details");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("H Area", p.HArea),
                ("Data Controller", p.DataController),
                ("Collection Block", p.CollectionBlock),
                ("Data Collector", p.DataCollector),
                ("SG Number", p.SGNumber),
                ("Centroid", p.Centroid),
                ("Erf", p.Erf),
                ("Extent", p.Extent),
                ("Sectional Title", p.SectionalTitle),
                ("Land Use Financials", p.LandUseFinancials),
                ("Municipality", p.Municipality),
                ("Ward", p.Ward),
                ("Township", p.Township),
                ("Zoning", p.Zoning),
                ("Sources", p.Sources),
                ("Address", p.Address),
                ("Property Description", p.PropertyDesc),
                ("Premise ID", p.PremiseId),
                ("Property ID", p.PropertyId),
                ("Valuation Key", p.ValuationKey),
                ("Sector", p.Sector),
                ("Roll Type", p.RollType)
            });
        }

        private static void AddValuationSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            var v = model.ValuationDetails;

            AddSectionTitle(col, "Valuation Details");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("Valuation Category on Roll", v.ValuationCategoryOnRoll),
                ("Actual Use", v.ActualUse),
                ("Mixed Use", v.IsMixedUse ? "Yes" : "No"),
                ("Alternate Usages", v.AlternateUsages),
                ("Owners Title Deeds", v.OwnersTitleDeeds),
                ("Owners Financials", v.OwnersFinancials)
            });
        }

        private static void AddAccessSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            var a = model.Access;

            AddSectionTitle(col, "Access");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("Access Type", a.AccessType),
                ("Permission Status", a.PermissionStatus),
                ("Comments", a.Comments)
            });
        }

        private static void AddContactSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "Contact Info");

            if (model.ContactInfos == null || model.ContactInfos.Count == 0)
            {
                col.Item().Text("No contact information captured.");
                return;
            }

            foreach (var c in model.ContactInfos)
            {
                AddTwoColumnTable(col, new List<(string Label, string? Value)>
                {
                    ("Contact Type", c.ContactType),
                    ("Is Company", c.IsCompany ? "Yes" : "No"),
                    ("Company Name", c.CompanyName),
                    ("Company Registration Number", c.CompanyRegistrationNumber),
                    ("First Names", c.FirstNames),
                    ("Surname", c.LastName),
                    ("ID Number", c.IDNumber),
                    ("Date of Birth", c.DateOfBirth?.ToString("yyyy-MM-dd")),
                    ("Gender", c.Gender),
                    ("Marital Status", c.MaritalStatus),
                    ("Citizenship", c.Citizenship),
                    ("Physical Address", c.PhysicalAddress),
                    ("Postal Address", c.PostalAddress),
                    ("Email", c.Email),
                    ("Home Phone", c.HomePhoneNo),
                    ("Work Phone", c.WorkPhoneNo),
                    ("Cell No", c.CellNo),
                    ("Fax No", c.FaxNo),
                    ("Interviewed", c.Interviewed == true ? "Yes" : c.Interviewed == false ? "No" : ""),
                    ("Comments", c.Comments)
                });

                col.Item().PaddingBottom(5);
            }
        }

        private static void AddResidentialPrimarySection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            var p = model.PrimaryAttributes;

            AddSectionTitle(col, "Primary Attributes");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("Tla1", p.Tla1?.ToString()),
                ("Tla2", p.Tla2?.ToString()),
                ("Tla3", p.Tla3?.ToString()),
                ("Garage", p.Garage?.ToString()),
                ("Carport CP", p.CarportCp?.ToString()),
                ("Granny Flat GF", p.GrannyFlatGf?.ToString()),
                ("Staff Quarters SQ", p.StaffQuartersSq?.ToString()),
                ("Storage", p.Storage?.ToString()),
                ("Adjustment Factor", p.AdjustmentFactor?.ToString())
            });
        }

        private static void AddResidentialSecondarySection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            var s = model.SecondaryAttributes;

            AddSectionTitle(col, "Secondary Attributes");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("Storeys", s.Storeys?.ToString()),
                ("Security", s.Security),
                ("Noise", s.Noise),
                ("Topography", s.Topography),
                ("Quality", s.Quality),
                ("Condition", s.Condition),
                ("Swimming Pool", BoolText(s.SwimmingPool)),
                ("Tennis Court", BoolText(s.TennisCourt))
            });
        }

        private static void AddStPrimarySection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            var p = model.PrimaryAttributes;

            AddSectionTitle(col, "Primary Attributes - ST");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("ST Main", p.STMain?.ToString()),
                ("Garage", p.Garage?.ToString()),
                ("Carport CP", p.CarportCp?.ToString()),
                ("Storage", p.Storage?.ToString())
            });
        }

        private static void AddStSecondarySection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            var s = model.SecondaryAttributes;

            AddSectionTitle(col, "Secondary Attributes - ST");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("ST Condition", s.STCondition?.ToString()),
                ("ST Floor", s.STFloor?.ToString()),
                ("Quality", s.Quality)
            });
        }

        private static void AddBusinessBuildingsSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "Business Buildings");

            AddSimpleTable(
                col,
                new[] { "Building Nr", "Quality", "Condition", "Year", "Storeys", "Dep", "GBA", "Cost", "DRC" },
                model.BusinessBuildings.Select(x => new[]
                {
                    x.BuildingNr,
                    x.Quality,
                    x.Condition,
                    x.YearBuilt?.ToString(),
                    x.Storeys?.ToString(),
                    x.Depreciation?.ToString(),
                    x.GBA?.ToString(),
                    x.Cost?.ToString(),
                    x.DRC?.ToString()
                }).ToList());
        }

        private static void AddBusinessSectionsSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "Business Sections");

            AddSimpleTable(
                col,
                new[] { "Building", "Usage", "Market", "Quality", "GBA", "NLA", "Cost Rate", "Cost", "Rental", "Value" },
                model.BusinessSections.Select(x => new[]
                {
                    x.BuildingNr,
                    x.Usage,
                    x.MarketGroup,
                    x.Quality,
                    x.GBA?.ToString(),
                    x.NLA?.ToString(),
                    x.CostRate?.ToString(),
                    x.Cost?.ToString(),
                    x.Rental?.ToString(),
                    x.Value?.ToString()
                }).ToList());
        }

        private static void AddBusinessGeneralSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "Business General");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("Unutilised Land Extent", model.BusinessGeneral.UnutilisedLandExtent?.ToString()),
                ("Unutilised Land Rate", model.BusinessGeneral.UnutilisedLandRate?.ToString())
            });
        }

        private static void AddDrcBuildingsSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "DRC Buildings");

            AddSimpleTable(
                col,
                new[] { "Description", "Quality", "GBA", "Condition", "Dep %", "Rate", "Dep Rate", "Replacement Cost" },
                model.DrcBuildings.Select(x => new[]
                {
                    x.BuildingDescription,
                    x.Quality,
                    x.GrossBuildingArea?.ToString(),
                    x.Condition,
                    x.DepreciationPercentage?.ToString(),
                    x.RatePerSQM?.ToString(),
                    x.DepreciatedRate?.ToString(),
                    x.ReplacementCost?.ToString()
                }).ToList());
        }

        private static void AddDrcImprovementsSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "DRC Improvements");

            AddSimpleTable(
                col,
                new[] { "Description", "Quality", "Area", "Condition", "Dep %", "Rate", "Dep Rate", "Replacement Cost" },
                model.DrcImprovements.Select(x => new[]
                {
                    x.ImprovementDescription,
                    x.Quality,
                    x.AreaUnit?.ToString(),
                    x.Condition,
                    x.DepreciationPercentage?.ToString(),
                    x.RatePerSQM?.ToString(),
                    x.DepreciatedRate?.ToString(),
                    x.ReplacementCost?.ToString()
                }).ToList());
        }

        private static void AddDrcVacantLandSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "DRC Vacant Land");

            AddSimpleTable(
                col,
                new[] { "Region", "Min", "Mid", "Max", "Area", "Rate", "Cost" },
                model.DrcVacantLands.Select(x => new[]
                {
                    x.Region,
                    x.MinRatePerSQM?.ToString(),
                    x.MidRatePerSQM?.ToString(),
                    x.MaxRatePerSQM?.ToString(),
                    x.Area?.ToString(),
                    x.Rate?.ToString(),
                    x.VacantLandCost?.ToString()
                }).ToList());
        }

        private static void AddDrcMarketValueSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "Market Value and Demolition");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("Demolition Rate", model.DrcMarketValueDemolition.DemolitionRate?.ToString()),
                ("Market Value", model.DrcMarketValueDemolition.MarketValue?.ToString()),
                ("Market Value After Demolition", model.DrcMarketValueDemolition.MarketValueAfterDemolition?.ToString())
            });
        }

        private static void AddCalculationsSection(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            var c = model.Calculations;

            AddSectionTitle(col, "Calculations");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
            {
                ("Calc Update TLA", c.CalcUpdateTla),
                ("TLA", c.Tla?.ToString()),
                ("Calc Update WGBA", c.CalcUpdateWgba),
                ("Adjusted WGBA", c.AdjustedWgba?.ToString()),
                ("Total Value Non Res", c.TotalValueNonRes?.ToString()),
                ("Total Value Unutilised Land", c.TotalValueUnutilisedLand?.ToString()),
                ("DRC Final Value", c.DRCFinalValue?.ToString()),
                ("Calculation Status", c.CalculationStatus)
            });
        }

        private static void AddSectionTitle(ColumnDescriptor col, string title)
        {
            col.Item()
                .PaddingTop(10)
                .Background("#D7ECEA")
                .Padding(5)
                .Text(title)
                .Bold()
                .FontSize(9);
        }
        private static void AddTwoColumnTable(ColumnDescriptor col, List<(string Label, string? Value)> rows)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                });

                for (var i = 0; i < rows.Count; i += 2)
                {
                    AddLabelCell(table, rows[i].Label);
                    AddValueCell(table, rows[i].Value);

                    if (i + 1 < rows.Count)
                    {
                        AddLabelCell(table, rows[i + 1].Label);
                        AddValueCell(table, rows[i + 1].Value);
                    }
                    else
                    {
                        AddLabelCell(table, "");
                        AddValueCell(table, "");
                    }
                }
            });
        }

        private static void AddSimpleTable(ColumnDescriptor col, string[] headers, List<string?[]> rows)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in headers)
                        columns.RelativeColumn();
                });

                foreach (var header in headers)
                {
                    table.Cell()
                        .Border(1)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(3)
                        .Text(header)
                        .Bold()
                        .FontSize(7);
                }

                var dataRows = rows.Where(r => r.Any(v => !string.IsNullOrWhiteSpace(v))).ToList();

                if (!dataRows.Any())
                {
                    table.Cell()
                        .ColumnSpan((uint)headers.Length)
                        .Border(1)
                        .Padding(4)
                        .Text("No data captured.")
                        .FontSize(7);

                    return;
                }

                foreach (var row in dataRows)
                {
                    foreach (var value in row)
                    {
                        table.Cell()
                            .Border(1)
                            .Padding(3)
                            .Text(value ?? "")
                            .FontSize(7);
                    }
                }
            });
        }

        private static void AddLabelCell(TableDescriptor table, string text)
        {
            table.Cell()
                .Border(1)
                .Background(Colors.Grey.Lighten4)
                .Padding(4)
                .Text(text)
                .Bold();
        }

        private static void AddValueCell(TableDescriptor table, string? text)
        {
            table.Cell()
                .Border(1)
                .Padding(4)
                .Text(text ?? "");
        }

        private static string? BoolText(bool? value)
        {
            if (value == true) return "Yes";
            if (value == false) return "No";
            return "";
        }

        private static string GetFormName(string formType)
        {
            return formType switch
            {
                "Residential" => "Residential",
                "BusinessCommercial" => "Business and Commercial",
                "DRCMethod" => "DRC Method",
                "ResidentialST" => "Residential ST",
                _ => formType
            };
        }

        private static void AddAcknowledgementIntro(
    ColumnDescriptor col,
    AttributeSubmissionViewModel model,
    AttrPropertyInfo propertyInfo)
        {
            col.Item()
                .Background("#FFF8E1")
                .Border(0.5f)
                .BorderColor("#E6B000")
                .Padding(8)
                .Column(box =>
                {
                    box.Item().Text("Submission Received").Bold().FontSize(11);
                    box.Item().PaddingTop(3).Text(
                        "This document is a copy of the property attribute information submitted through the City of Johannesburg Valuation Portal.");
                    box.Item().PaddingTop(3).Text(
                        "This acknowledgement does not imply acceptance or approval. The submitted information remains subject to review by the valuation team.");
                    box.Item().PaddingTop(5).Text($"Attribute Reference: {propertyInfo.Attr_No ?? model.AttrNo ?? ""}").Bold();
                });
        }

        private static void AddSubmittedPropertyDetails(
    ColumnDescriptor col,
    AttributeSubmissionViewModel model)
        {
            var p = model.PropertyDetails;

            AddSectionTitle(col, "1. Property Details");

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(2f);
                });

                AddFullRow(table, "Property Description", p?.PropertyDesc);

                AddTwoColumnRow(table, "H Area", p?.HArea, "Data Controller", p?.DataController);
                AddTwoColumnRow(table, "Collection Block", p?.CollectionBlock, "Data Collector", p?.DataCollector);
                AddTwoColumnRow(table, "SG Number", p?.SGNumber, "Centroid", p?.Centroid);
                AddTwoColumnRow(table, "Erf", p?.Erf, "Extent", p?.Extent);
                AddTwoColumnRow(table, "Sectional Title", p?.SectionalTitle, "Land Use Financials", p?.LandUseFinancials);
                AddTwoColumnRow(table, "Municipality", p?.Municipality, "Ward", p?.Ward);
                AddTwoColumnRow(table, "Township", p?.Township, "Zoning", p?.Zoning);
                AddTwoColumnRow(table, "Sources", p?.Sources, "Address", p?.Address);
            });
        }

        private static void AddSubmittedValuationDetails(
    ColumnDescriptor col,
    AttributeSubmissionViewModel model)
        {
            var v = model.ValuationDetails;

            AddSectionTitle(col, "2. Valuation Details");

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(2f);
                });

                AddTwoColumnRow(
                    table,
                    "Valuation Category on Roll",
                    v?.ValuationCategoryOnRoll,
                    "Actual Use",
                    v?.ActualUse);

                if (v?.IsMixedUse == true || !string.IsNullOrWhiteSpace(v?.AlternateUsages))
                {
                    AddFullRow(table, "Mixed Use", v?.AlternateUsages);
                }
            });
        }
        private static void AddSubmittedContactDetails(
    ColumnDescriptor col,
    AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "3. Contact Information");

            if (model.ContactInfos == null || !model.ContactInfos.Any())
            {
                col.Item().Padding(5).Text("No contact information supplied.");
                return;
            }

            for (var i = 0; i < model.ContactInfos.Count; i++)
            {
                var c = model.ContactInfos[i];

                col.Item().PaddingTop(6).Text($"Contact {i + 1}: {(c.IsCompany ? "Company" : "Owner")}").Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(2f);
                    });

                    if (c.IsCompany)
                    {
                        AddTwoColumnRow(table, "Company Name", c.CompanyName, "Registration No.", c.CompanyRegistrationNumber);
                    }
                    else
                    {
                        AddTwoColumnRow(table, "First Names", c.FirstNames, "Surname", c.LastName);
                    }

                    AddFullRow(table, "Physical Address", c.PhysicalAddress);
                    AddFullRow(table, "Postal Address", c.PostalAddress);
                    AddTwoColumnRow(table, "Email", c.Email, "Cell No", c.CellNo);
                    AddTwoColumnRow(table, "Home Phone", c.HomePhoneNo, "Work Phone", c.WorkPhoneNo);
                });
            }
        }
        private static void AddSubmittedFormSpecificDetails(
    ColumnDescriptor col,
    AttributeSubmissionViewModel model)
        {
            switch (model.FormType)
            {
                case "Residential":
                    AddResidentialSubmission(col, model);
                    break;

                case "ResidentialST":
                    AddResidentialStSubmission(col, model);
                    break;

                case "BusinessCommercial":
                    AddBusinessSubmission(col, model);
                    break;

                case "DRCMethod":
                    AddDrcSubmission(col, model);
                    break;
            }
        }
        private static void AddResidentialSubmission(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "4. Residential Attributes");

            var p = model.PrimaryAttributes;
            var s = model.SecondaryAttributes;

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(2f);
                });

                AddTwoColumnRow(table, "TLA 1", p?.Tla1?.ToString(), "TLA 2", p?.Tla2?.ToString());
                AddTwoColumnRow(table, "TLA 3", p?.Tla3?.ToString(), "Garage", p?.Garage?.ToString());
                AddTwoColumnRow(table, "Carport CP", p?.CarportCp?.ToString(), "Granny Flat GF", p?.GrannyFlatGf?.ToString());
                AddTwoColumnRow(table, "Staff Quarters SQ", p?.StaffQuartersSq?.ToString(), "Storage", p?.Storage?.ToString());

                AddTwoColumnRow(table, "Storeys", s?.Storeys?.ToString(), "Security", s?.Security?.ToString());
                AddTwoColumnRow(table, "Noise", s?.Noise?.ToString(), "Topography", s?.Topography?.ToString());
                AddTwoColumnRow(table, "Quality", s?.Quality?.ToString(), "Condition", s?.Condition?.ToString());
                AddTwoColumnRow(table, "Swimming Pool", BoolText(s?.SwimmingPool), "Tennis Court", BoolText(s?.TennisCourt));
            });
        }
        private static void AddResidentialStSubmission(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "4. Residential Sectional Title Attributes");

            var p = model.PrimaryAttributes;
            var s = model.SecondaryAttributes;

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(2f);
                });

                AddTwoColumnRow(table, "ST Main", p?.STMain?.ToString(), "Garage", p?.Garage?.ToString());
                AddTwoColumnRow(table, "Carport CP", p?.CarportCp?.ToString(), "Storage", p?.Storage?.ToString());
                AddTwoColumnRow(table, "ST Condition", s?.STCondition?.ToString(), "ST Floor", s?.STFloor?.ToString());
                AddTwoColumnRow(table, "Quality", s?.Quality?.ToString(), "", "");
            });
        }
        private static void AddBusinessSubmission(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "4. Non-Residential / Business and Commercial");

            if (model.BusinessBuildings?.Any() == true)
            {
                col.Item().PaddingTop(5).Text("Buildings").Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    AddHeader(table, "Building Nr", "Quality", "Condition", "Year Built", "Storeys", "GBA");

                    foreach (var b in model.BusinessBuildings.Where(x => HasAnyValue(x)))
                    {
                        table.Cell().Element(CellValue).Text(b.BuildingNr ?? "");
                        table.Cell().Element(CellValue).Text(b.Quality ?? "");
                        table.Cell().Element(CellValue).Text(b.Condition ?? "");
                        table.Cell().Element(CellValue).Text(b.YearBuilt?.ToString() ?? "");
                        table.Cell().Element(CellValue).Text(b.Storeys?.ToString() ?? "");
                        table.Cell().Element(CellValue).Text(b.GBA?.ToString() ?? "");
                    }
                });
            }

            if (model.BusinessSections?.Any() == true)
            {
                col.Item().PaddingTop(8).Text("Sections / Lease Details").Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    AddHeader(table, "Building Nr", "Usage", "GBA", "NLA", "Rental R/m²");

                    foreach (var s in model.BusinessSections.Where(x => HasAnyValue(x)))
                    {
                        table.Cell().Element(CellValue).Text(s.BuildingNr ?? "");
                        table.Cell().Element(CellValue).Text(s.Usage ?? "");
                        table.Cell().Element(CellValue).Text(s.GBA?.ToString() ?? "");
                        table.Cell().Element(CellValue).Text(s.NLA?.ToString() ?? "");
                        table.Cell().Element(CellValue).Text(s.Rental?.ToString() ?? "");
                    }
                });
            }

            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn(3);
                });

                table.Cell().Element(CellLabel).Text("Unutilised Land Extent");
                table.Cell().Element(CellValue).Text(model.BusinessGeneral?.UnutilisedLandExtent?.ToString() ?? "");
            });
        }
        private static void AddDrcSubmission(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "4. DRC Method");

            if (model.DrcBuildings?.Any() == true)
            {
                col.Item().PaddingTop(5).Text("Buildings").Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    AddHeader(table, "Building Description", "Quality", "GBA", "Condition");

                    foreach (var b in model.DrcBuildings.Where(x => HasAnyValue(x)))
                    {
                        table.Cell().Element(CellValue).Text(b.BuildingDescription ?? "");
                        table.Cell().Element(CellValue).Text(b.Quality ?? "");
                        table.Cell().Element(CellValue).Text(b.GrossBuildingArea?.ToString() ?? "");
                        table.Cell().Element(CellValue).Text(b.Condition ?? "");
                    }
                });
            }

            if (model.DrcImprovements?.Any() == true)
            {
                col.Item().PaddingTop(8).Text("Improvements").Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    AddHeader(table, "Improvement Description", "Quality", "Area / Unit", "Condition");

                    foreach (var i in model.DrcImprovements.Where(x => HasAnyValue(x)))
                    {
                        table.Cell().Element(CellValue).Text(i.ImprovementDescription ?? "");
                        table.Cell().Element(CellValue).Text(i.Quality ?? "");
                        table.Cell().Element(CellValue).Text(i.AreaUnit?.ToString() ?? "");
                        table.Cell().Element(CellValue).Text(i.Condition ?? "");
                    }
                });
            }

            if (model.DrcVacantLands?.Any() == true)
            {
                col.Item().PaddingTop(8).Text("Vacant Land").Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    AddHeader(table, "Region", "Area");

                    foreach (var v in model.DrcVacantLands.Where(x => HasAnyValue(x)))
                    {
                        table.Cell().Element(CellValue).Text(v.Region ?? "");
                        table.Cell().Element(CellValue).Text(v.Area?.ToString() ?? "");
                    }
                });
            }
        }
        private static void AddSubmittedComments(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ClientComment))
                return;

            AddSectionTitle(col, "5. Additional Comments");

            col.Item()
                .Border(0.5f)
                .BorderColor("#BFD8D6")
                .Padding(6)
                .Text(model.ClientComment);
        }

        private static void AddSubmittedEvidenceSummary(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "6. Supporting Documents");

            var fileNames = new List<string>();

            if (model.Files?.RepLetter != null)
                fileNames.Add(model.Files.RepLetter.FileName);

            if (model.Files?.EvidenceFiles != null)
                fileNames.AddRange(model.Files.EvidenceFiles.Select(f => f.FileName));

            if (!fileNames.Any())
            {
                col.Item().Padding(5).Text("No supporting documents uploaded.");
                return;
            }

            col.Item().Column(list =>
            {
                foreach (var file in fileNames)
                {
                    list.Item().Text($"• {file}");
                }
            });
        }

        private static void AddSubmittedDeclaration(ColumnDescriptor col, AttributeSubmissionViewModel model)
        {
            AddSectionTitle(col, "7. Declaration");

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn(3);
                });

                table.Cell().Element(CellLabel).Text("Declaration Accepted");
                table.Cell().Element(CellValue).Text(model.Declaration?.DeclarationAccepted == true ? "Yes" : "No");

                table.Cell().Element(CellLabel).Text("Submitted Date");
                table.Cell().Element(CellValue).Text(DateTime.Now.ToString("dd MMMM yyyy"));
            });
        }

        private static void AddEvidenceAccessDetails(
            ColumnDescriptor col,
            AttributeSubmissionViewModel model,
            AttrPropertyInfo propertyInfo)
        {
            var reference = propertyInfo.Attr_No ?? model.AttrNo ?? "";
            var pin = model.GeneratedEvidencePin;
            var deadline = model.GeneratedEvidenceDeadline;

            col.Item()
                .PaddingTop(8)
                .Background("#EAF7F5")
                .Border(1)
                .BorderColor("#006B70")
                .Padding(10)
                .Column(box =>
                {
                    box.Item()
                        .Text("Additional Evidence Access")
                        .Bold()
                        .FontSize(11)
                        .FontColor("#006B70");

                    box.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn(2.2f);
                        });

                        table.Cell().Element(CellLabel).Text("Reference Number");
                        table.Cell().Element(CellValue).Text(reference).Bold();

                        table.Cell().Element(CellLabel).Text("Evidence PIN");
                        table.Cell().Element(CellValue)
                            .Text(string.IsNullOrWhiteSpace(pin) ? "Not available" : pin)
                            .Bold()
                            .FontSize(12);

                        table.Cell().Element(CellLabel).Text("Evidence Window Closes");
                        table.Cell().Element(CellValue).Text(
                            deadline.HasValue
                                ? deadline.Value.ToString("dd MMMM yyyy HH:mm")
                                : "48 hours after declaration");
                    });

                    box.Item().PaddingTop(6).Text(
                        "Use the Attribute reference number and Evidence PIN on the Valuation Portal to upload additional evidence. The PIN expires when the 48-hour evidence window closes.")
                        .FontSize(8);
                });
        }
        private static void AddHeader(TableDescriptor table, params string[] headers)
        {
            foreach (var header in headers)
            {
                table.Cell()
                    .Background("#D7ECEA")
                    .Border(0.5f)
                    .BorderColor("#BFD8D6")
                    .Padding(4)
                    .Text(header)
                    .Bold();
            }
        }


        private static bool HasAnyValue(object? row)
        {
            if (row == null) return false;

            return row.GetType()
                .GetProperties()
                .Any(p =>
                {
                    var value = p.GetValue(row);
                    return value != null && !string.IsNullOrWhiteSpace(value.ToString());
                });
        }
        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "NA";

            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            value = value.Replace("/", "_")
                         .Replace("\\", "_")
                         .Replace(":", "_")
                         .Replace("*", "_")
                         .Replace("?", "_")
                         .Replace("\"", "_")
                         .Replace("<", "_")
                         .Replace(">", "_")
                         .Replace("|", "_");

            while (value.Contains("  "))
                value = value.Replace("  ", " ");

            return value.Trim();
        }

        private static void AddDeclarationSection(ColumnDescriptor col, AttributeSubmissionViewModel model, AttrPropertyInfo propertyInfo)
        {
            AddSectionTitle(col, "Declaration and Signature");

            AddTwoColumnTable(col, new List<(string Label, string? Value)>
    {
        ("Declaration Accepted", model.Declaration.DeclarationAccepted ? "Yes" : "No"),
        ("Signature Name", model.Declaration.SignatureName),
        ("Declaration Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm")),
        ("Attribute Number", propertyInfo.Attr_No)
    });

            if (!string.IsNullOrWhiteSpace(model.Declaration.DeclarationText))
            {
                col.Item()
                    .Border(1)
                    .Padding(5)
                    .Text(model.Declaration.DeclarationText)
                    .FontSize(8);
            }

            if (!string.IsNullOrWhiteSpace(model.Declaration.SignaturePicture))
            {
                col.Item()
                    .PaddingTop(5)
                    .Text("Signature captured electronically.")
                    .Italic()
                    .FontSize(8);
            }
        }
        public async Task<(byte[] Pdf, string FileName)> GenerateAcknowledgementPdfAsync(
    AttributeSubmissionViewModel model,
    AttrPropertyInfo propertyInfo)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (propertyInfo == null)
                throw new ArgumentNullException(nameof(propertyInfo));

            var attrNo = propertyInfo.Attr_No ?? model.AttrNo ?? $"ATTR-GV23-{propertyInfo.Attr_ID}";
            var propertyDesc = model.PropertyDetails?.PropertyDesc ?? propertyInfo.Property_Desc ?? "Property";
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var safeAttrNo = MakeSafeFileName(attrNo);
            var safePropertyDesc = MakeSafeFileName(propertyDesc);

            var fileName = $"{safeAttrNo}_{safePropertyDesc}_{timestamp}_Acknowledgement.pdf";

            var tempFolder = Path.Combine(Path.GetTempPath(), "AIVS_Attribute_Acknowledgements");

            Directory.CreateDirectory(tempFolder);

            var tempPath = Path.Combine(
                tempFolder,
                $"{Guid.NewGuid():N}_{fileName}");

            try
            {
                GenerateAcknowledgementReplicaPdf(
                    model,
                    propertyInfo,
                    tempPath);

                var bytes = await File.ReadAllBytesAsync(tempPath);

                return (bytes, fileName);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // Do not fail the download because temp cleanup failed.
                }
            }
        }
        private void GenerateAcknowledgementReplicaPdf(
         AttributeSubmissionViewModel model,
         AttrPropertyInfo propertyInfo,
         string pdfPath)
        {
            var headerPath = Path.Combine(_env.WebRootPath, "Images/Obj_Header.PNG");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(22);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Column(header =>
                    {
                        AddCojHeaderImage(header, headerPath);

                        header.Item()
                            .PaddingTop(6)
                            .AlignCenter()
                            .Text("Property Attribute Submission Acknowledgement")
                            .Bold()
                            .FontSize(13);

                        header.Item()
                            .AlignCenter()
                            .Text(GetFormLabel(model.FormType))
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken2);

                        header.Item().PaddingTop(6).LineHorizontal(0.5f);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        AddAcknowledgementIntro(col, model, propertyInfo);

                        AddEvidenceAccessDetails(col, model, propertyInfo);

                        AddSubmittedPropertyDetails(col, model);

                        AddSubmittedValuationDetails(col, model);

                        AddSubmittedContactDetails(col, model);

                        AddSubmittedFormSpecificDetails(col, model);

                        AddSubmittedComments(col, model);

                        AddSubmittedEvidenceSummary(col, model);

                        AddSubmittedDeclaration(col, model);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated on ");
                        text.Span(DateTime.Now.ToString("dd MMMM yyyy HH:mm")).SemiBold();
                        text.Span(" | Attribute Ref: ");
                        text.Span(propertyInfo.Attr_No ?? model.AttrNo ?? "").SemiBold();
                    });
                });
            }).GeneratePdf(pdfPath);
        }

        private static string GetFormLabel(string? formType)
        {
            return formType switch
            {
                "Residential" => "Residential",
                "ResidentialST" => "Residential — Sectional Title",
                "BusinessCommercial" => "Non-Residential / Business and Commercial",
                "DRCMethod" => "DRC Method",
                _ => "Property Attribute Submission"
            };
        }



        private static void AddTwoColumnRow(TableDescriptor table, string label1, string? value1, string label2, string? value2)
        {
            table.Cell().Element(CellLabel).Text(label1);
            table.Cell().Element(CellValue).Text(value1 ?? "");
            table.Cell().Element(CellLabel).Text(label2);
            table.Cell().Element(CellValue).Text(value2 ?? "");
        }

        private static void AddFullRow(TableDescriptor table, string label, string? value)
        {
            table.Cell().Element(CellLabel).Text(label);
            table.Cell().ColumnSpan(3).Element(CellValue).Text(value ?? "");
        }

        private static IContainer CellLabel(IContainer container)
        {
            return container
                .Border(0.5f)
                .BorderColor("#BFD8D6")
                .Background("#F3FAF9")
                .Padding(4)
                .DefaultTextStyle(x => x.Bold());
        }

        private static IContainer CellValue(IContainer container)
        {
            return container
                .Border(0.5f)
                .BorderColor("#BFD8D6")
                .Padding(4);
        }

        private static void AddAckRow(TableDescriptor table, string label, string? value, string background)
        {
            table.Cell().Border(1).Background(background).Padding(7).Text(label).Bold();
            table.Cell().Border(1).Background(background).Padding(7).Text(value ?? "");
        }

        private static void AddAckSectionTitle(ColumnDescriptor col, string title, string background)
        {
            col.Item()
                .PaddingTop(18)
                .Background(background)
                .Border(1)
                .Padding(7)
                .AlignCenter()
                .Text(title)
                .FontColor(Colors.White)
                .Bold()
                .FontSize(11);
        }

        private static void AddHeaderCell(TableDescriptor table, string text)
        {
            table.Cell()
                .Border(1)
                .Background(Colors.Grey.Lighten3)
                .Padding(5)
                .AlignCenter()
                .Text(text)
                .Bold()
                .FontSize(8);
        }

        private static void AddBodyCell(TableDescriptor table, string? text)
        {
            table.Cell()
                .Border(1)
                .Padding(5)
                .Text(text ?? "")
                .FontSize(8);
        }
    }
}
