using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using HSEBank.Main.Entities;

namespace HSEBank.ImportExport
{
    public class CSVExportVisitor : IExportVisitor
    {
        private readonly List<Dictionary<string, object?>> _rows = new();

        public void Visit(BankAccount acc)
        {
            var row = new Dictionary<string, object?>
            {
                ["EntityType"] = "Account",
                ["Id"] = acc.Id,
                ["Name"] = acc.Name,
                ["Balance"] = acc.Balance
            };
            _rows.Add(row);
        }

        public void Visit(Category cat)
        {
            var row = new Dictionary<string, object?>
            {
                ["EntityType"] = "Category",
                ["Id"] = cat.Id,
                ["Name"] = cat.Name,
                ["Type"] = cat.Type.ToString()
            };
            _rows.Add(row);
        }

        public void Visit(Operation op)
        {
            var row = new Dictionary<string, object?>
            {
                ["EntityType"] = "Operation",
                ["Id"] = op.Id,
                ["Type"] = op.Type.ToString(),
                ["BankAccountId"] = op.BankAccountId,
                ["Amount"] = op.Amount,
                ["SignedAmount"] = op.SignedAmount,
                ["Date"] = op.Date.ToString("o", CultureInfo.InvariantCulture),
                ["CategoryId"] = op.CategoryId,
                ["Description"] = op.Description
            };
            _rows.Add(row);
        }

        public string Result
        {
            get
            {
                if (!_rows.Any())
                {
                    return string.Empty;
                }

                var allKeys = _rows.SelectMany(d => d.Keys).Distinct().ToList();

                using var sw = new StringWriter();
                var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
                using var csv = new CsvWriter(sw, cfg);

                foreach (var k in allKeys)
                {
                    csv.WriteField(k);
                }

                csv.NextRecord();

                foreach (var r in _rows)
                {
                    foreach (var k in allKeys)
                    {
                        r.TryGetValue(k, out var val);
                        if (val == null)
                        {
                            csv.WriteField(string.Empty);
                            continue;
                        }

                        switch (val)
                        {
                            case decimal dec:
                                csv.WriteField(dec.ToString(CultureInfo.InvariantCulture));
                                break;
                            case double dbl:
                                csv.WriteField(dbl.ToString(CultureInfo.InvariantCulture));
                                break;
                            case DateTime dt:
                                csv.WriteField(dt.ToString("o", CultureInfo.InvariantCulture));
                                break;
                            default:
                                csv.WriteField(val.ToString());
                                break;
                        }
                    }

                    csv.NextRecord();
                }

                return sw.ToString();
            }
        }
    }
}