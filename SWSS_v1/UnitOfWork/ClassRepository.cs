using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class ClassRepository : Repository<Classes>, IClassRepository
    {
        CustomDbContext _context;
        public ClassRepository(CustomDbContext context) : base(context)
        {
            _context = context;
        }

        public bool IsExist(Classes obj)
        {
            bool isExist;
            var result = _context.Classes.Where<Classes>(x => x.ClassName == obj.ClassName).FirstOrDefault();
            return isExist = result == null ? false : true;
        }
        public bool IsExistUpdate(Classes obj)
        {
            bool isExist = false;
            var result = _context.Classes.Where<Classes>(x => x.ClassesId == obj.ClassesId).FirstOrDefault();
            if(result != null)
            {  
                result = _context.Classes.Where<Classes>(x => x.ClassName == obj.ClassName).FirstOrDefault();
                return isExist = result == null ? false : true;
            }
            else
            {
                isExist = true;
            }
            return isExist;
        }

        public override async Task InsertAsync(Classes obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            await _context.AddAsync(obj);
        }
        public override async Task UpdateAsync(Classes obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            var _emp = _context.Classes.FirstOrDefault(x => x.ClassesId == obj.ClassesId);
            _emp.ClassName = obj.ClassName;
            //wait _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Classes>> SearchLocationByName(string className)
        {
            return await _context.Classes.Where(x => x.ClassName == className).ToListAsync();
        }
    }
}
