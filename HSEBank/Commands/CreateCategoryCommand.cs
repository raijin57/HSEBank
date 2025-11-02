using HSEBank.Main.Entities;
using HSEBank.Repositories;

namespace HSEBank.Commands
{
    public class CreateCategoryCommand : ICommand
    {
        private readonly IRepository<Category> _catRepo;
        private readonly Category _category;

        public CreateCategoryCommand(Category category, IRepository<Category> catRepo)
        {
            _category = category;
            _catRepo = catRepo;
        }

        public void Execute()
        {
            _catRepo.Add(_category);
        }

        public void Undo()
        {
            _catRepo.Delete(_category.Id);
        }
    }
}