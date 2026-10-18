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
            var result = _context.Questions.Where<Question>(x => x.QuestionText == obj.QuestionText
            && x.InstituteId == obj.InstituteId && x.SubjectId == obj.SubjectId && x.ClassID == obj.ClassID).FirstOrDefault();
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
            _emp.Summary = obj.Summary;
            _emp.ModifiedDate = obj.ModifiedDate;
            _emp.ModifiedBy = obj.ModifiedBy;
            _emp.isActive = obj.isActive;
            await _context.SaveChangesAsync();
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
        public async Task<IQueryable<Question>> GetQuestionOptionsByInstituteAsync(int id)
        {
            var questionWithOptions = _context.Questions
                             .Where(x => x.InstituteId == id)
                             .Include(p => p.Options)
                             .Include(p => p.Classes)
                             .Include(p => p.Subjects);
                             //.ToList();
            return questionWithOptions;
        }

        public async Task<IQueryable<Question>> GetQuestionsByClassAndSubject(int clsId, int subId,int instituteId,int totalQuestion)
        {
            var questionWithOptions = _context.Questions
                             .Where(x => x.InstituteId == instituteId && x.ClassID == clsId && x.SubjectId == subId)
                             .Include(p => p.Options)
                             .Include(p => p.Classes)
                             .Include(p => p.Subjects)
                             // Group by the composite key to find duplicates
                            .GroupBy(x => new { x.ClassID, x.SubjectId, x.QuestionText })
                            // Select the first complete Question object (including its relation data) from each group
                            .Select(g => g.First())
                            .OrderBy(x=>EF.Functions.Random())
                            .Take(totalQuestion);
                            //.ToList();
            return questionWithOptions;
        }
        public async Task<IQueryable<Question>> GetQuizQuestionByClassAndSubject(int clsId, int subId, int instituteId,int totalQuestion)
        {
            int count = await _context.Questions.CountAsync();
            int index = new Random().Next(0, count);
            var questionWithOptions = _context.Questions
                             //.Skip(index)
                             .Where(x => x.InstituteId == instituteId && x.ClassID == clsId && x.SubjectId == subId && x.isSetAsQuestion==true)
                             .Include(p => p.Options)
                             .Include(p => p.Classes)
                             .Include(p => p.Subjects) 
                             // Group by the composite key to find duplicates
                            .GroupBy(x => new { x.ClassID, x.SubjectId, x.QuestionText })
                            // Select the first complete Question object (including its relation data) from each group
                            .Select(g => g.First())
                            .OrderBy(x => EF.Functions.Random())
                            .Take(totalQuestion); ;
            return questionWithOptions;
        }
        public async Task SetAsQuestions(int[] questionsIds,int classId, int subjectId)
        {
            await _context.Questions.Where(q => q.ClassID == classId && q.SubjectId == subjectId)
                .ExecuteUpdateAsync(s => s.SetProperty(q => q.isSetAsQuestion, false));
            
            await _context.Questions
                .Where(q => questionsIds.Contains(q.QuestionId)).Where(q=>q.ClassID==classId && q.SubjectId==subjectId)
                .ExecuteUpdateAsync(s => s.SetProperty(q => q.isSetAsQuestion, true));
                await _context.SaveChangesAsync();
        }
        public async Task AddQuestionFromExcel(List<Question>questions)
        {
            await _context.Questions.AddRangeAsync(questions);
        }
    }
}
