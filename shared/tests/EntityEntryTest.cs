using System;
using OrmDataAccess.Shared;
using Xunit;

namespace OrmDataAccess.Shared.Tests
{
    public class EntityEntryTest
    {
        [Fact]
        public void Clone_IsIndependent()
        {
            var e = new EntityEntry { Key = "user:1001", Value = "Visvantha" };
            var c = e.Clone();
            c.Value = "Other";
            Assert.Equal("Visvantha", e.Value);
        }
    }

    public class VersionInfoTest
    {
        [Fact]
        public void FromEnvironment_SetsFields()
        {
            var v = VersionInfo.FromEnvironment("6", "4.8", "CSharp_FE6_BE4.8");
            Assert.Equal("6", v.FrontendDotnet);
            Assert.Equal("4.8", v.BackendDotnet);
            Assert.Equal("ready", v.Orm);
        }

        [Fact]
        public void ParseBranch_RejectsSameVersion() =>
            Assert.Throws<InvalidOperationException>(() => BuildContext.ParseBranch("CSharp_FE8_BE8"));

        [Fact]
        public void ParseBranch_MapsFramework48()
        {
            var ctx = BuildContext.ParseBranch("CSharp_FE6_BE4.8");
            Assert.Equal("net6.0", ctx.FrontendTfm);
            Assert.Equal("net48", ctx.BackendTfm);
            Assert.Equal(".NET 6 / .NET Framework 4.8", ctx.CustomerVersion);
        }
    }
}
