using HSEBank.Main.Entities;
using HSEBank.Repositories;

namespace HSEBank.Facades
{
    public class AnalyticsFacade
    {
        private readonly IRepository<Operation> _opRepo;
        private readonly IRepository<Category> _catRepo;

        public AnalyticsFacade(IRepository<Operation> opRepo, IRepository<Category> catRepo)
        {
            _opRepo = opRepo;
            _catRepo = catRepo;
        }

        public decimal BalanceDelta(DateTime from, DateTime to)
        {
            var f = from.Date;
            var t = to.Date.AddDays(1).AddTicks(-1);
            return _opRepo.GetAll()
                .Where(o => o.Date >= f && o.Date <= t)
                .Sum(o => o.SignedAmount);
        }

        public Dictionary<string, decimal> GroupByCategory(DateTime from, DateTime to)
        {
            var f = from.Date;
            var t = to.Date.AddDays(1).AddTicks(-1);
            var ops = _opRepo.GetAll().Where(o => o.Date >= f && o.Date <= t);

            var groups = ops.GroupBy(o => o.CategoryId);

            var result = new Dictionary<string, decimal>();

            foreach (var g in groups)
            {
                string key;
                if (g.Key.HasValue)
                {
                    var cat = _catRepo.Get(g.Key.Value);
                    key = cat != null ? $"{cat.Name} ({cat.Type})" : "Неизвестная категория";
                }
                else
                {
                    key = "Без категории";
                }

                result[key] = g.Sum(o => o.SignedAmount);
            }

            return result;
        }
    }
}