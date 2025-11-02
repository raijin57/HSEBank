namespace HSEBank.Repositories
{
    public interface IRepository<T>
    {
        IEnumerable<T> GetAll();
        T? Get(Guid id);
        void Add(T entity);
        void Update(T entity);
        void Delete(Guid id);
    }
}