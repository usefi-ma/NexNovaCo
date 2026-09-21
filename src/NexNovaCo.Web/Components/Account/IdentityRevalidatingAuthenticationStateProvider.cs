using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Data;

namespace NexNovaCo.Web.Components.Account;

// .NET 10 Individual-auth template pattern: a fresh scope for each circuit revalidation.
public sealed class IdentityRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, IOptions<IdentityOptions> options)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var principal = authenticationState.User;
        var user = await users.GetUserAsync(principal);
        if (user is null) return false;
        if (users.SupportsUserSecurityStamp &&
            principal.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType) != await users.GetSecurityStampAsync(user))
            return false;
        // Role removal must also invalidate a formerly privileged live circuit.
        return !principal.IsInRole(IdentityDatabaseInitializer.AdminRole) ||
            await users.IsInRoleAsync(user, IdentityDatabaseInitializer.AdminRole);
    }
}
