using CoreAxis.Modules.MLMModule.Infrastructure.Persistence;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CoreAxis.Tests.MLMModule;

public class MLMTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    public MLMModuleDbContext DbContext { get; private set; }
    public Mock<IEventBus> EventBusMock { get; private set; }
    public Mock<ILogger> LoggerMock { get; private set; }

    public MLMTestFixture()
    {
        var services = new ServiceCollection();
        
        // Setup in-memory database
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<MLMModuleDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));
        
        // Setup mocks
        EventBusMock = new Mock<IEventBus>();
        LoggerMock = new Mock<ILogger>();
        
        services.AddSingleton(EventBusMock.Object);
        services.AddSingleton(LoggerMock.Object);
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<MLMModuleDbContext>();
        
        // Ensure database is created
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

[CollectionDefinition("MLM Collection")]
public class MLMTestCollection : ICollectionFixture<MLMTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public abstract class MLMTestBase : IClassFixture<MLMTestFixture>
{
    protected readonly MLMTestFixture Fixture;
    protected readonly MLMModuleDbContext DbContext;
    protected readonly Mock<IEventBus> EventBusMock;
    protected readonly Mock<ILogger> LoggerMock;

    protected MLMTestBase(MLMTestFixture fixture)
    {
        Fixture = fixture;
        DbContext = fixture.DbContext;
        EventBusMock = fixture.EventBusMock;
        LoggerMock = fixture.LoggerMock;
        
        // Reset mocks before each test
        EventBusMock.Reset();
        LoggerMock.Reset();
        
        // Clear database before each test
        ClearDatabase();
    }

    private void ClearDatabase()
    {
        DbContext.CommissionTransactions.RemoveRange(DbContext.CommissionTransactions);
        DbContext.CommissionRuleVersions.RemoveRange(DbContext.CommissionRuleVersions);
        DbContext.CommissionRuleSets.RemoveRange(DbContext.CommissionRuleSets);
        DbContext.UserReferrals.RemoveRange(DbContext.UserReferrals);
        DbContext.SaveChanges();
    }

    protected async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            var result = await operation();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    protected async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            await operation();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}