using System.Globalization;
using System.Text;
using HSEBank.Commands;
using HSEBank.Facades;
using HSEBank.ImportExport;
using HSEBank.Main;
using HSEBank.Main.Entities;
using HSEBank.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace HSEBank
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMainFactory, MainFactory>();
            services.AddSingleton<InMemoryRepository<BankAccount>>();
            services.AddSingleton<InMemoryRepository<Category>>();
            services.AddSingleton<InMemoryRepository<Operation>>();
            services.AddSingleton<IRepository<BankAccount>>(sp =>
                new CachingProxyRepository<BankAccount>(sp.GetRequiredService<InMemoryRepository<BankAccount>>()));
            services.AddSingleton<IRepository<Category>>(sp =>
                new CachingProxyRepository<Category>(sp.GetRequiredService<InMemoryRepository<Category>>()));
            services.AddSingleton<IRepository<Operation>>(sp =>
                new CachingProxyRepository<Operation>(sp.GetRequiredService<InMemoryRepository<Operation>>()));
            services.AddSingleton<BankAccountFacade>();
            services.AddSingleton<CategoryFacade>();
            services.AddSingleton<OperationFacade>();
            services.AddSingleton<AnalyticsFacade>();
            services.AddSingleton<JSONImporter>();
            services.AddSingleton<CSVImporter>();
            services.AddTransient<JSONExportVisitor>();
            services.AddTransient<CSVExportVisitor>();
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IMainFactory>();
            var accRepo = provider.GetRequiredService<IRepository<BankAccount>>();
            var catRepo = provider.GetRequiredService<IRepository<Category>>();
            var opRepo = provider.GetRequiredService<IRepository<Operation>>();
            var accFacade = provider.GetRequiredService<BankAccountFacade>();
            var catFacade = provider.GetRequiredService<CategoryFacade>();
            var opFacade = provider.GetRequiredService<OperationFacade>();
            var analytics = provider.GetRequiredService<AnalyticsFacade>();
            var jsonImporter = provider.GetRequiredService<JSONImporter>();
            var csvImporter = provider.GetRequiredService<CSVImporter>();
            var history = new Stack<ICommand>();
            AnsiConsole.MarkupLine("[bold green]FinancialApp — меню[/]");
            while (true)
            {
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Выберите действие")
                    .AddChoices(new[]
                    {
                        "Создать счёт",
                        "Список счетов",
                        "Удалить счёт",
                        "Создать категорию",
                        "Список категорий",
                        "Удалить категорию",
                        "Добавить операцию",
                        "Список операций",
                        "Удалить операцию",
                        "Аналитика (дельта / по категориям)",
                        "Импорт (JSON файл)",
                        "Импорт (CSV файл)",
                        "Экспорт в JSON (файл)",
                        "Экспорт CSV (файл)",
                        "Отменить последнюю команду (Undo)",
                        "Выход"
                    }));
                try
                {
                    switch (choice)
                    {
                        case "Создать счёт":
                            CreateAccountInteractive(factory, accRepo, history);
                            break;
                        case "Список счетов":
                            ListAccounts(accRepo);
                            break;
                        case "Удалить счёт":
                            DeleteAccountInteractive(accRepo, opRepo, history);
                            break;
                        case "Создать категорию":
                            CreateCategoryInteractive(catFacade);
                            break;
                        case "Список категорий":
                            ListCategories(catRepo);
                            break;
                        case "Удалить категорию":
                            DeleteCategoryInteractive(catRepo, history);
                            break;
                        case "Добавить операцию":
                            AddOperationInteractive(factory, accRepo, catRepo, opFacade, history);
                            break;
                        case "Список операций":
                            ListOperations(opRepo, accRepo, catRepo);
                            break;
                        case "Удалить операцию":
                            DeleteOperationInteractive(opRepo, accRepo, history);
                            break;
                        case "Аналитика (дельта / по категориям)":
                            AnalyticsInteractive(analytics);
                            break;
                        case "Импорт (JSON файл)":
                            ImportJsonInteractive(jsonImporter, factory, accRepo, catRepo, opRepo, opFacade, history);
                            break;
                        case "Импорт (CSV файл)":
                            ImportCsvInteractive(csvImporter, factory, accRepo, catRepo, opRepo, opFacade, history);
                            break;
                        case "Экспорт в JSON (файл)":
                            ExportJsonInteractive(provider, accRepo, catRepo, opRepo);
                            break;
                        case "Экспорт CSV (файл)":
                            ExportCsvInteractive(provider, accRepo, catRepo, opRepo);
                            break;
                        case "Отменить последнюю команду (Undo)":
                            UndoLast(history);
                            break;
                        case "Выход":
                            return;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                }

                AnsiConsole.WriteLine();
            }
        }

        static void CreateAccountInteractive(IMainFactory factory, IRepository<BankAccount> repo,
            Stack<ICommand> history)
        {
            var name = AnsiConsole.Ask<string>("Введите имя счёта (или 'c' для отмены):");
            if (string.Equals(name, "c", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[grey]Отменено[/]");
                return;
            }

            while (true)
            {
                var input = AnsiConsole.Ask<string>("Начальный баланс (число) или 'c' для отмены:");
                if (string.Equals(input, "c", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("[grey]Создание отменено[/]");
                    return;
                }

                if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var initial))
                {
                    if (initial < 0)
                    {
                        AnsiConsole.MarkupLine("[red]Баланс не может быть отрицательным[/]");
                        continue;
                    }

                    var acc = factory.CreateBankAccount(name, initial);
                    var cmd = new CreateAccountCommand(acc, repo);
                    var timed = new CommandTimerDecorator(cmd,
                        ts => AnsiConsole.MarkupLine($"[green]Создание счёта заняло[/] {ts.TotalMilliseconds} ms"));
                    timed.Execute();
                    history.Push(timed);
                    AnsiConsole.MarkupLine($"[yellow]Счёт создан:[/] {acc.Id} — {acc.Name} (баланс {acc.Balance})");
                    return;
                }

                AnsiConsole.MarkupLine("[red]Неверный ввод — введите число или 'c'[/]");
            }
        }

        static void ListAccounts(IRepository<BankAccount> repo)
        {
            var list = repo.GetAll().ToList();
            if (!list.Any())
            {
                AnsiConsole.MarkupLine("[grey]Счета отсутствуют[/]");
                return;
            }

            var table = new Table().AddColumn("Id").AddColumn("Name").AddColumn("Balance");
            foreach (var a in list)
                table.AddRow(a.Id.ToString(), a.Name, a.Balance.ToString(CultureInfo.InvariantCulture));
            AnsiConsole.Write(table);
        }

        static void DeleteAccountInteractive(IRepository<BankAccount> accRepo, IRepository<Operation> opRepo,
            Stack<ICommand> history)
        {
            var accounts = accRepo.GetAll().ToList();
            if (!accounts.Any())
            {
                AnsiConsole.MarkupLine("[grey]Счета отсутствуют[/]");
                return;
            }

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Выберите счёт для удаления:")
                .AddChoices(accounts.Select(a => $"{a.Id} | {a.Name}")));
            var id = Guid.Parse(choice.Split('|')[0].Trim());
            var hasOps = opRepo.GetAll().Any(o => o.BankAccountId == id);
            if (hasOps)
            {
                AnsiConsole.MarkupLine("[red]Нельзя удалить счёт с операциями — удалите операции сначала[/]");
                return;
            }

            var cmd = new DeleteAccountCommand(accRepo, opRepo, id);
            cmd.Execute();
            history.Push(cmd);
            AnsiConsole.MarkupLine("[yellow]Счёт удалён[/]");
        }

        static void CreateCategoryInteractive(CategoryFacade catFacade)
        {
            var type = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Тип категории:")
                .AddChoices(new[] { "Прибыль", "Расходы" }));
            var name = AnsiConsole.Ask<string>("Название категории (или 'c' для отмены):");
            if (string.Equals(name, "c", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[grey]Отменено[/]");
                return;
            }

            var ctype = type == "Прибыль" ? CategoryType.Прибыль : CategoryType.Расходы;
            var cat = catFacade.Create(ctype, name);
            AnsiConsole.MarkupLine($"[yellow]Категория создана:[/] {cat.Id} — {cat.Name} ({cat.Type})");
        }

        static void ListCategories(IRepository<Category> repo)
        {
            var list = repo.GetAll().ToList();
            if (!list.Any())
            {
                AnsiConsole.MarkupLine("[grey]Категории отсутствуют[/]");
                return;
            }

            var table = new Table().AddColumn("Id").AddColumn("Name").AddColumn("Type");
            foreach (var c in list)
            {
                table.AddRow(c.Id.ToString(), c.Name, c.Type.ToString());
            }

            AnsiConsole.Write(table);
        }

        static void DeleteCategoryInteractive(IRepository<Category> repo, Stack<ICommand> history)
        {
            var list = repo.GetAll().ToList();
            if (!list.Any())
            {
                AnsiConsole.MarkupLine("[grey]Категории отсутствуют[/]");
                return;
            }

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Выберите категорию для удаления:")
                .AddChoices(list.Select(c => $"{c.Id} | {c.Name} ({c.Type})")));
            var id = Guid.Parse(choice.Split('|')[0].Trim());
            var cmd = new DeleteCategoryCommand(repo, id);
            cmd.Execute();
            history.Push(cmd);
            AnsiConsole.MarkupLine("[yellow]Категория удалена[/]");
        }

        static void AddOperationInteractive(IMainFactory factory, IRepository<BankAccount> accRepo,
            IRepository<Category> catRepo, OperationFacade opFacade, Stack<ICommand> history)
        {
            var accounts = accRepo.GetAll().ToList();
            if (!accounts.Any())
            {
                AnsiConsole.MarkupLine("[red]Нет доступных счетов — создайте сначала счёт[/]");
                return;
            }

            var accountChoice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Выберите счёт:")
                .AddChoices(accounts.Select(a => $"{a.Id} | {a.Name}")));
            var accountId = Guid.Parse(accountChoice.Split('|')[0].Trim());
            var typeChoice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Тип операции:")
                .AddChoices(new[] { "Прибыль", "Расходы" }));
            var type = typeChoice == "Прибыль" ? CategoryType.Прибыль : CategoryType.Расходы;
            var categories = catRepo.GetAll().ToList();
            Guid? categoryId = null;
            if (categories.Any())
            {
                var choices = new List<string> { "Без категории" };
                choices.AddRange(categories.Select(c => $"{c.Id} | {c.Name} ({c.Type})"));
                var sel = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Выберите категорию (или Без категории):").AddChoices(choices));
                if (sel != "Без категории") categoryId = Guid.Parse(sel.Split('|')[0].Trim());
            }
            else
            {
                AnsiConsole.MarkupLine("[grey]Категории отсутствуют — операция будет без категории[/]");
            }

            decimal amount;
            while (true)
            {
                var input = AnsiConsole.Ask<string>("Сумма (введите 'c' для отмены):");
                if (string.Equals(input, "c", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("[grey]Отменено[/]");
                    return;
                }

                if (!decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
                {
                    AnsiConsole.MarkupLine("[red]Неверный формат числа — введите снова или 'c'[/]");
                    continue;
                }

                if (amount <= 0)
                {
                    AnsiConsole.MarkupLine("[red]Сумма должна быть > 0 — введите снова или 'c'[/]");
                    continue;
                }

                break;
            }

            var dateStr = AnsiConsole.Ask<string>("Дата операции (Enter = сейчас, формат YYYY-MM-DD):");
            DateTime date = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(dateStr))
            {
                if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    if (!DateTime.TryParse(dateStr, out date)) date = DateTime.Now;
                }
            }

            var desc = AnsiConsole.Ask<string>("Описание (необязательно):");
            var cmd = new AddOperationCommand(opFacade, type, accountId, amount, date, categoryId,
                string.IsNullOrWhiteSpace(desc) ? null : desc);
            var timed = new CommandTimerDecorator(cmd,
                ts => AnsiConsole.MarkupLine($"[green]Добавление операции заняло[/] {ts.TotalMilliseconds} ms"));
            timed.Execute();
            history.Push(timed);
            AnsiConsole.MarkupLine("[yellow]Операция добавлена[/]");
        }

        static void ListOperations(IRepository<Operation> opRepo, IRepository<BankAccount> accRepo,
            IRepository<Category> catRepo)
        {
            var ops = opRepo.GetAll().OrderByDescending(o => o.Date).ToList();
            if (!ops.Any())
            {
                AnsiConsole.MarkupLine("[grey]Операции отсутствуют[/]");
                return;
            }

            var table = new Table().AddColumn("Id").AddColumn("Date").AddColumn("Type").AddColumn("Amount")
                .AddColumn("Signed").AddColumn("Account").AddColumn("Category").AddColumn("Description");
            foreach (var o in ops)
            {
                var acc = accRepo.Get(o.BankAccountId);
                var accName = acc != null ? acc.Name : "(удалённый счёт)";
                var cat = o.CategoryId.HasValue ? catRepo.Get(o.CategoryId.Value) : null;
                var catName = cat != null ? cat.Name : "(нет)";
                table.AddRow(o.Id.ToString(), o.Date.ToString("yyyy-MM-dd HH:mm"), o.Type.ToString(),
                    o.Amount.ToString(CultureInfo.InvariantCulture),
                    o.SignedAmount.ToString(CultureInfo.InvariantCulture), accName, catName, o.Description ?? "");
            }

            AnsiConsole.Write(table);
        }

        static void DeleteOperationInteractive(IRepository<Operation> opRepo, IRepository<BankAccount> accRepo,
            Stack<ICommand> history)
        {
            var ops = opRepo.GetAll().ToList();
            if (!ops.Any())
            {
                AnsiConsole.MarkupLine("[grey]Операции отсутствуют[/]");
                return;
            }

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Выберите операцию для удаления:")
                .AddChoices(ops.Select(o => $"{o.Id} | {o.Date:yyyy-MM-dd} | {o.Amount}")));
            var id = Guid.Parse(choice.Split('|')[0].Trim());
            var cmd = new DeleteOperationCommand(opRepo, accRepo, id);
            cmd.Execute();
            history.Push(cmd);
            AnsiConsole.MarkupLine("[yellow]Операция удалена[/]");
        }

        static void AnalyticsInteractive(AnalyticsFacade analytics)
        {
            var fromStr = AnsiConsole.Ask<string>("Дата начала (YYYY-MM-DD, Enter = 30 дней назад):");
            var toStr = AnsiConsole.Ask<string>("Дата конца (YYYY-MM-DD, Enter = сегодня):");
            DateTime from = DateTime.Today.AddDays(-30), to = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(fromStr) && DateTime.TryParse(fromStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var pf)) from = pf;
            if (!string.IsNullOrWhiteSpace(toStr) &&
                DateTime.TryParse(toStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var pt)) to = pt;
            var delta = analytics.BalanceDelta(from, to);
            AnsiConsole.MarkupLine(
                $"[bold]Дельта баланса[/] с {from:yyyy-MM-dd} по {to:yyyy-MM-dd}: [green]{delta}[/]");
            var grouped = analytics.GroupByCategory(from, to);
            if (grouped.Any())
            {
                var table = new Table().AddColumn("Category").AddColumn("Amount");
                foreach (var kv in grouped) table.AddRow(kv.Key, kv.Value.ToString(CultureInfo.InvariantCulture));
                AnsiConsole.Write(table);
            }
            else AnsiConsole.MarkupLine("[grey]Нет операций за период[/]");
        }

        static void ImportJsonInteractive(JSONImporter importer, IMainFactory factory, IRepository<BankAccount> accRepo,
            IRepository<Category> catRepo, IRepository<Operation> opRepo, OperationFacade opFacade,
            Stack<ICommand> history)
        {
            var path = AnsiConsole.Ask<string>("Путь к JSON-файлу для импорта (или 'c' для отмены):");
            if (string.Equals(path, "c", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[grey]Отменено[/]");
                return;
            }

            if (!File.Exists(path))
            {
                AnsiConsole.MarkupLine("[red]Файл не найден[/]");
                return;
            }

            var records = importer.ParseFile(path).ToList();
            if (!records.Any())
            {
                AnsiConsole.MarkupLine("[grey]Файл пуст или некорректен[/]");
                return;
            }

            var composite = new CompositeCommand();
            var accountMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            var categoryMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var rec in records.Where(r =>
                         r.TryGetValue("EntityType", out var et) &&
                         string.Equals(et, "Account", StringComparison.OrdinalIgnoreCase)))
            {
                if (!rec.TryGetValue("Id", out var oldIdStr)) continue;
                if (!Guid.TryParse(oldIdStr, out var oldId)) continue;
                var existing = accRepo.Get(oldId);
                if (existing != null)
                {
                    accountMap[oldIdStr] = existing.Id;
                    continue;
                }

                rec.TryGetValue("Name", out var name);
                decimal balance = 0;
                if (rec.TryGetValue("Balance", out var balStr))
                {
                    decimal.TryParse(balStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out balance);
                }

                var newAcc = factory.CreateBankAccount(name ?? "Imported Account", balance);
                var cmd = new CreateAccountCommand(newAcc, accRepo);
                cmd.Execute();
                composite.Add(cmd);
                accountMap[oldIdStr] = newAcc.Id;
            }

            foreach (var rec in records.Where(r =>
                         r.TryGetValue("EntityType", out var et) &&
                         string.Equals(et, "Category", StringComparison.OrdinalIgnoreCase)))
            {
                if (!rec.TryGetValue("Id", out var oldIdStr)) continue;
                if (!Guid.TryParse(oldIdStr, out var oldId)) continue;
                var existing = catRepo.Get(oldId);
                if (existing != null)
                {
                    categoryMap[oldIdStr] = existing.Id;
                    continue;
                }

                rec.TryGetValue("Name", out var name);
                var type = CategoryType.Расходы;
                if (rec.TryGetValue("Type", out var tstr) &&
                    Enum.TryParse<CategoryType>(tstr, true, out var t))
                {
                    type = t;
                }

                var newCat =
                    new Category(Guid.NewGuid(), type, name ?? "Imported Category");
                var cmd = new CreateCategoryCommand(newCat, catRepo);
                cmd.Execute();
                composite.Add(cmd);
                categoryMap[oldIdStr] = newCat.Id;
            }

            int importedOps = 0;
            foreach (var rec in records.Where(r =>
                         r.TryGetValue("EntityType", out var et) &&
                         string.Equals(et, "Operation", StringComparison.OrdinalIgnoreCase)))
            {
                bool ok = TryBuildOperationFromRecord(rec, factory, out var ctype, out var accountId, out var amount,
                    out var date, out var catGuid, out var desc);
                if (!ok)
                {
                    if (!rec.TryGetValue("BankAccountId", out var accStr) &&
                        !rec.TryGetValue("AccountId", out accStr)) continue;
                    string accKey = accStr.Trim();
                    if (accountMap.ContainsKey(accKey))
                    {
                        accountId = accountMap[accKey];
                    }
                    else if (Guid.TryParse(accKey, out var aid) && accRepo.Get(aid) != null)
                    {
                        accountId = aid;
                    }
                    else continue;

                    if (!rec.TryGetValue("Amount", out var amountStr))
                    {
                        if (!rec.TryGetValue("SignedAmount", out amountStr))
                        {
                            continue;
                        }
                    }

                    if (!decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out amount))
                    {
                        continue;
                    }

                    if (!rec.TryGetValue("Type", out var typeStr))
                    {
                        if (rec.TryGetValue("SignedAmount", out var signedStr) && decimal.TryParse(signedStr,
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var sVal))
                        {
                            ctype = sVal >= 0 ? CategoryType.Прибыль : CategoryType.Расходы;
                        }
                        else continue;
                    }
                    else Enum.TryParse<CategoryType>(typeStr, true, out ctype);

                    date = DateTime.Now;
                    if (rec.TryGetValue("Date", out var dstr) && !string.IsNullOrWhiteSpace(dstr) &&
                        !DateTime.TryParse(dstr, out date))
                    {
                        DateTime.TryParse(dstr, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out date);
                    }

                    catGuid = null;
                    if (rec.TryGetValue("CategoryId", out var catStr))
                    {
                        var ck = catStr.Trim();
                        if (categoryMap.ContainsKey(ck))
                        {
                            catGuid = categoryMap[ck];
                        }
                        else if (Guid.TryParse(ck, out var realCat) && catRepo.Get(realCat) != null)
                        {
                            catGuid = realCat;
                        }
                    }

                    rec.TryGetValue("Description", out desc);
                }
                else
                {
                    var sAccKey = recordGetString(rec, "AccountId");
                    if (sAccKey != null && accountMap.TryGetValue(sAccKey, out var mapped))
                    {
                        accountId = mapped;
                    }
                    else if (accountMap.TryGetValue(accountId.ToString(), out mapped))
                    {
                        accountId = mapped;
                    }
                    else if (accRepo.Get(accountId) == null)
                    {
                        continue;
                    }

                    if (catGuid.HasValue)
                    {
                        var sCatKey = recordGetString(rec, "CategoryId");
                        if (sCatKey != null && categoryMap.TryGetValue(sCatKey, out var mappedC))
                        {
                            catGuid = mappedC;
                        }
                        else if (categoryMap.TryGetValue(catGuid.Value.ToString(), out mappedC))
                        {
                            catGuid = mappedC;
                        }
                        else if (catRepo.Get(catGuid.Value) == null)
                        {
                            catGuid = null;
                        }
                    }
                }

                var opCmd = new AddOperationCommand(opFacade, ctype, accountId, amount, date, catGuid, desc);
                opCmd.Execute();
                composite.Add(opCmd);
                importedOps++;
            }

            if (composite.Any())
            {
                history.Push(composite);
                AnsiConsole.MarkupLine(
                    $"[green]Импорт JSON завершён[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[grey]Ни одна запись не была импортирована[/]");
            }

            static string? recordGetString(Dictionary<string, string> rec, string key)
            {
                if (rec.TryGetValue(key, out var v))
                {
                    return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
                }

                return null;
            }
        }

        static void ImportCsvInteractive(CSVImporter importer, IMainFactory factory, IRepository<BankAccount> accRepo,
            IRepository<Category> catRepo, IRepository<Operation> opRepo, OperationFacade opFacade,
            Stack<ICommand> history)
        {
            var path = AnsiConsole.Ask<string>("Путь к CSV-файлу для импорта (или 'c' для отмены):");
            if (string.Equals(path, "c", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[grey]Отменено[/]");
                return;
            }

            if (!File.Exists(path))
            {
                AnsiConsole.MarkupLine("[red]Файл не найден[/]");
                return;
            }

            var records = importer.ParseFile(path).ToList();
            if (!records.Any())
            {
                AnsiConsole.MarkupLine("[grey]Нет записей в файле[/]");
                return;
            }

            var composite = new CompositeCommand();
            var accountMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            var categoryMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var rec in records.Where(r =>
                         r.TryGetValue("EntityType", out var et) &&
                         string.Equals(et, "Account", StringComparison.OrdinalIgnoreCase)))
            {
                if (!rec.TryGetValue("Id", out var oldIdStr))
                {
                    continue;
                }

                var oldKey = oldIdStr.Trim();
                if (Guid.TryParse(oldKey, out var oldGuid))
                {
                    var existing = accRepo.Get(oldGuid);
                    if (existing != null)
                    {
                        accountMap[oldKey] = existing.Id;
                        continue;
                    }
                }

                rec.TryGetValue("Name", out var name);
                decimal balance = 0;
                if (rec.TryGetValue("Balance", out var balStr))
                {
                    decimal.TryParse(balStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out balance);
                }

                var newAcc = factory.CreateBankAccount(name ?? "Imported Account", balance);
                var cmd = new CreateAccountCommand(newAcc, accRepo);
                cmd.Execute();
                composite.Add(cmd);
                accountMap[oldKey] = newAcc.Id;
            }

            foreach (var rec in records.Where(r =>
                         r.TryGetValue("EntityType", out var et) &&
                         string.Equals(et, "Category", StringComparison.OrdinalIgnoreCase)))
            {
                if (!rec.TryGetValue("Id", out var oldIdStr))
                {
                    continue;
                }

                var oldKey = oldIdStr.Trim();
                if (Guid.TryParse(oldKey, out var oldGuid))
                {
                    var existing = catRepo.Get(oldGuid);
                    if (existing != null)
                    {
                        categoryMap[oldKey] = existing.Id;
                        continue;
                    }
                }

                rec.TryGetValue("Name", out var name);
                var type = CategoryType.Расходы;
                if (rec.TryGetValue("Type", out var tstr) && Enum.TryParse<CategoryType>(tstr, true, out var parsedT))
                {
                    type = parsedT;
                }

                var newCat =
                    new Category(Guid.NewGuid(), type,
                        name ?? "Imported Category");
                var cmd = new CreateCategoryCommand(newCat, catRepo);
                cmd.Execute();
                composite.Add(cmd);
                categoryMap[oldKey] = newCat.Id;
            }

            int importedOps = 0;
            foreach (var rec in records.Where(r =>
                         r.TryGetValue("EntityType", out var et) &&
                         string.Equals(et, "Operation", StringComparison.OrdinalIgnoreCase)))
            {
                Guid acctId;
                if (rec.TryGetValue("BankAccountId", out var accStr) || rec.TryGetValue("AccountId", out accStr))
                {
                    var key = (accStr ?? "").Trim();
                    if (accountMap.TryGetValue(key, out var mapped))
                    {
                        acctId = mapped;
                    }
                    else if (Guid.TryParse(key, out var parsed) && accRepo.Get(parsed) != null)
                    {
                        acctId = parsed;
                    }
                    else continue;
                }
                else continue;

                string amountRaw = "";
                if (rec.TryGetValue("Amount", out var a1) && !string.IsNullOrWhiteSpace(a1))
                {
                    amountRaw = a1;
                }
                else if (rec.TryGetValue("SignedAmount", out var sa) && !string.IsNullOrWhiteSpace(sa))
                {
                    if (decimal.TryParse(sa, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var sval))
                    {
                        amountRaw = Math.Abs(sval).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else amountRaw = sa;
                }

                if (!decimal.TryParse(amountRaw, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var amount))
                {
                    continue;
                }

                if (amount <= 0)
                {
                    continue;
                }

                CategoryType ctype = CategoryType.Расходы;
                if (rec.TryGetValue("Type", out var tstr) && !string.IsNullOrWhiteSpace(tstr))
                {
                    if (!Enum.TryParse<CategoryType>(tstr, true, out ctype))
                    {
                        var low = tstr.ToLowerInvariant();
                        ctype = low.StartsWith("inc") ? CategoryType.Прибыль : CategoryType.Расходы;
                    }
                }
                else if (rec.TryGetValue("SignedAmount", out var signedStr) && decimal.TryParse(signedStr,
                             System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture,
                             out var sdec))
                {
                    ctype = sdec >= 0 ? CategoryType.Прибыль : CategoryType.Расходы;
                }

                DateTime date = DateTime.Now;
                if (rec.TryGetValue("Date", out var dateStr) && !string.IsNullOrWhiteSpace(dateStr))
                {
                    if (!DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out date))
                        DateTime.TryParse(dateStr, out date);
                }

                Guid? catId = null;
                if (rec.TryGetValue("CategoryId", out var catStr) && !string.IsNullOrWhiteSpace(catStr))
                {
                    var key = catStr.Trim();
                    if (categoryMap.TryGetValue(key, out var mapped))
                    {
                        catId = mapped;
                    }
                    else if (Guid.TryParse(key, out var parsed) && catRepo.Get(parsed) != null)
                    {
                        catId = parsed;
                    }
                    else
                    {
                        catId = null;
                    }
                }

                rec.TryGetValue("Description", out var desc);
                var opCmd = new AddOperationCommand(opFacade, ctype, acctId, amount, date, catId, desc);
                opCmd.Execute();
                composite.Add(opCmd);
                importedOps++;
            }

            if (composite.Any())
            {
                history.Push(composite);
                AnsiConsole.MarkupLine(
                    $"[green]CSV импорт завершён[/]");
            }
            else AnsiConsole.MarkupLine("[grey]Ни одна запись не была импортирована[/]");
        }

        static bool TryBuildOperationFromRecord(Dictionary<string, string> record, IMainFactory factory,
            out CategoryType type, out Guid accountId, out decimal amount, out DateTime date, out Guid? categoryId,
            out string? description)
        {
            type = CategoryType.Расходы;
            accountId = Guid.Empty;
            amount = 0;
            date = DateTime.Now;
            categoryId = null;
            description = null;

            static string Clean(string? s)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return string.Empty;
                }

                var sb = new System.Text.StringBuilder(s.Length);
                foreach (var ch in s)
                {
                    var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (cat == System.Globalization.UnicodeCategory.Control ||
                        cat == System.Globalization.UnicodeCategory.Format)
                    {
                        continue;
                    }

                    sb.Append(ch);
                }

                return sb.ToString().Trim().Trim('"').Trim();
            }

            try
            {
                if (!record.TryGetValue("Type", out var rawType))
                {
                    return false;
                }

                var typeStr = Clean(rawType).ToLowerInvariant();
                if (typeStr == "прибыль" )
                {
                    type = CategoryType.Прибыль;
                }
                else if (typeStr == "расходы")
                {
                    type = CategoryType.Расходы;
                }
                else
                {
                    return false;
                }

                if (!record.TryGetValue("AccountId", out var rawAcc))
                {
                    return false;
                }

                var accStr = Clean(rawAcc);
                if (!Guid.TryParse(accStr, out accountId))
                {
                    return false;
                }

                if (!record.TryGetValue("Amount", out var rawAmount))
                {
                    return false;
                }

                var amtStr = Clean(rawAmount);
                if (!decimal.TryParse(amtStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out amount))
                {
                    return false;
                }

                if (amount <= 0)
                {
                    return false;
                }

                date = DateTime.Now;
                if (record.TryGetValue("Date", out var rawDate) && !string.IsNullOrWhiteSpace(rawDate))
                {
                    var dateStr = Clean(rawDate);
                    if (!DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out date))
                    {
                        DateTime.TryParse(dateStr, out date);
                    }
                }

                categoryId = null;
                if (record.TryGetValue("CategoryId", out var rawCat) && !string.IsNullOrWhiteSpace(rawCat))
                {
                    var catStr = Clean(rawCat);
                    if (Guid.TryParse(catStr, out var gid))
                    {
                        categoryId = gid;
                    }
                    else
                    {
                        categoryId = null;
                    }
                }

                record.TryGetValue("Description", out var rawDesc);
                var desc = Clean(rawDesc);
                description = string.IsNullOrWhiteSpace(desc) ? null : desc;
                return true;
            }
            catch
            {
                return false;
            }
        }

        static void ExportJsonInteractive(ServiceProvider provider, IRepository<BankAccount> accRepo,
            IRepository<Category> catRepo, IRepository<Operation> opRepo)
        {
            var path = AnsiConsole.Ask<string>("Путь для записи JSON (перезапишет файл) или 'c' для отмены:");
            if (string.Equals(path, "c", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[grey]Отменено[/]");
                return;
            }

            var exporter = provider.GetRequiredService<JSONExportVisitor>();
            foreach (var a in accRepo.GetAll())
            {
                a.Accept(exporter);
            }

            foreach (var c in catRepo.GetAll())
            {
                c.Accept(exporter);
            }

            foreach (var o in opRepo.GetAll())
            {
                o.Accept(exporter);
            }

            File.WriteAllText(path, exporter.Result);
            AnsiConsole.MarkupLine($"[green]Экспорт сохранён в {path}[/]");
        }

        static void ExportCsvInteractive(ServiceProvider provider, IRepository<BankAccount> accRepo,
            IRepository<Category> catRepo, IRepository<Operation> opRepo)
        {
            var path = AnsiConsole.Ask<string>("Путь для записи CSV (перезапишет файл) или 'c' для отмены:");
            if (string.Equals(path, "c", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[grey]Отменено[/]");
                return;
            }

            var exporter = provider.GetRequiredService<CSVExportVisitor>();
            foreach (var a in accRepo.GetAll()) a.Accept(exporter);
            foreach (var c in catRepo.GetAll()) c.Accept(exporter);
            foreach (var o in opRepo.GetAll()) o.Accept(exporter);
            var result = exporter.Result;
            if (string.IsNullOrWhiteSpace(result))
            {
                AnsiConsole.MarkupLine("[grey]Нет данных для экспорта[/]");
                return;
            }

            File.WriteAllText(path, result);
            AnsiConsole.MarkupLine($"[green]CSV сохранён в {path}[/]");
        }

        static void UndoLast(Stack<ICommand> history)
        {
            if (!history.Any())
            {
                AnsiConsole.MarkupLine("[grey]Нет команд для отмены[/]");
                return;
            }

            var cmd = history.Pop();
            cmd.Undo();
            AnsiConsole.MarkupLine("[yellow]Последняя команда отменена[/]");
        }
    }
}