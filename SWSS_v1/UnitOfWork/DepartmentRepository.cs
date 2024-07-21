using SWSS_v1.Models;
using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class DepartmentRepository:Repository<Department>
    {
        CustomDbContext _context;
        public DepartmentRepository(CustomDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Department>> GetAllDepartmentAsync()
        {
            //return await _context.Departments.Include(e => e.DepartmentId).ToListAsync();
            return await _context.Departments.ToListAsync();
        }
        //Retrieves a single employee by their ID along with Department data.
        public async Task<Department?> GetDepartmentByIdAsync(int DepartmentId)
        {
            var department = await _context.Departments
               .Include(e => e.DepartmentId)
               .FirstOrDefaultAsync(m => m.DepartmentId == DepartmentId);
            return department;
        }
        //Retrieves Employees by Departmentid
        public async Task<IEnumerable<Department>> GetEmployeesByDepartmentAsync(int DepartmentId)
        {
            return await _context.Departments
                .Where(emp => emp.DepartmentId == DepartmentId)
                .Include(e => e.DepartmentId).ToListAsync();
        }
    }
}
