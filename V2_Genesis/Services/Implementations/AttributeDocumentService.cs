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

        public AttributeDocumentService(IOptions<AttributeStorageOptions> options)
        {
            _options = options.Value;
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

            GenerateAcknowledgementPdf(
                model,
                propertyInfo,
                acknowledgementPath,
                acknowledgementFileName);

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

            result.Files1 = await SaveEvidenceFileAsync(model.Files.Files1, evidenceFolder, safeAttrNo, "Evidence_1", result);
            result.Files2 = await SaveEvidenceFileAsync(model.Files.Files2, evidenceFolder, safeAttrNo, "Evidence_2", result);
            result.Files3 = await SaveEvidenceFileAsync(model.Files.Files3, evidenceFolder, safeAttrNo, "Evidence_3", result);
            result.Files4 = await SaveEvidenceFileAsync(model.Files.Files4, evidenceFolder, safeAttrNo, "Evidence_4", result);
            result.Files5 = await SaveEvidenceFileAsync(model.Files.Files5, evidenceFolder, safeAttrNo, "Evidence_5", result);
            result.Files6 = await SaveEvidenceFileAsync(model.Files.Files6, evidenceFolder, safeAttrNo, "Evidence_6", result);
            result.Files7 = await SaveEvidenceFileAsync(model.Files.Files7, evidenceFolder, safeAttrNo, "Evidence_7", result);
            result.Files8 = await SaveEvidenceFileAsync(model.Files.Files8, evidenceFolder, safeAttrNo, "Evidence_8", result);
            result.Files9 = await SaveEvidenceFileAsync(model.Files.Files9, evidenceFolder, safeAttrNo, "Evidence_9", result);
            result.Files10 = await SaveEvidenceFileAsync(model.Files.Files10, evidenceFolder, safeAttrNo, "Evidence_10", result);

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

        private static void GeneratePdf(
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

                    page.Header().Column(header =>
                    {
                        header.Item().Text("City of Johannesburg")
                            .Bold()
                            .FontSize(15);

                        header.Item().Text("Property Information Submission")
                            .Bold()
                            .FontSize(12);

                        header.Item().Text($"{formName} Form")
                            .FontSize(11);

                        header.Item().PaddingTop(5).LineHorizontal(1);
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
                    ("Last Name", c.LastName),
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
                .PaddingTop(8)
                .PaddingBottom(3)
                .Background(Colors.Grey.Lighten3)
                .Padding(5)
                .Text(title)
                .Bold()
                .FontSize(10);
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
    
    private static void GenerateAcknowledgementPdf(
    AttributeSubmissionViewModel model,
    AttrPropertyInfo propertyInfo,
    string pdfPath,
    string acknowledgementFileName)
        {
            var formBlue = "#1f6f78";
            var lightBlue = "#eaf5f6";

            var uploadedFiles = new List<string?>()
    {
        model.Files.Files1?.FileName,
        model.Files.Files2?.FileName,
        model.Files.Files3?.FileName,
        model.Files.Files4?.FileName,
        model.Files.Files5?.FileName,
        model.Files.Files6?.FileName,
        model.Files.Files7?.FileName,
        model.Files.Files8?.FileName,
        model.Files.Files9?.FileName,
        model.Files.Files10?.FileName
    }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();

            var evidencePin = model.GeneratedEvidencePin ?? "";
            var evidenceDeadline = model.GeneratedEvidenceDeadline;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.RelativeItem(1).Column(left =>
                            {
                                left.Item().Text("Joburg").Bold().FontSize(26);
                                left.Item().Text("a world class African city").FontSize(7);
                            });

                            row.RelativeItem(3).Column(mid =>
                            {
                                mid.Item().Text("City of Johannesburg").Bold().FontSize(11);
                                mid.Item().Text("Property Branch: Valuation Services").FontSize(9);
                                mid.Item().PaddingTop(8).Text("Valuation Administrations").FontSize(8);
                                mid.Item().Text("1st Floor, East Wing").FontSize(8);
                                mid.Item().Text("66 Jorissen Street").FontSize(8);
                                mid.Item().Text("Braamfontein").FontSize(8);
                            });

                            row.RelativeItem(2).Column(right =>
                            {
                                right.Item().AlignRight().Text("Email: propertydata@joburg.org.za").Bold().FontSize(8);
                                right.Item().AlignRight().Text("www.joburg.org.za").FontSize(8);
                            });
                        });

                        header.Item().PaddingTop(8).LineHorizontal(1);
                    });

                    page.Content().PaddingTop(12).Column(col =>
                    {
                        col.Item()
                            .Background(formBlue)
                            .Border(1)
                            .Padding(8)
                            .AlignCenter()
                            .Text("ATTRIBUTE SUBMISSION ACKNOWLEDGEMENT")
                            .FontColor(Colors.White)
                            .Bold()
                            .FontSize(16);

                        col.Item().PaddingTop(15).Text(text =>
                        {
                            text.Span("Your attribute submission has been successfully lodged. ").Bold();
                            text.Span("Thank you for submitting your property information. ");
                            text.Span("Please note you have 48 hours to upload any outstanding evidence.")
                                .FontColor(Colors.Red.Medium)
                                .Italic();
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            AddAckRow(table, "Property Description:", propertyInfo.Property_Desc ?? model.PropertyDetails.PropertyDesc, lightBlue);
                            AddAckRow(table, "ATTRIBUTE NUMBER:", propertyInfo.Attr_No, lightBlue);
                            AddAckRow(table, "PIN:", evidencePin, lightBlue);
                            AddAckRow(table, "Date:", DateTime.Now.ToString("yyyy-MM-dd HH:mm"), lightBlue);
                            AddAckRow(table, "Evidence Deadline:", evidenceDeadline?.ToString("yyyy-MM-dd HH:mm"), lightBlue);
                        });

                        AddAckSectionTitle(col, "PROPERTY DETAILS AS SUBMITTED", formBlue);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.4f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.6f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.4f);
                            });

                            AddHeaderCell(table, "Property Description");
                            AddHeaderCell(table, "Property Category");
                            AddHeaderCell(table, "Physical Address");
                            AddHeaderCell(table, "Market Value");
                            AddHeaderCell(table, "Extent");
                            AddHeaderCell(table, "Name of Owner");

                            AddBodyCell(table, propertyInfo.Property_Desc ?? model.PropertyDetails.PropertyDesc);
                            AddBodyCell(table, model.ValuationDetails.ValuationCategoryOnRoll);
                            AddBodyCell(table, model.PropertyDetails.Address);
                            AddBodyCell(table, model.Calculations.Tla?.ToString());
                            AddBodyCell(table, model.PropertyDetails.Extent);
                            AddBodyCell(table, model.ValuationDetails.OwnersFinancials ?? model.ValuationDetails.OwnersTitleDeeds);
                        });

                        AddAckSectionTitle(col, "DECLARATION", formBlue);

                        col.Item().Border(1).Padding(8).Column(dec =>
                        {
                            dec.Item().Text(model.Declaration.DeclarationText ??
                                "I declare that the information submitted is true and correct to the best of my knowledge. I understand that this information is subject to review.")
                                .FontSize(9);

                            dec.Item().PaddingTop(6).Text($"Signature Name: {model.Declaration.SignatureName}").Bold();
                            dec.Item().Text($"Declaration Accepted: {(model.Declaration.DeclarationAccepted ? "Yes" : "No")}");
                        });

                        col.Item().PaddingTop(18).Text("UPLOADED DOCUMENTS").Bold().FontSize(12);
                        col.Item().Text($"You have uploaded {uploadedFiles.Count} document(s)");

                        if (uploadedFiles.Any())
                        {
                            col.Item().PaddingTop(8).Column(docs =>
                            {
                                foreach (var file in uploadedFiles)
                                {
                                    docs.Item().PaddingLeft(8).Text($"- {file}");
                                }
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(8).Text("No evidence documents uploaded.");
                        }

                        col.Item().PaddingTop(20).Text("For any inquiries regarding your attribute submission, please contact:").FontSize(9);
                        col.Item().Text("Email: propertydata@joburg.org.za").FontSize(9);
                        col.Item().Text("Tel: 011 084 9823").FontSize(9);
                        col.Item().Text("Website: www.joburg.org.za").FontSize(9);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated acknowledgement: ");
                        text.Span(acknowledgementFileName).SemiBold();
                        text.Span(" | Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf(pdfPath);
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