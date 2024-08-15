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

        public bool IsExist(Location loc)
        {
            bool isExist;
            var result = _context.Locations.Where<Location>(x => x.LocationName == loc.LocationName).FirstOrDefault();
            return isExist = result == null ? false : true;
        }
        public bool IsExistUpdate(Location loc)
        {
            bool isExist = false;
            var result = _context.Locations.Where<Location>(x => x.LocationId == loc.LocationId).FirstOrDefault();
            if(result != null)
            {  
                result = _context.Locations.Where<Location>(x => x.LocationName == loc.LocationName).FirstOrDefault();
                return isExist = result == null ? false : true;
            }
            else
            {
                isExist = true;
            }
            return isExist;
        }

        public override async Task InsertAsync(Location obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            await _context.AddAsync(obj);
        }
        public override async Task UpdateAsync(Location obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            var _emp = _context.Locations.FirstOrDefault(x => x.LocationId == obj.LocationId);
            _emp.LocationName = obj.LocationName;
            //wait _context.SaveChangesAsync();
        }
    }
}
