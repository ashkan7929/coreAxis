using CoreAxis.Modules.MLMModule.Application.Contracts;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Presentation.Controllers;
using CoreAxis.SharedKernel.Application.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using CoreAxis.Modules.MLMModule.Application.DTOs;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class MLMControllerTests
{
    private readonly Mock<IMLMService> _mlmServiceMock;
    private readonly Mock<ILogger<MLMController>> _loggerMock;
    private readonly MLMController _controller;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public MLMControllerTests()
    {
        _mlmServiceMock = new Mock<IMLMService>();
        _loggerMock = new Mock<ILogger<MLMController>>();
        _controller = new MLMController(_mlmServiceMock.Object, _loggerMock.Object);
        
        // Setup user context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _currentUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task GetMyReferralInfo_ShouldReturnOkWithReferralInfo()
    {
        // Arrange
        var expectedResponse = new UserReferralInfoResponse
        {
            UserId = _currentUserId,
            ReferralCode = "REF123",
            Level = 2,
            ParentUserId = Guid.NewGuid(),
            TotalDownlineCount = 10,
            DirectChildrenCount = 3,
            IsActive = true,
            JoinedAt = DateTime.UtcNow.AddDays(-30)
        };

        _mlmServiceMock
            .Setup(x => x.GetUserReferralInfoAsync(_currentUserId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetMyReferralInfo();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserReferralInfoResponse>(okResult.Value);
        Assert.Equal(expectedResponse.UserId, response.UserId);
        Assert.Equal(expectedResponse.ReferralCode, response.ReferralCode);
    }

    [Fact]
    public async Task GetMyReferralInfo_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _mlmServiceMock
            .Setup(x => x.GetUserReferralInfoAsync(_currentUserId))
            .ReturnsAsync((UserReferralInfoResponse)null);

        // Act
        var result = await _controller.GetMyReferralInfo();

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetMyDownline_ShouldReturnPagedResults()
    {
        // Arrange
        var request = new GetDownlineRequest
        {
            PageNumber = 1,
            PageSize = 10,
            MaxDepth = 5
        };

        var expectedResponse = new PagedResult<DownlineUserResponse>
        {
            Items = new List<DownlineUserResponse>
            {
                new() { UserId = Guid.NewGuid(), Level = 1, IsActive = true },
                new() { UserId = Guid.NewGuid(), Level = 2, IsActive = true }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mlmServiceMock
            .Setup(x => x.GetUserDownlineAsync(_currentUserId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetMyDownline(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<DownlineUserResponse>>(okResult.Value);
        Assert.Equal(2, response.Items.Count());
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public async Task GetMyCommissions_ShouldReturnPagedCommissions()
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

        var expectedResponse = new PagedResult<CommissionResponse>
        {
            Items = new List<CommissionResponse>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Amount = 100m,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20
        };

        _mlmServiceMock
            .Setup(x => x.GetUserCommissionsAsync(_currentUserId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetMyCommissions(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<CommissionResponse>>(okResult.Value);
        Assert.Single(response.Items);
    }

    [Fact]
    public async Task JoinMLM_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = new JoinMLMRequest
        {
            ReferralCode = "REF123"
        };

        var expectedResponse = new UserReferralInfoResponse
        {
            UserId = _currentUserId,
            ReferralCode = "NEW_REF",
            Level = 1,
            ParentUserId = Guid.NewGuid(),
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        _mlmServiceMock
            .Setup(x => x.JoinMLMAsync(_currentUserId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.JoinMLM(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<UserReferralInfoResponse>(createdResult.Value);
        Assert.Equal(expectedResponse.UserId, response.UserId);
    }

    [Fact]
    public async Task JoinMLM_WithInvalidReferralCode_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new JoinMLMRequest
        {
            ReferralCode = "INVALID"
        };

        _mlmServiceMock
            .Setup(x => x.JoinMLMAsync(_currentUserId, request))
            .ThrowsAsync(new ArgumentException("Invalid referral code"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _controller.JoinMLM(request)
        );
    }

    [Theory]
    [InlineData(0, 10)] // Invalid page number
    [InlineData(1, 0)]  // Invalid page size
    [InlineData(1, 101)] // Page size too large
    public async Task GetMyDownline_WithInvalidPagination_ShouldReturnBadRequest(
        int pageNumber, 
        int pageSize)
    {
        // Arrange
        var request = new GetDownlineRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // Act
        var result = await _controller.GetMyDownline(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}

public class CommissionManagementControllerTests
{
    private readonly Mock<ICommissionManagementService> _commissionServiceMock;
    private readonly Mock<ILogger<CommissionManagementController>> _loggerMock;
    private readonly CommissionManagementController _controller;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public CommissionManagementControllerTests()
    {
        _commissionServiceMock = new Mock<ICommissionManagementService>();
        _loggerMock = new Mock<ILogger<CommissionManagementController>>();
        _controller = new CommissionManagementController(
            _commissionServiceMock.Object, 
            _loggerMock.Object
        );
        
        // Setup admin user context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _currentUserId.ToString()),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task GetPendingCommissions_ShouldReturnPagedResults()
    {
        // Arrange
        var request = new GetPendingCommissionsRequest
        {
            PageNumber = 1,
            PageSize = 50
        };

        var expectedResponse = new PagedResult<PendingCommissionResponse>
        {
            Items = new List<PendingCommissionResponse>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Amount = 100m,
                    CreatedAt = DateTime.UtcNow
                }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 50
        };

        _commissionServiceMock
            .Setup(x => x.GetPendingCommissionsAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPendingCommissions(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<PendingCommissionResponse>>(okResult.Value);
        Assert.Single(response.Items);
    }

    [Fact]
    public async Task ApproveCommission_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var commissionId = Guid.NewGuid();
        var request = new ApproveCommissionRequest
        {
            Notes = "Approved by admin"
        };

        _commissionServiceMock
            .Setup(x => x.ApproveCommissionAsync(commissionId, _currentUserId, request))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ApproveCommission(commissionId, request);

        // Assert
        Assert.IsType<OkResult>(result);
        _commissionServiceMock.Verify(
            x => x.ApproveCommissionAsync(commissionId, _currentUserId, request),
            Times.Once
        );
    }

    [Fact]
    public async Task RejectCommission_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var commissionId = Guid.NewGuid();
        var request = new RejectCommissionRequest
        {
            Reason = "Insufficient documentation",
            Notes = "Rejected by admin"
        };

        _commissionServiceMock
            .Setup(x => x.RejectCommissionAsync(commissionId, _currentUserId, request))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RejectCommission(commissionId, request);

        // Assert
        Assert.IsType<OkResult>(result);
        _commissionServiceMock.Verify(
            x => x.RejectCommissionAsync(commissionId, _currentUserId, request),
            Times.Once
        );
    }

    [Fact]
    public async Task ApproveCommission_WhenCommissionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var commissionId = Guid.NewGuid();
        var request = new ApproveCommissionRequest();

        _commissionServiceMock
            .Setup(x => x.ApproveCommissionAsync(commissionId, _currentUserId, request))
            .ThrowsAsync(new InvalidOperationException("Commission not found"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.ApproveCommission(commissionId, request)
        );
    }
}