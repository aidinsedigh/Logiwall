using Logiwall.Application.Abstractions.Global;
using Logiwall.Server.Data;
using Logiwall.Shared.Dtos.Global;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.Infrastructure.Services.Global;
public class AppSettingService : IAppSettingService
{
    private readonly AppDbContext dbContext;

    public AppSettingService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<AppSettingsDto> GetAllSettingsAsync()
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
