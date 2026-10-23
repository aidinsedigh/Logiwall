using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.People;

namespace Logiwall.Application.Abstractions.People;
public interface ICustomerService
{
    Task<ServiceResponse<bool>> CreateCustomerContactAsync(CreateCustomerContactRequestDto requestDto);

    Task<ServiceResponse<bool>> UpdateCustomerContactAsync(UpdateCustomerContactRequestDto requestDto);

    Task<ServiceResponse<IEnumerable<CustomerContactResponseDto>>> GetCustomerContactListAsync(GetCustomerContactListRequestDto requestDto);

    Task<ServiceResponse<CustomerContactResponseDto>> GetCustomerContactByIdAsync(int id);

}
