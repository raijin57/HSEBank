# ДЗ №2 — «Учет финансов»
---
## Что реализовано
- **Доменные классы:** `BankAccount`, `Category`, `Operation` (поля и бизнес-логика — проверки при создании, применение суммы к счету).
- **Репозиторий:** `IRepository<T>` и `InMemoryRepository<T>`.
- **Фабрика:** `IMainFactory` / `MainFactory` — централизованное создание сущностей (валидация при создании).
- **Фасады:** `BankAccountFacade`, `CategoryFacade`, `OperationFacade`, `AnalyticsFacade` — сгруппированные операции по областям.
- **Команды: **набор команд в папке `Commands` + `CompositeCommand` — сценарии пользовательских действий оформлены как команды.
- **Декоратор:** `CommandTimerDecorator` — обёртка для измерения времени выполнения команд.
- **Шаблонный метод:** `FileImporter` — общий алгоритм импорта; конкретные форматы — `CSVImporter`, `JSONImporter`.
- **Посетитель для экспорта:** `IExportVisitor`, `JSONExportVisitor`, `CSVExportVisitor`.
- **DI-контейнер:** в `Program.cs` использована `IServiceCollection` (регистрация сервисов `AddSingleton` и `AddTransient`).
- **Unit-тесты:** проект `HSEBank.Tests` содержит тесты для фабрики, репозитория и фасада операций
---
## Где какие принципы SOLID / GRASP соблюдены
- **SRP (Single Responsibility):**
  - `MainFactory` — отвечает только за создание объектов.
  - `OperationFacade` — отвечает за сценарии, связанные с операциями (создать, удалить, получить).
- **OCP (Open/Closed):**
  - `FileImporter` + `CSVImporter`/`JSONImporter` — добавление нового формата без правки существующего кода.
- **Liskov / Interface Segregation:**
  - `IRepository<T>` маленький и понятный интерфейс.
- **Dependency Inversion:**
  - `Program.cs` использует DI и регистрирует интерфейсы (`IMainFactory`, `IRepository<T>`) — верхние уровни зависят от абстракций.
- **GRASP (High Cohesion / Low Coupling):**
  - Фасады удерживают высокую связность операций по области, репозитории и фабрика — слабо связаны через интерфейсы.
---
## Какие паттерны GoF реализованы (и где)
- **Фабрика** — `MainFactory` / `IMainFactory`. Обеспечивает единое место создания объектов и валидацию (пример: `CreateOperation` проверяет amount > 0).
- **Фасад** — `OperationFacade`, `BankAccountFacade`, `CategoryFacade` (объединяют операции CRUD).
- **Команда** — папка `Commands` + `CompositeCommand` — сценарии как команды, возможность undo (история в `Program.cs`).
- **Декоратор  — `CommandTimerDecorator` — измеряет время выполнения любой команды.
- **Шаблон** — `FileImporter` + конкретные имплементации `CSVImporter` / `JSONImporter`.
- **Посетитель — `IExportVisitor` и реализации для CSV/JSON (генерация файлов экспорта).
---
## Тесты — что есть и как запускать
Команды (в корне `FinancialApp`):
```bash
dotnet restore
dotnet build
dotnet test FinancialApp.sln
```

**Тесты покрывают:**
- `InMemoryRepository`.
- `MainFactory` (проверки валидации создания объектов).
- `OperationFacade` (создание операции и обновление баланса).
---
