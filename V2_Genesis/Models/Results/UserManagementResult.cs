namespace V2_Genesis.Models.Results
{
    public class UserManagementResult
    {
        // ── Identity ──────────────────────────────────────────────────
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool Active { get; set; }

        // ── Name, role, contact (mapped from u.* in Login SP) ─────────
        public string? FirstName { get; set; }
        public string? SecondName { get; set; }
        public string? Surname { get; set; }
        public string? Position { get; set; }
        public string? SAPNumber { get; set; }
        public string? emailAddress { get; set; }  // ← ADDED for Windows auth
        public string Role { get; set; } = string.Empty;

        // ── Computed ───────────────────────────────────────────────────
        public string FullName =>
            string.Join(" ",
                new[] { FirstName?.Trim(), Surname?.Trim() }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

        public string DisplayLabel =>
            string.IsNullOrWhiteSpace(Position)
                ? FullName
                : $"{FullName} — {Position.Trim()}";
    }
}
