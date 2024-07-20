using Microsoft.Identity.Client;

namespace SWSS_v1.UnitOfBox
{
    public class CustomDbContext : AppDBContext
    {
        public CustomDbContext(DbContextOptions<CustomDbContext> options)
       : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Department> Departments { get; set; }
    }
}

