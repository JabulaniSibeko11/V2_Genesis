namespace V2_Genesis.Models.Emails 
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 25;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = false;
        public bool UseDefaultCredentials { get; set; } = false;
        public string FromName { get; set; } = string.Empty;

        public string SmtpUser { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public bool TestMode { get; set; } = true;

        public string TestRecipient { get; set; } = string.Empty;

    }

}
