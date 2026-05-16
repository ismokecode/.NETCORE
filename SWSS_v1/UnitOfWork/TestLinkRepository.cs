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
        public async Task SaveTestLinkAsync(TestLink obj)
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
                OnlineLineTestLink = Guid.NewGuid().ToString(),
                InstituteId=obj.InstituteId
            }).ToList();

            // 2. Add the range to the DbContext
            _context.TestLinks.AddRange(linkDetails);
            await _context.SaveChangesAsync();
        }
    }
}
