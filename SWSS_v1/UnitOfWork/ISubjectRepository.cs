using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface ISubjectRepository : IRepository<Subject>
    {
        public bool IsExist(Subject obj);
    }
}
