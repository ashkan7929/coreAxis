using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Domain.ValueObjects
{
    /// <summary>
    /// Represents an option for form fields like select, radio, checkbox, etc.
    /// </summary>
    public class FieldOption : IEquatable<FieldOption>
    {
        /// <summary>
        /// Gets the option value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Gets the option label/text displayed to users.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Gets the option description for additional context.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets whether this option is selected by default.
        /// </summary>
        public bool IsDefault { get; private set; }

        /// <summary>
        /// Gets whether this option is enabled/selectable.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Gets the display order of this option.
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// Gets the option group/category for grouping related options.
        /// </summary>
        public string Group { get; private set; }

        /// <summary>
        /// Gets the icon or image URL for this option.
        /// </summary>
        public string Icon { get; private set; }

        /// <summary>
        /// Gets the CSS class for styling this option.
        /// </summary>
        public string CssClass { get; private set; }

        /// <summary>
        /// Gets additional metadata for this option.
        /// </summary>
        public Dictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Gets the conditional expression for when this option should be visible.
        /// </summary>
        public string Condition { get; private set; }

        /// <summary>
        /// Gets localized labels for different languages.
        /// </summary>
        public Dictionary<string, string> LocalizedLabels { get; private set; }

        /// <summary>
        /// Gets localized descriptions for different languages.
        /// </summary>
        public Dictionary<string, string> LocalizedDescriptions { get; private set; }

        /// <summary>
        /// Initializes a new instance of the FieldOption class.
        /// </summary>
        /// <param name="value">The option value.</param>
        /// <param name="label">The option label.</param>
        /// <param name="description">The option description.</param>
        /// <param name="isDefault">Whether this option is selected by default.</param>
        /// <param name="isEnabled">Whether this option is enabled.</param>
        /// <param name="order">The display order.</param>
        /// <param name="group">The option group.</param>
        /// <param name="icon">The option icon.</param>
        /// <param name="cssClass">The CSS class.</param>
        /// <param name="metadata">Additional metadata.</param>
        /// <param name="condition">The conditional expression.</param>
        /// <param name="localizedLabels">Localized labels.</param>
        /// <param name="localizedDescriptions">Localized descriptions.</param>
        public FieldOption(
            string value,
            string label,
            string description = null,
            bool isDefault = false,
            bool isEnabled = true,
            int order = 0,
            string group = null,
            string icon = null,
            string cssClass = null,
            Dictionary<string, object> metadata = null,
            string condition = null,
            Dictionary<string, string> localizedLabels = null,
            Dictionary<string, string> localizedDescriptions = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Option value cannot be null or empty.", nameof(value));
            
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Option label cannot be null or empty.", nameof(label));

            Value = value.Trim();
            Label = label.Trim();
            Description = description?.Trim();
            IsDefault = isDefault;
            IsEnabled = isEnabled;
            Order = order;
            Group = group?.Trim();
            Icon = icon?.Trim();
            CssClass = cssClass?.Trim();
            Metadata = metadata ?? new Dictionary<string, object>();
            Condition = condition?.Trim();
            LocalizedLabels = localizedLabels ?? new Dictionary<string, string>();
            LocalizedDescriptions = localizedDescriptions ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Creates a simple option with value and label.
        /// </summary>
        /// <param name="value">The option value.</param>
        /// <param name="label">The option label.</param>
        /// <param name="isDefault">Whether this option is selected by default.</param>
        /// <returns>A new field option.</returns>
        public static FieldOption Create(string value, string label, bool isDefault = false)
        {
            return new FieldOption(value, label, isDefault: isDefault);
        }

        /// <summary>
        /// Creates an option with value, label, and description.
        /// </summary>
        /// <param name="value">The option value.</param>
        /// <param name="label">The option label.</param>
        /// <param name="description">The option description.</param>
        /// <param name="isDefault">Whether this option is selected by default.</param>
        /// <returns>A new field option.</returns>
        public static FieldOption CreateWithDescription(string value, string label, string description, bool isDefault = false)
        {
            return new FieldOption(value, label, description, isDefault);
        }

        /// <summary>
        /// Creates an option with grouping.
        /// </summary>
        /// <param name="value">The option value.</param>
        /// <param name="label">The option label.</param>
        /// <param name="group">The option group.</param>
        /// <param name="order">The display order within the group.</param>
        /// <param name="isDefault">Whether this option is selected by default.</param>
        /// <returns>A new field option.</returns>
        public static FieldOption CreateGrouped(string value, string label, string group, int order = 0, bool isDefault = false)
        {
            return new FieldOption(value, label, group: group, order: order, isDefault: isDefault);
        }

        /// <summary>
        /// Creates an option with icon.
        /// </summary>
        /// <param name="value">The option value.</param>
        /// <param name="label">The option label.</param>
        /// <param name="icon">The option icon.</param>
        /// <param name="isDefault">Whether this option is selected by default.</param>
        /// <returns>A new field option.</returns>
        public static FieldOption CreateWithIcon(string value, string label, string icon, bool isDefault = false)
        {
            return new FieldOption(value, label, icon: icon, isDefault: isDefault);
        }

        /// <summary>
        /// Creates a disabled option.
        /// </summary>
        /// <param name="value">The option value.</param>
        /// <param name="label">The option label.</param>
        /// <param name="description">The option description.</param>
        /// <returns>A new disabled field option.</returns>
        public static FieldOption CreateDisabled(string value, string label, string description = null)
        {
            return new FieldOption(value, label, description, isEnabled: false);
        }

        /// <summary>
        /// Creates a conditional option that is only visible when a condition is met.
        /// </summary>
        /// <param name="value">The option value.</param>
        /// <param name="label">The option label.</param>
        /// <param name="condition">The conditional expression.</param>
        /// <param name="isDefault">Whether this option is selected by default.</param>
        /// <returns>A new conditional field option.</returns>
        public static FieldOption CreateConditional(string value, string label, string condition, bool isDefault = false)
        {
            return new FieldOption(value, label, condition: condition, isDefault: isDefault);
        }

        /// <summary>
        /// Gets the label for a specific language, falling back to the default label.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The localized label or default label.</returns>
        public string GetLocalizedLabel(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return Label;

            return LocalizedLabels.TryGetValue(language, out var localizedLabel) && !string.IsNullOrWhiteSpace(localizedLabel)
                ? localizedLabel
                : Label;
        }

        /// <summary>
        /// Gets the description for a specific language, falling back to the default description.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The localized description or default description.</returns>
        public string GetLocalizedDescription(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return Description;

            return LocalizedDescriptions.TryGetValue(language, out var localizedDescription) && !string.IsNullOrWhiteSpace(localizedDescription)
                ? localizedDescription
                : Description;
        }

        /// <summary>
        /// Creates a copy of the option with updated properties.
        /// </summary>
        /// <param name="label">The new label.</param>
        /// <param name="description">The new description.</param>
        /// <param name="isDefault">The new default state.</param>
        /// <param name="isEnabled">The new enabled state.</param>
        /// <param name="order">The new order.</param>
        /// <param name="group">The new group.</param>
        /// <param name="icon">The new icon.</param>
        /// <param name="cssClass">The new CSS class.</param>
        /// <param name="condition">The new condition.</param>
        /// <returns>A new field option with updated properties.</returns>
        public FieldOption WithUpdates(
            string label = null,
            string description = null,
            bool? isDefault = null,
            bool? isEnabled = null,
            int? order = null,
            string group = null,
            string icon = null,
            string cssClass = null,
            string condition = null)
        {
            return new FieldOption(
                Value,
                label ?? Label,
                description ?? Description,
                isDefault ?? IsDefault,
                isEnabled ?? IsEnabled,
                order ?? Order,
                group ?? Group,
                icon ?? Icon,
                cssClass ?? CssClass,
                new Dictionary<string, object>(Metadata),
                condition ?? Condition,
                new Dictionary<string, string>(LocalizedLabels),
                new Dictionary<string, string>(LocalizedDescriptions));
        }

        /// <summary>
        /// Adds or updates a localized label.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <param name="label">The localized label.</param>
        /// <returns>A new field option with the updated localized label.</returns>
        public FieldOption WithLocalizedLabel(string language, string label)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("Language cannot be null or empty.", nameof(language));

            var newLocalizedLabels = new Dictionary<string, string>(LocalizedLabels)
            {
                [language] = label
            };

            return new FieldOption(
                Value, Label, Description, IsDefault, IsEnabled, Order, Group, Icon, CssClass,
                new Dictionary<string, object>(Metadata), Condition,
                newLocalizedLabels,
                new Dictionary<string, string>(LocalizedDescriptions));
        }

        /// <summary>
        /// Adds or updates a localized description.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <param name="description">The localized description.</param>
        /// <returns>A new field option with the updated localized description.</returns>
        public FieldOption WithLocalizedDescription(string language, string description)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("Language cannot be null or empty.", nameof(language));

            var newLocalizedDescriptions = new Dictionary<string, string>(LocalizedDescriptions)
            {
                [language] = description
            };

            return new FieldOption(
                Value, Label, Description, IsDefault, IsEnabled, Order, Group, Icon, CssClass,
                new Dictionary<string, object>(Metadata), Condition,
                new Dictionary<string, string>(LocalizedLabels),
                newLocalizedDescriptions);
        }

        /// <summary>
        /// Adds or updates metadata.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        /// <returns>A new field option with the updated metadata.</returns>
        public FieldOption WithMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty.", nameof(key));

            var newMetadata = new Dictionary<string, object>(Metadata)
            {
                [key] = value
            };

            return new FieldOption(
                Value, Label, Description, IsDefault, IsEnabled, Order, Group, Icon, CssClass,
                newMetadata, Condition,
                new Dictionary<string, string>(LocalizedLabels),
                new Dictionary<string, string>(LocalizedDescriptions));
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(FieldOption other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Value == other.Value &&
                   Label == other.Label &&
                   Description == other.Description &&
                   IsDefault == other.IsDefault &&
                   IsEnabled == other.IsEnabled &&
                   Order == other.Order &&
                   Group == other.Group &&
                   Icon == other.Icon &&
                   CssClass == other.CssClass &&
                   Condition == other.Condition &&
                   Metadata.SequenceEqual(other.Metadata) &&
                   LocalizedLabels.SequenceEqual(other.LocalizedLabels) &&
                   LocalizedDescriptions.SequenceEqual(other.LocalizedDescriptions);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as FieldOption);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Value,
                Label,
                Description,
                IsDefault,
                IsEnabled,
                Order,
                Group,
                Icon);
        }

        /// <summary>
        /// Returns a string representation of the field option.
        /// </summary>
        /// <returns>A string representation of the field option.</returns>
        public override string ToString()
        {
            var parts = new List<string> { $"Value: {Value}", $"Label: {Label}" };
            
            if (!string.IsNullOrEmpty(Description))
                parts.Add($"Description: {Description}");
            
            if (IsDefault)
                parts.Add("Default");
            
            if (!IsEnabled)
                parts.Add("Disabled");
            
            if (Order != 0)
                parts.Add($"Order: {Order}");
            
            if (!string.IsNullOrEmpty(Group))
                parts.Add($"Group: {Group}");

            return $"FieldOption({string.Join(", ", parts)})";
        }

        public static bool operator ==(FieldOption left, FieldOption right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FieldOption left, FieldOption right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Converts the field option to JSON string.
        /// </summary>
        /// <returns>JSON representation of the field option.</returns>
        public string ToJson()
        {
            var obj = new
            {
                value = Value,
                label = Label,
                description = Description,
                isDefault = IsDefault,
                isEnabled = IsEnabled,
                order = Order,
                group = Group,
                icon = Icon,
                cssClass = CssClass,
                localizedLabels = LocalizedLabels,
                localizedDescriptions = LocalizedDescriptions,
                metadata = Metadata,
                condition = Condition
            };
            
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }
    }
}