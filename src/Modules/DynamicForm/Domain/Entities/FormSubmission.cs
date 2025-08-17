using CoreAxis.Modules.DynamicForm.Domain.Events;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents a submission of a dynamic form.
    /// </summary>
    public class FormSubmission : EntityBase
    {
        private readonly List<FormStepSubmission> _stepSubmissions = new List<FormStepSubmission>();
        /// <summary>
        /// Gets or sets the form identifier that this submission belongs to.
        /// </summary>
        [Required]
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who submitted the form.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the submission data as JSON.
        /// </summary>
        [Required]
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the status of the submission.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = SubmissionStatus.Draft;

        /// <summary>
        /// Gets or sets the validation errors as JSON (if any).
        /// </summary>
        public string ValidationErrors { get; set; }

        /// <summary>
        /// Gets or sets the IP address from which the form was submitted.
        /// </summary>
        [MaxLength(45)] // IPv6 max length
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent string of the browser used for submission.
        /// </summary>
        [MaxLength(500)]
        public string UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the referrer URL from which the form was accessed.
        /// </summary>
        [MaxLength(1000)]
        public string Referrer { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the form was submitted.
        /// </summary>
        public DateTime? SubmittedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the submission was approved (if applicable).
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who approved the submission (if applicable).
        /// </summary>
        [MaxLength(100)]
        public string ApprovedBy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the submission was rejected (if applicable).
        /// </summary>
        public DateTime? RejectedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who rejected the submission (if applicable).
        /// </summary>
        [MaxLength(100)]
        public string RejectedBy { get; set; }

        /// <summary>
        /// Gets or sets the reason for rejection (if applicable).
        /// </summary>
        [MaxLength(1000)]
        public string RejectionReason { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the submission as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the parent form.
        /// </summary>
        public virtual Form Form { get; set; }

        /// <summary>
        /// Gets the collection of step submissions for multi-step forms.
        /// </summary>
        public virtual IReadOnlyCollection<FormStepSubmission> StepSubmissions => _stepSubmissions.AsReadOnly();

        /// <summary>
        /// Gets or sets the current step number for multi-step forms.
        /// </summary>
        public int? CurrentStepNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a multi-step submission.
        /// </summary>
        public bool IsMultiStep { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormSubmission"/> class.
        /// </summary>
        protected FormSubmission()
        {
            // Required for EF Core
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormSubmission"/> class.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="data">The submission data as JSON.</param>
        /// <param name="ipAddress">The IP address.</param>
        /// <param name="userAgent">The user agent.</param>
        public FormSubmission(Guid formId, Guid userId, string tenantId, string data, string ipAddress = null, string userAgent = null)
        {
            if (formId == Guid.Empty)
                throw new ArgumentException("Form ID cannot be empty.", nameof(formId));
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(data))
                throw new ArgumentException("Submission data cannot be null or empty.", nameof(data));

            Id = Guid.NewGuid();
            FormId = formId;
            UserId = userId;
            TenantId = tenantId;
            Data = data;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            Status = SubmissionStatus.Draft;
            CreatedBy = userId.ToString();
            CreatedOn = DateTime.UtcNow;
            IsActive = true;

            AddDomainEvent(new FormSubmissionCreatedEvent(Id, FormId, TenantId, UserId, Status.ToString(), CreatedBy));
        }

        /// <summary>
        /// Updates the submission data.
        /// </summary>
        /// <param name="data">The new submission data as JSON.</param>
        /// <param name="modifiedBy">The user who modified the submission.</param>
        public void UpdateData(string data, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(data))
                throw new ArgumentException("Submission data cannot be null or empty.", nameof(data));

            if (Status == SubmissionStatus.Submitted)
                throw new InvalidOperationException("Cannot update data of a submitted form.");

            Data = data;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormSubmissionUpdatedEvent(Id, FormId, TenantId, UserId, data, modifiedBy));
        }

        /// <summary>
        /// Submits the form submission.
        /// </summary>
        /// <param name="submittedBy">The user who submitted the form.</param>
        public void Submit(string submittedBy)
        {
            if (Status == SubmissionStatus.Submitted)
                throw new InvalidOperationException("Form submission is already submitted.");

            Status = SubmissionStatus.Submitted;
            SubmittedAt = DateTime.UtcNow;
            LastModifiedBy = submittedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormSubmissionSubmittedEvent(Id, FormId, TenantId, UserId, SubmittedAt.Value, string.Empty, string.Empty, submittedBy));
        }

        /// <summary>
        /// Approves the form submission.
        /// </summary>
        /// <param name="approvedBy">The user who approved the submission.</param>
        public void Approve(string approvedBy)
        {
            if (Status != SubmissionStatus.Submitted)
                throw new InvalidOperationException("Only submitted forms can be approved.");

            Status = SubmissionStatus.Approved;
            ApprovedAt = DateTime.UtcNow;
            ApprovedBy = approvedBy;
            LastModifiedBy = approvedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormSubmissionApprovedEvent(Id, FormId, TenantId, UserId, ApprovedAt.Value, Guid.Empty, approvedBy));
        }

        /// <summary>
        /// Rejects the form submission.
        /// </summary>
        /// <param name="rejectedBy">The user who rejected the submission.</param>
        /// <param name="reason">The reason for rejection.</param>
        public void Reject(string rejectedBy, string reason)
        {
            if (Status != SubmissionStatus.Submitted)
                throw new InvalidOperationException("Only submitted forms can be rejected.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Rejection reason cannot be null or empty.", nameof(reason));

            Status = SubmissionStatus.Rejected;
            RejectedAt = DateTime.UtcNow;
            RejectedBy = rejectedBy;
            RejectionReason = reason;
            LastModifiedBy = rejectedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormSubmissionRejectedEvent(Id, FormId, TenantId, UserId, RejectedAt.Value, Guid.Parse(rejectedBy), rejectedBy, reason));
        }

        /// <summary>
        /// Sets validation errors for the submission.
        /// </summary>
        /// <param name="validationErrors">The validation errors as JSON.</param>
        /// <param name="modifiedBy">The user who set the validation errors.</param>
        public void SetValidationErrors(string validationErrors, string modifiedBy)
        {
            ValidationErrors = validationErrors;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            // Count validation errors (assuming JSON array format)
            var errorCount = string.IsNullOrWhiteSpace(validationErrors) ? 0 : 
                validationErrors.Split('[', ']', '{', '}').Length - 1;

            AddDomainEvent(new FormSubmissionValidationErrorsSetEvent(Id, FormId, TenantId, validationErrors, errorCount, modifiedBy));
        }

        /// <summary>
        /// Clears validation errors for the submission.
        /// </summary>
        /// <param name="modifiedBy">The user who cleared the validation errors.</param>
        public void ClearValidationErrors(string modifiedBy)
        {
            ValidationErrors = null;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a step submission to this form submission.
        /// </summary>
        /// <param name="stepSubmission">The step submission to add.</param>
        public void AddStepSubmission(FormStepSubmission stepSubmission)
        {
            if (stepSubmission == null)
                throw new ArgumentNullException(nameof(stepSubmission));

            if (_stepSubmissions.Any(s => s.StepNumber == stepSubmission.StepNumber))
                throw new InvalidOperationException($"A step submission for step {stepSubmission.StepNumber} already exists.");

            _stepSubmissions.Add(stepSubmission);
            IsMultiStep = true;
        }

        /// <summary>
        /// Gets a step submission by step number.
        /// </summary>
        /// <param name="stepNumber">The step number.</param>
        /// <returns>The step submission if found, otherwise null.</returns>
        public FormStepSubmission GetStepSubmission(int stepNumber)
        {
            return _stepSubmissions.FirstOrDefault(s => s.StepNumber == stepNumber);
        }

        /// <summary>
        /// Gets all step submissions ordered by step number.
        /// </summary>
        /// <returns>The ordered collection of step submissions.</returns>
        public IEnumerable<FormStepSubmission> GetOrderedStepSubmissions()
        {
            return _stepSubmissions.OrderBy(s => s.StepNumber);
        }

        /// <summary>
        /// Moves to the next step in a multi-step form.
        /// </summary>
        /// <param name="nextStepNumber">The next step number.</param>
        /// <param name="modifiedBy">The user who moved to the next step.</param>
        public void MoveToNextStep(int nextStepNumber, string modifiedBy)
        {
            if (!IsMultiStep)
                throw new InvalidOperationException("Cannot move to next step in a single-step form.");

            CurrentStepNumber = nextStepNumber;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Completes the multi-step form submission.
        /// </summary>
        /// <param name="completedBy">The user who completed the submission.</param>
        public void CompleteMultiStepSubmission(string completedBy)
        {
            if (!IsMultiStep)
                throw new InvalidOperationException("Cannot complete multi-step submission for a single-step form.");

            var incompleteSteps = _stepSubmissions.Where(s => s.Status != StepSubmissionStatus.Completed && !s.IsSkipped).ToList();
            if (incompleteSteps.Any())
                throw new InvalidOperationException($"Cannot complete submission. Steps {string.Join(", ", incompleteSteps.Select(s => s.StepNumber))} are not completed.");

            Status = SubmissionStatus.Submitted;
            SubmittedAt = DateTime.UtcNow;
            CurrentStepNumber = null;
            LastModifiedBy = completedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormSubmissionSubmittedEvent(Id, FormId, TenantId, UserId, SubmittedAt.Value, string.Empty, string.Empty, completedBy));
        }
    }

    /// <summary>
    /// Constants for submission status values.
    /// </summary>
    public static class SubmissionStatus
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Processing = "Processing";
    }

    /// <summary>
    /// Constants for step submission status values.
    /// </summary>
    public static class StepSubmissionStatus
    {
        public const string NotStarted = "NotStarted";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Skipped = "Skipped";
        public const string Failed = "Failed";
    }


}