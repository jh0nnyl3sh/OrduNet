using OrduNet.Web.Data;
using Microsoft.Extensions.Caching.Memory;

namespace OrduNet.Web.Services
{
    public class SiteSettingsService : ISiteSettingsService
    {
        private readonly OrduNetDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "SiteSettingsCache";

        public SiteSettingsService(OrduNetDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public Dictionary<string, string> GetAllSettings()
        {
            if (!_cache.TryGetValue(CacheKey, out Dictionary<string, string>? settings))
            {
                settings = _context.SiteSettings
                    .ToDictionary(s => s.Key, s => s.Value);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(60));

                _cache.Set(CacheKey, settings, cacheEntryOptions);
            }

            return settings ?? new Dictionary<string, string>();
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            var settings = GetAllSettings();
            if (settings.TryGetValue(key, out var val))
            {
                return val;
            }
            return defaultValue;
        }
    }
}
