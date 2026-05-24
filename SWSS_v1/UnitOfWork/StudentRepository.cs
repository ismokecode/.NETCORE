using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class StudentRepository : Repository<Student>, IStudentRepository
    {
        CustomDbContext _context;
        public StudentRepository(CustomDbContext context) : base(context)
        {
            _context = context;
        }

        public bool IsExist(Student obj)
        {
            bool isExist;
            var result = _context.Students.Where<Student>(x => x.StudentName == obj.StudentName).FirstOrDefault();
            return isExist = result == null ? false : true;
        }
        public bool IsExistUpdate(Student obj)
        {
            bool isExist = false;
            var result = _context.Students.Where<Student>(x => x.StudentId == obj.StudentId).FirstOrDefault();
            if(result != null)
            {  
                result = _context.Students.Where<Student>(x => x.StudentName == obj.StudentName).FirstOrDefault();
                return isExist = result == null ? false : true;
            }
            else
            {
                isExist = true;
            }
            return isExist;
        }

        public override async Task InsertAsync(Student obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            await _context.AddAsync(obj);
        }
        public override async Task UpdateAsync(Student obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            var _emp = _context.Students.FirstOrDefault(x => x.StudentId == obj.StudentId);
            _emp.StudentName = obj.StudentName;
            //_emp.Classes = obj.Classes;
            _emp.Phone = obj.Phone;
            _emp.AlternatePhoneNumber = obj.AlternatePhoneNumber;
            _emp.Address = obj.Address;
            _emp.Email = obj.Email;
            _emp.PinCode = obj.PinCode;
            _emp.ClassesId = obj.ClassesId;
            _emp.isActive = obj.isActive;
            _emp.ModifiedBy = obj.ModifiedBy;
            _emp.ModifiedDate = obj.ModifiedDate;
            //wait _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Student>> SearchStudentByName(string input)
        {
            return await _context.Students.Where(x => x.StudentName == input).ToListAsync();
        }
        public async Task<ICollection<Student>> GetStudentClassDetailsAsync()
        {
            var studentsWithClasses = _context.Students.Where(u=>u.isActive==true)
             .Include(p => p.Classes)
             .ToList();
            return studentsWithClasses;
        }
        public async Task<Student> GetStudentClassDetailsById(int id)
        {
            var studentsWithClass = _context.Students
                         .Include(p => p.Classes)
                         .Where(p => p.StudentId == id).FirstOrDefault();
            return studentsWithClass;
        }
        public async Task InActiveStudentAsync(int id)
        {
            await _context.Students
                .Where(u => u.StudentId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.isActive, false)); 
            await _context.SaveChangesAsync();

        }

        public async Task<IQueryable<Student>> GetStudentsByInstitute(int id)
        {
            var studentsWithClasses = _context.Students.Where(u => u.isActive == true && u.InstituteId == id)
             .Include(p => p.Classes);
            return studentsWithClasses;
        }
        public async Task<IQueryable<Student>> GetStudentsByInstituteAndClassId(int id,int classId)
        {
            var studentsWithClasses = _context.Students.Where(u => u.isActive == true && u.InstituteId == id && u.ClassesId == classId)
             .Include(p => p.Classes);
            return studentsWithClasses;
        }
        public Task<string[]> GetStudentsEmailById(int[]guids)
        {
            var result = _context.Students.Where(u => guids.Contains(u.StudentId)).Select(u=>u.Email).ToArray();
            return Task.FromResult(result);
        }
    }
}
