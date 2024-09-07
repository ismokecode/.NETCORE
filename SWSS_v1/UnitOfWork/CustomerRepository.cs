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

    }
}
