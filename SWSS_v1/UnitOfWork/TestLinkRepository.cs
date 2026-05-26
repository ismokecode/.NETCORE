using SWSS_v1.Models;
using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class TestLinkRepository : Repository<TestLink>, ITestLinkRepository
    {
        CustomDbContext _context;
        public TestLinkRepository(CustomDbContext context) : base(context)
        {
            _context = context;
        }
        public void SaveTestLinkAsync(TestLink obj ,out IEnumerable<TestLink> returnVal)
        {
            //int userId = 101; // The shared value
            //int[] roleIds = { 1, 2, 3, 4, 5 }; // The array of IDs
            // 1. Create a list of entities by projecting the array

            var linkDetails = obj.StudentsId.Select(id => new TestLink
            {
                StudentId = id,
                ClassId = obj.ClassId,
                SubjectId = obj.SubjectId,
                ExpiryDateTime = obj.ExpiryDateTime,
                Durations = obj.Durations,
                TotalQuestions = obj.TotalQuestions,
                OnlineLineTestLink = Convert.ToString(Guid.NewGuid())+ "instituteId=" + obj.InstituteId+ "classId=" + obj.ClassId+ "subjectId=" + obj.SubjectId.ToString(),
                InstituteId=obj.InstituteId
            }).ToList();

            returnVal = linkDetails;
            // 2. Add the range to the DbContext
            _context.TestLinks.AddRange(linkDetails);
            _context.SaveChangesAsync();
        }
        public async Task SaveTestLinkAsync(List<TestLink> linkDetails)
        {
            // 2. Add the range to the DbContext
            _context.TestLinks.AddRange(linkDetails);
            await _context.SaveChangesAsync();
        }
        public async Task<TestLink>GetByIdAsync(string uri)
        {
            var result = _context.TestLinks.FirstOrDefault(x => x.OnlineLineTestLink == uri);

            //var result = _context.TestLinks.Where(x => x.OnlineLineTestLink.Equals(urlId, StringComparison.OrdinalIgnoreCase));
            //var result = _context.TestLinks.Contains(x => x.OnlineLineTestLink == urlId).FirstOrDefault();
            return result;
        }
        public bool isEarlier(DateTime dt)
        {
            bool isEarlier = DateTime.Now.CompareTo(dt) < 0;
            return isEarlier;
        }
    }
}
