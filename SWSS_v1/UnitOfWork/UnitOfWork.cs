using Microsoft.EntityFrameworkCore.Storage;
using SWSS_v1.UnitOfWork;
using System;

namespace SWSS_v1.UnitOfBox
{
    public class UnitOfWork: IUnitOfWork, IDisposable
    {
        private readonly CustomDbContext _dbContext;
        private IDbContextTransaction? _objTran = null;
        public AuthorRepository Authors { get; private set; }
        public DepartmentRepository Departments { get; private set; }
        public EmployeeRepository Employees { get; private set; }
        public IRepository<T> Repository<T>() where T : class
        {
            return new Repository<T>(_dbContext);
        }
        private bool disposed = false;
        public UnitOfWork(CustomDbContext dbContext)
        {
            _dbContext = dbContext;
            Authors = new AuthorRepository(_dbContext);
            Departments = new DepartmentRepository(_dbContext);
            Employees = new EmployeeRepository(_dbContext);
        }
        public void BeginTransaction()
        {
            _objTran = _dbContext.Database.BeginTransaction();
        }
        public void Commit()
        {
            _objTran?.Commit();
        }
        public void Rollback()
        {
            _objTran?.Rollback();
            _objTran?.Dispose();
        }
        public async Task Save()
        {
            try
            {
                //Calling DbContext Class SaveChanges method 
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Handle the exception, possibly logging the details
                // The InnerException often contains more specific details
                throw new Exception(ex.Message, ex);
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _dbContext.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
