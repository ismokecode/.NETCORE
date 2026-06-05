using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public class PasswordTokenGenerationRepository:Repository<PasswordTokenGeneration>, IPasswordTokenGeneration
    {
        CustomDbContext _context;
        public PasswordTokenGenerationRepository(CustomDbContext _dbContext) : base(_dbContext) 
        {
            _context = _dbContext;
        }
        public string GetEmail(string emailToken)
        {
            string email = _context.PasswordTokenGenerations.Where(x => x.Token == emailToken).
                Select(x => x.Email).
                FirstOrDefault().ToString();
            return email;
        }
    }
}
