
namespace SWSS_v1.UnitOfBox
{
    public interface IUnitOfWork : IDisposable
    {
        
        public void BeginTransaction();
        void Commit();
        void Rollback();
        Task Save();
        IRepository<T> Repository<T>() where T : class;
        AuthorRepository Authors { get; }
    }
}
