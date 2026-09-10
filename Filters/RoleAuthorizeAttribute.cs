using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Training.Services.Interfaces;
using Training.Services.IServices;

namespace Training.Filters
{
    /// <summary>
    /// Role authorization untuk MVC pages
    /// Menggunakan GetRoleAsync() untuk ambil highest role dari HierarchyLevel
    /// Implements IAsyncActionFilter untuk berjalan SETELAH ProfileAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RoleAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string[] _allowedRoles;

        /// <summary>
        /// Constructor dengan allowed roles
        /// </summary>
        public RoleAuthorizeAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RoleAuthorizeAttribute>)) as ILogger<RoleAuthorizeAttribute>;
                logger?.LogWarning("🟡 RoleAuthorizeAttribute.OnActionExecutionAsync TRIGGERED");

                // Get username dari session
                var username = context.HttpContext.Session.GetString("Username");
                logger?.LogWarning($"🟡 Session Username: '{username}'");

                if (string.IsNullOrEmpty(username))
                {
                    logger?.LogWarning("❌ Username is empty - Redirect to Index");
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }

                // Get TrainingService dari DI container
                var trainingService = context.HttpContext.RequestServices.GetService(typeof(ITrainingService)) as ITrainingService;

                if (trainingService == null)
                {
                    logger?.LogError("❌ TrainingService not available");
                    context.Result = new RedirectToActionResult("NotAuthorized", "Home", null);
                    return;
                }

                // Get highest role via GetRoleAsync
                var highestRole = await trainingService.GetRoleAsync(username);
                logger?.LogWarning($"🟢 Highest Role (from GetRoleAsync): '{highestRole}'");
                logger?.LogWarning($"🟢 Allowed Roles: {string.Join(", ", _allowedRoles)}");

                // Check if highest role ada dalam allowed roles
                if (!string.IsNullOrEmpty(highestRole) && _allowedRoles.Contains(highestRole))
                {
                    logger?.LogWarning($"✅ User '{highestRole}' AUTHORIZED - Continue to action");
                    await next();
                    return;
                }

                logger?.LogWarning($"❌ User '{highestRole}' NOT AUTHORIZED - Redirect to NotAuthorized");
                context.Result = new RedirectToActionResult("NotAuthorized", "Home", null);
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RoleAuthorizeAttribute>)) as ILogger<RoleAuthorizeAttribute>;
                logger?.LogError(ex, "❌ Error in RoleAuthorizeAttribute: {Message}", ex.Message);

                context.Result = new RedirectToActionResult("NotAuthorized", "Home", null);
            }
        }
    }
}