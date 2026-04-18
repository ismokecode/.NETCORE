using SWSS_v1.UnitOfBox;
namespace SWSS_v1.UnitOfWork
{
    public interface IOptionRepository:IRepository<Option>
    {
        public Task DeleteOptionByQuestionIdAsync(int questionId);
    }
}
