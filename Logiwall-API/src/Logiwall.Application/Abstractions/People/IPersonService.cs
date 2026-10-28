using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.People;

namespace Logiwall.Application.Abstractions.People;
public interface IPersonService
{
    Task<ServiceResponse<bool>> CreatePersonAsync(CreatePersonRequestDto requestDto);

    Task<ServiceResponse<UserProfileDto>> GetUserProfileAsync(int userId);

    Task<ServiceResponse<UserProfileDto>> UpdateUserProfileAsync(UserProfileDto requestDto);

    Task<ServiceResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequestDto requestDto);
}
