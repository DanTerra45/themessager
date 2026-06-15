using Domain.Dto.Response;
using Domain.Entities;

namespace Domain.Mapper
{
    public static class CustomerMapper
    {
        public static CustomerResponseDto ToResponseDto(this Customer customer)
        {
            return new CustomerResponseDto(
                Id: customer.Id,
                Ci: customer.Ci,
                Complement: customer.Complement ?? string.Empty,
                RazonSocial: customer.RazonSocial ?? string.Empty
            );
        }
    }
}