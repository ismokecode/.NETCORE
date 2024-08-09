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
        
    }
}
