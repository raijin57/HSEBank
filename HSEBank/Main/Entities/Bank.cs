using HSEBank.ImportExport;

namespace HSEBank.Main.Entities
{
    public class BankAccount
    {
        public Guid Id { get; }
        public string Name { get; private set; }
        public decimal Balance { get; private set; }

        public BankAccount(Guid id, string name, decimal initialBalance = 0)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (initialBalance < 0)
            {
                throw new ArgumentException("Initial balance cannot be negative", nameof(initialBalance));
            }

            Balance = initialBalance;
        }

        public void ChangeName(string newName) => Name = newName ?? throw new ArgumentNullException(nameof(newName));
        public void ApplyAmount(decimal amount) => Balance += amount;

        public void Accept(IExportVisitor visitor) => visitor.Visit(this);
    }
}