using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SWSS_v1.Models
{
    public class AppDBContext : IdentityDbContext<IdentityUser>
    {
        public AppDBContext()
        {

        }
        protected AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {

        }
        protected AppDBContext(DbContextOptions options) : base(options)
        {

        }
        /// <summary>
        /// define table name for RefreshTokens which used by EF to create a table in db
        /// run command Add-Migration RefreshTokensTableAdded so here partial class created name as RefreshTokensTableAdded
        /// run command Update-Database
        /// </summary>
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            #region Seed Roles and run command #1 add-migration SeedRoles(MigrationName) then #2. update-database
            //SeedRoles(builder);
            #endregion

            // Customize the ASP.NET Core Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Core Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
        #region Seed Roles in another way
        //private static void SeedRoles(ModelBuilder builder)
        //{
        //    builder.Entity<IdentityRole>().HasData(
        //    new IdentityRole() { Name="Student",ConcurrencyStamp="1",NormalizedName="Student"},
        //    new IdentityRole() { Name="Manager",ConcurrencyStamp="2",NormalizedName="Manager"}
        //    );
        //}
        #endregion
    }
}
