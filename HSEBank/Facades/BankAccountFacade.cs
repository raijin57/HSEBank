using HSEBank.Main;
using HSEBank.Main.Entities;
using HSEBank.Repositories;

namespace HSEBank.Facades
{
    public class BankAccountFacade
    {
        private readonly IRepository<BankAccount> _repo;
        private readonly IMainFactory _factory;

        public BankAccountFacade(IRepository<BankAccount> repo, IMainFactory factory)
        {
            _repo = repo;
            _factory = factory;
        }

        public BankAccount Create(string name, decimal initialBalance = 0)
        {
            var acc = _factory.CreateBankAccount(name, initialBalance);
            _repo.Add(acc);
            return acc;
        }

        public IEnumerable<BankAccount> GetAll() => _repo.GetAll();
        public BankAccount? Get(Guid id) => _repo.Get(id);

        public void ChangeName(Guid id, string newName)
        {
            var acc = _repo.Get(id) ?? throw new InvalidOperationException("Account not found");
            acc.ChangeName(newName);
            _repo.Update(acc);
        }

        public void Delete(Guid id)
        {
            _repo.Delete(id);
        }
    }
}