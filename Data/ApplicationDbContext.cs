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
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        
        // Phase 2: Enhanced RBAC
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<DepartmentPermission> DepartmentPermissions { get; set; }
        
        // Phase 2: Audit & Monitoring
        public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }
        public DbSet<SecurityAlert> SecurityAlerts { get; set; }
        public DbSet<SystemEvent> SystemEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints
            ConfigureUsers(modelBuilder);
            ConfigureDepartments(modelBuilder);
            ConfigureReports(modelBuilder);
            ConfigureReportData(modelBuilder);
            ConfigureAuditLogs(modelBuilder);
            ConfigureRefreshTokens(modelBuilder);
            
            // Phase 2: Enhanced RBAC
            ConfigurePermissions(modelBuilder);
            ConfigureRoles(modelBuilder);
            ConfigureRolePermissions(modelBuilder);
            ConfigureUserPermissions(modelBuilder);
            ConfigureDepartmentPermissions(modelBuilder);
            
            // Phase 2: Audit & Monitoring
            ConfigureSecurityAuditLogs(modelBuilder);
            ConfigureSecurityAlerts(modelBuilder);
            ConfigureSystemEvents(modelBuilder);

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

                // Relationship with Role (Phase 2 RBAC)
                entity.HasOne(e => e.RoleEntity)
                      .WithMany(r => r.Users)
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.SetNull);
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

        private void ConfigureRefreshTokens(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.CreatedByIp).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RevokedByIp).HasMaxLength(50);
                entity.Property(e => e.ReasonRevoked).HasMaxLength(200);
                entity.Property(e => e.ReplacedByToken).HasMaxLength(512);

                entity.HasOne(rt => rt.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Token).IsUnique();
            });
        }

        // Phase 2: Enhanced RBAC Configuration
        private void ConfigurePermissions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Resource).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                
                entity.HasIndex(e => new { e.Resource, e.Action }).IsUnique();
            });
        }

        private void ConfigureRoles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                
                entity.HasIndex(e => e.Name).IsUnique();
            });
        }

        private void ConfigureRolePermissions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(e => e.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.GrantedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.GrantedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            });
        }

        private void ConfigureUserPermissions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.Permission)
                      .WithMany(p => p.UserPermissions)
                      .HasForeignKey(e => e.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.GrantedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.GrantedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureDepartmentPermissions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DepartmentPermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Department)
                      .WithMany()
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.Permission)
                      .WithMany(p => p.DepartmentPermissions)
                      .HasForeignKey(e => e.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.GrantedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.GrantedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        // Phase 2: Audit & Monitoring Configuration
        private void ConfigureSecurityAuditLogs(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SecurityAuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Resource).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Username).HasMaxLength(50);
                entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.SessionId).HasMaxLength(100);
                entity.Property(e => e.Severity).HasMaxLength(20);
                entity.Property(e => e.FailureReason).HasMaxLength(500);
                
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
                      
                entity.HasOne(e => e.Department)
                      .WithMany()
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.NoAction);
                      
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.IpAddress);
            });
        }

        private void ConfigureSecurityAlerts(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SecurityAlert>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AlertType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Severity).HasMaxLength(20);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.ResolutionNotes).HasMaxLength(1000);
                
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
                      
                entity.HasOne(e => e.ResolvedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ResolvedByUserId)
                      .OnDelete(DeleteBehavior.NoAction);
                      
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.AlertType);
                entity.HasIndex(e => e.Severity);
                entity.HasIndex(e => e.IsResolved);
            });
        }

        private void ConfigureSystemEvents(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SystemEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Source).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Level).HasMaxLength(20);
                entity.Property(e => e.CorrelationId).HasMaxLength(100);
                entity.Property(e => e.UserId).HasMaxLength(50);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.Level);
                entity.HasIndex(e => e.CorrelationId);
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