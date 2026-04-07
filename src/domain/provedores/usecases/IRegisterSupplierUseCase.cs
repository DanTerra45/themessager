using Mercadito.src.domain.provedores.dto;
using Mercadito.src.domain.provedores.repository;
using Mercadito.src.domain.provedores.validator;
using Mercadito.src.shared.domain.validator;

namespace Mercadito.src.application.suppliers.use_cases
{
    public interface IRegisterSupplierUseCase
    {
        public Task<Result<long>> ExecuteAsync(CreateSupplierDto dto, CancellationToken cancellationToken=default);
    }
}