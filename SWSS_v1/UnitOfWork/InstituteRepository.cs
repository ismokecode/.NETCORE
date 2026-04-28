using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class InstituteRepository:Repository<Institute>, IInstituteRepository
    {
        CustomDbContext _context;
        public InstituteRepository(CustomDbContext context):base(context)
        {
            _context = context;
        }
        public override async Task UpdateAsync(Institute obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            var _institute = _context.Institutes.FirstOrDefault(x => x.InstituteId == obj.InstituteId);
            _institute.Name = obj.Name;
            _institute.Phone = obj.Phone;
            _institute.Email = obj.Email;
            _institute.Address = obj.Address;
            _institute.ModifiedBy = obj.ModifiedBy;
            _institute.ModifiedDate = obj.ModifiedDate;
            //wait _context.SaveChangesAsync();
        }
    }
}
