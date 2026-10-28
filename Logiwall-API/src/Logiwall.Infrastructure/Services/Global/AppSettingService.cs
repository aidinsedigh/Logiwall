using Logiwall.Application.Abstractions.Global;
using Logiwall.Infrastructure.Data;
using Logiwall.Shared.Dtos.Global;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Logiwall.Infrastructure.Services.Global;
public class AppSettingService : IAppSettingService
{
    private const string CacheKey = "app-settings";
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private readonly AppDbContext dbContext;
    private readonly IMemoryCache cache;

    public AppSettingService(AppDbContext dbContext, IMemoryCache cache)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    /// <remarks>Cached for 5 minutes; DB edits take effect after expiry or restart.</remarks>
    public async Task<AppSettingsDto> GetAllSettingsAsync()
    {
        return (await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheLifetime;
            return await LoadSettingsAsync();
        }))!;
    }

    private async Task<AppSettingsDto> LoadSettingsAsync()
    {
        var settings = await dbContext.AppSettings.ToListAsync();
        var dto = new AppSettingsDto();

        var dtoType = typeof(AppSettingsDto);

        foreach (var setting in settings)
        {
            var property = dtoType.GetProperty(setting.KeyName);

            if (property != null && property.CanWrite)
            {
                property.SetValue(dto, setting.Value); //toda can have different type value
            }
        }

        return dto;
    }
}
