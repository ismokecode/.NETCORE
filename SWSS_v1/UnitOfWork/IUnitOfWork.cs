namespace SWSS_v1.UnitOfBox
{
    public interface IUnitOfWork : IDisposable
    {
        void Commit();
        void Rollback();
        IRepository<T> Repository<T>() where T : class;
    }
}
