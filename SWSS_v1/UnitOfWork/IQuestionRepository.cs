using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface IQuestionRepository : IRepository<Question>
    {
        public bool IsExist(Question obj);
    }
}
