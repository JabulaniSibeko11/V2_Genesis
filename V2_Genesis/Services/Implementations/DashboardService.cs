using Dapper;
using GenesisV2.Services.PropertySearch;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;
using V2_Genesis.Data;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.Results.Atrributes;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services.Interfaces;

public class DashboardService : IDashboardService
{
    private readonly IConfiguration _config;
    private readonly AttributesDbContext _attrDb;
    private readonly ILogger<DashboardService> _logger;

    private const string SP_LINKED = "DashboardLinked";
    private const string SP_OBJECTED = "DashboardObjection";
    private const string SP_APPEALS = "DashboardAppeal";
    private const string SP_NOTIFICATIONS = "DashboardNotification";
    private const string SP_ATTR_LINKED = "Attr_DashboardLinked";
    private readonly string SP_QUERY_LINKED = "DashboardLinkedQ";
    private readonly string SP_QUERY_OBJECTED = "DashboardObjectionQ";
    private readonly string SP_QUERY_APPEAL = "DashboardAppeal";
    private readonly string SP_QUERY_NOTIFICATION = "DashboardNotification";
    public DashboardService(
        IConfiguration config,
        AttributesDbContext attrDb,
        ILogger<DashboardService> logger)
    {
        _config = config;
        _attrDb = attrDb;
        _logger = logger;
    }

    // ── Roll data — unchanged ─────────────────────────────────────────
    public async Task<RollData> GetRollDataAsync(
         string rollSource,
         string userId,
         string userEmail)
    {
        var rollData =
            new RollData();

        if (string.IsNullOrWhiteSpace(rollSource))
            return rollData;

        if (string.IsNullOrWhiteSpace(userId))
            return rollData;

        if (!RollSearchRegistry.Configs.TryGetValue(
                rollSource,
                out var config))
        {
            _logger.LogWarning(
                "Dashboard requested for unknown roll source {RollSource}",
                rollSource);

            return rollData;
        }

        var connectionString =
            _config.GetConnectionString(
                config.ConnectionKey)
            ?? _config.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError(
                "No dashboard connection string was found for roll {RollSource} using key {ConnectionKey}",
                rollSource,
                config.ConnectionKey);

            return rollData;
        }

        var isQuery =
            config.IsQuery ||
            rollSource.Equals(
                "Query",
                StringComparison.OrdinalIgnoreCase);

        var linkedProcedure =
            isQuery
                ? SP_QUERY_LINKED
                : SP_LINKED;

        var objectedProcedure =
            isQuery
                ? SP_QUERY_OBJECTED
                : SP_OBJECTED;

        await using var conn =
            new SqlConnection(connectionString);

        await using var rollDb =
            new DashboardReadDbContext(connectionString);

        await conn.OpenAsync();

        // ─────────────────────────────────────────────────────────
        // Linked properties
        // ─────────────────────────────────────────────────────────

