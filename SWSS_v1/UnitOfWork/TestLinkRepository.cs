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
            var userRoles = obj.StudentId.Select(id => new TestLink
            {
                ClassId = obj.ClassId,
                SubjectId = obj.SubjectId
            }).ToList();

            // 2. Add the range to the DbContext
            _context.TestLinks.AddRange(userRoles);
            await _context.AddAsync(obj);
        }
    }
}
