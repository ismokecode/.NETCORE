namespace SWSS_v1.UnitOfBox
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly CustomDbContext _dbContext;
        private readonly DbSet<T> _dbSet;
        public T GetById(object id)
        {
            return _dbSet.Find(id);
        }
        public Repository(DbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<T>();
        }
        public IList<T> GetAll()
        {
            return _dbSet.ToList();
        }
        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }
    }
}
