using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class OptionRepository: Repository<Option>,IOptionRepository
    {
        CustomDbContext _context;
        public OptionRepository(CustomDbContext context) : base(context)
        {
            _context = context;
        }

        public bool IsExist(Option obj)
        {
            bool isExist;
            var result = _context.Options.Where<Option>(x => x.OptionText == obj.OptionText).FirstOrDefault();
            return isExist = result == null ? false : true;
        }
        public bool IsExistUpdate(Option obj)
        {
            bool isExist = false;
            var result = _context.Options.Where<Option>(x => x.OptionId == obj.OptionId).FirstOrDefault();
            if (result != null)
            {
                result = _context.Options.Where<Option>(x => x.OptionText == obj.OptionText).FirstOrDefault();
                return isExist = result == null ? false : true;
            }
            else
            {
                isExist = true;
            }
            return isExist;
        }

        public override async Task InsertAsync(Option obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            await _context.AddAsync(obj);
        }
        public override async Task UpdateAsync(Option obj)
        {
            //_context.Entry(obj.Customer).State = EntityState.Unchanged;
            var _emp = _context.Options.FirstOrDefault(x => x.OptionId == obj.OptionId);
            _emp.OptionText = obj.OptionText;
            //wait _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Option>> SearchLocationByName(string input)
        {
            return await _context.Options.Where(x => x.OptionText == input).ToListAsync();
        }
        public async Task DeleteOptionByQuestionIdAsync(int questionId)
        {
            _context.Options.Where(x => x.QuestionId == questionId).ExecuteDeleteAsync();
        }
    }
}
