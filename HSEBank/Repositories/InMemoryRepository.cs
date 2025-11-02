namespace HSEBank.Repositories
{
    public class InMemoryRepository<T> : IRepository<T> where T : class
    {
        protected readonly Dictionary<Guid, T> Store = new();

        public virtual void Add(T entity)
        {
            var id = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity)!;
            Store[id] = entity;
        }

        public virtual void Update(T entity)
        {
            var id = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity)!;
            if (!Store.ContainsKey(id)) throw new KeyNotFoundException();
            Store[id] = entity;
        }

        public virtual void Delete(Guid id) => Store.Remove(id);
        public virtual T? Get(Guid id) => Store.TryGetValue(id, out var v) ? v : null;
        public virtual IEnumerable<T> GetAll() => Store.Values.ToList();
    }
}