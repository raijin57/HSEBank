using HSEBank.Main.Entities;
using HSEBank.Repositories;

namespace HSEBank.Commands
{
    public class DeleteCategoryCommand : ICommand
    {
        private readonly IRepository<Category> _catRepo;
        private Category? _deleted;

        public DeleteCategoryCommand(IRepository<Category> catRepo, Guid catId)
        {
            _catRepo = catRepo;
            _deleted = _catRepo.Get(catId);
        }

        public void Execute()
        {
            if (_deleted == null)
            {
                return;
            }

            _catRepo.Delete(_deleted.Id);
        }

        public void Undo()
        {
            if (_deleted == null)
            {
                return;
            }

            _catRepo.Add(_deleted);
        }
    }
}