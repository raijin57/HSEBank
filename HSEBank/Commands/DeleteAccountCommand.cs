using HSEBank.Main.Entities;
using HSEBank.Repositories;

namespace HSEBank.Commands
{
    public class DeleteAccountCommand : ICommand
    {
        private readonly IRepository<BankAccount> _accRepo;
        private readonly IRepository<Operation> _opRepo;
        private BankAccount? _deleted;

        public DeleteAccountCommand(IRepository<BankAccount> accRepo, IRepository<Operation> opRepo, Guid accId)
        {
            _accRepo = accRepo;
            _opRepo = opRepo;
            _deleted = _accRepo.Get(accId);
        }

        public void Execute()
        {
            if (_deleted == null)
            {
                return;
            }

            var hasOps = _opRepo.GetAll().Any(o => o.BankAccountId == _deleted.Id);
            if (hasOps)
            {
                return;
            }

            _accRepo.Delete(_deleted.Id);
        }

        public void Undo()
        {
            if (_deleted == null)
            {
                return;
            }

            _accRepo.Add(_deleted);
        }
    }
}