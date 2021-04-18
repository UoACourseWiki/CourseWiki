using CourseWiki.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CourseWiki.Data
{
    public class ApiDbContext : IdentityDbContext
    {
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<CourseInTerm> CoursesInTerms { get; set; }
        public virtual DbSet<Course> Courses { get; set; }
        public virtual DbSet<Lecturer> Lecturers { get; set; }
        public virtual DbSet<Cls> Clses { get; set; }

        public ApiDbContext(DbContextOptions<ApiDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<CourseInTerm>().HasAlternateKey(p => new {p.CrseId, p.Term});
            builder.Entity<Cls>().HasAlternateKey(p => new {p.CrseId, p.Term, p.ClassSection});
            builder.Entity<Course>()
                .HasGeneratedTsVectorColumn(p => p.SearchVector, "english",
                    p => new {p.Title, p.Description, p.Subject, p.CatalogNbr})
                .HasIndex(p => p.SearchVector).HasMethod("GIN");
            builder.Entity<Course>().HasIndex(p => p.CrseId).IsUnique();
        }
    }
}