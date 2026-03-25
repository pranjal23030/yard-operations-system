using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;
using YardOps.Models;

namespace YardOps.Services
{
    public class SnapshotService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ActivityLogger _activityLogger;

        public SnapshotService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ActivityLogger activityLogger)
        {
            _context = context;
            _userManager = userManager;
            _activityLogger = activityLogger;
        }

        public async Task<SnapshotOperationResult> CaptureCurrentInYardSnapshotAsync(ClaimsPrincipal user)
        {
            var currentUser = await _userManager.GetUserAsync(user);
            if (currentUser == null)
                return SnapshotOperationResult.Fail("User not found.");

            var now = DateTime.UtcNow;

            var trailersInYard = await _context.Trailers
                .Include(t => t.Carrier)
                .Include(t => t.DriverUser)
                .Include(t => t.CurrentLocation)
                .Where(t => t.CurrentStatus == "In-Yard" && t.CurrentLocationId != null)
                .OrderBy(t => t.TrailerCode)
                .ToListAsync();

            var run = new SnapshotRun
            {
                CapturedAt = now,
                CapturedBy = currentUser.Id,
                TotalInYard = trailersInYard.Count,
                CreatedOn = now,
                SnapshotItems = []
            };

            foreach (var trailer in trailersInYard)
            {
                var driverName = trailer.DriverUser != null
                    ? $"{trailer.DriverUser.FirstName} {trailer.DriverUser.LastName}".Trim()
                    : "Unknown";

                if (string.IsNullOrWhiteSpace(driverName))
                    driverName = trailer.DriverUser?.Email ?? "Unknown";

                run.SnapshotItems!.Add(new SnapshotItem
                {
                    TrailerId = trailer.TrailerId,
                    LocationId = trailer.CurrentLocationId!.Value,
                    Status = trailer.CurrentStatus,
                    ArrivalTime = trailer.ArrivalTime,
                    CapturedAt = now,
                    TrailerCode = trailer.TrailerCode,
                    CarrierName = trailer.Carrier?.CompanyName ?? "Unknown",
                    DriverName = driverName,
                    LocationName = trailer.CurrentLocation?.LocationName ?? "Unknown",
                    LocationType = trailer.CurrentLocation?.LocationType ?? "Unknown",
                    GoodsType = trailer.GoodsType,
                    CreatedBy = currentUser.Id,
                    CreatedOn = now
                });
            }

            _context.SnapshotRuns.Add(run);
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                action: "CaptureSnapshot",
                description: $"Captured yard snapshot run #{run.SnapshotRunId}",
                extraData: new
                {
                    SnapshotRunId = run.SnapshotRunId,
                    CapturedAt = run.CapturedAt,
                    TotalInYard = run.TotalInYard
                });

            return SnapshotOperationResult.Ok(
                $"Snapshot captured successfully. Run #{run.SnapshotRunId} with {run.TotalInYard} in-yard trailer(s).",
                run.SnapshotRunId);
        }

        public async Task<SnapshotRun?> GetLatestSnapshotAsync()
        {
            return await _context.SnapshotRuns
                .Include(sr => sr.CapturedByUser)
                .Include(sr => sr.SnapshotItems)
                .OrderByDescending(sr => sr.CapturedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<SnapshotRun?> GetSnapshotByRunIdAsync(int runId)
        {
            return await _context.SnapshotRuns
                .Include(sr => sr.CapturedByUser)
                .Include(sr => sr.SnapshotItems)
                .FirstOrDefaultAsync(sr => sr.SnapshotRunId == runId);
        }

        public sealed class SnapshotOperationResult
        {
            public bool IsSuccess { get; init; }
            public string Message { get; init; } = "";
            public int? SnapshotRunId { get; init; }

            public static SnapshotOperationResult Ok(string message, int snapshotRunId) =>
                new() { IsSuccess = true, Message = message, SnapshotRunId = snapshotRunId };

            public static SnapshotOperationResult Fail(string message) =>
                new() { IsSuccess = false, Message = message };
        }
    }
}