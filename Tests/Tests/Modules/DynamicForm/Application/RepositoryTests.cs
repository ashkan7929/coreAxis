using CoreAxis.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace CoreAxis.Tests.Modules.DynamicForm.Application;

public class TestEntity : EntityBase
{
    public string Name { get; set; }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities { get; set; }
}

public class RepositoryTests
{
    private TestDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Repository_AddAsync_ShouldAddEntity()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Name = "Test", Description = "Test Description" };

        // Act
        await repository.AddAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        var savedEntity = await repository.GetByIdAsync(entity.Id);
        savedEntity.Should().NotBeNull();
        savedEntity!.Name.Should().Be("Test");
        savedEntity.Description.Should().Be("Test Description");
    }

    [Fact]
    public async Task Repository_GetByIdAsync_ShouldReturnEntity()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Name = "Test", Description = "Test Description" };
        
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public async Task Repository_ExistsAsync_ShouldReturnTrue_WhenEntityExists()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Name = "Test", Description = "Test Description" };
        
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act
        var exists = await repository.ExistsAsync(entity.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Repository_CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var repository = new Repository<TestEntity>(context);
        
        context.TestEntities.AddRange(
            new TestEntity { Name = "Test1", Description = "Description1" },
            new TestEntity { Name = "Test2", Description = "Description2" }
        );
        await context.SaveChangesAsync();

        // Act
        var count = await repository.CountAsync();

        // Assert
        count.Should().Be(2);
    }
}