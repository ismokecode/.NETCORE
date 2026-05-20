using SWSS_v1.Models;
using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class ClassSubjectMapperRepository : Repository<ClassSubjectMapper>, IClassSubjectMapperRepository
    {
        CustomDbContext _context;
        public ClassSubjectMapperRepository(CustomDbContext context):base(context)
        {
            _context = context;
        }
        public async Task AddAsync(ClassSubjectMapper obj)
        {
            if (obj.Id != null)
                await _context.AddAsync(obj);
            else
            {
                await _context.ClassSubjectMappers
                  .Where(u => u.Id == obj.Id)
                  .ExecuteUpdateAsync(s => s.SetProperty(u => u.SubjectId, obj.SubjectId));
            }
        }

        public async Task<int[]> GetSubjectsIdByClassIdForVisitors(int instituteId, int classId)
        {
            return await _context.ClassSubjectMappers.Where(x => x.ClassId == classId && x.InstituteId == instituteId)
                .Select(u=>u.SubjectId).ToArrayAsync();
        }

        public bool IsExist(ClassSubjectMapper obj)
        {
            bool isExist;
            var result = _context.ClassSubjectMappers.Where<ClassSubjectMapper>(x => x.ClassId == obj.ClassId && x.InstituteId == obj.InstituteId && x.SubjectId==obj.SubjectId).FirstOrDefault();
            return isExist = result == null ? false : true;
        }

    }
}
