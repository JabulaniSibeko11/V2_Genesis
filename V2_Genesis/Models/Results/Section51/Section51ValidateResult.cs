namespace V2_Genesis.Models.Section51;

public class Section51ValidateResult
{
    public bool IsValid { get; set; }
    public bool AlreadyDone { get; set; }   // already uploaded
    public bool PastDeadline { get; set; }   // deadline passed
    public string? Error { get; set; }

    public static Section51ValidateResult Fail(string error)
        => new() { IsValid = false, Error = error };

    public static Section51ValidateResult Limit(bool alreadyDone, bool pastDeadline)
        => new()
        {
            IsValid = false,
            AlreadyDone = alreadyDone,
            PastDeadline = pastDeadline
        };

    public static Section51ValidateResult Ok()
        => new() { IsValid = true };
}