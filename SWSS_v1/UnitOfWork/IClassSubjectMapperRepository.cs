using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface IClassSubjectMapperRepository:IRepository<ClassSubjectMapper>
    {
        Task AddAsync(ClassSubjectMapper obj);
        bool IsExist(ClassSubjectMapper obj);
        Task<int[]> GetSubjectsIdByClassIdForVisitors(int instituteId, int classId);
    }
}
