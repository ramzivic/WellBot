using System;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WellBot.Admin.Security;

public class AuthService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IConfiguration config, ILogger<AuthService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public (bool IsValid, string Role) ValidateCredentials(string username, string password)
    {
        try
        {
            var authSection = _config.GetSection("AdminAuth");
            var localAdmin = authSection["LocalAdminUsername"];
            var localPass = authSection["LocalAdminPassword"];

            // 1. Check Local Admin credentials (for UI login)
            if (string.Equals(username, localAdmin, StringComparison.OrdinalIgnoreCase) && password == localPass)
            {
                _logger.LogInformation("Authenticated via local admin account.");
                return (true, "Admin");
            }

            // 2. Check API Service Account credentials (for desktop client)
            var apiSection = _config.GetSection("ApiServiceAccount");
            var apiUser = apiSection["Username"];
            var apiPass = apiSection["Password"];
            if (!string.IsNullOrEmpty(apiUser) && 
                string.Equals(username, apiUser, StringComparison.Ordinal) && password == apiPass)
            {
                _logger.LogInformation("Authenticated via API service account.");
                return (true, "DesktopClient");
            }

            // 3. Check Active Directory / Local Machine Groups
            var domain = authSection["AdDomain"];
            var groups = authSection.GetSection("AdminGroups").Get<string[]>() ?? Array.Empty<string>();

            if (groups.Length == 0) return (false, string.Empty);

            if (OperatingSystem.IsWindows())
            {
#pragma warning disable CA1416 // Validate platform compatibility
                ContextType contextType = string.IsNullOrWhiteSpace(domain) ? ContextType.Machine : ContextType.Domain;
                
                using var context = string.IsNullOrWhiteSpace(domain) 
                    ? new PrincipalContext(contextType) 
                    : new PrincipalContext(contextType, domain);

                bool isValid = context.ValidateCredentials(username, password);
                if (!isValid) return (false, string.Empty);

                // Get user and check groups
                using var user = UserPrincipal.FindByIdentity(context, username);
                if (user != null)
                {
                    var userGroups = user.GetAuthorizationGroups();
                    foreach (var principal in userGroups)
                    {
                        if (groups.Contains(principal.Name, StringComparer.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation($"Authenticated {username} via AD/Local group {principal.Name}.");
                            return (true, "Admin");
                        }
                    }
                }
#pragma warning restore CA1416
            }
            else
            {
                _logger.LogWarning("Active Directory/Local Group authentication skipped because the application is not running on Windows.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials");
        }

        return (false, string.Empty);
    }
}
