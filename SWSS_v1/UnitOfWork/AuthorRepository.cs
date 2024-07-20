using SWSS_v1.UnitOfWork;

namespace SWSS_v1.UnitOfBox
{
    public class AuthorRepository : Repository<Author>, IAuthorRepository
    {
        CustomDbContext _context;
        public AuthorRepository(CustomDbContext context):base(context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Author>> GetAllEmployeesAsync()
        {
            return await _context.Authors.Include(e => e.DepartmentId).ToListAsync();
        }
        //Retrieves a single employee by their ID along with Department data.
        public async Task<Author?> GetEmployeeByIdAsync(int EmployeeID)
        {
            var author = await _context.Authors
               .Include(e => e.DepartmentId)
               .FirstOrDefaultAsync(m => m.DepartmentId == EmployeeID);
            return author;
        }
        //Retrieves Employees by Departmentid
        public async Task<IEnumerable<Author>> GetEmployeesByDepartmentAsync(int DepartmentId)
        {
            return await _context.Authors
                .Where(emp => emp.DepartmentId == DepartmentId)
                .Include(e => e.DepartmentId).ToListAsync();
        }
    }
}
