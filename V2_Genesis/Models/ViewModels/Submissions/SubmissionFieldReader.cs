namespace V2_Genesis.Models.ViewModels.Submissions
{
    public static class SubmissionFieldReader
    {
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

        public static string Display(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Not provided"
                : value.Trim();
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
    }
}
