using System;
using System.IO;
using System.Linq;
using HSEBank.ImportExport;
using Xunit;

namespace HSEBank.Tests
{
    public class JSONImporterTests
    {
        [Fact]
        public void ParseFile_ParsesExportedJsonArray()
        {
            var json = @"[
  {
    ""EntityType"": ""Account"",
    ""Id"": ""00000000-0000-0000-0000-000000000001"",
    ""Name"": ""A"",
    ""Balance"": 100.0
  },
  {
    ""EntityType"": ""Operation"",
    ""Id"": ""00000000-0000-0000-0000-000000000010"",
    ""Type"": ""Income"",
    ""AccountId"": ""00000000-0000-0000-0000-000000000001"",
    ""Amount"": 50.0
  }
]";
            var tmp = Path.GetTempFileName();
            File.WriteAllText(tmp, json);
            var importer = new JSONImporter();
            var items = importer.ParseFile(tmp).ToList();
            Assert.Equal(2, items.Count);
            Assert.Equal("Account", items[0]["EntityType"]);
            Assert.Equal("100.0", items[0]["Balance"]);
            Assert.Equal("Operation", items[1]["EntityType"]);
            Assert.Equal("50.0", items[1]["Amount"]);
        }
    }
}