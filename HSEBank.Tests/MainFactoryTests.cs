using System;
using HSEBank.Main;
using Xunit;

namespace HSEBank.Tests
{
    public class DomainFactoryTests
    {
        private readonly MainFactory _factory = new MainFactory();

        [Fact]
        public void CreateBankAccount_InvalidName_Throws()
        {
            Assert.Throws<ArgumentException>(() => _factory.CreateBankAccount("", 0));
        }

        [Fact]
        public void CreateBankAccount_NegativeInitial_Throws()
        {
            Assert.Throws<ArgumentException>(() => _factory.CreateBankAccount("A", -1));
        }

        [Fact]
        public void CreateCategory_InvalidName_Throws()
        {
            Assert.Throws<ArgumentException>(() => _factory.CreateCategory(CategoryType.Income, ""));
        }

        [Fact]
        public void CreateOperation_NonPositiveAmount_Throws()
        {
            var accId = Guid.NewGuid();
            Assert.Throws<ArgumentException>(() => _factory.CreateOperation(CategoryType.Expense, accId, 0, DateTime.Now));
        }
    }
}