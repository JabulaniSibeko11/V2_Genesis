using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace V2_Genesis.Filters
{
    public sealed class GenesisActionLoggingFilter : IAsyncActionFilter
    {
        private readonly ILogger<GenesisActionLoggingFilter> _logger;
        private readonly IConfiguration _config;

        public GenesisActionLoggingFilter(
            ILogger<GenesisActionLoggingFilter> logger,
            IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var enabled = _config.GetValue<bool>(
                "GenesisLogging:LogControllerActions",
                true);

            if (!enabled)
            {
                await next();
                return;
            }

            var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
            var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
            var user = context.HttpContext.User?.Identity?.Name ?? "Anonymous";
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var sw = Stopwatch.StartNew();

            try
            {
                var result = await next();
                sw.Stop();

                if (result.Exception != null && !result.ExceptionHandled)
                {
                    _logger.LogError(
                        result.Exception,
                        "Controller action failed. Controller={Controller}, Action={Action}, User={User}, IP={IP}, DurationMs={DurationMs}",
                        controller, action, user, ip, sw.ElapsedMilliseconds);

                    return;
                }

                _logger.LogInformation(
                    "Controller action completed. Controller={Controller}, Action={Action}, User={User}, IP={IP}, StatusCode={StatusCode}, DurationMs={DurationMs}",
                    controller,
                    action,
                    user,
                    ip,
                    context.HttpContext.Response.StatusCode,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();

                _logger.LogError(
                    ex,
                    "Controller action failed. Controller={Controller}, Action={Action}, User={User}, IP={IP}, DurationMs={DurationMs}",
                    controller, action, user, ip, sw.ElapsedMilliseconds);

                throw;
            }
        }
    }
}
