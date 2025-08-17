using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Domain.ValueObjects
{
    /// <summary>
    /// Represents configuration settings for a form that control its behavior and appearance.
    /// </summary>
    public class FormConfiguration : IEquatable<FormConfiguration>
    {
        /// <summary>
        /// Gets whether the form allows multiple submissions from the same user.
        /// </summary>
        public bool AllowMultipleSubmissions { get; private set; }

        /// <summary>
        /// Gets whether the form requires authentication to access.
        /// </summary>
        public bool RequireAuthentication { get; private set; }

        /// <summary>
        /// Gets whether the form saves progress automatically.
        /// </summary>
        public bool AutoSave { get; private set; }

        /// <summary>
        /// Gets the auto-save interval in seconds.
        /// </summary>
        public int AutoSaveInterval { get; private set; }

        /// <summary>
        /// Gets whether the form shows a progress indicator.
        /// </summary>
        public bool ShowProgress { get; private set; }

        /// <summary>
        /// Gets whether the form allows saving as draft.
        /// </summary>
        public bool AllowDraft { get; private set; }

        /// <summary>
        /// Gets whether the form validates fields on blur.
        /// </summary>
        public bool ValidateOnBlur { get; private set; }

        /// <summary>
        /// Gets whether the form validates fields on change.
        /// </summary>
        public bool ValidateOnChange { get; private set; }

        /// <summary>
        /// Gets whether the form shows validation errors inline.
        /// </summary>
        public bool ShowInlineErrors { get; private set; }

        /// <summary>
        /// Gets whether the form shows a confirmation dialog before submission.
        /// </summary>
        public bool ShowConfirmationDialog { get; private set; }

        /// <summary>
        /// Gets the maximum number of submissions allowed (null for unlimited).
        /// </summary>
        public int? MaxSubmissions { get; private set; }

        /// <summary>
        /// Gets the submission rate limit per user (submissions per time period).
        /// </summary>
        public int? SubmissionRateLimit { get; private set; }

        /// <summary>
        /// Gets the time period for submission rate limiting in minutes.
        /// </summary>
        public int? SubmissionRatePeriod { get; private set; }

        /// <summary>
        /// Gets the form timeout in minutes (null for no timeout).
        /// </summary>
        public int? TimeoutMinutes { get; private set; }

        /// <summary>
        /// Gets whether the form shows a timeout warning.
        /// </summary>
        public bool ShowTimeoutWarning { get; private set; }

        /// <summary>
        /// Gets the timeout warning threshold in minutes.
        /// </summary>
        public int? TimeoutWarningMinutes { get; private set; }

        /// <summary>
        /// Gets the form layout mode.
        /// </summary>
        public FormLayoutMode LayoutMode { get; private set; }

        /// <summary>
        /// Gets the number of columns for the form layout.
        /// </summary>
        public int Columns { get; private set; }

        /// <summary>
        /// Gets whether the form uses responsive layout.
        /// </summary>
        public bool ResponsiveLayout { get; private set; }

        /// <summary>
        /// Gets the form theme name.
        /// </summary>
        public string Theme { get; private set; }

        /// <summary>
        /// Gets custom CSS classes for the form.
        /// </summary>
        public string CssClasses { get; private set; }

        /// <summary>
        /// Gets custom CSS styles for the form.
        /// </summary>
        public string CustomCss { get; private set; }

        /// <summary>
        /// Gets the success message to show after submission.
        /// </summary>
        public string SuccessMessage { get; private set; }

        /// <summary>
        /// Gets the error message to show on submission failure.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Gets the redirect URL after successful submission.
        /// </summary>
        public string RedirectUrl { get; private set; }

        /// <summary>
        /// Gets whether to redirect after successful submission.
        /// </summary>
        public bool RedirectAfterSubmission { get; private set; }

        /// <summary>
        /// Gets the email notifications configuration.
        /// </summary>
        public EmailNotificationConfig EmailNotifications { get; private set; }

        /// <summary>
        /// Gets the webhook configuration for form events.
        /// </summary>
        public WebhookConfig Webhooks { get; private set; }

        /// <summary>
        /// Gets additional metadata for the form configuration.
        /// </summary>
        public Dictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Gets the localization settings.
        /// </summary>
        public LocalizationConfig Localization { get; private set; }

        /// <summary>
        /// Gets the accessibility settings.
        /// </summary>
        public AccessibilityConfig Accessibility { get; private set; }

        /// <summary>
        /// Gets the security settings.
        /// </summary>
        public SecurityConfig Security { get; private set; }

        /// <summary>
        /// Initializes a new instance of the FormConfiguration class.
        /// </summary>
        public FormConfiguration(
            bool allowMultipleSubmissions = false,
            bool requireAuthentication = false,
            bool autoSave = false,
            int autoSaveInterval = 30,
            bool showProgress = true,
            bool allowDraft = false,
            bool validateOnBlur = true,
            bool validateOnChange = false,
            bool showInlineErrors = true,
            bool showConfirmationDialog = false,
            int? maxSubmissions = null,
            int? submissionRateLimit = null,
            int? submissionRatePeriod = null,
            int? timeoutMinutes = null,
            bool showTimeoutWarning = false,
            int? timeoutWarningMinutes = null,
            FormLayoutMode layoutMode = FormLayoutMode.Vertical,
            int columns = 1,
            bool responsiveLayout = true,
            string theme = "default",
            string cssClasses = null,
            string customCss = null,
            string successMessage = null,
            string errorMessage = null,
            string redirectUrl = null,
            bool redirectAfterSubmission = false,
            EmailNotificationConfig emailNotifications = null,
            WebhookConfig webhooks = null,
            Dictionary<string, object> metadata = null,
            LocalizationConfig localization = null,
            AccessibilityConfig accessibility = null,
            SecurityConfig security = null)
        {
            if (autoSaveInterval <= 0)
                throw new ArgumentException("Auto-save interval must be positive.", nameof(autoSaveInterval));
            
            if (columns <= 0)
                throw new ArgumentException("Columns must be positive.", nameof(columns));
            
            if (maxSubmissions.HasValue && maxSubmissions.Value <= 0)
                throw new ArgumentException("Max submissions must be positive.", nameof(maxSubmissions));
            
            if (submissionRateLimit.HasValue && submissionRateLimit.Value <= 0)
                throw new ArgumentException("Submission rate limit must be positive.", nameof(submissionRateLimit));
            
            if (submissionRatePeriod.HasValue && submissionRatePeriod.Value <= 0)
                throw new ArgumentException("Submission rate period must be positive.", nameof(submissionRatePeriod));
            
            if (timeoutMinutes.HasValue && timeoutMinutes.Value <= 0)
                throw new ArgumentException("Timeout minutes must be positive.", nameof(timeoutMinutes));
            
            if (timeoutWarningMinutes.HasValue && timeoutWarningMinutes.Value <= 0)
                throw new ArgumentException("Timeout warning minutes must be positive.", nameof(timeoutWarningMinutes));

            AllowMultipleSubmissions = allowMultipleSubmissions;
            RequireAuthentication = requireAuthentication;
            AutoSave = autoSave;
            AutoSaveInterval = autoSaveInterval;
            ShowProgress = showProgress;
            AllowDraft = allowDraft;
            ValidateOnBlur = validateOnBlur;
            ValidateOnChange = validateOnChange;
            ShowInlineErrors = showInlineErrors;
            ShowConfirmationDialog = showConfirmationDialog;
            MaxSubmissions = maxSubmissions;
            SubmissionRateLimit = submissionRateLimit;
            SubmissionRatePeriod = submissionRatePeriod;
            TimeoutMinutes = timeoutMinutes;
            ShowTimeoutWarning = showTimeoutWarning;
            TimeoutWarningMinutes = timeoutWarningMinutes;
            LayoutMode = layoutMode;
            Columns = columns;
            ResponsiveLayout = responsiveLayout;
            Theme = theme?.Trim() ?? "default";
            CssClasses = cssClasses?.Trim();
            CustomCss = customCss?.Trim();
            SuccessMessage = successMessage?.Trim();
            ErrorMessage = errorMessage?.Trim();
            RedirectUrl = redirectUrl?.Trim();
            RedirectAfterSubmission = redirectAfterSubmission;
            EmailNotifications = emailNotifications ?? EmailNotificationConfig.Default();
            Webhooks = webhooks ?? WebhookConfig.Default();
            Metadata = metadata ?? new Dictionary<string, object>();
            Localization = localization ?? LocalizationConfig.Default();
            Accessibility = accessibility ?? AccessibilityConfig.Default();
            Security = security ?? SecurityConfig.Default();
        }

        /// <summary>
        /// Creates a default form configuration.
        /// </summary>
        /// <returns>A new form configuration with default settings.</returns>
        public static FormConfiguration Default()
        {
            return new FormConfiguration();
        }

        /// <summary>
        /// Creates a simple form configuration for basic forms.
        /// </summary>
        /// <returns>A new form configuration optimized for simple forms.</returns>
        public static FormConfiguration Simple()
        {
            return new FormConfiguration(
                validateOnBlur: true,
                showInlineErrors: true,
                layoutMode: FormLayoutMode.Vertical,
                responsiveLayout: true);
        }

        /// <summary>
        /// Creates a multi-step form configuration.
        /// </summary>
        /// <returns>A new form configuration optimized for multi-step forms.</returns>
        public static FormConfiguration MultiStep()
        {
            return new FormConfiguration(
                autoSave: true,
                autoSaveInterval: 60,
                showProgress: true,
                allowDraft: true,
                validateOnBlur: true,
                showConfirmationDialog: true);
        }

        /// <summary>
        /// Creates a secure form configuration with enhanced security settings.
        /// </summary>
        /// <returns>A new form configuration with enhanced security.</returns>
        public static FormConfiguration Secure()
        {
            return new FormConfiguration(
                requireAuthentication: true,
                submissionRateLimit: 5,
                submissionRatePeriod: 60,
                timeoutMinutes: 30,
                showTimeoutWarning: true,
                timeoutWarningMinutes: 5,
                security: SecurityConfig.Enhanced());
        }

        /// <summary>
        /// Creates a survey form configuration optimized for surveys.
        /// </summary>
        /// <returns>A new form configuration optimized for surveys.</returns>
        public static FormConfiguration Survey()
        {
            return new FormConfiguration(
                allowMultipleSubmissions: false,
                autoSave: true,
                showProgress: true,
                validateOnChange: false,
                validateOnBlur: true,
                layoutMode: FormLayoutMode.Vertical,
                responsiveLayout: true);
        }

        /// <summary>
        /// Creates a copy of the form configuration with updated properties.
        /// </summary>
        /// <param name="allowMultipleSubmissions">Whether to allow multiple submissions.</param>
        /// <param name="requireAuthentication">Whether to require authentication.</param>
        /// <param name="autoSave">Whether to enable auto-save.</param>
        /// <param name="autoSaveInterval">The auto-save interval.</param>
        /// <param name="showProgress">Whether to show progress.</param>
        /// <param name="allowDraft">Whether to allow drafts.</param>
        /// <param name="validateOnBlur">Whether to validate on blur.</param>
        /// <param name="validateOnChange">Whether to validate on change.</param>
        /// <param name="showInlineErrors">Whether to show inline errors.</param>
        /// <param name="showConfirmationDialog">Whether to show confirmation dialog.</param>
        /// <param name="maxSubmissions">The maximum submissions allowed.</param>
        /// <param name="timeoutMinutes">The timeout in minutes.</param>
        /// <param name="layoutMode">The layout mode.</param>
        /// <param name="columns">The number of columns.</param>
        /// <param name="theme">The theme name.</param>
        /// <param name="successMessage">The success message.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="redirectUrl">The redirect URL.</param>
        /// <param name="redirectAfterSubmission">Whether to redirect after submission.</param>
        /// <returns>A new form configuration with updated properties.</returns>
        public FormConfiguration WithUpdates(
            bool? allowMultipleSubmissions = null,
            bool? requireAuthentication = null,
            bool? autoSave = null,
            int? autoSaveInterval = null,
            bool? showProgress = null,
            bool? allowDraft = null,
            bool? validateOnBlur = null,
            bool? validateOnChange = null,
            bool? showInlineErrors = null,
            bool? showConfirmationDialog = null,
            int? maxSubmissions = null,
            int? timeoutMinutes = null,
            FormLayoutMode? layoutMode = null,
            int? columns = null,
            string theme = null,
            string successMessage = null,
            string errorMessage = null,
            string redirectUrl = null,
            bool? redirectAfterSubmission = null)
        {
            return new FormConfiguration(
                allowMultipleSubmissions ?? AllowMultipleSubmissions,
                requireAuthentication ?? RequireAuthentication,
                autoSave ?? AutoSave,
                autoSaveInterval ?? AutoSaveInterval,
                showProgress ?? ShowProgress,
                allowDraft ?? AllowDraft,
                validateOnBlur ?? ValidateOnBlur,
                validateOnChange ?? ValidateOnChange,
                showInlineErrors ?? ShowInlineErrors,
                showConfirmationDialog ?? ShowConfirmationDialog,
                maxSubmissions ?? MaxSubmissions,
                SubmissionRateLimit,
                SubmissionRatePeriod,
                timeoutMinutes ?? TimeoutMinutes,
                ShowTimeoutWarning,
                TimeoutWarningMinutes,
                layoutMode ?? LayoutMode,
                columns ?? Columns,
                ResponsiveLayout,
                theme ?? Theme,
                CssClasses,
                CustomCss,
                successMessage ?? SuccessMessage,
                errorMessage ?? ErrorMessage,
                redirectUrl ?? RedirectUrl,
                redirectAfterSubmission ?? RedirectAfterSubmission,
                EmailNotifications,
                Webhooks,
                new Dictionary<string, object>(Metadata),
                Localization,
                Accessibility,
                Security);
        }

        /// <summary>
        /// Adds metadata to the form configuration.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        /// <returns>A new form configuration with the added metadata.</returns>
        public FormConfiguration WithMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty.", nameof(key));

            var newMetadata = new Dictionary<string, object>(Metadata)
            {
                [key] = value
            };

            return new FormConfiguration(
                AllowMultipleSubmissions, RequireAuthentication, AutoSave, AutoSaveInterval,
                ShowProgress, AllowDraft, ValidateOnBlur, ValidateOnChange, ShowInlineErrors,
                ShowConfirmationDialog, MaxSubmissions, SubmissionRateLimit, SubmissionRatePeriod,
                TimeoutMinutes, ShowTimeoutWarning, TimeoutWarningMinutes, LayoutMode, Columns,
                ResponsiveLayout, Theme, CssClasses, CustomCss, SuccessMessage, ErrorMessage,
                RedirectUrl, RedirectAfterSubmission, EmailNotifications, Webhooks, newMetadata,
                Localization, Accessibility, Security);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(FormConfiguration other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return AllowMultipleSubmissions == other.AllowMultipleSubmissions &&
                   RequireAuthentication == other.RequireAuthentication &&
                   AutoSave == other.AutoSave &&
                   AutoSaveInterval == other.AutoSaveInterval &&
                   ShowProgress == other.ShowProgress &&
                   AllowDraft == other.AllowDraft &&
                   ValidateOnBlur == other.ValidateOnBlur &&
                   ValidateOnChange == other.ValidateOnChange &&
                   ShowInlineErrors == other.ShowInlineErrors &&
                   ShowConfirmationDialog == other.ShowConfirmationDialog &&
                   MaxSubmissions == other.MaxSubmissions &&
                   SubmissionRateLimit == other.SubmissionRateLimit &&
                   SubmissionRatePeriod == other.SubmissionRatePeriod &&
                   TimeoutMinutes == other.TimeoutMinutes &&
                   ShowTimeoutWarning == other.ShowTimeoutWarning &&
                   TimeoutWarningMinutes == other.TimeoutWarningMinutes &&
                   LayoutMode == other.LayoutMode &&
                   Columns == other.Columns &&
                   ResponsiveLayout == other.ResponsiveLayout &&
                   Theme == other.Theme &&
                   CssClasses == other.CssClasses &&
                   CustomCss == other.CustomCss &&
                   SuccessMessage == other.SuccessMessage &&
                   ErrorMessage == other.ErrorMessage &&
                   RedirectUrl == other.RedirectUrl &&
                   RedirectAfterSubmission == other.RedirectAfterSubmission &&
                   Equals(EmailNotifications, other.EmailNotifications) &&
                   Equals(Webhooks, other.Webhooks) &&
                   Metadata.SequenceEqual(other.Metadata) &&
                   Equals(Localization, other.Localization) &&
                   Equals(Accessibility, other.Accessibility) &&
                   Equals(Security, other.Security);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as FormConfiguration);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(AllowMultipleSubmissions);
            hash.Add(RequireAuthentication);
            hash.Add(AutoSave);
            hash.Add(AutoSaveInterval);
            hash.Add(ShowProgress);
            hash.Add(AllowDraft);
            hash.Add(ValidateOnBlur);
            hash.Add(ValidateOnChange);
            hash.Add(ShowInlineErrors);
            hash.Add(ShowConfirmationDialog);
            hash.Add(MaxSubmissions);
            hash.Add(SubmissionRateLimit);
            hash.Add(SubmissionRatePeriod);
            hash.Add(TimeoutMinutes);
            hash.Add(ShowTimeoutWarning);
            hash.Add(TimeoutWarningMinutes);
            hash.Add(LayoutMode);
            hash.Add(Columns);
            hash.Add(ResponsiveLayout);
            hash.Add(Theme);
            hash.Add(CssClasses);
            hash.Add(CustomCss);
            hash.Add(SuccessMessage);
            hash.Add(ErrorMessage);
            hash.Add(RedirectUrl);
            hash.Add(RedirectAfterSubmission);
            return hash.ToHashCode();
        }

        public static bool operator ==(FormConfiguration left, FormConfiguration right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FormConfiguration left, FormConfiguration right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Represents the layout mode for a form.
    /// </summary>
    public enum FormLayoutMode
    {
        /// <summary>
        /// Vertical layout with fields stacked vertically.
        /// </summary>
        Vertical,

        /// <summary>
        /// Horizontal layout with fields arranged horizontally.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Grid layout with fields arranged in a grid.
        /// </summary>
        Grid,

        /// <summary>
        /// Inline layout with fields arranged inline.
        /// </summary>
        Inline,

        /// <summary>
        /// Custom layout defined by CSS.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Represents email notification configuration.
    /// </summary>
    public class EmailNotificationConfig : IEquatable<EmailNotificationConfig>
    {
        public bool Enabled { get; private set; }
        public string[] Recipients { get; private set; }
        public string Subject { get; private set; }
        public string Template { get; private set; }
        public bool IncludeSubmissionData { get; private set; }

        public EmailNotificationConfig(bool enabled = false, string[] recipients = null, string subject = null, string template = null, bool includeSubmissionData = true)
        {
            Enabled = enabled;
            Recipients = recipients ?? Array.Empty<string>();
            Subject = subject;
            Template = template;
            IncludeSubmissionData = includeSubmissionData;
        }

        public static EmailNotificationConfig Default() => new EmailNotificationConfig();

        public bool Equals(EmailNotificationConfig other)
        {
            if (other is null) return false;
            return Enabled == other.Enabled &&
                   Recipients.SequenceEqual(other.Recipients) &&
                   Subject == other.Subject &&
                   Template == other.Template &&
                   IncludeSubmissionData == other.IncludeSubmissionData;
        }

        public override bool Equals(object obj) => Equals(obj as EmailNotificationConfig);
        public override int GetHashCode() => HashCode.Combine(Enabled, Recipients, Subject, Template, IncludeSubmissionData);
    }

    /// <summary>
    /// Represents webhook configuration.
    /// </summary>
    public class WebhookConfig : IEquatable<WebhookConfig>
    {
        public bool Enabled { get; private set; }
        public string Url { get; private set; }
        public string[] Events { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public string Secret { get; private set; }

        public WebhookConfig(bool enabled = false, string url = null, string[] events = null, Dictionary<string, string> headers = null, string secret = null)
        {
            Enabled = enabled;
            Url = url;
            Events = events ?? Array.Empty<string>();
            Headers = headers ?? new Dictionary<string, string>();
            Secret = secret;
        }

        public static WebhookConfig Default() => new WebhookConfig();

        public bool Equals(WebhookConfig other)
        {
            if (other is null) return false;
            return Enabled == other.Enabled &&
                   Url == other.Url &&
                   Events.SequenceEqual(other.Events) &&
                   Headers.SequenceEqual(other.Headers) &&
                   Secret == other.Secret;
        }

        public override bool Equals(object obj) => Equals(obj as WebhookConfig);
        public override int GetHashCode() => HashCode.Combine(Enabled, Url, Events, Headers, Secret);
    }

    /// <summary>
    /// Represents localization configuration.
    /// </summary>
    public class LocalizationConfig : IEquatable<LocalizationConfig>
    {
        public bool Enabled { get; private set; }
        public string DefaultLanguage { get; private set; }
        public string[] SupportedLanguages { get; private set; }
        public bool AutoDetectLanguage { get; private set; }

        public LocalizationConfig(bool enabled = false, string defaultLanguage = "en", string[] supportedLanguages = null, bool autoDetectLanguage = false)
        {
            Enabled = enabled;
            DefaultLanguage = defaultLanguage ?? "en";
            SupportedLanguages = supportedLanguages ?? new[] { "en" };
            AutoDetectLanguage = autoDetectLanguage;
        }

        public static LocalizationConfig Default() => new LocalizationConfig();

        public bool Equals(LocalizationConfig other)
        {
            if (other is null) return false;
            return Enabled == other.Enabled &&
                   DefaultLanguage == other.DefaultLanguage &&
                   SupportedLanguages.SequenceEqual(other.SupportedLanguages) &&
                   AutoDetectLanguage == other.AutoDetectLanguage;
        }

        public override bool Equals(object obj) => Equals(obj as LocalizationConfig);
        public override int GetHashCode() => HashCode.Combine(Enabled, DefaultLanguage, SupportedLanguages, AutoDetectLanguage);
    }

    /// <summary>
    /// Represents accessibility configuration.
    /// </summary>
    public class AccessibilityConfig : IEquatable<AccessibilityConfig>
    {
        public bool HighContrast { get; private set; }
        public bool ScreenReaderSupport { get; private set; }
        public bool KeyboardNavigation { get; private set; }
        public string AriaLabels { get; private set; }

        public AccessibilityConfig(bool highContrast = false, bool screenReaderSupport = true, bool keyboardNavigation = true, string ariaLabels = null)
        {
            HighContrast = highContrast;
            ScreenReaderSupport = screenReaderSupport;
            KeyboardNavigation = keyboardNavigation;
            AriaLabels = ariaLabels;
        }

        public static AccessibilityConfig Default() => new AccessibilityConfig();

        public bool Equals(AccessibilityConfig other)
        {
            if (other is null) return false;
            return HighContrast == other.HighContrast &&
                   ScreenReaderSupport == other.ScreenReaderSupport &&
                   KeyboardNavigation == other.KeyboardNavigation &&
                   AriaLabels == other.AriaLabels;
        }

        public override bool Equals(object obj) => Equals(obj as AccessibilityConfig);
        public override int GetHashCode() => HashCode.Combine(HighContrast, ScreenReaderSupport, KeyboardNavigation, AriaLabels);
    }

    /// <summary>
    /// Represents security configuration.
    /// </summary>
    public class SecurityConfig : IEquatable<SecurityConfig>
    {
        public bool EnableCsrfProtection { get; private set; }
        public bool EnableRateLimiting { get; private set; }
        public bool EnableCaptcha { get; private set; }
        public bool EnableEncryption { get; private set; }
        public string[] AllowedOrigins { get; private set; }

        public SecurityConfig(bool enableCsrfProtection = true, bool enableRateLimiting = false, bool enableCaptcha = false, bool enableEncryption = false, string[] allowedOrigins = null)
        {
            EnableCsrfProtection = enableCsrfProtection;
            EnableRateLimiting = enableRateLimiting;
            EnableCaptcha = enableCaptcha;
            EnableEncryption = enableEncryption;
            AllowedOrigins = allowedOrigins ?? Array.Empty<string>();
        }

        public static SecurityConfig Default() => new SecurityConfig();
        public static SecurityConfig Enhanced() => new SecurityConfig(true, true, true, true);

        public bool Equals(SecurityConfig other)
        {
            if (other is null) return false;
            return EnableCsrfProtection == other.EnableCsrfProtection &&
                   EnableRateLimiting == other.EnableRateLimiting &&
                   EnableCaptcha == other.EnableCaptcha &&
                   EnableEncryption == other.EnableEncryption &&
                   AllowedOrigins.SequenceEqual(other.AllowedOrigins);
        }

        public override bool Equals(object obj) => Equals(obj as SecurityConfig);
        public override int GetHashCode() => HashCode.Combine(EnableCsrfProtection, EnableRateLimiting, EnableCaptcha, EnableEncryption, AllowedOrigins);
    }
}