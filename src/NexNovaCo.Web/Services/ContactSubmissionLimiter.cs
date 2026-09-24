using System.Threading.RateLimiting;

namespace NexNovaCo.Web.Services;

// Same supported primitives used by ASP.NET Core rate-limiting middleware, applied at
// the Interactive Server service boundary: there is no standalone Contact HTTP POST.
public sealed class ContactSubmissionLimiter : IDisposable
{
    private readonly FixedWindowRateLimiter _global = Window(30, TimeSpan.FromMinutes(1));
    private readonly ConcurrencyLimiter _concurrent = new(new ConcurrencyLimiterOptions { PermitLimit = 2, QueueLimit = 0 });
    internal static FixedWindowRateLimiter Window(int permits, TimeSpan window) => new(new FixedWindowRateLimiterOptions {
        PermitLimit = permits, Window = window, QueueLimit = 0, AutoReplenishment = true
    });
    public RateLimitLease AcquireAttempt() => _global.AttemptAcquire();
    public RateLimitLease AcquireSend() => _concurrent.AttemptAcquire();
    public void Dispose() { _global.Dispose(); _concurrent.Dispose(); }
}
