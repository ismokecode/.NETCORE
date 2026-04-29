using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface IClassRepository : IRepository<Classes>
    {
        public bool IsExist(Classes cls);
        public Task<IQueryable<Classes>> GetClassesByInstitute(int Id);
    }
}
