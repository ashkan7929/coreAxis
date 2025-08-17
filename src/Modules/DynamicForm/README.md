# Dynamic Form Module

A comprehensive module for creating, managing, and processing dynamic forms with advanced validation, dependency management, and submission handling capabilities.

## Features

### Core Features
- **Dynamic Form Creation**: Create forms with various field types and configurations
- **Advanced Validation Engine**: Support for complex validation rules and cross-field validation
- **Dependency Management**: Handle field dependencies and conditional logic
- **Submission Processing**: Manage form submissions with validation and processing workflows
- **Form Versioning**: Track form changes and maintain version history
- **Access Control**: Role-based access control for forms and submissions
- **Audit Logging**: Complete audit trail for all form operations

### Advanced Features
- **Analytics and Reporting**: Comprehensive analytics for form usage and performance
- **Import/Export**: Support for multiple formats (JSON, XML, CSV, Excel, PDF)
- **Templates**: Pre-built form templates for common use cases
- **Workflows**: Configurable workflows for form processing
- **Notifications**: Multi-channel notification system (Email, SMS, Push, In-App)
- **Caching**: Multi-level caching for optimal performance
- **Security**: Field-level security, encryption, and protection mechanisms
- **Integration APIs**: REST APIs, webhooks, and external service integration

## Supported Field Types

### Basic Fields
- `text` - Single line text input
- `textarea` - Multi-line text input
- `number` - Numeric input
- `email` - Email address input
- `password` - Password input
- `url` - URL input
- `tel` - Telephone number input

### Date/Time Fields
- `date` - Date picker
- `time` - Time picker
- `datetime` - Date and time picker
- `month` - Month picker
- `week` - Week picker

### Selection Fields
- `checkbox` - Single checkbox
- `radio` - Radio button group
- `select` - Dropdown selection
- `multiselect` - Multiple selection dropdown

### File Fields
- `file` - File upload
- `image` - Image upload with preview

### Advanced Fields
- `color` - Color picker
- `range` - Range slider
- `hidden` - Hidden field
- `section` - Form section divider
- `html` - Custom HTML content
- `calculated` - Calculated field based on expressions
- `lookup` - Lookup field with external data source
- `signature` - Digital signature capture
- `rating` - Star rating input
- `matrix` - Matrix/grid input
- `repeater` - Repeatable field groups
- `conditional` - Conditionally visible fields

## Validation Rules

### Basic Validation
- `required` - Field is required
- `minLength` - Minimum text length
- `maxLength` - Maximum text length
- `min` - Minimum numeric value
- `max` - Maximum numeric value
- `pattern` - Regular expression pattern

### Type-Specific Validation
- `email` - Valid email format
- `url` - Valid URL format
- `numeric` - Numeric value
- `integer` - Integer value
- `date` - Valid date
- `time` - Valid time

### Advanced Validation
- `custom` - Custom validation logic
- `conditional` - Conditional validation rules
- `crossField` - Cross-field validation
- `async` - Asynchronous validation
- `fileType` - File type validation
- `fileSize` - File size validation
- `imageSize` - Image dimension validation

## Expression Engine

The module includes a powerful expression engine that supports:

### Expression Types
- **Arithmetic**: `+`, `-`, `*`, `/`, `%`, `^`
- **Logical**: `AND`, `OR`, `NOT`
- **Comparison**: `=`, `!=`, `<`, `>`, `<=`, `>=`
- **String**: `CONCAT`, `SUBSTRING`, `LENGTH`, `UPPER`, `LOWER`
- **Date**: `NOW`, `TODAY`, `DATEADD`, `DATEDIFF`
- **Conditional**: `IF`, `SWITCH`, `CASE`
- **Lookup**: `LOOKUP`, `VLOOKUP`
- **Aggregate**: `SUM`, `AVG`, `COUNT`, `MIN`, `MAX`

### Expression Examples
```javascript
// Simple calculation
"price * quantity"

// Conditional logic
"IF(age >= 18, 'Adult', 'Minor')"

// Date calculation
"DATEDIFF(birthDate, TODAY(), 'years')"

// String manipulation
"CONCAT(firstName, ' ', lastName)"

// Lookup from external source
"LOOKUP('countries', countryCode, 'name')"
```

## API Endpoints

### Forms API
```http
GET    /api/forms                    # Get forms with filtering
GET    /api/forms/{id}               # Get form by ID
GET    /api/forms/by-name/{name}     # Get form by name
POST   /api/forms                    # Create new form
PUT    /api/forms/{id}               # Update form
DELETE /api/forms/{id}               # Delete form
GET    /api/forms/{id}/schema        # Get form schema
POST   /api/forms/{id}/validate      # Validate form data
POST   /api/forms/{id}/submit        # Submit form
GET    /api/forms/{id}/submissions   # Get form submissions
GET    /api/forms/{id}/stats         # Get form statistics
```

### Submissions API
```http
GET    /api/submissions              # Get submissions with filtering
GET    /api/submissions/{id}         # Get submission by ID
POST   /api/submissions              # Create new submission
PUT    /api/submissions/{id}         # Update submission
DELETE /api/submissions/{id}         # Delete submission
POST   /api/submissions/validate     # Validate submission data
GET    /api/submissions/stats        # Get submission statistics
PATCH  /api/submissions/bulk-status  # Bulk update submission status
```

## Configuration

### Basic Configuration
```json
{
  "DynamicForm": {
    "MaxFormSize": 1000,
    "MaxSubmissionSize": 5000,
    "MaxFieldsPerForm": 100,
    "MaxSubmissionsPerForm": 10000,
    "EnableAuditLog": true,
    "EnableVersioning": true,
    "EnableCaching": true,
    "DefaultLanguage": "en",
    "SupportedLanguages": ["en", "fa"]
  }
}
```

