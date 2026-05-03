using Mercadito.src.application.sales.models;
using Mercadito.src.domain.shared.validation;

namespace Mercadito.src.application.sales.validation
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
