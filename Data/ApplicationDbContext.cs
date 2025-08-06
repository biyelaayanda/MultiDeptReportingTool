using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Users> Users { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportData> ReportData { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints
            ConfigureUsers(modelBuilder);
            ConfigureDepartments(modelBuilder);
            ConfigureReports(modelBuilder);
            ConfigureReportData(modelBuilder);
            ConfigureAuditLogs(modelBuilder);

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                // Relationship with Department
                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Users)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureDepartments(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
                entity.HasIndex(e => e.Code).IsUnique();
            });
        }

        private void ConfigureReports(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ReportType).IsRequired().HasMaxLength(100);
                
                // Relationship with Department
                entity.HasOne(r => r.Department)
                      .WithMany(d => d.Reports)
                      .HasForeignKey(r => r.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship with CreatedByUser
                entity.HasOne(r => r.CreatedByUser)
                      .WithMany(u => u.CreatedReports)
                      .HasForeignKey(r => r.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship with ApprovedByUser
                entity.HasOne(r => r.ApprovedByUser)
                      .WithMany(u => u.ApprovedReports)
                      .HasForeignKey(r => r.ApprovedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureReportData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReportData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FieldType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.NumericValue).HasPrecision(18, 2); // SQL Server decimal precision
                
                // Relationship with Report
                entity.HasOne(rd => rd.Report)
                      .WithMany(r => r.ReportData)
                      .HasForeignKey(rd => rd.ReportId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureAuditLogs(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityId).IsRequired();
                
                entity.HasOne(al => al.User)
                      .WithMany(u => u.AuditLogs)
                      .HasForeignKey(al => al.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Departments with static dates
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Finance", Code = "FIN", Description = "Financial operations and reporting", IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 2, Name = "Human Resources", Code = "HR", Description = "Human resources management", IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 3, Name = "Operations", Code = "OPS", Description = "Operational activities and processes", IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 4, Name = "Compliance", Code = "COMP", Description = "Compliance and risk management", IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 5, Name = "Information Technology", Code = "IT", Description = "IT infrastructure and systems", IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}