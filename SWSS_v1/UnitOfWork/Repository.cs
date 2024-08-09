namespace SWSS_v1.UnitOfBox
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly CustomDbContext _dbContext;
        private readonly DbSet<T> _dbSet;
        //public Repository(DbContext dbContext)
        //{
        //    _dbContext = dbContext;
        //    //dbContext = _dbContext;
        //    _dbSet = _dbContext.Set<T>();
        //}
        public Repository(CustomDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<T>();
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }
        public async Task<T?> GetByIdAsync(object Id)
        {
            return await _dbSet.FindAsync(Id);
        }
        public virtual async Task InsertAsync(T Entity)
        {
            //It will mark the Entity state as Added
            await _dbSet.AddAsync(Entity);
        }
        //This method is going to update an Existing Entity
        public async Task UpdateAsync(T Entity)
        {
            _dbSet.Update(Entity);
        }
        //This method is going to remove an existing record from the table
        public async Task DeleteAsync(object Id)
        {
            var entity = await _dbSet.FindAsync(Id);
            if (entity != null)
            {
                //This will mark the Entity State as Deleted
                _dbSet.Remove(entity);
            }
        }
        //This method will make the changes permanent in the database
        public async Task SaveAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
