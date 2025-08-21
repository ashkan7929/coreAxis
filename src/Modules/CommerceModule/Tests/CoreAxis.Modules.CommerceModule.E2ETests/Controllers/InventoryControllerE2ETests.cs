using CoreAxis.Modules.CommerceModule.E2ETests.Infrastructure;
using CoreAxis.Modules.CommerceModule.Api.DTOs;
using FluentAssertions;
using System.Net;
using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.CommerceModule.Domain.Entities;

namespace CoreAxis.Modules.CommerceModule.E2ETests.Controllers;

public class InventoryControllerE2ETests : BaseE2ETest
{
    public InventoryControllerE2ETests(CommerceTestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateInventoryItem_WithValidData_ShouldReturnCreatedItem()
    {
        // Arrange
        var createDto = CreateValidInventoryItemDto();

        // Act
        var result = await PostAsync<InventoryItemDto>("/api/v1/inventory", createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(createDto.Name);
        result.Description.Should().Be(createDto.Description);
        result.Sku.Should().Be(createDto.Sku);
        result.Price.Should().Be(createDto.Price);
        result.Currency.Should().Be(createDto.Currency);
        result.Quantity.Should().Be(createDto.Quantity);
        result.LowStockThreshold.Should().Be(createDto.LowStockThreshold);
        result.Category.Should().Be(createDto.Category);
        result.IsActive.Should().Be(createDto.IsActive);

        // Verify in database
        var dbItem = await DbContext.InventoryItems
            .FirstOrDefaultAsync(x => x.Id == result.Id);
        dbItem.Should().NotBeNull();
        dbItem!.Name.Should().Be(createDto.Name);
    }

    [Fact]
    public async Task GetInventoryItem_WithValidId_ShouldReturnItem()
    {
        // Arrange
        var item = await CreateTestInventoryItemAsync();

        // Act
        var result = await GetAsync<InventoryItemDto>($"/api/v1/inventory/{item.Id}");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(item.Id);
        result.Name.Should().Be(item.Name);
        result.Sku.Should().Be(item.Sku);
    }

    [Fact]
    public async Task GetInventoryItem_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/api/v1/inventory/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllInventoryItems_ShouldReturnPagedResult()
    {
        // Arrange
        var items = new List<InventoryItem>();
        for (int i = 0; i < 5; i++)
        {
            items.Add(await CreateTestInventoryItemAsync());
        }

        // Act
        var result = await GetAsync<PagedResultDto<InventoryItemDto>>("/api/v1/inventory?page=1&pageSize=10");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterOrEqualTo(5);
        result.TotalCount.Should().BeGreaterOrEqualTo(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task UpdateInventoryItem_WithValidData_ShouldReturnUpdatedItem()
    {
        // Arrange
        var item = await CreateTestInventoryItemAsync();
        var updateDto = new UpdateInventoryItemDto
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Price = 999.99m,
            Quantity = 50,
            LowStockThreshold = 5,
            Category = "Updated Category",
            IsActive = false
        };

        // Act
        var response = await PutAsync($"/api/v1/inventory/{item.Id}", updateDto);
        response.EnsureSuccessStatusCode();

        // Assert
        var updatedItem = await GetAsync<InventoryItemDto>($"/api/v1/inventory/{item.Id}");
        updatedItem.Name.Should().Be(updateDto.Name);
        updatedItem.Description.Should().Be(updateDto.Description);
        updatedItem.Price.Should().Be(updateDto.Price);
        updatedItem.Quantity.Should().Be(updateDto.Quantity);
        updatedItem.IsActive.Should().Be(updateDto.IsActive);
    }

    [Fact]
    public async Task DeleteInventoryItem_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var item = await CreateTestInventoryItemAsync();

        // Act
        var response = await DeleteAsync($"/api/v1/inventory/{item.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify item is deleted
        var getResponse = await HttpClient.GetAsync($"/api/v1/inventory/{item.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReserveInventory_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var item = await CreateTestInventoryItemAsync();
        var reserveDto = new ReserveInventoryDto
        {
            InventoryItemId = item.Id,
            Quantity = 5,
            ReservationReference = Guid.NewGuid().ToString()
        };

        // Act
        var response = await PostAsync<object>("/api/v1/inventory/reserve", reserveDto);

        // Assert
        response.Should().NotBeNull();

        // Verify reservation in database
        var updatedItem = await DbContext.InventoryItems
            .FirstOrDefaultAsync(x => x.Id == item.Id);
        updatedItem!.ReservedQuantity.Should().Be(5);
    }

    [Fact]
    public async Task ReleaseInventory_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var item = await CreateTestInventoryItemAsync();
        var reservationReference = Guid.NewGuid().ToString();
        
        // First reserve some inventory
        var reserveDto = new ReserveInventoryDto
        {
            InventoryItemId = item.Id,
            Quantity = 5,
            ReservationReference = reservationReference
        };
        await PostAsync<object>("/api/v1/inventory/reserve", reserveDto);

        var releaseDto = new ReleaseInventoryDto
        {
            InventoryItemId = item.Id,
            Quantity = 5,
            ReservationReference = reservationReference
        };

        // Act
        var response = await PostAsync<object>("/api/v1/inventory/release", releaseDto);

        // Assert
        response.Should().NotBeNull();

        // Verify release in database
        var updatedItem = await DbContext.InventoryItems
            .FirstOrDefaultAsync(x => x.Id == item.Id);
        updatedItem!.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task GetLowStockItems_ShouldReturnItemsBelowThreshold()
    {
        // Arrange
        var lowStockItem = new InventoryItem(
            "Low Stock Item",
            "Description",
            "SKU001",
            100m,
            "USD",
            2, // Quantity below threshold
            5, // Low stock threshold
            "Category"
        );
        await SeedInventoryItemAsync(lowStockItem);

        var normalStockItem = new InventoryItem(
            "Normal Stock Item",
            "Description",
            "SKU002",
            100m,
            "USD",
            10, // Quantity above threshold
            5, // Low stock threshold
            "Category"
        );
        await SeedInventoryItemAsync(normalStockItem);

        // Act
        var result = await GetAsync<List<InventoryItemDto>>("/api/v1/inventory/low-stock");

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(x => x.Id == lowStockItem.Id);
        result.Should().NotContain(x => x.Id == normalStockItem.Id);
    }

    [Theory]
    [InlineData("", "Name is required")]
    [InlineData(null, "Name is required")]
    public async Task CreateInventoryItem_WithInvalidName_ShouldReturnBadRequest(string name, string expectedError)
    {
        // Arrange
        var createDto = CreateValidInventoryItemDto();
        createDto.Name = name;

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/inventory", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task CreateInventoryItem_WithInvalidPrice_ShouldReturnBadRequest(decimal price)
    {
        // Arrange
        var createDto = CreateValidInventoryItemDto();
        createDto.Price = price;

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/inventory", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateInventoryItem_WithDuplicateSku_ShouldReturnConflict()
    {
        // Arrange
        var existingItem = await CreateTestInventoryItemAsync();
        var createDto = CreateValidInventoryItemDto();
        createDto.Sku = existingItem.Sku; // Use same SKU

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/inventory", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}