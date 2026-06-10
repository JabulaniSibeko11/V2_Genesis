namespace V2_Genesis.Services
{
    public class UserManagementResult
    {
        // ── Identity ──────────────────────────────────────────────────
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool Active { get; set; }

        // ── Name & role (mapped directly from u.* in Login SP) ────────
        public string? FirstName { get; set; }
        public string? SecondName { get; set; }
        public string? Surname { get; set; }
        public string? Position { get; set; }
        public string? SAPNumber { get; set; }   // from Users table
        public string Role { get; set; } = string.Empty;
        public string ? EmailAddress { get; set; }

        // ── Computed: FirstName + Surname ─────────────────────────────
        // Dapper won't map this — it's built from the real columns above.
        public string FullName =>
            string.Join(" ",
                new[] { FirstName?.Trim(), Surname?.Trim() }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

        // ── Display: FullName + Position ─────────────────────────────
        public string DisplayLabel =>
            string.IsNullOrWhiteSpace(Position)
                ? FullName
                : $"{FullName} — {Position.Trim()}";
    }

}
