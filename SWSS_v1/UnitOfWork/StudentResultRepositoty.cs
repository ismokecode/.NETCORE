using SWSS_v1.Models;
using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class StudentResultRepositoty : Repository<StudentResult>, IStudentResultRepositoty
    {
        CustomDbContext _dbContext;
        public StudentResultRepositoty(CustomDbContext dbContext) : base(dbContext)
        {
            this._dbContext = dbContext;
        }
        public async Task<StudentResult> GetByIdAsync(string uri)
        {
            var result = _dbContext.StudentResults.FirstOrDefault(x => x.TestLinkGuid == uri);

            //var result = _context.TestLinks.Where(x => x.OnlineLineTestLink.Equals(urlId, StringComparison.OrdinalIgnoreCase));
            //var result = _context.TestLinks.Contains(x => x.OnlineLineTestLink == urlId).FirstOrDefault();
            return result;
        }
    }
}
