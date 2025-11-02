using HSEBank.Main.Entities;

namespace HSEBank.Main
{
    public interface IMainFactory
    {
        BankAccount CreateBankAccount(string name, decimal initialBalance = 0);
        Category CreateCategory(CategoryType type, string name);

        Operation CreateOperation(CategoryType type, Guid bankAccountId, decimal amount, DateTime date,
            Guid? categoryId = null, string? description = null);
    }

    public class MainFactory : IMainFactory
    {
        public BankAccount CreateBankAccount(string name, decimal initialBalance = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name required", nameof(name));
            }

            if (initialBalance < 0)
            {
                throw new ArgumentException("Initial balance cannot be negative", nameof(initialBalance));
            }

            return new BankAccount(Guid.NewGuid(), name, initialBalance);
        }

        public Category CreateCategory(CategoryType type, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name required", nameof(name));
            }

            return new Category(Guid.NewGuid(), type, name);
        }

        public Operation CreateOperation(CategoryType type, Guid bankAccountId, decimal amount, DateTime date,
            Guid? categoryId = null, string? description = null)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            return new Operation(Guid.NewGuid(), type, bankAccountId, amount, date, categoryId, description);
        }
    }
}