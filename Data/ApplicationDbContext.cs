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

        // ==================== NEW DBSETS - TRAILERS MODULE ====================

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

            // ==================== NEW CONFIGURATIONS - TRAILERS MODULE ====================

            // ========== TRAILER CONFIGURATION ==========
            
            /// <summary>
            /// Configure Trailer -> Carrier relationship
            /// A trailer must belong to exactly one carrier.
            /// If carrier is deleted, trailer cascade deletes (orphaned trailers cleanup).
            /// </summary>
            modelBuilder.Entity<Trailer>()
                .HasOne(t => t.Carrier)
                .WithMany()
                .HasForeignKey(t => t.CarrierId)
                .OnDelete(DeleteBehavior.Cascade);

            /// <summary>
            /// Configure Trailer -> Location relationship (optional)
            /// A trailer may or may not have a current location assigned.
            /// </summary>
            modelBuilder.Entity<Trailer>()
                .HasOne(t => t.CurrentLocation)
                .WithMany()
                .HasForeignKey(t => t.CurrentLocationId)
                .OnDelete(DeleteBehavior.SetNull);

            /// <summary>
            /// Configure Trailer -> CreatedByUser relationship
            /// Track who created the trailer record
            /// </summary>
            modelBuilder.Entity<Trailer>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            /// <summary>
            /// Configure Trailer -> DriverUser relationship
            /// Track which driver is assigned to the trailer
            /// </summary>
            modelBuilder.Entity<Trailer>()
                .HasOne(t => t.DriverUser)
                .WithMany()
                .HasForeignKey(t => t.DriverUserId)
                .OnDelete(DeleteBehavior.NoAction);

            /// <summary>
            /// Unique constraint on TrailerCode
            /// Ensures each trailer has a unique identifier
            /// </summary>
            modelBuilder.Entity<Trailer>()
                .HasIndex(t => t.TrailerCode)
                .IsUnique();

            // ========== GOODS CONFIGURATION ==========

            /// <summary>
            /// Configure Goods -> Trailer relationship
            /// A goods item must belong to exactly one trailer.
            /// If trailer is deleted, all its goods are cascade deleted.
            /// </summary>
            modelBuilder.Entity<Goods>()
                .HasOne(g => g.Trailer)
                .WithMany(t => t.GoodsItems)
                .HasForeignKey(g => g.TrailerId)
                .OnDelete(DeleteBehavior.Cascade);

            /// <summary>
            /// Configure Goods -> CreatedByUser relationship
            /// Track who added this goods item
            /// </summary>
            modelBuilder.Entity<Goods>()
                .HasOne(g => g.CreatedByUser)
                .WithMany()
                .HasForeignKey(g => g.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Set precision for Goods.Weight column
            modelBuilder.Entity<Goods>()
                .Property(g => g.Weight)
                .HasPrecision(18, 2);

            // ========== TRAILER HISTORY CONFIGURATION ==========

            /// <summary>
            /// Configure TrailerHistory -> Trailer relationship
            /// A history record must belong to exactly one trailer.
            /// If trailer is deleted, all its history is cascade deleted.
            /// </summary>
            modelBuilder.Entity<TrailerHistory>()
                .HasOne(th => th.Trailer)
                .WithMany(t => t.HistoryRecords)
                .HasForeignKey(th => th.TrailerId)
                .OnDelete(DeleteBehavior.Cascade);

            /// <summary>
            /// Configure TrailerHistory -> Location relationship
            /// A history record references a location where trailer stayed.
            /// If location is deleted, set to null (historical record remains).
            /// </summary>
            modelBuilder.Entity<TrailerHistory>()
                .HasOne(th => th.Location)
                .WithMany()
                .HasForeignKey(th => th.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            /// <summary>
            /// Configure TrailerHistory -> CreatedByUser relationship
            /// Track who created this history record
            /// </summary>
            modelBuilder.Entity<TrailerHistory>()
                .HasOne(th => th.CreatedByUser)
                .WithMany()
                .HasForeignKey(th => th.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            /// <summary>
            /// Create composite index on TrailerId and StartTime
            /// Optimizes queries for finding trailer history within a date range
            /// </summary>
            modelBuilder.Entity<TrailerHistory>()
                .HasIndex(th => new { th.TrailerId, th.StartTime });

            // ========== INGATE CONFIGURATION ==========

            /// <summary>
            /// Configure Ingate -> Trailer relationship
            /// An ingate record must reference exactly one trailer.
            /// If trailer is deleted, cascade delete the ingate record.
            /// </summary>
            modelBuilder.Entity<Ingate>()
                .HasOne(ig => ig.Trailer)
                .WithMany(t => t.IngateRecords)
                .HasForeignKey(ig => ig.TrailerId)
                .OnDelete(DeleteBehavior.Cascade);

            /// <summary>
            /// Configure Ingate -> Location relationship
            /// An ingate record references the gate location through which trailer entered.
            /// If location is deleted, set to null (audit record still valuable).
            /// </summary>
            modelBuilder.Entity<Ingate>()
                .HasOne(ig => ig.Location)
                .WithMany()
                .HasForeignKey(ig => ig.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            /// <summary>
            /// Configure Ingate -> PerformedByUser relationship
            /// References the ApplicationUser who performed the ingate operation.
            /// </summary>
            modelBuilder.Entity<Ingate>()
                .HasOne(ig => ig.PerformedByUser)
                .WithMany()
                .HasForeignKey(ig => ig.PerformedByUserId)
                .OnDelete(DeleteBehavior.NoAction); // CHANGED from SetNull

            /// <summary>
            /// Configure Ingate -> CreatedByUser relationship
            /// Track who created this audit record
            /// </summary>
            modelBuilder.Entity<Ingate>()
                .HasOne(ig => ig.CreatedByUser)
                .WithMany()
                .HasForeignKey(ig => ig.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            /// <summary>
            /// Create composite index on TrailerId and Timestamp
            /// Optimizes queries for finding ingate operations for a specific trailer
            /// </summary>
            modelBuilder.Entity<Ingate>()
                .HasIndex(ig => new { ig.TrailerId, ig.Timestamp });

            // ========== OUTGATE CONFIGURATION ==========

            /// <summary>
            /// Configure Outgate -> Trailer relationship
            /// An outgate record must reference exactly one trailer.
            /// If trailer is deleted, cascade delete the outgate record.
            /// </summary>
            modelBuilder.Entity<Outgate>()
                .HasOne(og => og.Trailer)
                .WithMany(t => t.OutgateRecords)
                .HasForeignKey(og => og.TrailerId)
                .OnDelete(DeleteBehavior.Cascade);

            /// <summary>
            /// Configure Outgate -> Location relationship
            /// An outgate record references the gate location through which trailer exited.
            /// If location is deleted, set to null (audit record still valuable).
            /// </summary>
            modelBuilder.Entity<Outgate>()
                .HasOne(og => og.Location)
                .WithMany()
                .HasForeignKey(og => og.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            /// <summary>
            /// Configure Outgate -> PerformedByUser relationship
            /// References the ApplicationUser who performed the outgate operation.
            /// </summary>
            modelBuilder.Entity<Outgate>()
                .HasOne(og => og.PerformedByUser)
                .WithMany()
                .HasForeignKey(og => og.PerformedByUserId)
                .OnDelete(DeleteBehavior.NoAction); // CHANGED from SetNull

            /// <summary>
            /// Configure Outgate -> CreatedByUser relationship
            /// Track who created this audit record
            /// </summary>
            modelBuilder.Entity<Outgate>()
                .HasOne(og => og.CreatedByUser)
                .WithMany()
                .HasForeignKey(og => og.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            /// <summary>
            /// Create composite index on TrailerId and Timestamp
            /// Optimizes queries for finding outgate operations for a specific trailer
            /// </summary>
            modelBuilder.Entity<Outgate>()
                .HasIndex(og => new { og.TrailerId, og.Timestamp });
        }
    }
}