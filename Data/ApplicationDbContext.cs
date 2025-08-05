using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DocReachApi.Models;

namespace DocReachApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<KycSubmission> KycSubmissions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure Doctor
            builder.Entity<Doctor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MedicalLicenseNumber).IsRequired();
                entity.Property(e => e.HospitalAffiliation).IsRequired();
                entity.Property(e => e.Degree).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                // Relationship with ApplicationUser
                entity.HasOne(d => d.User)
                    .WithOne(u => u.Doctor)
                    .HasForeignKey<Doctor>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Admin
            builder.Entity<Admin>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AdminRole).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                // Relationship with ApplicationUser
                entity.HasOne(a => a.User)
                    .WithOne(u => u.Admin)
                    .HasForeignKey<Admin>(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure KycSubmission
            builder.Entity<KycSubmission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DocumentType).IsRequired();
                entity.Property(e => e.DocumentPath).IsRequired();
                entity.Property(e => e.Status).HasDefaultValue("Pending");
                entity.Property(e => e.SubmittedAt).HasDefaultValueSql("GETUTCDATE()");
                
                // Relationship with ApplicationUser
                entity.HasOne(k => k.User)
                    .WithMany()
                    .HasForeignKey(k => k.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
} 