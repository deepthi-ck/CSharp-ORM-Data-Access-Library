namespace OrmDataAccess.Shared
{
    public sealed class DataAccessResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; } = "OK";
        public string Operation { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Key { get; set; }
        public string? Value { get; set; }
        public string? Kind { get; set; }
        public string? NodeSlot { get; set; }
        public bool Found { get; set; }
        public int Revision { get; set; }
        public EntityEntry? Entry { get; set; }
    }
}
