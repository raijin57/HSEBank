using HSEBank.Main.Entities;
using HSEBank.Repositories;

namespace HSEBank.Commands
{
    public class CreateAccountCommand : ICommand
    {
        private readonly BankAccount _account;
        private readonly IRepository<BankAccount> _repo;

        public CreateAccountCommand(BankAccount account, IRepository<BankAccount> repo)
        {
            _account = account;
            _repo = repo;
        }

        public void Execute() => _repo.Add(_account);

        public void Undo() => _repo.Delete(_account.Id);
    }
}