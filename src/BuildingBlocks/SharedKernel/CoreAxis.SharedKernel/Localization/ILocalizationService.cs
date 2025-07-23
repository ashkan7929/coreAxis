using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Globalization;

namespace CoreAxis.SharedKernel.Localization
{
    /// <summary>
    /// Interface for localization services in the CoreAxis platform.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// Gets the localized string for the specified key.
        /// </summary>
        /// <param name="key">The key of the string to localize.</param>
        /// <returns>The localized string.</returns>
        string GetString(string key);

        /// <summary>
        /// Gets the localized string for the specified key with formatting.
        /// </summary>
        /// <param name="key">The key of the string to localize.</param>
        /// <param name="arguments">The arguments to format the string with.</param>
        /// <returns>The localized string.</returns>
        string GetString(string key, params object[] arguments);

        /// <summary>
        /// Gets all localized strings.
        /// </summary>
        /// <returns>A collection of localized strings.</returns>
        IEnumerable<LocalizedString> GetAllStrings();

        /// <summary>
        /// Gets the current culture.
        /// </summary>
        /// <returns>The current culture.</returns>
        CultureInfo GetCurrentCulture();

        /// <summary>
        /// Gets the current culture name.
        /// </summary>
        /// <returns>The current culture name.</returns>
        string GetCurrentCultureName();
    }
}