namespace V2_Genesis.Models.ViewModels.Submissions
{
    public sealed class SubmissionApplicantViewModel
{
    public string ObjectorType { get; set; } = string.Empty;

    public string ApplicantName { get; set; } = string.Empty;

    public string ApplicantSurname { get; set; } = string.Empty;

    public string ApplicantFullName =>
        string.Join(
            " ",
            new[]
            {
                ApplicantName?.Trim(),
                ApplicantSurname?.Trim()
            }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

    public string ApplicantIdNumber { get; set; } = string.Empty;

    public string ApplicantCompanyRegistrationNumber { get; set; } = string.Empty;

    public string ApplicantEmail { get; set; } = string.Empty;

    public string ApplicantTelephone { get; set; } = string.Empty;

    public string ApplicantCellphone { get; set; } = string.Empty;

    public string ApplicantAddress1 { get; set; } = string.Empty;

    public string ApplicantAddress2 { get; set; } = string.Empty;

    public string ApplicantAddress3 { get; set; } = string.Empty;

    public string ApplicantAddress4 { get; set; } = string.Empty;

    public string ApplicantPostalCode { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string OwnerSurname { get; set; } = string.Empty;

    public string OwnerFullName =>
        string.Join(
            " ",
            new[]
            {
                OwnerName?.Trim(),
                OwnerSurname?.Trim()
            }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

    public string OwnerIdNumber { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public string OwnerTelephone { get; set; } = string.Empty;

    public string OwnerCellphone { get; set; } = string.Empty;

    public string RepresentativeName { get; set; } = string.Empty;

    public string RepresentativeSurname { get; set; } = string.Empty;

    public string RepresentativeFullName =>
        string.Join(
            " ",
            new[]
            {
                RepresentativeName?.Trim(),
                RepresentativeSurname?.Trim()
            }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

    public string RepresentativeIdNumber { get; set; } = string.Empty;

    public string RepresentativeCompanyName { get; set; } = string.Empty;

    public string RepresentativeCompanyRegistrationNumber { get; set; } =
        string.Empty;

    public string RepresentativeEmail { get; set; } = string.Empty;

    public string RepresentativeTelephone { get; set; } = string.Empty;

    public string RepresentativeCellphone { get; set; } = string.Empty;

    public string RepresentativeAddress1 { get; set; } = string.Empty;

    public string RepresentativeAddress2 { get; set; } = string.Empty;

    public string RepresentativeAddress3 { get; set; } = string.Empty;

    public string RepresentativeAddress4 { get; set; } = string.Empty;

    public string RepresentativePostalCode { get; set; } = string.Empty;

    public string Capacity { get; set; } = string.Empty;

    public bool IsOwner =>
        ObjectorType.Equals(
            "Owner",
            StringComparison.OrdinalIgnoreCase);

    public bool IsRepresentative =>
        ObjectorType.Equals(
            "Representative",
            StringComparison.OrdinalIgnoreCase);

    public bool IsThirdParty =>
        ObjectorType.Equals(
            "Third_Party",
            StringComparison.OrdinalIgnoreCase)
        || ObjectorType.Equals(
            "Third Party",
            StringComparison.OrdinalIgnoreCase);

    public bool HasOwnerDetails =>
        !string.IsNullOrWhiteSpace(OwnerFullName)
        || !string.IsNullOrWhiteSpace(OwnerEmail)
        || !string.IsNullOrWhiteSpace(OwnerIdNumber);

    public bool HasRepresentativeDetails =>
        !string.IsNullOrWhiteSpace(RepresentativeFullName)
        || !string.IsNullOrWhiteSpace(RepresentativeEmail)
        || !string.IsNullOrWhiteSpace(RepresentativeCompanyName);
}
}
