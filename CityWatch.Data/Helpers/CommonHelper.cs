using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Reflection;
using CityWatch.Data.Models;

namespace CityWatch.Data.Helpers
{
    public static class CommonHelper
    {
        public static bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();
            if (trimmedEmail.EndsWith("."))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }
    }

    public static class DateTimeExtensions
    {
        public static DateTime UtcToAest(this DateTime utcDateTime)
        {
            return utcDateTime.AddHours(10);
        }
    }

    public static class EnumExtensions
    {
        public static string ToDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }

        public static string ToDisplayName(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DisplayAttribute attr = Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)) as DisplayAttribute;
                    if (attr != null)
                    {
                        return attr.GetName();
                    }
                }
            }
            return name;
        }
    }

    // Task p6#73_TimeZone issue -- added by Binoy - Start
    public static class DateTimeHelper
    {
        public static DateTime GetCurrentLocalTimeFromUtcMinute(int utcmin)
        {
            var CurrLocalTime = DateTime.UtcNow.AddMinutes(utcmin);
            return CurrLocalTime;
        }

       
        public static DateTime GetLogbookEndTimeFromDate(DateTime ldtm)
        {            
            return new DateTime(ldtm.Year, ldtm.Month, ldtm.Day, 23, 59, 00);
        }

        //p7-RosterDurationQuarterHourFix-start
        // WHY THIS LOGIC CHANGED (roster shift duration - reported: 21:29-23:59 showed 2.75h, should be 2.50h)
        //
        // The roster does not allow 00:00 or 24:00 to be typed, so midnight is stored as 00:01 (start)
        // or 23:59 (end)  - see Booking.cshtml "Minimum value is 00:01 and maximum is 23:59".
        // To undo that, this method used to push those two values back to real midnight BEFORE rounding:
        //       00:01 -> 00:00        (made the shift 1 minute LONGER)
        //       23:59 -> 24:00        (made the shift 1 minute LONGER)
        //
        // Durations are only ever shown in quarter hours (.00 / .25 / .50 / .75) and the rounding below
        // is Math.Ceiling - it always rounds UP. So that single extra minute pushed any shift whose real
        // length already landed exactly on a quarter-hour boundary up by a WHOLE quarter hour:
        //       21:29 - 23:59  =  2h 30m  =  2.50   ->  +1 min = 2h 31m  ->  rounded up to 2.75   WRONG
        // and because the roster card multiplies duration x pay rate, the shift was also over-paid
        // (2.75 x $40 = $110 instead of 2.50 x $40 = $100).
        // The overnight-split confirmation popup already measured the same shift literally as "2h 30m",
        // so the app was contradicting itself between the popup and the card.
        //
        // The two normalisations are NOT needed: rounding UP to the next quarter already absorbs the
        // missing minute, so a full day still comes out at exactly 24.00 without them -
        //       00:01 - 23:59  =  23h 59m  =  23.9833  ->  x4 = 95.93  ->  ceiling 96  ->  24.00  CORRECT
        // Removing them therefore fixes the over-count and leaves every other shift unchanged.
        //
        // This is the ONLY place roster duration is calculated, so this fix applies everywhere at once:
        // roster board (Booking), external group view, guard roster popup (GuardRosterAction /
        // _GuardRosterModal), the mobile API, and both PDF reports (RosterReportGenerator and
        // GuardRosterReportGenerator).
        public static double CalculateDisplayDuration(DateTime start, DateTime end)
        {
            // OLD - added 1 minute at each end before rounding, which pushed exact quarter-hour
            //       shifts up a full 15 minutes (see explanation above):
            // // Normalize Start: Treat 00:01 as 00:00
            // if (start.TimeOfDay == TimeSpan.FromMinutes(1))
            // {
            //     start = start.Date;
            // }
            //
            // // Normalize End: Treat 23:59 as 24:00 (00:00 of next day)
            // if (end.Hour == 23 && end.Minute == 59)
            // {
            //     end = end.Date.AddDays(1);
            // }

            // NEW - use the times exactly as they are stored. The round-up below already covers the
            //       00:01 / 23:59 midnight convention, so no minute is added first.
            double totalHours = (end - start).TotalHours;

            // Round UP to the next 0.25 increment (durations are only ever .00 / .25 / .50 / .75)
            // We multiply by 4, take the Ceiling (rounds up to next whole number), then divide by 4.
            return Math.Ceiling(totalHours * 4) / 4;
        }
        //p7-RosterDurationQuarterHourFix-end
    }

    // Task p6#73_TimeZone issue -- added by Binoy - End


    // ------------------------------------------------------------------------------------------
    // Roster shift ordering inside a single day-cell.
    //
    // WHY THIS EXISTS (real roster example):
    //   A guard can be split into several shifts in the same day because his POSITION changes,
    //   even though he works one continuous stretch:
    //       Shane  08:00 - 16:00  (COX)
    //       Shane  16:00 - 20:00  (G1)     <- continuous handover: his end 16:00 == his start 16:00
    //       Jesse  15:00 - 23:00  (G2)
    //
    //   A plain OrderBy(ShiftStart) sorts every card by its own start time and produces:
    //       Shane 08:00,  Jesse 15:00,  Shane 16:00
    //   i.e. Jesse gets "slammed" in BETWEEN Shane's two continuous shifts, only because 15:00 is
    //   numerically before 16:00. The grid is not smart enough to know Shane actually started 08:00.
    //
    //   This sorter keeps back-to-back shifts of the SAME guard together as one block, anchored to
    //   the block's earliest start, so the result is:
    //       Shane 08:00 - 16:00,  Shane 16:00 - 20:00,  Jesse 15:00 - 23:00
    //
    //   Rule (from the requirement): "WHERE a guard finishes @ X time, and starts again @ X time,
    //   then irrespective of times, the shifts should be next to each other."
    //   Trigger = SAME guard AND previous.ShiftEnd == next.ShiftStart (exact continuous handover).
    //
    // Used by: Booking page (Projects + Groups grids), External Group view, and the Roster PDF
    // generator, so the on-screen order and the printed order always match.
    // ------------------------------------------------------------------------------------------
    public static class RosterShiftSorter
    {
        /// <summary>
        /// Orders the shifts of a single day-cell so that continuous (back-to-back) shifts of the
        /// same guard stay adjacent instead of being interleaved with another guard's shift.
        /// Behaviour is identical to OrderBy(ShiftStart) for cells where no guard has continuous
        /// split shifts, so existing rosters are unaffected.
        /// </summary>
        public static IEnumerable<RosterSchedule> OrderByContinuousBlocks(IEnumerable<RosterSchedule> shifts)
        {
            var list = shifts as IList<RosterSchedule> ?? shifts.ToList();

            // Identity for "the same guard". Falls back to provider name for external shifts that
            // are not tied to a specific guard, so two back-to-back external shifts also chain.
            // Returns null when neither is known -> such a shift never chains with anything.
            string KeyOf(RosterSchedule s) =>
                s.GuardId.HasValue ? "G:" + s.GuardId.Value
                : (!string.IsNullOrEmpty(s.ProviderName) ? "P:" + s.ProviderName : null);

            // anchorStart[shift] = earliest start time of the continuous block the shift belongs to.
            var anchorStart = new Dictionary<RosterSchedule, DateTime>();

            // Walk each guard/provider independently, in start-time order, chaining a shift onto the
            // previous one whenever the previous shift ends exactly when this one starts.
            foreach (var grp in list.GroupBy(KeyOf))
            {
                if (grp.Key == null)
                {
                    // Unknown guard/provider: never chains; each shift anchors to itself.
                    foreach (var s in grp)
                        anchorStart[s] = s.ShiftStart;
                    continue;
                }

                DateTime blockAnchor = DateTime.MinValue;
                DateTime prevEnd = DateTime.MinValue;
                bool inBlock = false;

                foreach (var s in grp.OrderBy(x => x.ShiftStart))
                {
                    if (!(inBlock && s.ShiftStart == prevEnd))
                    {
                        // Not a continuous handover -> this shift starts a brand new block.
                        blockAnchor = s.ShiftStart;
                        inBlock = true;
                    }
                    anchorStart[s] = blockAnchor;
                    prevEnd = s.ShiftEnd;
                }
            }

            // Final order: by block anchor first (so a whole continuous block moves as one unit),
            // then by guard/provider key (keeps two blocks that share an anchor grouped together),
            // then by the shift's own start time (orders the cards inside a block correctly).
            return list
                .OrderBy(s => anchorStart[s])
                .ThenBy(s => KeyOf(s) ?? "~")
                .ThenBy(s => s.ShiftStart)
                .ToList();
        }
    }


    public static class TimeZoneHelper
    {

        public static string GetCurrentTimeZone()
        {            
            TimeZoneInfo localZone = TimeZoneInfo.Local;
            var mint = (int)localZone.BaseUtcOffset.TotalMinutes;
            string[] arr = Convert.ToString(localZone.BaseUtcOffset).Split(":");
            var CurrLocalTime = localZone.StandardName + " " + string.Format("GMT{0}:{1}", mint > 0 ? '+' + arr[0] : arr[0], arr[1]); 
            return CurrLocalTime;
        }
        public static string GetCurrentTimeZoneShortName()
        {
            TimeZoneInfo localZone = TimeZoneInfo.Local;
            var mint = (int)localZone.BaseUtcOffset.TotalMinutes;
            string[] arr = Convert.ToString(localZone.BaseUtcOffset).Split(":");
            var CurrLocalTime = string.Format("GMT{0}:{1}", mint > 0 ? '+' + arr[0] : arr[0], arr[1]);
            return CurrLocalTime;
        }

        public static int GetCurrentTimeZoneOffsetMinute()
        {
            TimeZoneInfo localZone = TimeZoneInfo.Local;
            var CurrLocalTime = localZone.BaseUtcOffset ;
            return (int) CurrLocalTime.TotalMinutes;
        }

        public static DateTime GetCurrentTimeZoneCurrentTime()
        {            
            var CurrLocalTime = DateTime.Now;
            return CurrLocalTime;
        }

        public static DateTimeOffset GetCurrentTimeZoneCurrentTimeWithOffset()
        {
            var CurrLocalTime = DateTimeOffset.Now;
            return CurrLocalTime;
        }

        // The business date as staff in Australia see it. Worked out from UTC against an
        // explicit zone, so it stays correct whichever timezone the server itself runs in.
        public static DateTime GetBusinessToday()
        {
            try
            {
                var ausEasternZone = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ausEasternZone).Date;
            }
            catch (Exception)
            {
                // Zone data unavailable - fall back to the server's own date.
                return DateTime.Today;
            }
        }

        public static DateTime ConvertToSystemLocalTime(DateTime eventDateTimeLocal, int utcOffsetMinutes)
        {
            // Create DateTimeOffset using supplied local time and offset
            var sourceDateTime = new DateTimeOffset(eventDateTimeLocal,TimeSpan.FromMinutes(utcOffsetMinutes));

            // Convert to system local time
            return sourceDateTime.ToLocalTime().DateTime;
        }

    }

    public static class ColorConvertorHelper
    {
        public static string GetHexToRGBConvertedColorCode(string HexColorCode)
        {
            var color = ColorTranslator.FromHtml(HexColorCode); // System.Drawing.Color.FromString(HexColorCode);
            // Convert HEX to RGB 
            int r = Convert.ToInt16(color.R);
            int g = Convert.ToInt16(color.G);
            int b = Convert.ToInt16(color.B);

            string rgbColor = string.Format("rgba({0}, {1}, {2});", r, g, b);

            return rgbColor;
        }
    }
}
