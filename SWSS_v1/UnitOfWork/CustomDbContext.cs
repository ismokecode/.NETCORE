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
            modelBuilder.Entity<Student>(b =>
            {
                b.HasOne<Classes>(b => b.Classes)
                .WithMany(b => b.lstStudents)
                .HasForeignKey(ur => ur.ClassesId).IsRequired();
            });

            //modelBuilder.Entity<Question> question has subject one but many question for one subject
            //Defines SubjectId inside the Question entity as the Foreign Key that links it to the Subject table.
            modelBuilder.Entity<Question>(b =>
            {
                b.HasOne<Subject>(b => b.Subjects)
                .WithMany(b => b.Questions)
                .HasForeignKey(ur => ur.SubjectId).IsRequired();
            });

            //one question entity has many options called question.options list and 
            modelBuilder.Entity<Option>(b =>
            {
                b.HasOne<Question>(b => b.Question)
                .WithMany(b => b.Options)
                .HasForeignKey(ur => ur.QuestionId);
            });

            modelBuilder.Entity<Classes>()
            .HasMany(o => o.lstQuestion)
            .WithOne(q => q.Classes)
            .HasForeignKey(o => o.ClassID);

            //it tells the database that one Subject can have many Questions, and each Question belongs to exactly one Subject.
            modelBuilder.Entity<Subject>()
            .HasMany(o => o.Questions)
            .WithOne(q => q.Subjects)
            .HasForeignKey(o => o.SubjectId);

            //modelBuilder.Entity<StudentResult>()
            //.HasOne(o => o.TestLink)
            //.WithOne(s => s.StudentResults)
            //.HasForeignKey(typeof(StudentResult), "OnlineTestLink");

            modelBuilder.Entity<TestLink>()
            .HasOne(o => o.Classes)
            .WithMany(s=>s.TestLinksList)
            .HasForeignKey(o=>o.ClassId);

            modelBuilder.Entity<TestLink>()
            .HasOne(o => o.Subjects);

            modelBuilder.Entity<TestLink>()
            .HasOne(o => o.Students)
            .WithMany(o=>o.TestLinksList)
            .HasForeignKey(f=>f.StudentId);

            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Customer> Customers { get; set; }
        //Online Test 
        public DbSet<Classes> Classes { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Institute> Institutes { get; set; }
        public DbSet<TestLink> TestLinks { get; set; }
        public DbSet<ClassSubjectMapper> ClassSubjectMappers { get; set; }
        public DbSet<PasswordTokenGeneration> PasswordTokenGenerations { get; set; }
        public DbSet<StudentResult> StudentResults { get; set;}
    }
}

