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
        public DbSet<Carrier> Carriers { get; set; }  // ⭐ NEW

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Yard -> User relationship
            modelBuilder.Entity<Yard>()
                .HasOne(y => y.CreatedByUser)
                .WithMany()
                .HasForeignKey(y => y.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull); // If user deleted, set CreatedBy to null

            // ⭐ Configure Carrier -> User relationship
            modelBuilder.Entity<Carrier>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // ⭐ Unique constraint on CarrierCode
            modelBuilder.Entity<Carrier>()
                .HasIndex(c => c.CarrierCode)
                .IsUnique();
        }
    }
}