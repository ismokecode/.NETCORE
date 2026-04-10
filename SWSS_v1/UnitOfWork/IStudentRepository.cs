using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface IStudentRepository : IRepository<Student>
    {
        public bool IsExist(Student obj);
        public Task<ICollection<Student>> GetStudentClassDetailsAsync();
    }
}
