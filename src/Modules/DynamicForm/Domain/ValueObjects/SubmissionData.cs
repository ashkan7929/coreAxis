using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreAxis.Modules.DynamicForm.Domain.ValueObjects
{
    /// <summary>
    /// Represents the data submitted in a form submission with type-safe field access and validation.
    /// </summary>
    public class SubmissionData : IEquatable<SubmissionData>
    {
        private readonly Dictionary<string, FieldValue> _fields;

        /// <summary>
        /// Gets the collection of field values in the submission.
        /// </summary>
        public IReadOnlyDictionary<string, FieldValue> Fields => _fields;

        /// <summary>
        /// Gets the metadata associated with the submission.
        /// </summary>
        public Dictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Gets the timestamp when the submission data was created.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets the timestamp when the submission data was last modified.
        /// </summary>
        public DateTime ModifiedAt { get; private set; }

        /// <summary>
        /// Gets the version of the submission data format.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the checksum of the submission data for integrity verification.
        /// </summary>
        public string Checksum { get; private set; }

        /// <summary>
        /// Gets whether the submission data is encrypted.
        /// </summary>
        public bool IsEncrypted { get; private set; }

        /// <summary>
        /// Gets the size of the submission data in bytes.
        /// </summary>
        public long SizeBytes { get; private set; }

        /// <summary>
        /// Initializes a new instance of the SubmissionData class.
        /// </summary>
        /// <param name="fields">The field values.</param>
        /// <param name="metadata">The metadata.</param>
        /// <param name="version">The data format version.</param>
        /// <param name="isEncrypted">Whether the data is encrypted.</param>
        public SubmissionData(
            Dictionary<string, FieldValue> fields = null,
            Dictionary<string, object> metadata = null,
            string version = "1.0",
            bool isEncrypted = false)
        {
            _fields = fields ?? new Dictionary<string, FieldValue>();
            Metadata = metadata ?? new Dictionary<string, object>();
            Version = version ?? "1.0";
            IsEncrypted = isEncrypted;
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
            Checksum = CalculateChecksum();
            SizeBytes = CalculateSize();
        }

        /// <summary>
        /// Creates an empty submission data.
        /// </summary>
        /// <returns>A new empty submission data.</returns>
        public static SubmissionData Empty()
        {
            return new SubmissionData();
        }

        /// <summary>
        /// Creates submission data from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>A new submission data parsed from JSON.</returns>
        public static SubmissionData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be null or empty.", nameof(json));

            try
            {
                var jsonDocument = JsonDocument.Parse(json);
                var fields = new Dictionary<string, FieldValue>();
                var metadata = new Dictionary<string, object>();

                if (jsonDocument.RootElement.TryGetProperty("fields", out var fieldsElement))
                {
                    foreach (var field in fieldsElement.EnumerateObject())
                    {
                        var fieldValue = ParseFieldValue(field.Value);
                        fields[field.Name] = fieldValue;
                    }
                }

                if (jsonDocument.RootElement.TryGetProperty("metadata", out var metadataElement))
                {
                    foreach (var meta in metadataElement.EnumerateObject())
                    {
                        metadata[meta.Name] = ParseJsonValue(meta.Value);
                    }
                }

                var version = jsonDocument.RootElement.TryGetProperty("version", out var versionElement) 
                    ? versionElement.GetString() : "1.0";
                
                var isEncrypted = jsonDocument.RootElement.TryGetProperty("isEncrypted", out var encryptedElement) 
                    && encryptedElement.GetBoolean();

                return new SubmissionData(fields, metadata, version, isEncrypted);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON format: {ex.Message}", nameof(json), ex);
            }
        }

        /// <summary>
        /// Creates submission data from a dictionary of raw values.
        /// </summary>
        /// <param name="rawData">The raw data dictionary.</param>
        /// <param name="fieldTypes">The field type mappings.</param>
        /// <returns>A new submission data with typed field values.</returns>
        public static SubmissionData FromRawData(Dictionary<string, object> rawData, Dictionary<string, Type> fieldTypes = null)
        {
            if (rawData == null)
                throw new ArgumentNullException(nameof(rawData));

            var fields = new Dictionary<string, FieldValue>();
            fieldTypes ??= new Dictionary<string, Type>();

            foreach (var kvp in rawData)
            {
                var fieldType = fieldTypes.ContainsKey(kvp.Key) ? fieldTypes[kvp.Key] : typeof(string);
                var fieldValue = FieldValue.Create(kvp.Value, fieldType);
                fields[kvp.Key] = fieldValue;
            }

            return new SubmissionData(fields);
        }

        /// <summary>
        /// Gets a field value by name.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The field value if found; otherwise, null.</returns>
        public FieldValue GetField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name cannot be null or empty.", nameof(fieldName));

            return _fields.TryGetValue(fieldName, out var value) ? value : null;
        }

        /// <summary>
        /// Gets a typed field value by name.
        /// </summary>
        /// <typeparam name="T">The expected field value type.</typeparam>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The typed field value if found and can be converted; otherwise, the default value for T.</returns>
        public T? GetFieldValue<T>(string fieldName) where T : class
        {
            var field = GetField(fieldName);
            return field?.GetValue<T>();
        }

        /// <summary>
        /// Gets a field value as string.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The field value as string; otherwise, null.</returns>
        public string GetFieldValueAsString(string fieldName)
        {
            return GetField(fieldName)?.ToString();
        }

        /// <summary>
        /// Checks if a field exists in the submission.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>True if the field exists; otherwise, false.</returns>
        public bool HasField(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName) && _fields.ContainsKey(fieldName);
        }

        /// <summary>
        /// Gets all field names in the submission.
        /// </summary>
        /// <returns>A collection of field names.</returns>
        public IEnumerable<string> GetFieldNames()
        {
            return _fields.Keys;
        }

        /// <summary>
        /// Gets the number of fields in the submission.
        /// </summary>
        /// <returns>The number of fields.</returns>
        public int GetFieldCount()
        {
            return _fields.Count;
        }

        /// <summary>
        /// Adds or updates a field value in the submission.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The field value.</param>
        /// <returns>A new submission data with the updated field.</returns>
        public SubmissionData WithField(string fieldName, FieldValue value)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name cannot be null or empty.", nameof(fieldName));

            var newFields = new Dictionary<string, FieldValue>(_fields)
            {
                [fieldName] = value
            };

            return new SubmissionData(newFields, new Dictionary<string, object>(Metadata), Version, IsEncrypted);
        }

        /// <summary>
        /// Adds or updates a field value in the submission.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The raw field value.</param>
        /// <param name="fieldType">The field type.</param>
        /// <returns>A new submission data with the updated field.</returns>
        public SubmissionData WithField(string fieldName, object value, Type fieldType = null)
        {
            var fieldValue = FieldValue.Create(value, fieldType ?? typeof(string));
            return WithField(fieldName, fieldValue);
        }

        /// <summary>
        /// Removes a field from the submission.
        /// </summary>
        /// <param name="fieldName">The field name to remove.</param>
        /// <returns>A new submission data without the specified field.</returns>
        public SubmissionData WithoutField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName) || !_fields.ContainsKey(fieldName))
                return this;

            var newFields = new Dictionary<string, FieldValue>(_fields);
            newFields.Remove(fieldName);

            return new SubmissionData(newFields, new Dictionary<string, object>(Metadata), Version, IsEncrypted);
        }

        /// <summary>
        /// Adds or updates metadata in the submission.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        /// <returns>A new submission data with the updated metadata.</returns>
        public SubmissionData WithMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty.", nameof(key));

            var newMetadata = new Dictionary<string, object>(Metadata)
            {
                [key] = value
            };

            return new SubmissionData(new Dictionary<string, FieldValue>(_fields), newMetadata, Version, IsEncrypted);
        }

        /// <summary>
        /// Converts the submission data to a JSON string.
        /// </summary>
        /// <param name="includeMetadata">Whether to include metadata in the JSON.</param>
        /// <returns>A JSON representation of the submission data.</returns>
        public string ToJson(bool includeMetadata = true)
        {
            var data = new Dictionary<string, object>
            {
                ["fields"] = _fields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.RawValue),
                ["version"] = Version,
                ["isEncrypted"] = IsEncrypted,
                ["createdAt"] = CreatedAt,
                ["modifiedAt"] = ModifiedAt,
                ["checksum"] = Checksum,
                ["sizeBytes"] = SizeBytes
            };

            if (includeMetadata && Metadata.Any())
            {
                data["metadata"] = Metadata;
            }

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        /// <summary>
        /// Converts the submission data to a dictionary of raw values.
        /// </summary>
        /// <returns>A dictionary containing the raw field values.</returns>
        public Dictionary<string, object> ToRawData()
        {
            return _fields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.RawValue);
        }

        /// <summary>
        /// Validates the submission data against a set of field definitions.
        /// </summary>
        /// <param name="fieldDefinitions">The field definitions to validate against.</param>
        /// <returns>A collection of validation errors.</returns>
        public IEnumerable<ValidationError> Validate(IEnumerable<FieldDefinition> fieldDefinitions)
        {
            var errors = new List<ValidationError>();
            var fieldDefs = fieldDefinitions?.ToList() ?? new List<FieldDefinition>();

            // Check required fields
            foreach (var fieldDef in fieldDefs.Where(f => f.IsRequired))
            {
                if (!HasField(fieldDef.Name) || GetField(fieldDef.Name)?.IsEmpty == true)
                {
                    errors.Add(new ValidationError(fieldDef.Name, "Field is required."));
                }
            }

            // Validate field values
            foreach (var field in _fields)
            {
                var fieldDef = fieldDefs.FirstOrDefault(f => f.Name == field.Key);
                if (fieldDef != null)
                {
                    var fieldErrors = field.Value.Validate(fieldDef.ValidationRules);
                    errors.AddRange(fieldErrors.Select(e => new ValidationError(field.Key, e)));
                }
            }

            return errors;
        }

        /// <summary>
        /// Creates a copy of the submission data with updated modification timestamp.
        /// </summary>
        /// <returns>A new submission data with updated modification timestamp.</returns>
        public SubmissionData Touch()
        {
            var result = new SubmissionData(new Dictionary<string, FieldValue>(_fields), new Dictionary<string, object>(Metadata), Version, IsEncrypted);
            result.ModifiedAt = DateTime.UtcNow;
            result.Checksum = result.CalculateChecksum();
            result.SizeBytes = result.CalculateSize();
            return result;
        }

        private string CalculateChecksum()
        {
            var data = ToJson(false);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        private long CalculateSize()
        {
            var json = ToJson();
            return System.Text.Encoding.UTF8.GetByteCount(json);
        }

        private static FieldValue ParseFieldValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => FieldValue.Create(element.GetString(), typeof(string)),
                JsonValueKind.Number => FieldValue.Create(element.GetDecimal(), typeof(decimal)),
                JsonValueKind.True or JsonValueKind.False => FieldValue.Create(element.GetBoolean(), typeof(bool)),
                JsonValueKind.Array => FieldValue.Create(element.EnumerateArray().Select(ParseJsonValue).ToArray(), typeof(object[])),
                JsonValueKind.Object => FieldValue.Create(ParseJsonObject(element), typeof(Dictionary<string, object>)),
                JsonValueKind.Null => FieldValue.Create(null, typeof(object)),
                _ => FieldValue.Create(element.GetRawText(), typeof(string))
            };
        }

        private static object ParseJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetDecimal(),
                JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
                JsonValueKind.Array => element.EnumerateArray().Select(ParseJsonValue).ToArray(),
                JsonValueKind.Object => ParseJsonObject(element),
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        private static Dictionary<string, object> ParseJsonObject(JsonElement element)
        {
            var result = new Dictionary<string, object>();
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ParseJsonValue(property.Value);
            }
            return result;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(SubmissionData other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return _fields.SequenceEqual(other._fields) &&
                   Metadata.SequenceEqual(other.Metadata) &&
                   Version == other.Version &&
                   IsEncrypted == other.IsEncrypted;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as SubmissionData);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Checksum, Version, IsEncrypted);
        }

        /// <summary>
        /// Returns a string representation of the submission data.
        /// </summary>
        /// <returns>A string representation of the submission data.</returns>
        public override string ToString()
        {
            return $"SubmissionData(Fields: {_fields.Count}, Size: {SizeBytes} bytes, Version: {Version})";
        }

        public static bool operator ==(SubmissionData left, SubmissionData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SubmissionData left, SubmissionData right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Represents a typed field value in a form submission.
    /// </summary>
    public class FieldValue : IEquatable<FieldValue>
    {
        /// <summary>
        /// Gets the raw value of the field.
        /// </summary>
        public object RawValue { get; private set; }

        /// <summary>
        /// Gets the type of the field value.
        /// </summary>
        public Type ValueType { get; private set; }

        /// <summary>
        /// Gets whether the field value is empty or null.
        /// </summary>
        public bool IsEmpty => RawValue == null || (RawValue is string str && string.IsNullOrWhiteSpace(str));

        /// <summary>
        /// Gets whether the field value is a collection.
        /// </summary>
        public bool IsCollection => RawValue is System.Collections.IEnumerable && !(RawValue is string);

        /// <summary>
        /// Initializes a new instance of the FieldValue class.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <param name="valueType">The value type.</param>
        public FieldValue(object value, Type valueType)
        {
            RawValue = value;
            ValueType = valueType ?? typeof(object);
        }

        /// <summary>
        /// Creates a field value from a raw value and type.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <param name="valueType">The value type.</param>
        /// <returns>A new field value.</returns>
        public static FieldValue Create(object value, Type valueType)
        {
            return new FieldValue(value, valueType);
        }

        /// <summary>
        /// Gets the typed value of the field.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <returns>The typed value if conversion is possible; otherwise, the default value for T.</returns>
        public T GetValue<T>()
        {
            if (RawValue == null)
                return default(T);

            try
            {
                if (RawValue is T directValue)
                    return directValue;

                return (T)Convert.ChangeType(RawValue, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Validates the field value against a set of validation rules.
        /// </summary>
        /// <param name="validationRules">The validation rules to apply.</param>
        /// <returns>A collection of validation error messages.</returns>
        public IEnumerable<string> Validate(IEnumerable<ValidationRule> validationRules)
        {
            var errors = new List<string>();
            var rules = validationRules?.Where(r => r.IsEnabled) ?? Enumerable.Empty<ValidationRule>();

            foreach (var rule in rules)
            {
                if (!ValidateRule(rule))
                {
                    errors.Add(rule.ErrorMessage ?? $"Validation failed for rule: {rule.Type}");
                }
            }

            return errors;
        }

        private bool ValidateRule(ValidationRule rule)
        {
            // This is a simplified validation - in a real implementation,
            // you would have a more sophisticated validation engine
            return rule.Type switch
            {
                "required" => !IsEmpty,
                "minLength" => RawValue?.ToString()?.Length >= (int)(rule.Value ?? 0),
                "maxLength" => RawValue?.ToString()?.Length <= (int)(rule.Value ?? int.MaxValue),
                "pattern" => RawValue == null || System.Text.RegularExpressions.Regex.IsMatch(RawValue.ToString(), rule.Value?.ToString() ?? ""),
                _ => true // Unknown rules pass by default
            };
        }

        /// <summary>
        /// Returns a string representation of the field value.
        /// </summary>
        /// <returns>A string representation of the field value.</returns>
        public override string ToString()
        {
            return RawValue?.ToString() ?? "";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(FieldValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(RawValue, other.RawValue) && ValueType == other.ValueType;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as FieldValue);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(RawValue, ValueType);
        }

        public static bool operator ==(FieldValue left, FieldValue right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FieldValue left, FieldValue right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Represents a validation error for a specific field.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets the field name that has the validation error.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Gets the validation error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the ValidationError class.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="message">The error message.</param>
        public ValidationError(string fieldName, string message)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>
        /// Returns a string representation of the validation error.
        /// </summary>
        /// <returns>A string representation of the validation error.</returns>
        public override string ToString()
        {
            return $"{FieldName}: {Message}";
        }
    }


}