using SWSS_v1.Models;
using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class EmployeeRepository:Repository<Employee>
    {
        CustomDbContext _context;
        public EmployeeRepository(CustomDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees.Include(e => e.DepartmentId).ToListAsync();
        }
        //Retrieves a single employee by their ID along with Department data.
        public async Task<Employee?> GetEmployeeByIdAsync(int EmployeeID)
        {
            var emp = await _context.Employees
               .Include(e => e.DepartmentId)
               .FirstOrDefaultAsync(m => m.EmployeeId == EmployeeID);
            return emp;
        }
        //Retrieves Employees by Departmentid
        public async Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(int DepartmentId)
        {
            return await _context.Employees
                .Where(emp => emp.DepartmentId == DepartmentId)
                .Include(e => e.DepartmentId).ToListAsync();
        }
    }
}
