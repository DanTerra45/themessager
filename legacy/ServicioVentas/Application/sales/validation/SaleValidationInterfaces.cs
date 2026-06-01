using Mercadito.Sales.Api.Application.Sales.Models;
using Mercadito.Sales.Api.Domain.Shared.Validation;

namespace Mercadito.Sales.Api.Application.Sales.Validation
{
    public interface IRegisterSaleValidator : IValidator<RegisterSaleDto, RegisterSaleDto>
    {
    }

    public interface IUpdateSaleValidator : IValidator<UpdateSaleDto, UpdateSaleDto>
    {
    }

    public interface ICancelSaleValidator : IValidator<CancelSaleDto, CancelSaleDto>
    {
    }
}
