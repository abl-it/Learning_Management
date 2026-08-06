//using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Training.Middleware
{
    public class SessionTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionTokenMiddleware> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly string _secretKey = "fQdrtYklILI4/muy00eWb80w2OrGs6hugJIbUCZKvJQ=";
       // private readonly string _login = "http://localhost:5006/Account/Login/";
        
        public SessionTokenMiddleware(RequestDelegate next
                                        , ILogger<SessionTokenMiddleware> logger
                                        , IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (_env.IsDevelopment())
            {
                // In development environment, bypass token validation and set username to "admin1"
                context.Session.SetString("Username", "vikri");
                await _next(context);
                return;
            }

            var sessionId = context.Request.Query["token"].FirstOrDefault();
            string sId = context.Session.GetString("appId");

            if (!string.IsNullOrEmpty(sessionId))
            {
                var token = await RequestTokenFromP01(context, sessionId);
                
                // Validate and extract information from the token
                var username = ValidateAndExtractToken(token);
                if (username != null)
                {
                    // Store the token and username in the session
                    context.Session.SetString("JWT", token);
                    context.Session.SetString("Username", username);
                }
                else
                {
                    // Invalid token
                    context.Response.Redirect(GetLoginUrl(context));
					return;
                }
            }
            else if (string.IsNullOrEmpty(context.Session.GetString("JWT")))
            {
                // Redirect to P01 for login
                //context.Response.Redirect(_login);
				context.Response.Redirect(GetLoginUrl(context));
				return;
            }

            await _next(context);
        }

        private async Task<string> RequestTokenFromP01(HttpContext context, string sessionId)
        {
            using var client = new HttpClient();
            //client.GetAsync($"http://localhost:5006/api/token?sessionId={sessionId}");
			//production
			//client.GetAsync($"http://192.168.41.72:1115/home/api/token?sessionId={sessionId}");
			var response = await client.GetAsync(GetToken(context, sessionId));

			if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }
        private string ValidateAndExtractToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
                
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                // Extract username from claims
                var username = principal.FindFirst(ClaimTypes.Name)?.Value;
                return username;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Token validation failed: {ex.Message}");
                return null;
            }
        }

		private string GetLoginUrl(HttpContext context)
		{
			// Get the scheme (http/https) and host (domain or IP)
			var scheme = context.Request.Scheme;
			var host = context.Request.Host.ToString();

			// Get the base path (if any, e.g., /app)
			var basePath = context.Request.PathBase.ToString();

			// Construct the login URL dynamically
			var loginUrl = $"{scheme}://{host}/home/Account/Login/";

			return loginUrl;
		}
		private string GetToken(HttpContext context, string sessionId)
		{
			// Get the scheme (http/https) and host (domain or IP)
			var scheme = context.Request.Scheme;
			var host = context.Request.Host.ToString();

			// Get the base path (if any, e.g., /app)
			var basePath = context.Request.PathBase.ToString();

			// Construct the login URL dynamically
			var loginUrl = $"{scheme}://{host}/home/api/token?sessionId={sessionId}";

			return loginUrl;
		}

	}
}
