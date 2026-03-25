using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YardOps.Models;

namespace YardOps.Data
{
    /// <summary>
    /// Application database context for YardOps system.
    /// Inherits from IdentityDbContext to use ASP.NET Core Identity for user management.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ==================== EXISTING DBSETS ====================

        /// <summary>
        /// Activity logs for audit trail and user action tracking
        /// </summary>
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        /// <summary>
        /// Yard locations/facilities
        /// </summary>
        public DbSet<Yard> Yards { get; set; }

        /// <summary>
        /// Carriers/transportation companies
        /// </summary>
        public DbSet<Carrier> Carriers { get; set; }

        /// <summary>
        /// Locations within yards (zones, slots, docks, gates)
        /// </summary>
        public DbSet<Location> Locations { get; set; }

        // ==================== DBSETS - TRAILERS MODULE ====================

        /// <summary>
        /// Trailers in the yard operations system
        /// </summary>
        public DbSet<Trailer> Trailers { get; set; }

        /// <summary>
        /// Goods items within trailers (manifest)
        /// </summary>
        public DbSet<Goods> Goods { get; set; }

        /// <summary>
        /// Historical records of trailer movements and dwell times
        /// Used for analytics and ML training data
        /// </summary>
        public DbSet<TrailerHistory> TrailerHistories { get; set; }

        /// <summary>
        /// Gate entry operations (ingate audit log)
        /// </summary>
        public DbSet<Ingate> Ingates { get; set; }

        /// <summary>
        /// Gate exit operations (outgate audit log)
        /// </summary>
        public DbSet<Outgate> Outgates { get; set; }

        // ==================== DBSETS - SNAPSHOT MODULE ====================

        /// <summary>
        /// Snapshot capture run headers.
        /// </summary>
        public DbSet<SnapshotRun> SnapshotRuns { get; set; }

        /// <summary>
        /// Snapshot capture detail rows (one per trailer per run).
        /// </summary>
        public DbSet<SnapshotItem> SnapshotItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== EXISTING CONFIGURATIONS ====================

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

            // Configure Location -> Yard relationship
            modelBuilder.Entity<Location>()
                .HasOne(l => l.Yard)
                .WithMany()
                .HasForeignKey(l => l.YardId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Location -> User relationship
            modelBuilder.Entity<Location>()
                .HasOne(l => l.CreatedByUser)
                .WithMany()
                .HasForeignKey(l => l.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraint on LocationName per Yard
            modelBuilder.Entity<Location>()
                .HasIndex(l => new { l.YardId, l.LocationName })
                .IsUnique();

            // ==================== CONFIGURATIONS - TRAILERS MODULE ====================

            // ========== TRAILER CONFIGURATION ==========
            modelBuilder.Entity<Trailer>()
                .HasOne(t => t.Carrier)
                .WithMany()
                .HasForeignKey(t => t.CarrierId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Trailer>()
                .HasOne(t => t.CurrentLocation)
                .WithMany()
                .HasForeignKey(t => t.CurrentLocationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Trailer>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Trailer>()
                .HasOne(t => t.DriverUser)
                .WithMany()
                .HasForeignKey(t => t.DriverUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Trailer>()
                .HasIndex(t => t.TrailerCode)
                .IsUnique();

            // ========== GOODS CONFIGURATION ==========
            modelBuilder.Entity<Goods>()
                .HasOne(g => g.Trailer)
                .WithMany(t => t.GoodsItems)
                .HasForeignKey(g => g.TrailerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Goods>()
                .HasOne(g => g.CreatedByUser)
                .WithMany()
                .HasForeignKey(g => g.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Goods>()
                .Property(g => g.Weight)
                .HasPrecision(18, 2);

            // ========== TRAILER HISTORY CONFIGURATION ==========
            modelBuilder.Entity<TrailerHistory>()
                .HasOne(th => th.Trailer)
                .WithMany(t => t.HistoryRecords)
                .HasForeignKey(th => th.TrailerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrailerHistory>()
                .HasOne(th => th.Location)
                .WithMany()
                .HasForeignKey(th => th.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TrailerHistory>()
                .HasOne(th => th.CreatedByUser)
                .WithMany()
                .HasForeignKey(th => th.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TrailerHistory>()
                .HasIndex(th => new { th.TrailerId, th.StartTime });

            // ========== INGATE CONFIGURATION ==========
            modelBuilder.Entity<Ingate>()
                .HasOne(ig => ig.Trailer)
                .WithMany(t => t.IngateRecords)
                .HasForeignKey(ig => ig.TrailerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ingate>()
                .HasOne(ig => ig.Location)
                .WithMany()
                .HasForeignKey(ig => ig.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Ingate>()
                .HasOne(ig => ig.PerformedByUser)
                .WithMany()
                .HasForeignKey(ig => ig.PerformedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Ingate>()
                .HasOne(ig => ig.CreatedByUser)
                .WithMany()
                .HasForeignKey(ig => ig.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Ingate>()
                .HasIndex(ig => new { ig.TrailerId, ig.Timestamp });

            // ========== OUTGATE CONFIGURATION ==========
            modelBuilder.Entity<Outgate>()
                .HasOne(og => og.Trailer)
                .WithMany(t => t.OutgateRecords)
                .HasForeignKey(og => og.TrailerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Outgate>()
                .HasOne(og => og.Location)
                .WithMany()
                .HasForeignKey(og => og.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Outgate>()
                .HasOne(og => og.PerformedByUser)
                .WithMany()
                .HasForeignKey(og => og.PerformedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Outgate>()
                .HasOne(og => og.CreatedByUser)
                .WithMany()
                .HasForeignKey(og => og.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Outgate>()
                .HasIndex(og => new { og.TrailerId, og.Timestamp });

            // ==================== CONFIGURATIONS - SNAPSHOT MODULE ====================

            // SnapshotRun -> CapturedByUser
            modelBuilder.Entity<SnapshotRun>()
                .HasOne(sr => sr.CapturedByUser)
                .WithMany()
                .HasForeignKey(sr => sr.CapturedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SnapshotRun>()
                .HasIndex(sr => sr.CapturedAt);

            // SnapshotItem -> SnapshotRun
            modelBuilder.Entity<SnapshotItem>()
                .HasOne(si => si.SnapshotRun)
                .WithMany(sr => sr.SnapshotItems)
                .HasForeignKey(si => si.SnapshotRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // SnapshotItem -> Trailer
            modelBuilder.Entity<SnapshotItem>()
                .HasOne(si => si.Trailer)
                .WithMany()
                .HasForeignKey(si => si.TrailerId)
                .OnDelete(DeleteBehavior.NoAction);

            // SnapshotItem -> Location
            modelBuilder.Entity<SnapshotItem>()
                .HasOne(si => si.Location)
                .WithMany()
                .HasForeignKey(si => si.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            // SnapshotItem -> CreatedByUser
            modelBuilder.Entity<SnapshotItem>()
                .HasOne(si => si.CreatedByUser)
                .WithMany()
                .HasForeignKey(si => si.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SnapshotItem>()
                .HasIndex(si => si.SnapshotRunId);

            modelBuilder.Entity<SnapshotItem>()
                .HasIndex(si => si.CapturedAt);

            modelBuilder.Entity<SnapshotItem>()
                .HasIndex(si => si.Status);

            modelBuilder.Entity<SnapshotItem>()
                .HasIndex(si => new { si.SnapshotRunId, si.TrailerId })
                .IsUnique();
        }
    }
}