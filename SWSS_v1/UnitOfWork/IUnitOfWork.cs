
using SWSS_v1.UnitOfWork;

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
        EmployeeRepository Employees { get; }
        DepartmentRepository Departments { get; }
        CustomerRepository Customers { get; }
        LocationRepository Locations { get; }
    }
}
