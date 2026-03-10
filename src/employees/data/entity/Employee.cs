namespace Mercadito.src.employees.data.entity
{
    public class Employee
    {
        public long Id { get; set; }
        public long Ci { get; set; }
        public string? Complemento { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string PrimerApellido { get; set; } = string.Empty;
        public string? SegundoApellido { get; set; }
        public string Rol { get; set; } = string.Empty;
        public string NumeroContacto { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}