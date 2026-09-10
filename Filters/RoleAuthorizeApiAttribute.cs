using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Training.Services.Interfaces;
using Training.Services.IServices;

namespace Training.Filters
{
    /// <summary>
    /// Role authorization untuk API endpoints
    /// Menggunakan GetRoleAsync() untuk ambil highest role dari HierarchyLevel
    /// Implements IAsyncResourceFilter (runs lebih awal untuk API)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RoleAuthorizeApiAttribute : Attribute, IAsyncResourceFilter
    {
        private readonly string[] _allowedRoles;

        public RoleAuthorizeApiAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            try
            {
                var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RoleAuthorizeApiAttribute>)) as ILogger<RoleAuthorizeApiAttribute>;
                logger?.LogWarning("🔵 RoleAuthorizeApiAttribute.OnResourceExecutionAsync TRIGGERED for API");

                // Get username dari session
                var username = context.HttpContext.Session.GetString("Username");
                logger?.LogWarning($"🔵 API: Session Username: '{username}'");

                if (string.IsNullOrEmpty(username))
                {
                    logger?.LogWarning("❌ API: Username is empty");
                    context.Result = new ObjectResult(new { success = false, message = "Unauthorized" })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }

                // Get TrainingService dari DI container
                var trainingService = context.HttpContext.RequestServices.GetService(typeof(ITrainingService)) as ITrainingService;

                if (trainingService == null)
                {
                    logger?.LogError("❌ API: TrainingService not available");
                    context.Result = new ObjectResult(new { success = false, message = "Service error" })
                    {
                        StatusCode = StatusCodes.Status500InternalServerError
                    };
                    return;
                }

                // Get highest role via GetRoleAsync
                var highestRole = await trainingService.GetRoleAsync(username);
                logger?.LogWarning($"🔵 API: Highest Role: '{highestRole}'");
                logger?.LogWarning($"🔵 API: Allowed Roles: {string.Join(", ", _allowedRoles)}");

                // Check if highest role ada dalam allowed roles
                if (!string.IsNullOrEmpty(highestRole) && _allowedRoles.Contains(highestRole))
                {
                    logger?.LogWarning($"✅ API: User '{highestRole}' AUTHORIZED");
                    await next();
                    return;
                }

                logger?.LogWarning($"❌ API: User '{highestRole}' NOT AUTHORIZED");
                context.Result = new ObjectResult(new { success = false, message = "Unauthorized" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RoleAuthorizeApiAttribute>)) as ILogger<RoleAuthorizeApiAttribute>;
                logger?.LogError(ex, "❌ Error in RoleAuthorizeApiAttribute: {Message}", ex.Message);

                context.Result = new ObjectResult(new { success = false, message = "Error" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}