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
        private readonly IConfiguration _configuration;

        public SessionTokenMiddleware(RequestDelegate next
                                        , ILogger<SessionTokenMiddleware> logger
                                        , IWebHostEnvironment env
                                        , IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _env = env;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("SessionTokenMiddleware started.");
            _logger.LogInformation("Request : {Path}", context.Request.Path);

            if (_env.IsDevelopment())
            {
                context.Session.SetString("Username", "vikri");
                await _next(context);
                return;
            }

            var sessionId = context.Request.Query["token"].FirstOrDefault();

            _logger.LogInformation("SessionID : {SessionId}", sessionId);

            if (!string.IsNullOrEmpty(sessionId))
            {
                var token = await RequestTokenFromP01(sessionId);

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("Token not found.");

                    context.Response.Redirect(GetLoginUrl());
                    return;
                }

                var username = ValidateAndExtractToken(token);

                if (!string.IsNullOrEmpty(username))
                {
                    context.Session.SetString("JWT", token);
                    context.Session.SetString("Username", username);
                    context.Session.SetString("SessionID", sessionId);
                }
                else
                {
                    context.Response.Redirect(GetLoginUrl());
                    return;
                }
            }
            else if (string.IsNullOrEmpty(context.Session.GetString("JWT")))
            {
                context.Response.Redirect(GetLoginUrl());
                return;
            }

            await _next(context);
        }

        private async Task<string?> RequestTokenFromP01(string sessionId)
        {
            try
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var response = await client.GetAsync(GetToken(sessionId));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Token API returned {StatusCode}",
                        response.StatusCode);

                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot connect to Token API.");

                return null;
            }
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
            //_logger.LogInformation("User : {Username}",username);
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

        private string GetLoginUrl()
        {
            return _configuration["SSO:PublicLogin"]!;
        }
        private string GetToken(string sessionId)
        {
            var baseUrl = _configuration["SSO:InternalApi"]!.TrimEnd('/');

            var url = $"{baseUrl}/home/api/token?sessionId={sessionId}";

            _logger.LogInformation("Token URL : {Url}", url);

            return url;
        }

    }
}
