using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CoreAxis.SharedKernel.Localization
{
    /// <summary>
    /// Service for handling localization in the CoreAxis platform.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private readonly IStringLocalizer _localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationService"/> class.
        /// </summary>
        /// <param name="localizer">The string localizer to use for localization.</param>
        public LocalizationService(IStringLocalizer localizer)
        {
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Gets the localized string for the specified key.
        /// </summary>
        /// <param name="key">The key of the string to localize.</param>
        /// <returns>The localized string.</returns>
        public string GetString(string key)
        {
            return _localizer[key];
        }

        /// <summary>
        /// Gets the localized string for the specified key with formatting.
        /// </summary>
        /// <param name="key">The key of the string to localize.</param>
        /// <param name="arguments">The arguments to format the string with.</param>
        /// <returns>The localized string.</returns>
        public string GetString(string key, params object[] arguments)
        {
            return _localizer[key, arguments];
        }

        /// <summary>
        /// Gets all localized strings.
        /// </summary>
        /// <returns>A collection of localized strings.</returns>
        public IEnumerable<LocalizedString> GetAllStrings()
        {
            return _localizer.GetAllStrings();
        }

        /// <summary>
        /// Gets the current culture.
        /// </summary>
        /// <returns>The current culture.</returns>
        public CultureInfo GetCurrentCulture()
        {
            return CultureInfo.CurrentUICulture;
        }

        /// <summary>
        /// Gets the current culture name.
        /// </summary>
        /// <returns>The current culture name.</returns>
        public string GetCurrentCultureName()
        {
            return CultureInfo.CurrentUICulture.Name;
        }
    }
}