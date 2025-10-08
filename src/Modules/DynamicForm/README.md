# 📋 Dynamic Form Module

The Dynamic Form Module enables creation, management, and processing of complex, multi‑step forms with validation, conditional logic, and robust submission workflows. This document mirrors the Wallet Module style: high‑level scope, limits, roadmap, domain, API, schema, snippets, and operational notes.

---

## 🔹 Scope
- Rich form builder: fields, steps, and conditional visibility.
- Validation engine: sync/async rules, cross‑field and step‑level checks.
- Submission lifecycle: create, update, complete, analytics.
- Form events: trigger handlers for automation and integrations.
- Expressions: formula evaluation for calculated fields.
- XML Swagger docs: detailed remarks and response codes per action.

## 🔹 Limitations
- Workflow orchestration beyond step completion is basic.
- External data lookups require custom handlers or adapters.
- Bulk operations are limited for extremely large datasets.

## 🔹 Roadmap
- Visual rule builder for conditional logic.
- Pluggable data sources for `lookup` fields.
- Advanced analytics dashboards and export pipelines.
- Versioned form schemas with migration helpers.

---

## 🔹 Domain
- Entities: `Form`, `FormStep`, `FormSubmission`, `FormStepSubmission`.
- Aggregates: Form (owns steps), FormSubmission (owns step submissions).
- Events: `FormCreated`, `FormStepAdded`, `FormSubmitted`, `FormStepCompleted`.
- Policies: step order integrity, validation before completion, idempotent writes.

---

## 🔹 API

All endpoints return structured responses and Problem+JSON errors where applicable. Selected endpoints support idempotency and correlation headers.

### Forms
- `GET /api/forms` — List/filter forms
  - Query: `name?`, `tenantId?`, `businessId?`, `cursor?`, `limit?`
  - Responses: `200 OK`

- `GET /api/forms/{id}` — Get form by id
  - Responses: `200 OK`, `404 NotFound`

- `POST /api/forms` — Create form
  - Body: `{ name, title, description?, tenantId, businessId, schemaJson }`
  - Responses: `201 Created`, `400 BadRequest`

- `PUT /api/forms/{id}` — Update form
  - Responses: `200 OK`, `404 NotFound`, `400 BadRequest`

- `DELETE /api/forms/{id}` — Delete form
  - Responses: `204 NoContent`, `404 NotFound`

### Form Steps
- `GET /api/forms/{formId}/steps` — List steps for a form
  - Responses: `200 OK`, `404 NotFound`

- `GET /api/forms/steps/{id}` — Get step by id
  - Responses: `200 OK`, `404 NotFound`

- `POST /api/forms/{formId}/steps` — Create step
  - Body: `{ title, order, schemaJson, isOptional? }`
  - Responses: `201 Created`, `400 BadRequest`, `404 NotFound`

- `PUT /api/forms/steps/{id}` — Update step
  - Responses: `200 OK`, `404 NotFound`, `400 BadRequest`

- `DELETE /api/forms/steps/{id}` — Delete step
  - Responses: `204 NoContent`, `404 NotFound`

### Submissions
- `GET /api/submissions` — List/filter submissions
  - Query: `formId?`, `userId?`, `status?`, `cursor?`, `limit?`
  - Responses: `200 OK`, `400 BadRequest`

- `GET /api/submissions/{id}` — Get submission by id
  - Responses: `200 OK`, `404 NotFound`

- `POST /api/submissions` — Create submission
  - Body: `{ formId, userId, dataJson, validateBeforeSubmit? }`
  - Responses: `201 Created`, `400 BadRequest`

- `PUT /api/submissions/{id}` — Update submission
  - Responses: `200 OK`, `404 NotFound`, `400 BadRequest`

- `DELETE /api/submissions/{id}` — Delete submission
  - Responses: `204 NoContent`, `404 NotFound`

### Form Step Submissions
- `GET /api/form-step-submissions/{id}` — Get step submission by id
  - Responses: `200 OK`, `404 NotFound`

- `GET /api/form-step-submissions/by-submission/{formSubmissionId}` — List step submissions for submission
  - Responses: `200 OK`, `404 NotFound`

- `POST /api/form-step-submissions` — Create step submission
  - Body: `{ formSubmissionId, formStepId, dataJson }`
  - Responses: `201 Created`, `400 BadRequest`

- `PUT /api/form-step-submissions/{id}` — Update step submission
  - Responses: `200 OK`, `404 NotFound`, `400 BadRequest`

- `POST /api/form-step-submissions/{id}/complete` — Complete a step submission
  - Responses: `200 OK`, `404 NotFound`, `400 BadRequest`, `422 UnprocessableEntity`

### Events
- `POST /api/form-events/trigger` — Trigger an event handler
  - Body: `{ eventName, payload }`
  - Responses: `200 OK`, `400 BadRequest`

- `GET /api/form-events/handlers` — List available handlers
  - Responses: `200 OK`

### Formula
- `POST /api/formula/evaluate` — Evaluate expression
  - Body: `{ expression, contextJson }`
  - Responses: `200 OK`, `400 BadRequest`

---

## 🔹 Schema

### Core Entities
- `Form`: `{ id, name, title, description?, tenantId, businessId, schemaJson, createdAt }`
- `FormStep`: `{ id, formId, title, order, schemaJson, isOptional? }`
- `FormSubmission`: `{ id, formId, userId, status, dataJson, createdAt, updatedAt }`
- `FormStepSubmission`: `{ id, formSubmissionId, formStepId, status, dataJson, createdAt, updatedAt }`

### DTOs (common)
- `CreateFormRequest`, `UpdateFormRequest`
- `CreateFormStepRequest`, `UpdateFormStepRequest`
- `CreateSubmissionRequest`, `UpdateSubmissionRequest`
- `CreateFormStepSubmissionRequest`, `UpdateFormStepSubmissionRequest`
- `PagedResult<T>` with `items` and `cursor`

---

## 🔹 Code Snippets

### Register module (API layer)
```csharp
services.AddDynamicFormModule(configuration);
```

### Sample: Create Form
```http
POST /api/forms
Content-Type: application/json

{
  "name": "contact",
  "title": "Contact Us",
  "tenantId": "00000000-0000-0000-0000-000000000001",
  "businessId": "00000000-0000-0000-0000-000000000002",
  "schemaJson": {
    "fields": [
      { "id": "name", "type": "text", "label": "Full Name", "required": true },
      { "id": "email", "type": "email", "label": "Email", "required": true },
      { "id": "message", "type": "textarea", "label": "Message", "required": true }
    ]
  }
}
```

Response (201 Created)
```json
{
  "id": "0d9e6bca-...",
  "name": "contact",
  "title": "Contact Us"
}
```

### Sample: Complete Step Submission
```http
POST /api/form-step-submissions/0d9e6bca-.../complete
Content-Type: application/json

{
  "validate": true
}
```

Response (200 OK)
```json
{
  "id": "0d9e6bca-...",
  "status": "Completed"
}
```

---

## 🔹 Operational Notes
- Provide `X-Correlation-ID` when invoking event triggers for traceability.
- Use cursor pagination (`cursor`, `limit`) for long listings.
- Validate step data before completion to avoid `422` responses.
- Swagger XML remarks are enabled across controllers for rich documentation.
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