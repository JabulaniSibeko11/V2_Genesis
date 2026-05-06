using Microsoft.Extensions.Options;
using System.Text.Json;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class ReCaptchaService : IReCaptchaService
    {
        private readonly HttpClient _http;
        private readonly ReCaptchaSettings _cfg;
        private readonly ILogger<ReCaptchaService> _logger;

        public ReCaptchaService(
            HttpClient http,
            IOptions<ReCaptchaSettings> opts,
            ILogger<ReCaptchaService> logger)
        {
            _http = http;
            _cfg = opts.Value;
            _logger = logger;
        }

        public async Task<bool> VerifyAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            try
            {
                var response = await _http.PostAsync(
                    _cfg.VerifyUrl,
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["secret"] = _cfg.SecretKey,
                        ["response"] = token
                    }));

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("success").GetBoolean();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "reCAPTCHA verification failed");
                return false;
            }
        }
    }
}
