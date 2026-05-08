namespace V2_Genesis.Services.Notice;

public class NoticeRollEntry
{
    public string Section49Path { get; set; } = string.Empty;
    public string SignatureFile { get; set; } = string.Empty;
    public string RollTitle { get; set; } = string.Empty;
    public string FinancialYears { get; set; } = string.Empty;
    public string? ExtendedPeriodText { get; set; }
}

public class NoticeRollSettings
{
    public Dictionary<string, NoticeRollEntry> NoticeRolls { get; set; } = new();

    public NoticeRollEntry For(string rollSource) =>
        NoticeRolls.TryGetValue(rollSource, out var e) ? e
        : new NoticeRollEntry { RollTitle = rollSource };
}