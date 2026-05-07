namespace Mercadito.Frontend.Dtos.Sales;

public sealed record CancelSaleRequestDto(
    long SaleId,
    string Reason);
