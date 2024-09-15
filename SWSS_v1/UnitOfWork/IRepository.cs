namespace SWSS_v1.UnitOfBox
{
    public interface IRepository<T> where T : class
    {
        //IEnumerable<T> GetAll();
        //void Insert(T obj);
        //void Update(T obj);
        //void Delete(T obj);
        //T GetById(object id);
        //IList<T> GetAll();
        //void Add(T entity);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(object Id);
        Task InsertAsync(T Entity);
        Task UpdateAsync(T Entity);
        Task DeleteAsync(int Id);
        Task SaveAsync();
    }
}
