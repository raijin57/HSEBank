using System;
using HSEBank.Facades;
using HSEBank.Main;
using HSEBank.Main.Entities;
using HSEBank.Repositories;
using Xunit;

namespace HSEBank.Tests
{
    public class OperationFacadeTests
    {
        [Fact]
        public void CreatingOperation_UpdatesAccountBalance()
        {
            var accRepo = new InMemoryRepository<BankAccount>();
            var opRepo = new InMemoryRepository<Operation>();
            var factory = new MainFactory();

            var acc = factory.CreateBankAccount("A", 100m);
            accRepo.Add(acc);

            var opFacade = new OperationFacade(opRepo, accRepo, factory);

            var op = opFacade.CreateOperation(CategoryType.Income, acc.Id, 50m, DateTime.Now);
            var updated = accRepo.Get(acc.Id);
            Assert.Equal(150m, updated!.Balance);
        }
    }
}