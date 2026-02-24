using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YardOps.Models;

namespace YardOps.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Yard> Yards { get; set; }
        public DbSet<Carrier> Carriers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Yard -> User relationship
            modelBuilder.Entity<Yard>()
                .HasOne(y => y.CreatedByUser)
                .WithMany()
                .HasForeignKey(y => y.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Carrier -> User relationship
            modelBuilder.Entity<Carrier>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraint on CarrierCode
            modelBuilder.Entity<Carrier>()
                .HasIndex(c => c.CarrierCode)
                .IsUnique();

            // Configure ActivityLog -> User relationship
            modelBuilder.Entity<ActivityLog>()
                .HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedBy)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ApplicationUser -> CreatedByUser (self-referencing)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.CreatedByUser)
                .WithMany()
                .HasForeignKey(u => u.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure ApplicationRole -> CreatedByUser
            modelBuilder.Entity<ApplicationRole>()
                .HasOne(r => r.CreatedByUser)
                .WithMany()
                .HasForeignKey(r => r.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}