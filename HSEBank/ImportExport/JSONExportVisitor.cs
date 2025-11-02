using System.Text.Json;
using HSEBank.Main.Entities;

namespace HSEBank.ImportExport
{
    public class JSONExportVisitor : IExportVisitor
    {
        private readonly List<Dictionary<string, object?>> _items = new();

        public void Visit(BankAccount acc)
        {
            _items.Add(new Dictionary<string, object?>
            {
                ["EntityType"] = "Account",
                ["Id"] = acc.Id,
                ["Name"] = acc.Name,
                ["Balance"] = acc.Balance
            });
        }

        public void Visit(Category cat)
        {
            _items.Add(new Dictionary<string, object?>
            {
                ["EntityType"] = "Category",
                ["Id"] = cat.Id,
                ["Name"] = cat.Name,
                ["Type"] = cat.Type.ToString()
            });
        }

        public void Visit(Operation op)
        {
            _items.Add(new Dictionary<string, object?>
            {
                ["EntityType"] = "Operation",
                ["Id"] = op.Id,
                ["Type"] = op.Type.ToString(),
                ["BankAccountId"] = op.BankAccountId,
                ["Amount"] = op.Amount,
                ["SignedAmount"] = op.SignedAmount,
                ["Date"] = op.Date.ToString("o"),
                ["CategoryId"] = op.CategoryId,
                ["Description"] = op.Description
            });
        }

        public string Result
        {
            get
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                return JsonSerializer.Serialize(_items, opts);
            }
        }
    }
}