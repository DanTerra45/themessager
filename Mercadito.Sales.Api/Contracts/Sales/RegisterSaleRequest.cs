namespace Mercadito.Sales.Api.Contracts.Sales;

public sealed record RegisterSaleRequest(
    long? CustomerId,
    CreateSaleCustomerRequest? NewCustomer,
    string Channel,
    string PaymentMethod,
    IReadOnlyList<RegisterSaleLineRequest> Lines);

public sealed record CreateSaleCustomerRequest(
    string CiNit,
    string BusinessName,
    string? Phone,
    string? Email,
    string? Address);
