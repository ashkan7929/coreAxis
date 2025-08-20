using CoreAxis.Modules.MLMModule.Application.Contracts;
using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class JoinMLMRequestValidatorTests
{
    private readonly JoinMLMRequestValidator _validator;

    public JoinMLMRequestValidatorTests()
    {
        _validator = new JoinMLMRequestValidator();
    }

    [Fact]
    public void Validate_WithValidReferralCode_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new JoinMLMRequest
        {
            ReferralCode = "REF123456"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReferralCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyReferralCode_ShouldHaveValidationError(string referralCode)
    {
        // Arrange
        var request = new JoinMLMRequest
        {
            ReferralCode = referralCode
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReferralCode)
            .WithErrorMessage("Referral code is required.");
    }

    [Theory]
    [InlineData("AB")]  // Too short
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789")] // Too long
    public void Validate_WithInvalidReferralCodeLength_ShouldHaveValidationError(string referralCode)
    {
        // Arrange
        var request = new JoinMLMRequest
        {
            ReferralCode = referralCode
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReferralCode)
            .WithErrorMessage("Referral code must be between 3 and 20 characters.");
    }

    [Theory]
    [InlineData("REF@123")] // Contains special character
    [InlineData("REF 123")] // Contains space
    [InlineData("REF#123")] // Contains hash
    public void Validate_WithInvalidReferralCodeFormat_ShouldHaveValidationError(string referralCode)
    {
        // Arrange
        var request = new JoinMLMRequest
        {
            ReferralCode = referralCode
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReferralCode)
            .WithErrorMessage("Referral code can only contain letters and numbers.");
    }
}

public class GetDownlineRequestValidatorTests
{
    private readonly GetDownlineRequestValidator _validator;

    public GetDownlineRequestValidatorTests()
    {
        _validator = new GetDownlineRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new GetDownlineRequest
        {
            PageNumber = 1,
            PageSize = 10,
            MaxDepth = 5
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidPageNumber_ShouldHaveValidationError(int pageNumber)
    {
        // Arrange
        var request = new GetDownlineRequest
        {
            PageNumber = pageNumber,
            PageSize = 10
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorMessage("Page number must be greater than 0.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)] // Too large
    public void Validate_WithInvalidPageSize_ShouldHaveValidationError(int pageSize)
    {
        // Arrange
        var request = new GetDownlineRequest
        {
            PageNumber = 1,
            PageSize = pageSize
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)] // Too deep
    public void Validate_WithInvalidMaxDepth_ShouldHaveValidationError(int maxDepth)
    {
        // Arrange
        var request = new GetDownlineRequest
        {
            PageNumber = 1,
            PageSize = 10,
            MaxDepth = maxDepth
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxDepth);
    }
}

public class GetCommissionsRequestValidatorTests
{
    private readonly GetCommissionsRequestValidator _validator;

    public GetCommissionsRequestValidatorTests()
    {
        _validator = new GetCommissionsRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new GetCommissionsRequest
        {
            PageNumber = 1,
            PageSize = 20,
            Status = "Pending",
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("InvalidStatus")]
    [InlineData("PENDING")] // Case sensitive
    [InlineData("pending")] // Case sensitive
    public void Validate_WithInvalidStatus_ShouldHaveValidationError(string status)
    {
        // Arrange
        var request = new GetCommissionsRequest
        {
            PageNumber = 1,
            PageSize = 20,
            Status = status
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage("Status must be one of: Pending, Approved, Rejected, Expired.");
    }

    [Fact]
    public void Validate_WithFromDateAfterToDate_ShouldHaveValidationError()
    {
        // Arrange
        var request = new GetCommissionsRequest
        {
            PageNumber = 1,
            PageSize = 20,
            FromDate = DateTime.UtcNow,
            ToDate = DateTime.UtcNow.AddDays(-1) // ToDate before FromDate
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ToDate)
            .WithErrorMessage("To date must be after from date.");
    }

    [Fact]
    public void Validate_WithDateRangeTooLarge_ShouldHaveValidationError()
    {
        // Arrange
        var request = new GetCommissionsRequest
        {
            PageNumber = 1,
            PageSize = 20,
            FromDate = DateTime.UtcNow.AddDays(-400), // More than 1 year
            ToDate = DateTime.UtcNow
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Date range cannot exceed 365 days.");
    }
}

public class ApproveCommissionRequestValidatorTests
{
    private readonly ApproveCommissionRequestValidator _validator;

    public ApproveCommissionRequestValidatorTests()
    {
        _validator = new ApproveCommissionRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new ApproveCommissionRequest
        {
            Notes = "Approved after verification"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyNotes_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new ApproveCommissionRequest
        {
            Notes = null // Notes are optional
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Validate_WithTooLongNotes_ShouldHaveValidationError()
    {
        // Arrange
        var longNotes = new string('A', 1001); // More than 1000 characters
        var request = new ApproveCommissionRequest
        {
            Notes = longNotes
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 1000 characters.");
    }
}

public class RejectCommissionRequestValidatorTests
{
    private readonly RejectCommissionRequestValidator _validator;

    public RejectCommissionRequestValidatorTests()
    {
        _validator = new RejectCommissionRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new RejectCommissionRequest
        {
            Reason = "Insufficient documentation",
            Notes = "Additional verification required"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyReason_ShouldHaveValidationError(string reason)
    {
        // Arrange
        var request = new RejectCommissionRequest
        {
            Reason = reason,
            Notes = "Some notes"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason is required when rejecting a commission.");
    }

    [Fact]
    public void Validate_WithTooLongReason_ShouldHaveValidationError()
    {
        // Arrange
        var longReason = new string('A', 501); // More than 500 characters
        var request = new RejectCommissionRequest
        {
            Reason = longReason,
            Notes = "Some notes"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_WithTooLongNotes_ShouldHaveValidationError()
    {
        // Arrange
        var longNotes = new string('A', 1001); // More than 1000 characters
        var request = new RejectCommissionRequest
        {
            Reason = "Valid reason",
            Notes = longNotes
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 1000 characters.");
    }
}

public class CreateCommissionRuleSetRequestValidatorTests
{
    private readonly CreateCommissionRuleSetRequestValidator _validator;

    public CreateCommissionRuleSetRequestValidatorTests()
    {
        _validator = new CreateCommissionRuleSetRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new CreateCommissionRuleSetRequest
        {
            Name = "Standard Commission Rules",
            Description = "Standard commission structure for all products"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ShouldHaveValidationError(string name)
    {
        // Arrange
        var request = new CreateCommissionRuleSetRequest
        {
            Name = name,
            Description = "Valid description"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void Validate_WithTooLongName_ShouldHaveValidationError()
    {
        // Arrange
        var longName = new string('A', 201); // More than 200 characters
        var request = new CreateCommissionRuleSetRequest
        {
            Name = longName,
            Description = "Valid description"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_WithTooLongDescription_ShouldHaveValidationError()
    {
        // Arrange
        var longDescription = new string('A', 1001); // More than 1000 characters
        var request = new CreateCommissionRuleSetRequest
        {
            Name = "Valid name",
            Description = longDescription
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 1000 characters.");
    }
}

public class CreateCommissionRuleVersionRequestValidatorTests
{
    private readonly CreateCommissionRuleVersionRequestValidator _validator;

    public CreateCommissionRuleVersionRequestValidatorTests()
    {
        _validator = new CreateCommissionRuleVersionRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var validJson = """
        {
            "levels": [
                { "level": 1, "percentage": 10 },
                { "level": 2, "percentage": 5 }
            ]
        }
        """;
        
        var request = new CreateCommissionRuleVersionRequest
        {
            SchemaJson = validJson,
            Description = "Version 1.0 with basic commission structure"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptySchemaJson_ShouldHaveValidationError(string schemaJson)
    {
        // Arrange
        var request = new CreateCommissionRuleVersionRequest
        {
            SchemaJson = schemaJson,
            Description = "Valid description"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SchemaJson)
            .WithErrorMessage("Schema JSON is required.");
    }

    [Fact]
    public void Validate_WithInvalidJson_ShouldHaveValidationError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var request = new CreateCommissionRuleVersionRequest
        {
            SchemaJson = invalidJson,
            Description = "Valid description"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SchemaJson)
            .WithErrorMessage("Schema JSON must be valid JSON format.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyDescription_ShouldHaveValidationError(string description)
    {
        // Arrange
        var validJson = "{ \"levels\": [] }";
        var request = new CreateCommissionRuleVersionRequest
        {
            SchemaJson = validJson,
            Description = description
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required.");
    }
}