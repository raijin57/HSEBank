namespace HSEBank.Repositories
{
    public class CachingProxyRepository<T> : IRepository<T> where T : class
    {
        private readonly IRepository<T> _inner;
        private readonly Dictionary<Guid, T> _cache = new();

        public CachingProxyRepository(IRepository<T> inner)
        {
            _inner = inner;
            foreach (var e in _inner.GetAll())
            {
                var id = (Guid)e.GetType().GetProperty("Id")!.GetValue(e)!;
                _cache[id] = e;
            }
        }

        public void Add(T entity)
        {
            var id = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity)!;
            _cache[id] = entity;
            _inner.Add(entity);
        }

        public void Update(T entity)
        {
            var id = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity)!;
            _cache[id] = entity;
            _inner.Update(entity);
        }

        public void Delete(Guid id)
        {
            _cache.Remove(id);
            _inner.Delete(id);
        }

        public T? Get(Guid id) => _cache.TryGetValue(id, out var v) ? v : _inner.Get(id);
        public IEnumerable<T> GetAll() => _cache.Values.ToList();
    }
}