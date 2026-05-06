namespace V2_Genesis.Services.Interfaces
{
    public interface IReCaptchaService
    {
        Task<bool> VerifyAsync(string token);
    }
    public class ReCaptchaSettings
    {
        public string SiteKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
    }

}
