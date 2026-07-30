using System.Globalization;

namespace V2_Genesis.Models.ViewModels.Submissions
{
    public static class SubmissionFieldReader
    {
        private static readonly CultureInfo SouthAfricanCulture =
            CultureInfo.GetCultureInfo("en-ZA");

        private static readonly string[] BinaryChoiceOptions =
        {
            "Yes",
            "No"
        };

        private static readonly string[] GradedChoiceOptions =
        {
            "Good",
            "Average",
            "Poor"
        };

        private static readonly string[] MoneyFieldTerms =
        {
            "marketvalue",
            "market_value",
            "sellingprice",
            "selling_price",
            "askingprice",
            "asking_price",
            "purchaseprice",
            "purchase_price",
            "rental",
            "rent",
            "cost",
            "drc",
            "replacementcost",
            "replacement_cost",
            "vacantlandcost",
            "vacant_land_cost",
            "demolitionrate",
            "demolition_rate",
            "ratepersqm",
            "rate_per_sqm",
            "costrate",
            "cost_rate",
            "totalvalue",
            "total_value",
            "grossincome",
            "gross_income",
            "nett",
            "netincome",
            "net_income",
            "compensationamount",
            "compensation_amount",
            "registeredamount",
            "registered_amount",
            "offerreceived",
            "offer_received"
        };

        private static readonly string[] NonMoneyFieldTerms =
        {
            "valuationkey",
            "valuation_key",
            "propertykey",
            "property_key",
            "unitkey",
            "unit_key",
            "premiseid",
            "premise_id",
            "propertyid",
            "property_id",
            "attrid",
            "attr_id",
            "number",
            "count",
            "percentage",
            "percent",
            "extent",
            "area",
            "gba",
            "nla",
            "tla",
            "storeys",
            "year"
        };

        public static SubmissionFieldViewModel? Find(
            SubmissionViewModel model,
            params string[] names)
        {
            if (model is null || names.Length == 0)
                return null;

            foreach (var name in names)
            {
                var field = model.Sections
                    .SelectMany(section => section.Fields)
                    .FirstOrDefault(item =>
                        item.Name.Equals(
                            name,
                            StringComparison.OrdinalIgnoreCase));

                if (field is not null)
                    return field;
            }

            return null;
        }

        public static string Value(
            SubmissionViewModel model,
            params string[] names)
        {
            return Find(model, names)?.Value?.Trim()
                   ?? string.Empty;
        }

        public static bool HasValue(
            SubmissionViewModel model,
            params string[] names)
        {
            return !string.IsNullOrWhiteSpace(
                Value(model, names));
        }

        public static IReadOnlyList<SubmissionFieldViewModel> ByPrefix(
            SubmissionViewModel model,
            string prefix,
            params string[] excludedPrefixes)
        {
            return model.Sections
                .SelectMany(section => section.Fields)
                .Where(field =>
                    field.Name.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                .Where(field =>
                    excludedPrefixes.All(excluded =>
                        !field.Name.StartsWith(
                            excluded,
                            StringComparison.OrdinalIgnoreCase)))
                .GroupBy(
                    field => field.Name,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(field => field.Label)
                .ToList();
        }

        public static IReadOnlyList<SubmissionFieldViewModel> FromSection(
            SubmissionViewModel model,
            params string[] sectionKeys)
        {
            return model.Sections
                .Where(section =>
                    sectionKeys.Contains(
                        section.Key,
                        StringComparer.OrdinalIgnoreCase))
                .SelectMany(section => section.Fields)
                .GroupBy(
                    field => field.Name,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        public static IReadOnlyList<SubmissionFieldViewModel> Except(
            IEnumerable<SubmissionFieldViewModel> fields,
            params string[] names)
        {
            return fields
                .Where(field =>
                    !names.Contains(
                        field.Name,
                        StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Displays blank values as an empty string.
        /// </summary>
        public static string Display(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        /// <summary>
        /// Displays a value according to its field name. Monetary fields
        /// are formatted using South African rand.
        /// </summary>
        public static string Display(
            string? value,
            string? fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return IsMoneyField(fieldName)
                ? DisplayMoney(value)
                : value.Trim();
        }

        /// <summary>
        /// Formats a numeric value as South African rand.
        /// Existing values beginning with R are normalised where possible.
        /// </summary>
        public static string DisplayMoney(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var trimmed = value.Trim();

            var cleaned = trimmed
                .Replace("R", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("\u00A0", string.Empty)
                .Replace(" ", string.Empty);

            if (decimal.TryParse(
                    cleaned,
                    NumberStyles.Number |
                    NumberStyles.AllowCurrencySymbol,
                    SouthAfricanCulture,
                    out var southAfricanAmount))
            {
                return southAfricanAmount.ToString(
                    "C2",
                    SouthAfricanCulture);
            }

            if (decimal.TryParse(
                    cleaned,
                    NumberStyles.Number |
                    NumberStyles.AllowDecimalPoint |
                    NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out var invariantAmount))
            {
                return invariantAmount.ToString(
                    "C2",
                    SouthAfricanCulture);
            }

            // Do not destroy non-numeric database content.
            return trimmed.StartsWith(
                    "R",
                    StringComparison.OrdinalIgnoreCase)
                ? trimmed
                : $"R {trimmed}";
        }

        public static bool IsMoneyField(string? fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;

            var normalised = fieldName
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Trim()
                .ToLowerInvariant();

            if (NonMoneyFieldTerms.Any(term =>
                    normalised.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (MoneyFieldTerms.Any(term =>
                    normalised.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Generic "Value" fields are treated as money, except identifiers
            // and calculated area/count fields excluded above.
            return normalised.EndsWith(
                       "value",
                       StringComparison.OrdinalIgnoreCase)
                   || normalised.Contains(
                       "amount",
                       StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsYes(string? value)
        {
            return value?.Trim().Equals(
                "Yes",
                StringComparison.OrdinalIgnoreCase) == true
                || value?.Trim().Equals(
                    "True",
                    StringComparison.OrdinalIgnoreCase) == true
                || value?.Trim() == "1";
        }

        public static bool IsNo(string? value)
        {
            return value?.Trim().Equals(
                "No",
                StringComparison.OrdinalIgnoreCase) == true
                || value?.Trim().Equals(
                    "False",
                    StringComparison.OrdinalIgnoreCase) == true
                || value?.Trim() == "0";
        }

        public static string[]? GetChoiceOptions(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();

            if (BinaryChoiceOptions.Any(option =>
                    option.Equals(
                        trimmed,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return BinaryChoiceOptions;
            }

            if (GradedChoiceOptions.Any(option =>
                    option.Equals(
                        trimmed,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return GradedChoiceOptions;
            }

            return null;
        }
    }
}
