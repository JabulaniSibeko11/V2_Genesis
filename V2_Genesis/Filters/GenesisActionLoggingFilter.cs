using Microsoft.AspNetCore.Mvc.Filters;
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

            try
            {
                var result =
                    await next();

                sw.Stop();

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

                var statusCode =
                    httpContext.Response.StatusCode;

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
    }
}