        try
        {
            var linked =
                await conn.QueryAsync<LinkedPropertyResult>(
                    linkedProcedure,
                    new
                    {
                        userName = userId
                    },
                    commandType:
                        CommandType.StoredProcedure,
                    commandTimeout: 60);

            var linkedProperties =
                linked.ToList();

            NormaliseLinkedPropertyKeys(
                linkedProperties);

            if (isQuery)
            {
                NormaliseQueryLinkedProperties(
                    linkedProperties);
            }

            rollData.LinkedProperties =
                linkedProperties;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{StoredProcedure} failed for roll {RollSource} and user {UserId}",
                linkedProcedure,
                rollSource,
                userId);
        }

        // ─────────────────────────────────────────────────────────
        // Submitted Queries / Reviews / Objections
        // ─────────────────────────────────────────────────────────

        try
        {
            var objected =
                await conn.QueryAsync<ObjectedPropertyResult>(
                    objectedProcedure,
                    new
                    {
                        userName = userId
                    },
                    commandType:
                        CommandType.StoredProcedure,
                    commandTimeout: 60);

            var objectedProperties =
                objected.ToList();

            if (isQuery)
            {
                NormaliseSubmittedQueryProperties(
                    objectedProperties);
            }
            else
            {
                try
                {
                    await PopulateAppealDecisionTypesAsync(
                        rollDb,
                        objectedProperties);
                }
                catch (Exception ex)
                {
                    // Keep the dashboard available if an older roll database
                    // does not yet contain the Appeal_Decision table.
                    _logger.LogWarning(
                        ex,
                        "Could not resolve appeal decision labels for roll {RollSource}",
                        rollSource);
                }
            }

            rollData.ObjectedProperties =
                objectedProperties;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{StoredProcedure} failed for roll {RollSource} and user {UserId}",
                objectedProcedure,
                rollSource,
                userId);
        }

        // ─────────────────────────────────────────────────────────
        // Appeals
        // ─────────────────────────────────────────────────────────

        try
        {
            var appeals =
                (await conn.QueryAsync<AppealResult>(
                    SP_APPEALS,
                    new
                    {
                        userName = userId
                    },
                    commandType:
                        CommandType.StoredProcedure,
                    commandTimeout: 60))
                .ToList();

            // DashboardAppeal does not currently return the Appeal evidence
            // submission date. Hydrate the date and 48-hour window directly
            // from Obj_Property_Info_Appeal for the Appeals returned by the SP.
            var appealNumbers = appeals
                .Select(x => x.Appeal_No?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (appealNumbers.Length > 0)
            {
                var appealRows = await rollDb.Appeals
                    .AsNoTracking()
                    .Where(x => appealNumbers.Contains(
                        (x.AppealNo ?? string.Empty).Trim()))
                    .Select(x => new
                    {
                        x.AppealNo,
                        x.AppealStartDateTime,
                        x.AppealStatus
                    })
                    .ToListAsync();

                var now = DateTime.Now;
                var windowRows = appealRows.Select(x =>
                {
                    var expiresAt = x.AppealStartDateTime?.AddHours(48);
                    var status = x.AppealStatus?.Trim();

                    return new AppealEvidenceWindowRow
                    {
                        Appeal_No = x.AppealNo?.Trim(),
                        Appeal_Start_DateTime = x.AppealStartDateTime,
                        Evidence_Expires_At = expiresAt,
                        Evidence_Window_Open =
                            expiresAt.HasValue &&
                            now <= expiresAt.Value &&
                            (string.Equals(status, "App-Lodging", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(status, "App-Unallocated", StringComparison.OrdinalIgnoreCase))
                    };
                });

                var windowByAppeal = windowRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.Appeal_No))
                    .GroupBy(
                        x => x.Appeal_No!.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => group.First(),
                        StringComparer.OrdinalIgnoreCase);

                foreach (var appeal in appeals)
                {
                    var appealNo = appeal.Appeal_No?.Trim();

                    if (string.IsNullOrWhiteSpace(appealNo)
                        || !windowByAppeal.TryGetValue(
                            appealNo,
                            out var window))
                    {
                        continue;
                    }

                    appeal.Appeal_Start_DateTime =
                        window.Appeal_Start_DateTime;

                    appeal.Evidence_Expires_At =
                        window.Evidence_Expires_At;

                    appeal.Evidence_Window_Open =
                        window.Evidence_Window_Open;
                }
            }

            rollData.Appeals = appeals;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{StoredProcedure} failed for roll {RollSource} and user {UserId}",
                SP_APPEALS,
                rollSource,
                userId);
        }

        // ─────────────────────────────────────────────────────────
        // Notifications
        // ─────────────────────────────────────────────────────────

        try
        {
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                var notifications =
                    await conn.QueryAsync<NotificationResult>(
                        SP_NOTIFICATIONS,
                        new
                        {
                            userEmail
                        },
                        commandType:
                            CommandType.StoredProcedure,
                        commandTimeout: 60);

                rollData.Notifications =
                    notifications.ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{StoredProcedure} failed for roll {RollSource} and email {UserEmail}",
                SP_NOTIFICATIONS,
                rollSource,
                userEmail);
        }

        return rollData;
    }

    private static async Task PopulateAppealDecisionTypesAsync(
        DashboardReadDbContext db,
        List<ObjectedPropertyResult> properties)
    {
        var finalisedAppeals = properties
            .Where(property =>
                property.Sub_typ == 1 &&
                string.Equals(
                    property.objection_Status?.Trim(),
                    "App-Finalized",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        var references = finalisedAppeals
            .SelectMany(property => new[]
            {
                property.Appeal_No?.Trim(),
                property.Objection_No?.Trim()
            })
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Select(reference => reference!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (references.Length == 0)
            return;

        var decisions = await db.AppealDecisions
            .AsNoTracking()
            .Where(x =>
                references.Contains((x.AppealNo ?? string.Empty).Trim()) ||
                references.Contains((x.ObjectionNo ?? string.Empty).Trim()))
            .Select(x => new AppealDecisionTypeRow
            {
                AppealNo = x.AppealNo,
                ObjectionNo = x.ObjectionNo,
                DecisionUserId = x.DecisionUserId
            })
            .ToListAsync();

        var decisionByReference = new Dictionary<string, string?>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var decision in decisions)
        {
            if (!string.IsNullOrWhiteSpace(decision.AppealNo))
                decisionByReference[decision.AppealNo.Trim()] = decision.DecisionUserId;

            if (!string.IsNullOrWhiteSpace(decision.ObjectionNo))
                decisionByReference[decision.ObjectionNo.Trim()] = decision.DecisionUserId;
        }

        foreach (var appeal in finalisedAppeals)
        {
            var reference = !string.IsNullOrWhiteSpace(appeal.Appeal_No)
                ? appeal.Appeal_No.Trim()
                : appeal.Objection_No?.Trim();

            if (!string.IsNullOrWhiteSpace(reference) &&
                decisionByReference.TryGetValue(reference, out var decisionUserId))
            {
                appeal.AppealDecisionUserId = decisionUserId;
            }
        }
    }

    private sealed class AppealDecisionTypeRow
    {
        public string? AppealNo { get; set; }
        public string? ObjectionNo { get; set; }
        public string? DecisionUserId { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // Section 78 Query dashboard safeguards
    // ─────────────────────────────────────────────────────────────

    private static void NormaliseLinkedPropertyKeys(
        List<LinkedPropertyResult> properties)
    {
        foreach (var property in properties)
        {
            /*
             * DashboardLinked and DashboardLinkedQ expose the property key
             * through Id. Copy it into IDProperty when the procedure does not
             * return an IDProperty column so downstream actions can use one
             * consistent property-key field.
             */
            if (string.IsNullOrWhiteSpace(property.IDProperty) &&
                property.Id > 0)
            {
                property.IDProperty = property.Id.ToString();
            }
        }
    }

    private static void NormaliseQueryLinkedProperties(
        List<LinkedPropertyResult> properties)
    {
        foreach (var property in properties)
        {
            property.Review_Status =
                NormaliseReviewStatus(
                    property.Review_Status,
                    property.Review_Close_Date);

            /*
             * The updated DashboardLinkedQ procedure should return
             * AvailableAction. This fallback protects the UI if an
             * older row or partial result does not contain it.
             */
            if (string.IsNullOrWhiteSpace(
                    property.AvailableAction))
            {
                property.AvailableAction =
                    ResolveAvailableAction(
                        property.Review_Status,
                        property.HasCompletedQuery);
            }
        }
    }

    private static void NormaliseSubmittedQueryProperties(
        List<ObjectedPropertyResult> properties)
    {
        foreach (var property in properties)
        {
            property.Review_Status =
                NormaliseReviewStatus(
                    property.Review_Status,
                    property.Review_Close_Date);

            property.CanLodgeReview =
                string.Equals(
                    property.Review_Status,
                    "Open",
                    StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(
                    property.ReviewActionText))
            {
                property.ReviewActionText =
                    property.CanLodgeReview
                        ? "Lodge Review"
                        : "Review Closed";
            }
        }
    }

    private static string NormaliseReviewStatus(
        string? databaseStatus,
        DateTime? reviewCloseDate)
    {
        /*
         * The persisted Review_Status is the primary value.
         */
        if (string.Equals(
                databaseStatus,
                "Closed",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Closed";
        }

        /*
         * A past closing date must never remain open in the UI,
         * even if the SQL Agent job has not run yet.
         */
        if (reviewCloseDate.HasValue &&
            reviewCloseDate.Value.Date < DateTime.Today)
        {
            return "Closed";
        }

        /*
         * NULL closing date means the initial Query process is
         * still available.
         */
        return "Open";
    }

    private static string ResolveAvailableAction(
        string? reviewStatus,
        bool hasCompletedQuery)
    {
        if (string.Equals(
                reviewStatus,
                "Closed",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Closed";
        }

        return hasCompletedQuery
            ? "Review"
            : "Query";
    }

    // ── Attributes linked properties ──────────────────────────────────
    // Calls Attr_DashboardLinked SP on GenesisAttributes DB.
    // Returns full property details + FormType + HasSubmission.
    public async Task<List<AttrLinkedPropertyResult>> GetAttributesLinkedAsync(string userId)
    {
        var results = new List<AttrLinkedPropertyResult>();

        try
        {
            var connString = _config.GetConnectionString("AttributesConnection");

            if (string.IsNullOrWhiteSpace(connString))
            {
                Console.Error.WriteLine("[Dashboard] AttributesConnection is missing.");
                return results;
            }

            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();

            // 1. Get only the linked properties for this user.
            var linkedRows = await _attrDb.LinkedProperties
                .AsNoTracking()
                .Where(x => x.UserID == userId)
                .OrderByDescending(x => x.ID)
                .Select(x => new AttrLinkedPropertyResult
                {
                    Id = x.ID,
                    IDProperty = x.IDProperty,
                    PropertyFrom = x.PropertyFrom ?? "Attributes"
                })
                .ToListAsync();

            // Load all active submissions once to avoid one COUNT query per row.
            var submittedAttributes = await _attrDb.AttrPropertyInfo
                    .AsNoTracking()
                    .Where(x =>
                        x.SubmittedByUserId == userId &&
                        x.IsActive)
                    .OrderByDescending(x => x.Attr_ID)
                    .Select(x => new
                    {
                        x.Unit_key,
                        x.Attr_No
                    })
                    .ToListAsync();

            var submissionByUnitKey = submittedAttributes
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Unit_key) &&
                    !string.IsNullOrWhiteSpace(x.Attr_No))
                .GroupBy(
                    x => x.Unit_key!.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Attr_No!.Trim(),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var linked in linkedRows)
            {
                // 2. Pull the full property detail using the same backend SP used by the Attributes form
                var detail = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "Attr_GetPropertyForCheck",
                    new { UnitKey = linked.IDProperty },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60);

                if (detail == null)
                {
                    results.Add(new AttrLinkedPropertyResult
                    {
                        Id = linked.Id,
                        IDProperty = linked.IDProperty,
                        PropertyFrom = linked.PropertyFrom,
                        PropertyDesc = linked.IDProperty,
                        FormType = "Residential",
                        HasSubmission = submissionByUnitKey.TryGetValue(
                            linked.IDProperty.Trim(),
                            out var submissionR),
                        SubmissionRef = submissionR
                    });

                    continue;
                }

                var catDesc = GetValue(detail, "CatDesc");
                var schemeName = GetValue(detail, "SchemeName");
                var unitNoText = GetValue(detail, "UnitNo");

                var item = new AttrLinkedPropertyResult
                {
                    Id = linked.Id,
                    IDProperty = linked.IDProperty,
                    PropertyFrom = linked.PropertyFrom,

                    PropertyDesc = BuildAttributePropertyDescription(detail),
                    CatDesc = catDesc,
                    TownNameDesc = GetValue(detail, "TownNameDesc"),
                    MarketValue = FormatMoney(GetValue(detail, "MarketValue")),
                    RateableArea = GetValue(detail, "RateableArea"),
                    LisStreetAddress = GetValue(detail, "LisStreetAddress"),
                    SchemeName = schemeName,

                    FormType = ResolveAttributeFormType(catDesc, schemeName, unitNoText),
                    HasSubmission = submissionByUnitKey.TryGetValue(
                        linked.IDProperty.Trim(),
                        out var submissionRef),
                    SubmissionRef = submissionRef
                };

                var erfText = GetValue(detail, "Erf");

                if (int.TryParse(erfText, out int erf))
                    item.Erf = erf;

                if (int.TryParse(unitNoText, out int unitNo))
                    item.UnitNo = unitNo;


                results.Add(item);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Dashboard] GetAttributesLinkedAsync failed for user {userId}: {ex.Message}");
        }

        return results;
    }

    private static string GetValue(dynamic row, string name)
    {
        if (row == null) return "";

        var dict = row as IDictionary<string, object>;

        if (dict != null && dict.TryGetValue(name, out var value))
            return value?.ToString() ?? "";

        return "";
    }

    private static string BuildAttributePropertyDescription(dynamic detail)
    {
        var propertyDesc = GetValue(detail, "PropertyDesc");

        if (!string.IsNullOrWhiteSpace(propertyDesc))
            return propertyDesc;

        var erf = GetValue(detail, "Erf");
        var ptn = GetValue(detail, "Ptn");
        var re = GetValue(detail, "Re");
        var town = GetValue(detail, "TownNameDesc");
        var scheme = GetValue(detail, "SchemeName");
        var unitNo = GetValue(detail, "UnitNo");

        // Sectional title:
        // Scheme UNIT 28, MULBARTON GARDENS, BEVERLEY EXT.100
        if (!string.IsNullOrWhiteSpace(scheme) ||
            (!string.IsNullOrWhiteSpace(unitNo) && unitNo != "0"))
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(unitNo) && unitNo != "0")
                parts.Add($"UNIT {unitNo}");

            if (!string.IsNullOrWhiteSpace(scheme))
                parts.Add(scheme);

            if (!string.IsNullOrWhiteSpace(town))
                parts.Add(town);

            return "Scheme " + string.Join(", ", parts);
        }

        // Portion:
        // PORTION 42 RUIMSIG 265-IQ
        if (!string.IsNullOrWhiteSpace(ptn) &&
            ptn != "0" &&
            !string.IsNullOrWhiteSpace(town))
        {
            if (!string.IsNullOrWhiteSpace(re) &&
                re.Equals("RE", StringComparison.OrdinalIgnoreCase))
            {
                return $"RE PORTION {ptn} {town}";
            }

            return $"PORTION {ptn} {town}";
        }

        // Full title:
        // Full Title ERF 334 LINBRO PARK EXT.181
        if (!string.IsNullOrWhiteSpace(erf) &&
            erf != "0" &&
            !string.IsNullOrWhiteSpace(town))
        {
            return $"Full Title ERF {erf} {town}";
        }

        if (!string.IsNullOrWhiteSpace(town))
            return town;

        return "";
    }

    private static string ResolveAttributeFormType(
        string? catDesc,
        string? schemeName,
        string? unitNo)
    {
        var cat = (catDesc ?? "").Trim().ToLower();

        // Sectional Title Residential
        if (!string.IsNullOrWhiteSpace(schemeName) ||
            (!string.IsNullOrWhiteSpace(unitNo) && unitNo != "0"))
        {
            return "ResidentialST";
        }

        // DRC Method
        if (cat.Contains("public service") ||
            cat.Contains("municipal") ||
            cat.Contains("religious") ||
            cat.Contains("mining") ||
            cat.Contains("agricultural") ||
            cat.Contains("vacant") ||
            cat.Contains("drc"))
        {
            return "DRCMethod";
        }

        // Non-Residential / Business
        if (cat.Contains("business") ||
            cat.Contains("commercial") ||
            cat.Contains("industrial") ||
            cat.Contains("retail") ||
            cat.Contains("office"))
        {
            return "BusinessCommercial";
        }

        return "Residential";
    }

    private static string FormatMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var clean = value
            .Replace("R", "", StringComparison.OrdinalIgnoreCase)
            .Replace(",", "")
            .Replace(" ", "")
            .Trim();

        if (!decimal.TryParse(clean, out var amount))
            return value;

        return "R " + amount.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture)
            .Replace(",", " ");
    }
    // ── FormType — 4 types based on property category ─────────────────
    private static string ResolveFormType(string? catDesc)
    {
        if (string.IsNullOrWhiteSpace(catDesc)) return "Residential";

        var cat = catDesc.Trim().ToLower();

        // Sectional title — form expects "ResidentialST"
        if (cat.Contains("sectional") ||
            cat.Contains("residential-st") ||
            cat.Contains("st ") || cat.Contains("unit"))
            return "ResidentialST";

        // Business / commercial — form expects "BusinessCommercial"
        if (cat.Contains("business") ||
            cat.Contains("commercial") ||
            cat.Contains("industrial") ||
            cat.Contains("retail") ||
            cat.Contains("office"))
            return "BusinessCommercial";

        // DRC — form expects "DRCMethod"
        if (cat.Contains("drc") ||
            cat.Contains("public service") ||
            cat.Contains("institutional"))
            return "DRCMethod";

        // Default
        return "Residential";
    }




    private sealed class DashboardReadDbContext : DbContext
    {
        private readonly string _connectionString;

        public DashboardReadDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbSet<DashboardAppealEntity> Appeals =>
            Set<DashboardAppealEntity>();

        public DbSet<DashboardAppealDecisionEntity> AppealDecisions =>
            Set<DashboardAppealDecisionEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                _connectionString,
                sqlServer => sqlServer.CommandTimeout(60));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DashboardAppealEntity>(entity =>
            {
                entity.HasKey(x => x.AppealId);
                entity.ToTable("Obj_Property_Info_Appeal", "dbo");
                entity.Property(x => x.AppealId).HasColumnName("Appeal_ID");
                entity.Property(x => x.AppealNo).HasColumnName("Appeal_No");
                entity.Property(x => x.AppealStartDateTime)
                    .HasColumnName("Appeal_Start_DateTime");
                entity.Property(x => x.AppealStatus).HasColumnName("Appeal_Status");
            });

            modelBuilder.Entity<DashboardAppealDecisionEntity>(entity =>
            {
                entity.HasNoKey();
                entity.ToTable("Appeal_Decision", "dbo");
                entity.Property(x => x.AppealNo).HasColumnName("Appeal_No");
                entity.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
                entity.Property(x => x.DecisionUserId).HasColumnName("A_UserID");
            });
        }
    }

    private sealed class DashboardAppealEntity
    {
        public long AppealId { get; set; }
        public string? AppealNo { get; set; }
        public DateTime? AppealStartDateTime { get; set; }
        public string? AppealStatus { get; set; }
    }

    private sealed class DashboardAppealDecisionEntity
    {
        public string? AppealNo { get; set; }
        public string? ObjectionNo { get; set; }
        public string? DecisionUserId { get; set; }
    }

    private sealed class AppealEvidenceWindowRow
    {
        public string? Appeal_No { get; set; }
        public DateTime? Appeal_Start_DateTime { get; set; }
        public DateTime? Evidence_Expires_At { get; set; }
        public bool Evidence_Window_Open { get; set; }
    }
}
