using SWSS_v1;
using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface IStudentResultRepositoty: IRepository<StudentResult>
    {
        Task<List<StudentResult>> GetStudentResultsAsync(int classId, int subjectId, int instituteId);
    }
}
