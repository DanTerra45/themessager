namespace ServicioVentas.Contracts.Sales;

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

public sealed record RegisterSaleLineRequest(
    long ProductId,
    string LotCode,
    int Quantity);
