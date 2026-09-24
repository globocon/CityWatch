using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CityWatch.Tracking.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Hosted
{
    /// <summary>
    /// Drains the point channel and writes with SqlBulkCopy — never through EF Core change
    /// tracking (§8.3 read rule's write-side twin). Flush triggers: 500 points or 1 second,
    /// whichever first. Runs on every instance (each drains its own channel); it is NOT a
    /// leader-only job (§7.4).
    ///
    /// If the database is briefly unavailable, points are retried in place: ingest keeps
    /// accepting, the live map keeps moving, and the bounded channel ahead of us absorbs the
    /// backlog by dropping oldest — degraded honestly rather than failing loudly.
    /// </summary>
    public sealed class PositionWriter : BackgroundService
    {
        private const int FlushCount = 500;
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        private readonly ChannelReader<TrackPoint> _reader;
        private readonly string _connectionString;
        private readonly ILogger<PositionWriter> _logger;

        public PositionWriter(ChannelReader<TrackPoint> reader, string connectionString, ILogger<PositionWriter> logger)
        {
            _reader = reader;
            _connectionString = connectionString;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var buffer = new List<TrackPoint>(FlushCount);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    buffer.Clear();

                    /* Block until the first point, then sweep whatever else arrives inside the
                       flush window. One bulk copy per window, not one insert per point. */
                    if (!await _reader.WaitToReadAsync(stoppingToken))
                        break;

                    var windowEnds = DateTime.UtcNow + FlushInterval;
                    while (buffer.Count < FlushCount && _reader.TryRead(out var point))
                        buffer.Add(point);

                    while (buffer.Count < FlushCount && DateTime.UtcNow < windowEnds)
                    {
                        if (_reader.TryRead(out var point))
                            buffer.Add(point);
                        else
                            await Task.Delay(50, stoppingToken);
                    }

                    if (buffer.Count > 0)
                        await FlushWithRetryAsync(buffer, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown: fall through to the final drain below.
            }

            /* Best-effort final drain so a clean shutdown does not orphan buffered points. */
            buffer.Clear();
            while (_reader.TryRead(out var point))
                buffer.Add(point);
            if (buffer.Count > 0)
                await FlushWithRetryAsync(buffer, CancellationToken.None, maxAttempts: 1);
        }

        private async Task FlushWithRetryAsync(List<TrackPoint> points, CancellationToken ct, int maxAttempts = 3)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await BulkCopyAsync(points, ct);
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts && !ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "TrackPoint flush failed (attempt {Attempt}); retrying {Count} points.",
                        attempt, points.Count);
                    await Task.Delay(RetryDelay, ct);
                }
                catch (Exception ex)
                {
                    /* Dropping after retries is the designed failure mode, and it must be loud.
                       The evidentiary loss is bounded: NFC anchors are recoverable from the
                       platform's own scan records. */
                    _logger.LogError(ex, "TrackPoint flush abandoned after {Attempts} attempts; {Count} points lost.",
                        attempt, points.Count);
                    return;
                }
            }
        }

        private async Task BulkCopyAsync(List<TrackPoint> points, CancellationToken ct)
        {
            var table = BuildTable(points);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            using var bulk = new SqlBulkCopy(connection)
            {
                DestinationTableName = "dbo.TrackPoint",
                BatchSize = points.Count,
                BulkCopyTimeout = 30
            };

            /* Explicit mappings: never positional against a table someone may extend. */
            foreach (var column in new[]
            {
                "UnitId", "SessionId", "Seq", "RecordedUtc", "ReceivedUtc", "Latitude", "Longitude",
                "SpeedKph", "HeadingDeg", "AccuracyM", "BatteryPct", "SourceType", "ModeAtCapture",
                "Flags", "AnchorTagUid"
            })
            {
                bulk.ColumnMappings.Add(column, column);
            }

            await bulk.WriteToServerAsync(table, ct);
        }

        private static DataTable BuildTable(List<TrackPoint> points)
        {
            var table = new DataTable();
            table.Columns.Add("UnitId", typeof(int));
            table.Columns.Add("SessionId", typeof(Guid));
            table.Columns.Add("Seq", typeof(int));
            table.Columns.Add("RecordedUtc", typeof(DateTime));
            table.Columns.Add("ReceivedUtc", typeof(DateTime));
            table.Columns.Add("Latitude", typeof(decimal));
            table.Columns.Add("Longitude", typeof(decimal));
            table.Columns.Add("SpeedKph", typeof(short));
            table.Columns.Add("HeadingDeg", typeof(short));
            table.Columns.Add("AccuracyM", typeof(short));
            table.Columns.Add("BatteryPct", typeof(byte));
            table.Columns.Add("SourceType", typeof(byte));
            table.Columns.Add("ModeAtCapture", typeof(byte));
            table.Columns.Add("Flags", typeof(byte));
            table.Columns.Add("AnchorTagUid", typeof(string));

            foreach (var p in points)
            {
                table.Rows.Add(
                    p.UnitId, p.SessionId, p.Seq, p.RecordedUtc, p.ReceivedUtc, p.Latitude, p.Longitude,
                    (object?)p.SpeedKph ?? DBNull.Value,
                    (object?)p.HeadingDeg ?? DBNull.Value,
                    (object?)p.AccuracyM ?? DBNull.Value,
                    (object?)p.BatteryPct ?? DBNull.Value,
                    p.SourceType, p.ModeAtCapture, p.Flags,
                    (object?)p.AnchorTagUid ?? DBNull.Value);
            }

            return table;
        }
    }
}
