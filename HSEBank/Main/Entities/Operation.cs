using HSEBank.ImportExport;

namespace HSEBank.Main.Entities
{
    public class Operation
    {
        public Guid Id { get; }
        public CategoryType Type { get; }
        public Guid BankAccountId { get; }
        public decimal Amount { get; }
        public DateTime Date { get; }
        public string? Description { get; }
        public Guid? CategoryId { get; }

        public Operation(Guid id, CategoryType type, Guid bankAccountId, decimal amount, DateTime date,
            Guid? categoryId = null, string? description = null)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
            Id = id;
            Type = type;
            BankAccountId = bankAccountId;
            Amount = amount;
            Date = date;
            CategoryId = categoryId;
            Description = description;
        }

        public decimal SignedAmount => Type == CategoryType.Прибыль ? Amount : -Amount;

        public void Accept(IExportVisitor visitor) => visitor.Visit(this);
    }
}