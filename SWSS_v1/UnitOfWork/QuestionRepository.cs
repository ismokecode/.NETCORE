using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class QuestionRepository : Repository<Question>, IQuestionRepository
    {
        CustomDbContext _context;
        public QuestionRepository(CustomDbContext context) : base(context)
        {
            _context = context;
        }

        public bool IsExist(Question obj)
        {
            bool isExist;
            var result = _context.Questions.Where<Question>(x => x.QuestionText == obj.QuestionText).FirstOrDefault();
            return isExist = result == null ? false : true;
        }
        public bool IsExistUpdate(Question obj)
        {
            bool isExist = false;
            var result = _context.Questions.Where<Question>(x => x.QuestionId == obj.QuestionId).FirstOrDefault();
            if(result != null)
            {  
                result = _context.Questions.Where<Question>(x => x.QuestionId == obj.QuestionId).FirstOrDefault();
                return isExist = result == null ? false : true;
            }
            else
            {
                isExist = true;
            }
            return isExist;
        }

        public override async Task InsertAsync(Question obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            await _context.AddAsync(obj);
        }
        public override async Task UpdateAsync(Question obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            var _emp = _context.Questions.FirstOrDefault(x => x.QuestionId == obj.QuestionId);
            _emp.QuestionText = obj.QuestionText;
            //wait _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Question>> SearchStudentByName(string input)
        {
            return await _context.Questions.Where(x => x.QuestionText == input).ToListAsync();
        }
        public async Task<IEnumerable<Question>> GetQuestionOptionsAsync()
        {
            var questionWithOptions = _context.Questions
                             .Include(p => p.Options)
                             .Include(p => p.Classes)   
                             .Include(p => p.Subjects)
                             .ToList();
            return questionWithOptions;
        }
        public async Task<Question> GetQuestionOptionsByIdAsync(int id)
        {
            var questionWithOptions = _context.Questions.Where(p => p.QuestionId == id)
                             .Include(p => p.Options)
                             .Include(p => p.Classes)
                             .Include(p => p.Subjects).FirstOrDefault();
            return questionWithOptions;
        }
    }
}
