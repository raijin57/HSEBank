using HSEBank.Main;
using HSEBank.Main.Entities;
using HSEBank.Repositories;

namespace HSEBank.Facades
{
    public class CategoryFacade
    {
        private readonly IRepository<Category> _repo;
        private readonly IMainFactory _factory;

        public CategoryFacade(IRepository<Category> repo, IMainFactory factory)
        {
            _repo = repo;
            _factory = factory;
        }

        public Category Create(CategoryType type, string name)
        {
            var cat = _factory.CreateCategory(type, name);
            _repo.Add(cat);
            return cat;
        }

        public IEnumerable<Category> GetAll() => _repo.GetAll();
        public Category? Get(Guid id) => _repo.Get(id);
        public void Delete(Guid id) => _repo.Delete(id);
    }
}