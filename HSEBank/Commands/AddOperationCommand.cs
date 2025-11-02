using HSEBank.Facades;
using HSEBank.Main;
using HSEBank.Main.Entities;

namespace HSEBank.Commands
{
    public class AddOperationCommand : ICommand
    {
        private readonly OperationFacade _opFacade;
        private readonly CategoryType _type;
        private readonly Guid _accountId;
        private readonly decimal _amount;
        private readonly DateTime _date;
        private readonly Guid? _categoryId;
        private readonly string? _description;
        private Operation? _createdOperation;

        public AddOperationCommand(OperationFacade opFacade, CategoryType type, Guid accountId, decimal amount,
            DateTime date, Guid? categoryId = null, string? description = null)
        {
            _opFacade = opFacade;
            _type = type;
            _accountId = accountId;
            _amount = amount;
            _date = date;
            _categoryId = categoryId;
            _description = description;
        }

        public void Execute()
        {
            _createdOperation = _opFacade.CreateOperation(_type, _accountId, _amount, _date, _categoryId, _description);
        }

        public void Undo()
        {
            if (_createdOperation == null)
            {
                return;
            }

            _opFacade.DeleteOperation(_createdOperation.Id);
        }
    }
}