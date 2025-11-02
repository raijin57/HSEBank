using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HSEBank.Facades;
using HSEBank.Main;
using HSEBank.Main.Entities;
using HSEBank.Repositories;
using CsvHelper;
using CsvHelper.Configuration;

namespace HSEBank.ImportExport
{
    public class CSVImporter : FileImporter
    {
        public OperationFacade? OperationFacade { get; set; }
        public IMainFactory? Factory { get; set; }
        public IRepository<BankAccount>? AccountRepository { get; set; }
        public IRepository<Category>? CategoryRepository { get; set; }
        public IRepository<Operation>? OperationRepository { get; set; }

        public IEnumerable<Dictionary<string, string>> ParseFile(string path)
        {
            var content = File.ReadAllText(path);
            return Parse(content);
        }

        protected override IEnumerable<Dictionary<string, string>> Parse(string content)
        {
            using var reader = new StringReader(content);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
            };
            using var csv = new CsvReader(reader, config);
            if (!csv.Read())
            {
                yield break;
            }

            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? Array.Empty<string>();
            while (csv.Read())
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var h in headers)
                {
                    dict[h] = csv.GetField(h) ?? string.Empty;
                }

                yield return dict;
            }
        }

        protected override void ProcessRecord(Dictionary<string, string> record)
        {
            if (!record.TryGetValue("Type", out var typeS) ||
                !record.TryGetValue("AccountId", out var accIdS) ||
                !record.TryGetValue("Amount", out var amountS) ||
                !record.TryGetValue("Date", out var dateS))
            {
                return;
            }

            if (!Enum.TryParse<CategoryType>(typeS, true, out var type))
            {
                throw new ArgumentException($"Unknown Type value: {typeS}");
            }

            if (!Guid.TryParse(accIdS, out var accId))
            {
                throw new ArgumentException($"Invalid AccountId: {accIdS}");
            }

            if (!decimal.TryParse(amountS, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                throw new ArgumentException($"Invalid Amount: {amountS}");
            }

            if (!DateTime.TryParse(dateS, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
            {
                if (!DateTime.TryParse(dateS, null, DateTimeStyles.RoundtripKind, out date))
                {
                    throw new ArgumentException($"Invalid Date: {dateS}");
                }
            }

            Guid? catId = null;
            if (record.TryGetValue("CategoryId", out var catIdS) && !string.IsNullOrWhiteSpace(catIdS))
            {
                if (Guid.TryParse(catIdS, out var parsedCat))
                {
                    catId = parsedCat;
                }
            }

            record.TryGetValue("Description", out var description);

            if (OperationFacade != null)
            {
                OperationFacade.CreateOperation(type, accId, amount, date, catId, description);
                return;
            }

            if (Factory != null && OperationRepository != null && AccountRepository != null)
            {
                var op = Factory.CreateOperation(type, accId, amount, date, catId, description);
                OperationRepository.Add(op);
                var acc = AccountRepository.Get(accId);
                if (acc != null)
                {
                    acc.ApplyAmount(op.SignedAmount);
                    AccountRepository.Update(acc);
                }

                return;
            }

            throw new InvalidOperationException();
        }
    }
}