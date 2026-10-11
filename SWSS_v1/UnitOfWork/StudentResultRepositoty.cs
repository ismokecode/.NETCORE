using HandlebarsDotNet;
using SWSS_v1.Models;
using SWSS_v1.UnitOfBox;
using System.Globalization;

namespace SWSS_v1.UnitOfWork
{
    public class StudentResultRepositoty : Repository<StudentResult>, IStudentResultRepositoty
    {
        CustomDbContext _context;
        public StudentResultRepositoty(CustomDbContext context) : base(context)
        {
            this._context = context;
        }
        public async Task<StudentResult> GetByIdAsync(string uri)
        {
            var result = _context.StudentResults.FirstOrDefault(x => x.TestLinkGuid == uri);

            //var result = _context.TestLinks.Where(x => x.OnlineLineTestLink.Equals(urlId, StringComparison.OrdinalIgnoreCase));
            //var result = _context.TestLinks.Contains(x => x.OnlineLineTestLink == urlId).FirstOrDefault();
            return result;
        }
        public async Task<List<StudentResult>> GetStudentResultsAsync(int classId, int subjectId, int instituteId)
        {
            // 1. First, group and find the best StudentResultId for each student
            var targetResultIdsQuery = _context.StudentResults
                .Join(_context.TestLinks, sr => sr.TestLinkId, tl => tl.LinkId, (sr, tl) => new { sr, tl })
                .Join(_context.Students, c => c.tl.StudentId, student => student.StudentId, (c, student) => new { c.sr, c.tl, student })
                .Where(m => m.tl.ClassId == classId &&
                            m.tl.SubjectId == subjectId &&
                            m.student.InstituteId == instituteId)
                .GroupBy(x => x.student.StudentId)
                .Select(g => g.OrderByDescending(x => x.sr.CreatedDateTime)
                              .ThenByDescending(x => x.sr.MarksObtained)
                              .Select(x => x.sr.StudentResultId) // Just grab the ID
                              .FirstOrDefault());

            // 2. Now perform your full joins only on those specific unique records
            var finalResultList = await _context.StudentResults
                .Where(sr => targetResultIdsQuery.Contains(sr.StudentResultId)) // Filter by unique IDs
                .Join(_context.TestLinks, sr => sr.TestLinkId, tl => tl.LinkId, (sr, tl) => new { sr, tl })
                .Join(_context.Classes, c => c.tl.ClassId, cls => cls.ClassesId, (c, cls) => new { c.sr, c.tl, cls })
                .Join(_context.Subjects, c => c.tl.SubjectId, sub => sub.SubjectID, (c, sub) => new { c.sr, c.tl, c.cls, sub })
                .Join(_context.Students, c => c.tl.StudentId, student => student.StudentId, (c, student) => new { c.sr, c.tl, c.cls, c.sub, student })
                .Select(m => new StudentResult
                {
                    ClassId = (int?)m.cls.ClassesId,
                    ClassName = m.cls.ClassName,
                    SubjectId = (int?)m.sub.SubjectID,
                    SubjectName = m.sub.SubjectName,
                    StudentId = (int?)m.student.StudentId,
                    StudentName = m.student.StudentName,
                    Email = m.student.Email,
                    Phone = m.student.Phone,
                    TotalQuestions = m.tl.TotalQuestions,
                    MarksObtained = m.sr.MarksObtained,
                    LinkId = (int?)m.tl.LinkId,
                    OnlineTestLink = m.tl.OnlineTestLink,
                    CreatedDateTime = m.sr.CreatedDateTime
                })
                .OrderByDescending(x => x.MarksObtained)
                .ThenByDescending(x => x.CreatedDateTime)
                .ToListAsync();

            return finalResultList;
        }

        public async Task<StudentResult> GetStudentResultsByIdAsync(int testLinkId, int instituteId)
        {
            var result = await _context.StudentResults
            // 1. Include the TestLink navigation property and its nested relations
            .Include(sr => sr.TestLinks)
            .ThenInclude(tl => tl.Classes)
            .Include(sr => sr.TestLinks)
            .ThenInclude(tl => tl.Subjects)
            .Include(sr => sr.TestLinks)
             .ThenInclude(tl => tl.Students)
            // 2. Apply filters
            .Where(sr => sr.TestLinkId == testLinkId
             && sr.TestLinks.InstituteId == instituteId)
               // 3. Sort by creation date
            .OrderByDescending(sr => sr.CreatedDateTime)
            // 4. Project into a new object
            .Select(sr => new StudentResult
            {
                ClassId = sr.TestLinks.ClassId,
                ClassName = sr.TestLinks.Classes.ClassName,
                SubjectId = sr.TestLinks.SubjectId,
                SubjectName = sr.TestLinks.Subjects.SubjectName,
                StudentId = sr.TestLinks.StudentId,
                StudentName = sr.TestLinks.Students.StudentName,
                Email = sr.TestLinks.Students.Email,
                Phone = sr.TestLinks.Students.Phone,
                TotalQuestions = sr.TestLinks.TotalQuestions,
                MarksObtained = sr.MarksObtained,
                LinkId = sr.TestLinkId,
                OnlineTestLink = sr.TestLinks.OnlineTestLink,
            })
            // 5. Get the first distinct or default record matching the criteria
            .FirstOrDefaultAsync();
                    return result;
        }
    }
}
