using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;
using YardOps.Models;
using YardOps.Models.ViewModels.Operations;

namespace YardOps.Services
{
    public class GateOperationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ActivityLogger _activityLogger;

        public GateOperationService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ActivityLogger activityLogger)
        {
            _context = context;
            _userManager = userManager;
            _activityLogger = activityLogger;
        }

        public async Task<GateOperationResult> LogIngateAsync(CreateIngateInput input, ClaimsPrincipal user)
        {
            var currentUser = await _userManager.GetUserAsync(user);
            if (currentUser == null)
                return GateOperationResult.Fail("User not found.");

            var trailer = await _context.Trailers.FirstOrDefaultAsync(t => t.TrailerId == input.TrailerId);
            if (trailer == null)
                return GateOperationResult.Fail("Trailer not found.");

            var gate = await _context.Locations.FirstOrDefaultAsync(l => l.LocationId == input.GateLocationId);
            if (gate == null || gate.LocationType != "Gate")
                return GateOperationResult.Fail("Selected location is not a valid gate.");

            var resolvedLocationId = input.AssignedLocationId > 0 ? input.AssignedLocationId : input.GateLocationId;
            var assignedLocation = await _context.Locations.FirstOrDefaultAsync(l => l.LocationId == resolvedLocationId);
            if (assignedLocation == null)
                return GateOperationResult.Fail("Assigned location is invalid.");

            var now = DateTime.UtcNow;

            _context.Ingates.Add(new Ingate
            {
                TrailerId = trailer.TrailerId,
                LocationId = input.GateLocationId,
                PerformedByUserId = currentUser.Id,
                Timestamp = now,
                Notes = input.Notes?.Trim(),
                CreatedBy = currentUser.Id,
                CreatedOn = now
            });

            trailer.CurrentStatus = "In-Yard";
            trailer.ArrivalTime ??= now;
            trailer.CurrentLocationId = resolvedLocationId;

            _context.TrailerHistories.Add(new TrailerHistory
            {
                TrailerId = trailer.TrailerId,
                LocationId = resolvedLocationId,
                StartTime = now,
                EndTime = null,
                CreatedBy = currentUser.Id,
                CreatedOn = now
            });

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                action: "Ingate",
                description: $"Trailer {trailer.TrailerCode} entered through {gate.LocationName}",
                extraData: new
                {
                    TrailerCode = trailer.TrailerCode,
                    Gate = gate.LocationName,
                    AssignedLocation = assignedLocation.LocationName,
                    input.Notes
                });

            return GateOperationResult.Ok($"Ingate recorded for trailer '{trailer.TrailerCode}'.");
        }

        public async Task<GateOperationResult> LogOutgateAsync(CreateOutgateInput input, ClaimsPrincipal user)
        {
            var currentUser = await _userManager.GetUserAsync(user);
            if (currentUser == null)
                return GateOperationResult.Fail("User not found.");

            var trailer = await _context.Trailers.FirstOrDefaultAsync(t => t.TrailerId == input.TrailerId);
            if (trailer == null)
                return GateOperationResult.Fail("Trailer not found.");

            var gate = await _context.Locations.FirstOrDefaultAsync(l => l.LocationId == input.GateLocationId);
            if (gate == null || gate.LocationType != "Gate")
                return GateOperationResult.Fail("Selected location is not a valid gate.");

            var now = DateTime.UtcNow;
            var previousLocationId = trailer.CurrentLocationId;

            _context.Outgates.Add(new Outgate
            {
                TrailerId = trailer.TrailerId,
                LocationId = input.GateLocationId,
                PerformedByUserId = currentUser.Id,
                Timestamp = now,
                Notes = input.Notes?.Trim(),
                CreatedBy = currentUser.Id,
                CreatedOn = now
            });

            trailer.CurrentStatus = "Checked Out";
            trailer.DepartureTime = now;
            trailer.CurrentLocationId = null;

            var activeHistory = await _context.TrailerHistories
                .Where(h => h.TrailerId == trailer.TrailerId && h.EndTime == null)
                .OrderByDescending(h => h.StartTime)
                .FirstOrDefaultAsync();

            if (activeHistory != null)
            {
                activeHistory.EndTime = now;
            }
            else
            {
                _context.TrailerHistories.Add(new TrailerHistory
                {
                    TrailerId = trailer.TrailerId,
                    LocationId = previousLocationId ?? input.GateLocationId,
                    StartTime = trailer.ArrivalTime ?? trailer.CreatedOn,
                    EndTime = now,
                    CreatedBy = currentUser.Id,
                    CreatedOn = now
                });
            }

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                action: "Outgate",
                description: $"Trailer {trailer.TrailerCode} exited through {gate.LocationName}",
                extraData: new
                {
                    TrailerCode = trailer.TrailerCode,
                    Gate = gate.LocationName,
                    input.Notes
                });

            return GateOperationResult.Ok($"Outgate recorded for trailer '{trailer.TrailerCode}'.");
        }

        public sealed class GateOperationResult
        {
            public bool IsSuccess { get; init; }
            public string Message { get; init; } = "";

            public static GateOperationResult Ok(string message) => new() { IsSuccess = true, Message = message };
            public static GateOperationResult Fail(string message) => new() { IsSuccess = false, Message = message };
        }
    }
}