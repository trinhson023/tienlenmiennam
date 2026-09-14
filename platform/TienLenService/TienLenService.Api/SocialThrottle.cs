using Microsoft.Extensions.Caching.Memory;

namespace TienLenService.Api;

public sealed class SocialThrottle(IMemoryCache cache)
{
    private readonly object[] _stripes = Enumerable.Range(0, 64).Select(_ => new object()).ToArray();
    private static readonly TimeSpan Window = TimeSpan.FromMilliseconds(350);

    public bool TryAcquire(Guid userId)
    {
        var stripe = _stripes[(userId.GetHashCode() & int.MaxValue) % _stripes.Length];
        lock (stripe)
        {
            var key = $"social-throttle:{userId:N}";
            var now = DateTimeOffset.UtcNow;
            if (cache.TryGetValue<DateTimeOffset>(key, out var last) && now - last < Window) return false;
            cache.Set(key, now, TimeSpan.FromSeconds(2));
            return true;
        }
    }
}
