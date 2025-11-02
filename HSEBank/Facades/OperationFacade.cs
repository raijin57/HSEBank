using HSEBank.Main;
using HSEBank.Main.Entities;
using HSEBank.Repositories;

namespace HSEBank.Facades
{
    public class OperationFacade
    {
        private readonly IRepository<Operation> _opRepo;
        private readonly IRepository<BankAccount> _accRepo;
        private readonly IMainFactory _factory;

        public OperationFacade(IRepository<Operation> opRepo, IRepository<BankAccount> accRepo, IMainFactory factory)
        {
            _opRepo = opRepo;
            _accRepo = accRepo;
            _factory = factory;
        }

        public Operation CreateOperation(CategoryType type, Guid accountId, decimal amount, DateTime date,
            Guid? categoryId = null, string? description = null)
        {
            var acc = _accRepo.Get(accountId) ?? throw new InvalidOperationException("Account not found");
            var op = _factory.CreateOperation(type, accountId, amount, date, categoryId, description);
            _opRepo.Add(op);
            acc.ApplyAmount(op.SignedAmount);
            _accRepo.Update(acc);
            return op;
        }

        public IEnumerable<Operation> GetAll() => _opRepo.GetAll();
        public Operation? Get(Guid id) => _opRepo.Get(id);

        public void DeleteOperation(Guid operationId)
        {
            var op = _opRepo.Get(operationId) ?? throw new InvalidOperationException("Operation not found");
            var acc = _accRepo.Get(op.BankAccountId) ?? throw new InvalidOperationException("Account not found");
            acc.ApplyAmount(-op.SignedAmount);
            _accRepo.Update(acc);
            _opRepo.Delete(operationId);
        }
    }
}