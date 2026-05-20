using SWSS_v1.Models;
using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class SubjectRepository : Repository<Subject>, ISubjectRepository
    {
        CustomDbContext _context;
        public SubjectRepository(CustomDbContext context) : base(context)
        {
            _context = context;
        }

        public bool IsExist(Subject obj)
        {
            bool isExist;
            var result = _context.Subjects.Where<Subject>(x => x.SubjectName == obj.SubjectName && x.InstituteId==obj.InstituteId).FirstOrDefault();
            return isExist = result == null ? false : true;
        }
        public bool IsExistUpdate(Subject obj)
        {
            bool isExist = false;
            var result = _context.Subjects.Where<Subject>(x => x.SubjectID == obj.SubjectID).FirstOrDefault();
            if(result != null)
            {  
                result = _context.Subjects.Where<Subject>(x => x.SubjectName == obj.SubjectName).FirstOrDefault();
                return isExist = result == null ? false : true;
            }
            else
            {
                isExist = true;
            }
            return isExist;
        }

        public override async Task InsertAsync(Subject obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            await _context.AddAsync(obj);
        }
        public override async Task UpdateAsync(Subject obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            var _emp = _context.Subjects.FirstOrDefault(x => x.SubjectID == obj.SubjectID);
            _emp.SubjectName = obj.SubjectName;
            _emp.ModifiedBy = obj.ModifiedBy;
            _emp.ModifiedDate = obj.ModifiedDate;
            _emp.isActive = obj.isActive;
            //wait _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Subject>> SearchLocationByName(string input)
        {
            return await _context.Subjects.Where(x => x.SubjectName == input).ToListAsync();
        }
        public async Task<IQueryable<Subject>> GetSubjectsByInstitute(int Id)
        {
            var result = _context.Subjects.Where(x => x.InstituteId == Id && x.isActive==true);
            return result;
        }
        public async Task ClassAndSubjectMapper(int classId,int subjecId)
        {
            await _context.Subjects
                  .Where(u => u.SubjectID==subjecId)
                  .ExecuteUpdateAsync(s => s.SetProperty(u => u.ClassId, classId));

        }
        public async Task<IQueryable<Subject>> GetSubjectsForVisitors(int instituteId,int[] subjectsId)
        {
            int[] tar = new[] { 1,2,3};
            return _context.Subjects.Where(x => subjectsId.Contains(x.SubjectID));
            //var result = _context.Subjects.Where(x => x.InstituteId == instituteId && x.isActive == true && x.ClassId==classId);
            //return result;
        }
    }
}
