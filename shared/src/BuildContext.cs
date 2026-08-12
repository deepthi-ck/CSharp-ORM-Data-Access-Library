using System;

namespace OrmDataAccess.Shared
{
    public sealed class BuildContext
    {
        public string Branch { get; set; } = string.Empty;
        public string FrontendVersion { get; set; } = string.Empty;
        public string BackendVersion { get; set; } = string.Empty;
        public string FrontendTfm { get; set; } = string.Empty;
        public string BackendTfm { get; set; } = string.Empty;
        public string CustomerVersion { get; set; } = string.Empty;

        public static BuildContext ParseBranch(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch)) throw new ArgumentException("Branch is required.", nameof(branch));
            var parts = branch.Split('_');
            if (parts.Length != 3 || parts[0] != "CSharp" || !parts[1].StartsWith("FE") || !parts[2].StartsWith("BE"))
                throw new ArgumentException("Invalid branch format: " + branch, nameof(branch));

            var fe = parts[1].Substring(2);
            var be = parts[2].Substring(2);
            if (string.Equals(fe, be, StringComparison.Ordinal))
                throw new InvalidOperationException("Same-version FE/BE branches are forbidden.");

            return new BuildContext
            {
                Branch = branch,
                FrontendVersion = fe,
                BackendVersion = be,
                FrontendTfm = ToTfm(fe),
                BackendTfm = ToTfm(be),
                CustomerVersion = ToCustomer(fe) + " / " + ToCustomer(be)
            };
        }

        public static string ToTfm(string version) =>
            version == "4.8" ? "net48" : "net" + version + ".0";

        public static string ToCustomer(string version) =>
            version == "4.8" ? ".NET Framework 4.8" : ".NET " + version;
    }
}