### Validation Configuration
```json
{
  "DynamicForm": {
    "Validation": {
      "EnableStrictValidation": true,
      "EnableAsyncValidation": true,
      "EnableCrossFieldValidation": true,
      "MaxValidationErrors": 50,
      "ValidationTimeout": 30,
      "CacheValidationResults": true
    }
  }
}
```

### Cache Configuration
```json
{
  "DynamicForm": {
    "Cache": {
      "EnableDistributedCache": true,
      "EnableMemoryCache": true,
      "DefaultCacheExpiry": 3600,
      "FormCacheExpiry": 1800,
      "SchemaCacheExpiry": 3600,
      "CacheKeyPrefix": "DF:"
    }
  }
}
```

### Security Configuration
```json
{
  "DynamicForm": {
    "Security": {
      "EnableEncryption": false,
      "EnableFieldLevelSecurity": true,
      "EnableAccessControl": true,
      "EnableRateLimiting": true,
      "MaxRequestsPerMinute": 100,
      "AllowedFileTypes": [".pdf", ".doc", ".jpg", ".png"],
      "MaxFileSize": 10
    }
  }
}
```

## Usage Examples

### Creating a Form
```csharp
var createFormCommand = new CreateFormCommand
{
    Name = "Contact Form",
    Title = "Contact Us",
    Description = "Please fill out this form to contact us",
    TenantId = tenantId,
    BusinessId = businessId,
    SchemaJson = JsonSerializer.Serialize(new
    {
        fields = new[]
        {
            new
            {
                id = "name",
                type = "text",
                label = "Full Name",
                required = true,
                validation = new { minLength = 2, maxLength = 100 }
            },
            new
            {
                id = "email",
                type = "email",
                label = "Email Address",
                required = true,
                validation = new { email = true }
            },
            new
            {
                id = "message",
                type = "textarea",
                label = "Message",
                required = true,
                validation = new { minLength = 10, maxLength = 1000 }
            }
        }
    })
};

var result = await mediator.Send(createFormCommand);
```

### Submitting Form Data
```csharp
var submitCommand = new CreateSubmissionCommand
{
    FormId = formId,
    UserId = userId,
    Data = JsonSerializer.Serialize(new
    {
        name = "John Doe",
        email = "john.doe@example.com",
        message = "Hello, I would like to get in touch with you."
    }),
    ValidateBeforeSubmit = true
};

var result = await mediator.Send(submitCommand);
```

### Validating Form Data
```csharp
var validateCommand = new ValidateFormCommand
{
    FormId = formId,
    Data = JsonSerializer.Serialize(formData),
    Language = "en"
};

var validationResult = await mediator.Send(validateCommand);
if (!validationResult.Data.IsValid)
{
    foreach (var error in validationResult.Data.Errors)
    {
        Console.WriteLine($"Field {error.FieldId}: {error.Message}");
    }
}
```

## Integration

### Adding to Startup
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add Dynamic Form Module
    services.AddDynamicFormModule(Configuration);
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Use Dynamic Form Module
    app.UseDynamicFormModule();
}
```

### Health Checks
The module provides comprehensive health checks:
- Database connectivity
- Cache service availability
- Validation engine status
- Integration service status

### Background Services
- **Form Cleanup Service**: Removes expired forms and submissions
- **Analytics Processor**: Processes form analytics data
- **Notification Processor**: Handles notification queue
- **Backup Scheduler**: Automated backup operations

## Performance Considerations

### Caching Strategy
- **Form Schema**: Cached for 1 hour
- **Form Definitions**: Cached for 30 minutes
- **Validation Results**: Cached for 5 minutes
- **Submission Data**: Not cached by default

### Database Optimization
- Proper indexing on frequently queried fields
- Connection pooling for better performance
- Query optimization for large datasets
- Async operations for non-blocking execution

### Scalability
- Horizontal scaling support
- Distributed caching with Redis
- Background job processing
- Rate limiting to prevent abuse

## Security Features

### Data Protection
- Field-level encryption for sensitive data
- CSRF protection for form submissions
- XSS protection for user input
- SQL injection prevention

### Access Control
- Role-based access control (RBAC)
- Tenant-level data isolation
- API key authentication
- JWT token validation

### Audit Trail
- Complete audit log for all operations
- User activity tracking
- Data change history
- Security event logging

## Monitoring and Analytics

### Metrics
- Form creation and usage statistics
- Submission success/failure rates
- Validation error patterns
- Performance metrics

### Logging
- Structured logging with Serilog
- Error tracking and alerting
- Performance monitoring
- Security event logging

## Troubleshooting

### Common Issues

1. **Form Schema Validation Errors**
   - Check JSON schema format
   - Verify field type definitions
   - Validate expression syntax

2. **Submission Validation Failures**
   - Review validation rules
   - Check data type compatibility
   - Verify required field values

3. **Performance Issues**
   - Enable caching
   - Optimize database queries
   - Review form complexity

4. **Integration Problems**
   - Check API endpoints
   - Verify authentication
   - Review webhook configurations

### Debug Mode
Enable debug logging for detailed troubleshooting:
```json
{
  "Logging": {
    "LogLevel": {
      "CoreAxis.Modules.DynamicForm": "Debug"
    }
  }
}
```

## Contributing

When contributing to this module:
1. Follow the established coding standards
2. Add unit tests for new features
3. Update documentation
4. Ensure backward compatibility
5. Test with multiple scenarios

## License

This module is part of the CoreAxis platform and is subject to the CoreAxis license terms.