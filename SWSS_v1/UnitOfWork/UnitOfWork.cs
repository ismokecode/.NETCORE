using Microsoft.EntityFrameworkCore.Storage;
using SWSS_v1.UnitOfWork;
using System;

namespace SWSS_v1.UnitOfBox
{
    public class UnitOfWork: IUnitOfWork, IDisposable
    {
        //dbContext object shared from here to the individual entity repository class and common repository class
        //to maintain integrity and transaction
        private readonly CustomDbContext _dbContext;
        private IDbContextTransaction? _objTran = null;
        public DepartmentRepository Departments { get; private set; }
        public EmployeeRepository Employees { get; private set; }
        public CustomerRepository Customers { get; private set; }
        public LocationRepository Locations { get; private set; }
        public ClassRepository Classes { get; private set; }
        public SubjectRepository Subjects { get; private set; }
        public QuestionRepository Questions { get; private set; }
        public StudentRepository Students { get; private set; }
        public OptionRepository Options { get; private set; }
        public InstituteRepository Institutes { get; private set; }
        public TestLinkRepository TestLinks { get; private set; }
        public ClassSubjectMapperRepository ClassSubjectMappers { get; private set; }
        public PasswordTokenGenerationRepository PasswordTokenGenerations { get; private set; }
        //public class Repository<T> : IRepository<T> where T : class
        public IRepository<T> Repository<T>() where T : class
        {
            return new Repository<T>(_dbContext);
        }

        private bool disposed = false;
        public UnitOfWork(CustomDbContext dbContext)
        {
            _dbContext = dbContext;
            Departments = new DepartmentRepository(_dbContext);
            Employees = new EmployeeRepository(_dbContext);
            Customers = new CustomerRepository(_dbContext);
            Locations = new LocationRepository(_dbContext);
            Classes = new ClassRepository(_dbContext);
            Subjects = new SubjectRepository(_dbContext);
            Questions = new QuestionRepository(_dbContext);
            Students = new StudentRepository(_dbContext);
            Options = new OptionRepository(_dbContext);
            Institutes = new InstituteRepository(_dbContext);
            TestLinks = new TestLinkRepository(_dbContext);
            ClassSubjectMappers = new ClassSubjectMapperRepository(_dbContext);
            PasswordTokenGenerations = new PasswordTokenGenerationRepository(_dbContext);
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
