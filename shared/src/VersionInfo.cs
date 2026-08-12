namespace OrmDataAccess.Shared
{
    public sealed class VersionInfo
    {
        public string Application { get; set; } = "C# ORM / Data Access Library";
        public string FrontendDotnet { get; set; } = string.Empty;
        public string BackendDotnet { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string Orm { get; set; } = "ready";

        public static VersionInfo FromEnvironment(string fe, string be, string branch, string orm = "ready") =>
            new VersionInfo { FrontendDotnet = fe, BackendDotnet = be, Branch = branch, Orm = orm };
    }
}
