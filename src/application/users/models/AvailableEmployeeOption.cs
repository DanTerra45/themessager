namespace Mercadito.src.application.users.models
{
    public sealed class AvailableEmployeeOption
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string CiDisplay { get; set; } = string.Empty;
    }
}
