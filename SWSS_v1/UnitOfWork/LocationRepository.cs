using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class LocationRepository : Repository<Location>, ILocationRepository
    {
        CustomDbContext _context;
        public LocationRepository(CustomDbContext context) : base(context)
        {
            _context = context;
        }
        public override async Task InsertAsync(Location obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            await _context.AddAsync(obj);
        }
    }
}
