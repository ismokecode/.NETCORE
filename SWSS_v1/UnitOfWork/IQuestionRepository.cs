using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface IQuestionRepository : IRepository<Question>
    {
        public bool IsExist(Question obj);
        public Task<IEnumerable<Question>> GetQuestionOptionsAsync();
        public Task<Question> GetQuestionOptionsByIdAsync(int id);
        public Task<IQueryable<Question>> GetQuestionOptionsByInstituteAsync(int id);
        public Task<IQueryable<Question>> GetQuestionsByClassAndSubject(int clsId,int subId, int instituteId);
        public Task SetAsQuestions(int[] questionsId);
    }
}
