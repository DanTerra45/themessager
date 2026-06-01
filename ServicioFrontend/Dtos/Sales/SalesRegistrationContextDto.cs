namespace ServicioFrontend.Dtos.Sales;

public sealed record SalesRegistrationContextDto(
    string NextSaleCode,
    IReadOnlyList<CustomerOptionDto> Customers,
    IReadOnlyList<SaleProductOptionDto> Products);
