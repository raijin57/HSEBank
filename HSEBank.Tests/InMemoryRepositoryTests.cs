using System;
using HSEBank.Main.Entities;
using HSEBank.Repositories;
using Xunit;

namespace HSEBank.Tests
{
    public class InMemoryRepositoryTests
    {
        [Fact]
        public void AddGetUpdateDelete_Works()
        {
            var repo = new InMemoryRepository<BankAccount>();
            var acc = new BankAccount(Guid.NewGuid(), "t", 10m);
            repo.Add(acc);
            var loaded = repo.Get(acc.Id);
            Assert.NotNull(loaded);
            Assert.Equal(10m, loaded!.Balance);

            acc.ChangeName("new");
            repo.Update(acc);
            var upd = repo.Get(acc.Id);
            Assert.Equal("new", upd!.Name);

            repo.Delete(acc.Id);
            var del = repo.Get(acc.Id);
            Assert.Null(del);
        }
    }
}