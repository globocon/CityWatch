using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Helpers
{
    /// <summary>
    /// Shared parsing for email recipient settings, so Web and Kpi read the same configuration
    /// the same way. Lives in CityWatch.Data because that is where <see cref="EmailOptions"/>
    /// is and both apps reference it.
    ///
    /// Parsing only - no MimeKit here, since CityWatch.Data does not reference it. Applying a
    /// redirect to an actual message stays in whichever app is sending it.
    /// </summary>
    public static class EmailRecipientHelper
    {
        /// <summary>
        /// Splits a configured recipient list - comma or semicolon separated - into validated
        /// addresses. Invalid entries are dropped rather than throwing: a typo in one address
        /// must not stop the mail reaching the others.
        /// </summary>
        public static IReadOnlyList<string> ParseAddressList(string configuredValue)
        {
            if (string.IsNullOrWhiteSpace(configuredValue))
                return Array.Empty<string>();

            return configuredValue
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(z => z.Trim())
                .Where(z => CommonHelper.IsValidEmail(z))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Parses <c>Email:TestModeRedirectTo</c>. Empty when the setting is absent or blank,
        /// which is the live configuration - test mode is opt-in, so production cannot start
        /// redirecting by accident.
        /// </summary>
        public static IReadOnlyList<string> GetTestModeRedirectAddresses(EmailOptions emailOptions)
        {
            return ParseAddressList(emailOptions?.TestModeRedirectTo);
        }

        /// <summary>
        /// The <c>address|Display Name</c> form used by <see cref="EmailOptions.ToAddress"/> and
        /// <see cref="EmailOptions.FromAddress"/>, reduced to the address alone.
        /// </summary>
        public static string AddressOnly(string configuredValue)
        {
            if (string.IsNullOrWhiteSpace(configuredValue))
                return string.Empty;

            return configuredValue.Split('|')[0].Trim();
        }
    }
}
