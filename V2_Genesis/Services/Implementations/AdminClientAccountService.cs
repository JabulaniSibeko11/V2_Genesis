using Microsoft.EntityFrameworkCore;
using System.Globalization;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.ViewModels.Admin;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services.Attributes;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public sealed class AdminClientAccountService : IAdminClientAccountService
{
    private readonly ApplicationDbContext _db;
    private readonly IDashboardService _dashboardService;
    private readonly IAttributesDashboardService _attributesService;
    private readonly IRebatesService _rebatesService;
    private readonly ILogger<AdminClientAccountService> _logger;

    public AdminClientAccountService(
        ApplicationDbContext db,
        IDashboardService dashboardService,
        IAttributesDashboardService attributesService,
        IRebatesService rebatesService,
        ILogger<AdminClientAccountService> logger)
    {
        _db = db;
        _dashboardService = dashboardService;
        _attributesService = attributesService;
        _rebatesService = rebatesService;
        _logger = logger;
    }

    public async Task<AdminClientAccountViewModel?> GetClientAccountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var cleanUserId = userId.Trim();

        var user = await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == cleanUserId)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.PhoneNumber,
                x.FirstName,
                x.LastName,
                x.CompanyName,
                x.CompanyRegistration,
                x.CreationDate,
                x.EmailConfirmed
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        var isCompany =
            !string.IsNullOrWhiteSpace(user.CompanyName)
            || !string.IsNullOrWhiteSpace(user.CompanyRegistration);

        var displayName = isCompany
            ? user.CompanyName?.Trim()
            : string.Join(
                " ",
                new[] { user.FirstName, user.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim()));

        var model = new AdminClientAccountViewModel
        {
            UserId = user.Id,
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? user.Email ?? "Client"
                : displayName,
            AccountType = isCompany ? "Company" : "Individual",
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            AccountCreatedAt = user.CreationDate,
            EmailConfirmed = user.EmailConfirmed
        };

        var rolls = await _db.GvList
            .AsNoTracking()
            .Where(x => x.Source != "Objection_Supp5")
            .OrderBy(x => x.ID)
            .ToListAsync(cancellationToken);

        var rollTasks = rolls.ToDictionary(
            roll => roll.Source,
            roll => _dashboardService.GetRollDataAsync(
                roll.Source,
                cleanUserId,
                user.Email ?? string.Empty));

        await Task.WhenAll(rollTasks.Values);

        var propertyMap =
            new Dictionary<string, AdminClientPropertyViewModel>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var roll in rolls)
        {
            var data = await rollTasks[roll.Source];

            foreach (var linked in data.LinkedProperties)
            {
                var property = GetOrCreateProperty(
                    propertyMap,
                    roll,
                    linked.PropertyDesc,
                    linked.TownNameDesc,
                    linked.CatDesc,
                    linked.MarketValue,
                    linked.PremiseId,
                    linked.UnitKey,
                    linked.ValuationKey,
                    linked.PropertyFrom);

                property.IsLinked = true;
            }

            foreach (var objection in data.ObjectedProperties)
            {
                var submissionType = roll.IsQuery
                    ? objection.Sub_typ == 1
                        ? "Review"
                        : "Query"
                    : "Objection";

                var referenceNumber =
                    !string.IsNullOrWhiteSpace(objection.Query_No)
                        ? objection.Query_No!
                        : objection.Objection_No ?? string.Empty;

                var property = GetOrCreateProperty(
                    propertyMap,
                    roll,
                    objection.Property_Desc,
                    objection.Town_Name,
                    objection.Old_Category,
                    objection.Old_Market_Value,
                    premiseId: null,
                    objection.Unit_key,
                    objection.Valuation_Key,
                    objection.PropertyFrom);

                var submission = new AdminClientSubmissionViewModel
                {
                    SubmissionType = submissionType,
                    ReferenceNumber = referenceNumber,
                    Status = objection.objection_Status ?? string.Empty,
                    RollSource = roll.Source,
                    RollName = roll.Name,
                    PropertyKey = property.PropertyKey,
                    PropertyDescription = property.PropertyDescription,
                    Town = property.Town,
                    Category = property.Category,
                    MarketValue = property.MarketValue,
                    UnitKey = property.UnitKey,
                    ValuationKey = property.ValuationKey
                };

                AddSubmission(model, property, submission);
            }

            foreach (var appeal in data.Appeals)
            {
                var property = GetOrCreateProperty(
                    propertyMap,
                    roll,
                    appeal.A_Property_Desc,
                    appeal.Town_Name,
                    appeal.Old_Category,
                    appeal.Old_Market_Value,
                    premiseId: null,
                    appeal.A_Unit_key,
                    appeal.A_Valuation_Key,
                    propertyFrom: null);

                var submission = new AdminClientSubmissionViewModel
                {
                    SubmissionType = "Appeal",
                    ReferenceNumber = appeal.Appeal_No ?? string.Empty,
                    Status = appeal.Appeal_Status ?? string.Empty,
                    RollSource = roll.Source,
                    RollName = roll.Name,
                    PropertyKey = property.PropertyKey,
                    PropertyDescription = property.PropertyDescription,
                    Town = property.Town,
                    Category = property.Category,
                    MarketValue = property.MarketValue,
                    UnitKey = property.UnitKey,
                    ValuationKey = property.ValuationKey
                };

                AddSubmission(model, property, submission);
            }
        }

        try
        {
            var attributeData =
                await _attributesService.GetDashboardDataAsync(cleanUserId);

            var appointmentsByAttributeId =
                attributeData.Appointments
                    .GroupBy(x => x.AttrId)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .OrderByDescending(x =>
                                x.AppointmentDate
                                ?? x.ConfirmedDateTime
                                ?? x.CreatedDate)
                            .First());

            foreach (var linked in attributeData.LinkedProperties)
            {
                var property = GetOrCreateProperty(
                    propertyMap,
                    roll: null,
                    linked.PropertyDesc,
                    linked.TownNameDesc,
                    linked.CatDesc,
                    linked.MarketValue,
                    premiseId: null,
                    linked.UnitKey,
                    linked.ValuationKey,
                    linked.PropertyFrom,
                    rollSourceOverride: "Attributes",
                    rollNameOverride: "Property Attributes");

                property.IsLinked = true;
            }

            foreach (var attribute in attributeData.Submissions)
            {
                var property = GetOrCreateProperty(
                    propertyMap,
                    roll: null,
                    attribute.PropertyDesc,
                    town: null,
                    category: null,
                    marketValue: null,
                    premiseId: null,
                    unitKey: null,
                    valuationKey: null,
                    propertyFrom: null,
                    rollSourceOverride: "Attributes",
                    rollNameOverride: "Property Attributes");

                appointmentsByAttributeId.TryGetValue(
                    attribute.Id,
                    out var appointment);

                var submission = new AdminClientSubmissionViewModel
                {
                    SubmissionType = "Attribute",
                    ReferenceNumber = attribute.SubmissionRef ?? string.Empty,
                    Status = attribute.Status ?? string.Empty,
                    RollSource = "Attributes",
                    RollName = attribute.FormType ?? "Property Attributes",
                    PropertyKey = property.PropertyKey,
                    PropertyDescription = property.PropertyDescription,
                    Town = property.Town,
                    Category = property.Category,
                    MarketValue = property.MarketValue,
                    UnitKey = property.UnitKey,
                    ValuationKey = property.ValuationKey,
                    SubmittedAt = attribute.SubmittedAt,
                    InspectionRequestId = appointment?.Id,
                    InspectionAppointmentRef =
                        appointment?.AppointmentRef ?? string.Empty,
                    InspectionStatus =
                        appointment?.Status ?? string.Empty,
                    InspectionDate =
                        appointment?.AppointmentDate,
                    InspectionTimeSlot =
                        appointment?.TimeSlot ?? string.Empty,
                    InspectionValuerName =
                        appointment?.ValuerName ?? string.Empty
                };

                AddSubmission(model, property, submission);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AdminClientAccount] Attribute data could not be loaded for {UserId}.",
                cleanUserId);
        }

        try
        {
            model.Rebates =
                await _rebatesService.GetDashboardAsync(cleanUserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AdminClientAccount] Rebate data could not be loaded for {UserId}.",
                cleanUserId);
        }

        model.Properties = propertyMap.Values
            .OrderBy(x => x.PropertyDescription)
            .ThenBy(x => x.RollName)
            .ToList();

        model.Submissions = model.Submissions
            .OrderByDescending(x => x.SubmittedAt)
            .ThenBy(x => x.SubmissionType)
            .ThenBy(x => x.ReferenceNumber)
            .ToList();

        return model;
    }

    private static AdminClientPropertyViewModel GetOrCreateProperty(
        IDictionary<string, AdminClientPropertyViewModel> propertyMap,
        GvList? roll,
        string? propertyDescription,
        string? town,
        string? category,
        string? marketValue,
        string? premiseId,
        string? unitKey,
        string? valuationKey,
        string? propertyFrom,
        string? rollSourceOverride = null,
        string? rollNameOverride = null)
    {
        var rollSource =
            rollSourceOverride
            ?? roll?.Source
            ?? string.Empty;

        var rollName =
            rollNameOverride
            ?? roll?.Name
            ?? rollSource;

        var key = BuildPropertyKey(
            premiseId,
            unitKey,
            valuationKey,
            propertyDescription,
            rollSource);

        if (!propertyMap.TryGetValue(key, out var property))
        {
            property = new AdminClientPropertyViewModel
            {
                PropertyKey = key,
                PropertyDescription = Clean(propertyDescription),
                Town = Clean(town),
                Category = Clean(category),
                MarketValue = Clean(marketValue),
                PremiseId = Clean(premiseId),
                UnitKey = Clean(unitKey),
                ValuationKey = Clean(valuationKey),
                RollSource = rollSource,
                RollName = rollName,
                PropertyFrom = Clean(propertyFrom)
            };

            propertyMap[key] = property;
        }
        else
        {
            property.PropertyDescription =
                First(property.PropertyDescription, propertyDescription);

            property.Town =
                First(property.Town, town);

            property.Category =
                First(property.Category, category);

            property.MarketValue =
                First(property.MarketValue, marketValue);

            property.PremiseId =
                First(property.PremiseId, premiseId);

            property.UnitKey =
                First(property.UnitKey, unitKey);

            property.ValuationKey =
                First(property.ValuationKey, valuationKey);

            property.PropertyFrom =
                First(property.PropertyFrom, propertyFrom);
        }

        return property;
    }

    private static void AddSubmission(
        AdminClientAccountViewModel model,
        AdminClientPropertyViewModel property,
        AdminClientSubmissionViewModel submission)
    {
        if (string.IsNullOrWhiteSpace(submission.ReferenceNumber))
            return;

        var duplicate = model.Submissions.Any(x =>
            x.SubmissionType.Equals(
                submission.SubmissionType,
                StringComparison.OrdinalIgnoreCase)
            && x.ReferenceNumber.Equals(
                submission.ReferenceNumber,
                StringComparison.OrdinalIgnoreCase));

        if (duplicate)
            return;

        model.Submissions.Add(submission);
        property.Submissions.Add(submission);
    }

    private static string BuildPropertyKey(
        string? premiseId,
        string? unitKey,
        string? valuationKey,
        string? propertyDescription,
        string? rollSource)
    {
        var strongestKey =
            First(
                string.Empty,
                premiseId,
                unitKey,
                valuationKey);

        if (!string.IsNullOrWhiteSpace(strongestKey))
        {
            return $"{Clean(rollSource)}::{Normalise(strongestKey)}";
        }

        return $"{Clean(rollSource)}::{Normalise(propertyDescription)}";
    }

    private static string First(
        string? current,
        params string?[] candidates)
    {
        if (!string.IsNullOrWhiteSpace(current))
            return current.Trim();

        return candidates
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            ?.Trim()
            ?? string.Empty;
    }

    private static string Clean(string? value) =>
        value?.Trim() ?? string.Empty;

    private static string Normalise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "UNKNOWN";

        return new string(
            value.Trim()
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
    }
}
