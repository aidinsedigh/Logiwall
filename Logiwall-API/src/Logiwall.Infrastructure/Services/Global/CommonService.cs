using Logiwall.Application.Abstractions.Global;
using Logiwall.Infrastructure.Data;
using Logiwall.Shared.Dtos.Global;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.Infrastructure.Services.Global;
public class CommonService(AppDbContext appDbContext) : ICommonService
{
    public async Task<ServiceResponse<List<GetStaticDataResponseDto>>> GetStaticDatasAsync(GetStaticDataRequestDto requestDto)
    {
        var query = appDbContext.StaticDatas.Where(t => t.Type == requestDto.Type
            )
            .Select(t => new GetStaticDataResponseDto
            {
                Id = t.Id,
                Title = t.Title,
            });

        if (requestDto.SearchBy is not null)
        {
            query = query.Where(t => t.Title.Contains(requestDto.SearchBy));
        }
        var staticDatas = await query.ToListAsync();
        return new ServiceResponse<List<GetStaticDataResponseDto>>
        {
            Data = staticDatas,
            Succuss = true,
            Message = string.Empty
        };
    }
}
