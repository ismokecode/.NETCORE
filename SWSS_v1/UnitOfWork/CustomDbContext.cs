using Microsoft.Identity.Client;
using SWSS_v1.Models;

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
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Test> Test { get; set; }
    }
}

