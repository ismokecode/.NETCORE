using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class StudentResultRepositoty : Repository<StudentResult>, IStudentResultRepositoty
    {
        public StudentResultRepositoty(CustomDbContext dbContext) : base(dbContext)
        {
        }
    }
}
