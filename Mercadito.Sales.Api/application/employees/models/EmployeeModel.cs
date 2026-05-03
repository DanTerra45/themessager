namespace Mercadito.src.application.employees.models
{
    public class EmployeeModel
    {
        public long Id { get; set; }
        public long Ci { get; set; }
        public string? Complemento { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string PrimerApellido { get; set; } = string.Empty;
        public string? SegundoApellido { get; set; }
        public string Cargo { get; set; } = string.Empty;
        public string NumeroContacto { get; set; } = string.Empty;
    }
}
