using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface IAuthorRepository:IRepository<Author>
    {
        Task<IEnumerable<Author>> GetAllEmployeesAsync();
        //This method returns one the Employee along with Department data based on the Employee Id
        Task<Author?> GetEmployeeByIdAsync(int EmployeeID);
        //This method will return Employees by Departmentid
        Task<IEnumerable<Author>> GetEmployeesByDepartmentAsync(int Departmentid);
    }
}
