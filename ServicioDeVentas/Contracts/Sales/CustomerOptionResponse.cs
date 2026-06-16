namespace ServicioVentas.Contracts.Sales;

public sealed record CustomerOptionResponse(
    long Id,
    string CiNit,
    string BusinessName)
{
    public CustomerOptionResponse(int id, string ciNit, string businessName)
        : this((long)id, ciNit, businessName)
    {
    }
}
