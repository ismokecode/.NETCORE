using Microsoft.EntityFrameworkCore;
using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        CustomDbContext _context;
        public CustomerRepository(CustomDbContext context) : base(context)
        {
            _context = context;
        }
        public bool IsExist(Customer obj)
        {
            bool isExist;
            var result = _context.Customers.Where<Customer>(x => x.Phone == obj.Phone).FirstOrDefault();
            return isExist = result == null ? false : true;
        }
        public bool IsExistUpdate(Customer obj)
        {
            bool isExist = false;
            var result = _context.Customers.Where<Customer>(x => x.CustomerId == obj.CustomerId).FirstOrDefault();
            if (result != null)
            {
                result = _context.Customers.Where<Customer>(x => x.Phone == obj.Phone).FirstOrDefault();
                return isExist = result == null ? false : true;
            }
            else
            {
                isExist = true;
            }
            return isExist;
        }
        public override async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _context.Customers.Include("Location").ToListAsync();
        }
        public async Task<IEnumerable<Customer>> SearchCustomerByPhoneAsync(string phone)
        {
            return await _context.Customers.Where(x=>x.Phone==phone).Include("Location").ToListAsync();
        }
        public override async Task UpdateAsync(Customer obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            var _emp = _context.Customers.FirstOrDefault(x => x.CustomerId == obj.CustomerId);
            _emp.Name = obj.Name;
            _emp.Phone = obj.Phone;
            _emp.SecondaryNumber = obj.SecondaryNumber;
            _emp.DepartmentId = obj.DepartmentId;
            _emp.LocationId = obj.LocationId;
            //wait _context.SaveChangesAsync();
        }


    }
}
