using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<FileDocument> FileDocuments { get; set; }
        public DbSet<LogsModel> Logs { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<FileDocument>(f =>
            {
                f.HasIndex(f => f.Name).IsUnique();
                f.HasIndex(f => f.OriginalFilename).IsUnique();
                f.HasIndex(f => f.Company);
                f.HasIndex(f => f.Year);
                f.HasIndex(f => f.Category);
                f.HasIndex(f => f.DateUploaded);
                f.HasIndex(f => f.OcrStatus);
                f.Property(f => f.ExtractedText).HasColumnType("text");
                f.Property(f => f.OcrError).HasColumnType("text");
            });

            builder.Entity<LogsModel>(l => l.HasIndex(l => l.Date));
            builder.Entity<Company>(c => c.HasIndex(c => c.CompanyName).IsUnique());
            builder.Entity<Department>(d => d.HasIndex(d => d.DepartmentName).IsUnique());
            builder.Entity<Category>(c => c.HasIndex(c => c.CategoryName).IsUnique());
            builder.Entity<Account>(a =>
            {
                a.HasIndex(account => account.Username).IsUnique();
                a.HasIndex(account => account.EmployeeNumber).IsUnique();
            });
            builder.Entity<SubCategory>(sc =>
            {
                sc.HasIndex(sc => new { sc.CategoryId, sc.SubCategoryName })
                    .IsUnique();
                sc.HasOne(c => c.Category)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(sc => sc.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
