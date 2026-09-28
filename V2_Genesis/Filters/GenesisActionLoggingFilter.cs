using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Diagnostics;

namespace V2_Genesis.Filters
{
    public sealed class GenesisActionLoggingFilter : IAsyncActionFilter
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _config;

        public GenesisActionLoggingFilter(
            ILoggerFactory loggerFactory,
            IConfiguration config)
        {
            _loggerFactory = loggerFactory;
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

            var httpContext = context.HttpContext;

            var controller =
                context.RouteData.Values["controller"]?.ToString()
                ?? context.Controller.GetType().Name;

            var action =
                context.RouteData.Values["action"]?.ToString()
                ?? "Unknown";

            var user =
                httpContext.User?.Identity?.Name
                ?? "Anonymous";

            var ip =
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "Unknown";

            var method =
                httpContext.Request.Method;

            var path =
                httpContext.Request.Path.Value
                ?? "/";

            /*
             * Important:
             * Create the logger using the actual controller type.
             *
             * Example categories:
             * V2_Genesis.Controllers.ObjectionController
             * V2_Genesis.Controllers.AttributesController
             * V2_Genesis.Controllers.AdminController
             */
            var controllerType =
                context.Controller.GetType();

            var logger =
                _loggerFactory.CreateLogger(
                    controllerType.FullName
                    ?? controllerType.Name);

            var sw =
                Stopwatch.StartNew();

            logger.LogInformation(
                "Controller action started. " +
                "Controller={Controller}, " +
                "Action={Action}, " +
                "Method={Method}, " +
                "Path={Path}, " +
                "User={User}, " +
                "IP={IP}",
                controller,
                action,
                method,
                path,
                user,
                ip);

            // Error messages already in TempData before the action runs
            // (so only messages set by THIS action are logged below).
            var tempData = (context.Controller as Controller)?.TempData;
            var errorsBefore = ReadErrorMessages(tempData);

            try
            {
                var result =
                    await next();

                sw.Stop();

                // ── Errors the user was shown ──────────────────────────
                // Many actions catch an exception, put the message in
                // TempData (NoticeError, AttributeError, Error, ...) and
                // redirect, so nothing reached the log. Log those here.
                var redirectTo = DescribeRedirect(result.Result);

                foreach (var (key, message) in ReadErrorMessages(tempData))
                {
                    if (errorsBefore.TryGetValue(key, out var previous) && previous == message)
                        continue;

                    logger.LogWarning(
                        "User was shown an error. " +
                        "Controller={Controller}, " +
                        "Action={Action}, " +
                        "Method={Method}, " +
                        "Path={Path}, " +
                        "User={User}, " +
                        "IP={IP}, " +
                        "Key={ErrorKey}, " +
                        "Message={ErrorMessage}, " +
                        "RedirectTo={RedirectTo}",
                        controller,
                        action,
                        method,
                        path,
                        user,
                        ip,
                        key,
                        message,
                        redirectTo ?? "(none)");
                }
                var logValidationErrors = _config.GetValue<bool>(
    "GenesisLogging:LogValidationErrors",
    true);

                if (logValidationErrors && !context.ModelState.IsValid)
                {
                    var validationErrors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(error =>
                        {
                            var message = !string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? error.ErrorMessage
                                : error.Exception?.Message ?? "Validation failed.";

                            var field = string.IsNullOrWhiteSpace(x.Key)
                                ? "(general)"
                                : x.Key;

                            return $"{field}: {message}";
                        }))
                        .ToArray();

                    logger.LogWarning(
                        "Model validation failed. " +
                        "Controller={Controller}, " +
                        "Action={Action}, " +
                        "Method={Method}, " +
                        "Path={Path}, " +
                        "User={User}, " +
                        "IP={IP}, " +
                        "ValidationErrors={ValidationErrors}",
                        controller,
                        action,
                        method,
                        path,
                        user,
                        ip,
                        string.Join(" | ", validationErrors));
                }
                if (result.Exception != null &&
                    !result.ExceptionHandled)
                {
                    logger.LogError(
                        result.Exception,
                        "Controller action failed. " +
                        "Controller={Controller}, " +
                        "Action={Action}, " +
                        "Method={Method}, " +
                        "Path={Path}, " +
                        "User={User}, " +
                        "IP={IP}, " +
                        "DurationMs={DurationMs}",
                        controller,
                        action,
                        method,
                        path,
                        user,
                        ip,
                        sw.ElapsedMilliseconds);

                    return;
                }

                // The response status is not set yet when an action filter
                // runs, so read it from the action result (redirects = 302).
                var statusCode =
                    ResultStatusCode(result.Result)
                    ?? httpContext.Response.StatusCode;

                if (statusCode >= 500)
                {
                    logger.LogError(
                        "Controller action returned server error. " +
                        "Controller={Controller}, " +
                        "Action={Action}, " +
                        "Method={Method}, " +
                        "Path={Path}, " +
                        "User={User}, " +
                        "IP={IP}, " +
                        "StatusCode={StatusCode}, " +
                        "DurationMs={DurationMs}",
                        controller,
                        action,
                        method,
                        path,
                        user,
                        ip,
                        statusCode,
                        sw.ElapsedMilliseconds);
                }
                else if (statusCode >= 400)
                {
                    logger.LogWarning(
                        "Controller action returned client error. " +
                        "Controller={Controller}, " +
                        "Action={Action}, " +
                        "Method={Method}, " +
                        "Path={Path}, " +
                        "User={User}, " +
                        "IP={IP}, " +
                        "StatusCode={StatusCode}, " +
                        "DurationMs={DurationMs}",
                        controller,
                        action,
                        method,
                        path,
                        user,
                        ip,
                        statusCode,
                        sw.ElapsedMilliseconds);
                }
                else
                {
                    logger.LogInformation(
                        "Controller action completed. " +
                        "Controller={Controller}, " +
                        "Action={Action}, " +
                        "Method={Method}, " +
                        "Path={Path}, " +
                        "User={User}, " +
                        "IP={IP}, " +
                        "StatusCode={StatusCode}, " +
                        "DurationMs={DurationMs}",
                        controller,
                        action,
                        method,
                        path,
                        user,
                        ip,
                        statusCode,
                        sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();

                logger.LogError(
                    ex,
                    "Controller action threw an exception. " +
                    "Controller={Controller}, " +
                    "Action={Action}, " +
                    "Method={Method}, " +
                    "Path={Path}, " +
                    "User={User}, " +
                    "IP={IP}, " +
                    "DurationMs={DurationMs}",
                    controller,
                    action,
                    method,
                    path,
                    user,
                    ip,
                    sw.ElapsedMilliseconds);

                throw;
            }
        }

        private static Dictionary<string, string> ReadErrorMessages(ITempDataDictionary? tempData)
        {
            var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (tempData is null)
                return errors;

            foreach (var key in tempData.Keys.ToList())
            {
                if (!key.Contains("Error", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Peek: do not mark the message as read, the view still needs it.
                var value = tempData.Peek(key)?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                    errors[key] = value.Trim();
            }

            return errors;
        }

        private static string? DescribeRedirect(IActionResult? result) => result switch
        {
            RedirectToActionResult r => $"{r.ControllerName ?? "(same)"}/{r.ActionName}",
            RedirectToRouteResult r => r.RouteName ?? "route",
            RedirectResult r => r.Url,
            LocalRedirectResult r => r.Url,
            _ => null
        };

        private static int? ResultStatusCode(IActionResult? result) => result switch
        {
            RedirectToActionResult r => r.Permanent ? 301 : 302,
            RedirectToRouteResult r => r.Permanent ? 301 : 302,
            RedirectResult r => r.Permanent ? 301 : 302,
            LocalRedirectResult r => r.Permanent ? 301 : 302,
            IStatusCodeActionResult r when r.StatusCode.HasValue => r.StatusCode,
            _ => null
        };
    }
}