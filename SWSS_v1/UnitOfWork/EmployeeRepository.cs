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
        public async Task InsertAsync2(Employee obj)
        {
            _context.Entry(obj.Department).State = EntityState.Unchanged;
            //It will mark the Entity state as Added
            await _context.AddAsync(obj);
        }
        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees.Include(e => e.Department).ToListAsync();
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
