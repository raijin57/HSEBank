using HSEBank.Main.Entities;
using HSEBank.Repositories;

namespace HSEBank.Commands
{
    public class DeleteOperationCommand : ICommand
    {
        private readonly IRepository<Operation> _opRepo;
        private readonly IRepository<BankAccount> _accRepo;
        private Operation? _deleted;

        public DeleteOperationCommand(IRepository<Operation> opRepo, IRepository<BankAccount> accRepo, Guid opId)
        {
            _opRepo = opRepo;
            _accRepo = accRepo;
            _deleted = _opRepo.Get(opId);
        }

        public void Execute()
        {
            if (_deleted == null)
            {
                return;
            }

            var acc = _accRepo.Get(_deleted.BankAccountId);
            if (acc != null)
            {
                acc.ApplyAmount(-_deleted.SignedAmount);
            }

            _opRepo.Delete(_deleted.Id);
            if (acc != null)
            {
                _accRepo.Update(acc);
            }
        }

        public void Undo()
        {
            if (_deleted == null)
            {
                return;
            }

            _opRepo.Add(_deleted);
            var acc = _accRepo.Get(_deleted.BankAccountId);
            if (acc != null)
            {
                acc.ApplyAmount(_deleted.SignedAmount);
            }

            if (acc != null)
            {
                _accRepo.Update(acc);
            }
        }
    }
}