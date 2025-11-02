using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace HSEBank.ImportExport
{
    public class JSONImporter : FileImporter
    {
        public IEnumerable<Dictionary<string, string>> ParseFile(string path)
        {
            var content = File.ReadAllText(path);
            return Parse(content);
        }

        protected override IEnumerable<Dictionary<string, string>> Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                yield break;
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var item in root.EnumerateArray())
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (item.TryGetProperty("EntityType", out var etEl))
                {
                    dict["EntityType"] = Clean(etEl);
                }
                else
                {
                    dict["EntityType"] = "Unknown";
                }

                foreach (var prop in item.EnumerateObject())
                {
                    var key = prop.Name;
                    if (string.Equals(key, "EntityType", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    dict[key] = Clean(prop.Value);
                }

                yield return dict;
            }
        }

        static string Clean(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.String:
                    var s = el.GetString() ?? string.Empty;
                    if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                    {
                        return dt.ToString("o", CultureInfo.InvariantCulture);
                    }

                    return s;

                case JsonValueKind.Number:
                    return el.GetRawText();
                case JsonValueKind.True:
                    return "true";
                case JsonValueKind.False:
                    return "false";
                case JsonValueKind.Null:
                    return string.Empty;
                default:
                    return el.GetRawText();
            }
        }

        protected override void ProcessRecord(Dictionary<string, string> record)
            => throw new NotImplementedException();
    }
}