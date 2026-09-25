using SWSS_v1.Models;
using SWSS_v1.UnitOfBox;

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
            var results = await _context.StudentResults
                // Ensure your navigation property to TestLink and related tables are included/accessed properly
                .Where(t => t.TestLinks.ClassId == classId &&
                            t.TestLinks.SubjectId == subjectId &&
                            t.TestLinks.InstituteId == instituteId)
                .OrderByDescending(t => t.CreatedDateTime)
                .Select(t => new StudentResult
                {
                    ClassId = t.TestLinks.ClassId,
                    ClassName = t.TestLinks.Classes.ClassName, // From Classes table via TestLinks
                    SubjectId = t.TestLinks.SubjectId,
                    SubjectName = t.TestLinks.Subjects.SubjectName, // From Subjects table via TestLinks
                    StudentId = t.TestLinks.StudentId,
                    StudentName = t.TestLinks.Students.StudentName, // From Students table via TestLinks
                    Email = t.TestLinks.Students.Email,             // From Students table via TestLinks
                    Phone = t.TestLinks.Students.Phone,             // From Students table via TestLinks
                    TotalQuestions = t.TestLinks.TotalQuestions,
                    MarksObtained = t.MarksObtained,                        // From StudentResults table
                    LinkId = t.TestLinkId,
                    OnlineTestLink = t.TestLinks.OnlineTestLink
                })
                .Distinct()
                .ToListAsync();

            return results;
        }

    }
}
