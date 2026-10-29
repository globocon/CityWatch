using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services
{
    public interface IDeviceTokenService
    {
        /// <summary>Registers (or re-homes) a token for a unit. False when the (unit,
        /// session) pair has no ACTIVE session — the ingest trust model: a device can only
        /// register a token for the unit it is currently signed into (§13.1.1).</summary>
        Task<bool> RegisterAsync(int unitId, Guid sessionId, string token, string? platform, CancellationToken ct);

        /// <summary>Logout path: deactivates by token value. Knowing the token IS the
        /// credential here — only the device that holds it can release it. Idempotent.</summary>
        Task ReleaseAsync(string token, CancellationToken ct);

        Task<List<TrackingDeviceToken>> GetActiveAsync(int unitId, CancellationToken ct);

        /// <summary>All active tokens for a set of units in ONE query — a broadcast must
        /// never turn into an N+1 over the token table.</summary>
        Task<List<TrackingDeviceToken>> GetActiveForUnitsAsync(IReadOnlyCollection<int> unitIds,
            CancellationToken ct);

        /// <summary>FCM said Unregistered/InvalidArgument: the token is dead, stop using it.</summary>
        Task MarkInvalidAsync(int tokenId, CancellationToken ct);
    }

    public sealed class DeviceTokenService : IDeviceTokenService
    {
        private readonly TrackingDbContext _db;
        private readonly ILogger<DeviceTokenService> _logger;
        private readonly Func<DateTime> _utcNow;

        public DeviceTokenService(TrackingDbContext db, ILogger<DeviceTokenService> logger,
            Func<DateTime>? utcNow = null)
        {
            _db = db;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<bool> RegisterAsync(int unitId, Guid sessionId, string token, string? platform,
            CancellationToken ct)
        {
            var hasActiveSession = await _db.TrackingSessions
                .AnyAsync(s => s.Id == sessionId && s.UnitId == unitId && s.Status == "Active", ct);
            if (!hasActiveSession)
                return false;

            var now = _utcNow();

            /* Upsert by token, AsTracking because we mutate (the context defaults to
               NoTracking — the 12 Aug lesson). A token that re-registers under a different
               unit RE-HOMES: the phone changed cars, and a live token must never keep
               pointing at a unit its phone no longer serves. */
            var existing = await _db.TrackingDeviceTokens
                .AsTracking()
                .FirstOrDefaultAsync(t => t.FcmToken == token, ct);

            if (existing != null)
            {
                existing.UnitId = unitId;
                existing.Platform = string.IsNullOrWhiteSpace(platform) ? existing.Platform : platform.Trim();
                existing.UpdatedUtc = now;
                existing.LastSeenUtc = now;
                existing.IsActive = true;
                existing.InvalidatedUtc = null;
            }
            else
            {
                _db.TrackingDeviceTokens.Add(new TrackingDeviceToken
                {
                    UnitId = unitId,
                    FcmToken = token,
                    Platform = string.IsNullOrWhiteSpace(platform) ? "android" : platform.Trim(),
                    CreatedUtc = now,
                    UpdatedUtc = now,
                    LastSeenUtc = now,
                    IsActive = true
                });
            }
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Device token registered for unit {Unit} ({Action}).",
                unitId, existing == null ? "new" : "refreshed");
            return true;
        }

        public async Task ReleaseAsync(string token, CancellationToken ct)
        {
            var now = _utcNow();
            var existing = await _db.TrackingDeviceTokens
                .AsTracking()
                .FirstOrDefaultAsync(t => t.FcmToken == token && t.IsActive, ct);
            if (existing == null)
                return;                              // already released / never registered

            existing.IsActive = false;
            existing.InvalidatedUtc = now;
            existing.UpdatedUtc = now;
            await _db.SaveChangesAsync(ct);
        }

        public Task<List<TrackingDeviceToken>> GetActiveAsync(int unitId, CancellationToken ct)
            => _db.TrackingDeviceTokens
                .Where(t => t.UnitId == unitId && t.IsActive)
                .OrderByDescending(t => t.LastSeenUtc)
                .ToListAsync(ct);

        public Task<List<TrackingDeviceToken>> GetActiveForUnitsAsync(IReadOnlyCollection<int> unitIds,
            CancellationToken ct)
            => _db.TrackingDeviceTokens
                .Where(t => unitIds.Contains(t.UnitId) && t.IsActive)
                .ToListAsync(ct);

        public async Task MarkInvalidAsync(int tokenId, CancellationToken ct)
        {
            var now = _utcNow();
            var token = await _db.TrackingDeviceTokens
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == tokenId, ct);
            if (token == null || !token.IsActive)
                return;

            token.IsActive = false;
            token.InvalidatedUtc = now;
            token.UpdatedUtc = now;
            await _db.SaveChangesAsync(ct);
        }
    }
}
