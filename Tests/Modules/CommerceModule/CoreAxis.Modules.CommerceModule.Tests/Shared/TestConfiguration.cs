using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.CommerceModule.Tests.Shared;

/// <summary>
/// Configuration settings for Commerce Module tests
/// </summary>
public class TestConfiguration
{
    public DatabaseSettings Database { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();
    public MockSettings Mocks { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public ExternalServiceSettings ExternalServices { get; set; } = new();
    
    public class DatabaseSettings
    {
        public bool UseInMemoryDatabase { get; set; } = true;
        public string ConnectionString { get; set; } = "";
        public bool EnableSensitiveDataLogging { get; set; } = true;
        public bool EnableDetailedErrors { get; set; } = true;
        public int CommandTimeout { get; set; } = 30;
    }
    
    public class PerformanceSettings
    {
        public int DefaultTimeout { get; set; } = 30000; // 30 seconds
        public int StressTestOperations { get; set; } = 1000;
        public int ConcurrencyLevel { get; set; } = 10;
        public int LargeDatasetSize { get; set; } = 10000;
        public int BenchmarkIterations { get; set; } = 100;
        public double AcceptablePerformanceDegradation { get; set; } = 0.1; // 10%
    }
    
    public class MockSettings
    {
        public PaymentServiceMockSettings PaymentService { get; set; } = new();
        public WalletServiceMockSettings WalletService { get; set; } = new();
        public NotificationServiceMockSettings NotificationService { get; set; } = new();
        public EmailServiceMockSettings EmailService { get; set; } = new();
        
        public class PaymentServiceMockSettings
        {
            public bool SimulateFailures { get; set; } = false;
            public double FailureRate { get; set; } = 0.05; // 5%
            public int ProcessingDelayMs { get; set; } = 100;
            public decimal DefaultFeePercentage { get; set; } = 0.029m; // 2.9%
            public decimal DefaultFixedFee { get; set; } = 0.30m;
        }
        
        public class WalletServiceMockSettings
        {
            public decimal DefaultBalance { get; set; } = 1000.00m;
            public bool SimulateInsufficientBalance { get; set; } = false;
            public int ProcessingDelayMs { get; set; } = 50;
        }
        
        public class NotificationServiceMockSettings
        {
            public bool SimulateFailures { get; set; } = false;
            public double FailureRate { get; set; } = 0.01; // 1%
            public int DeliveryDelayMs { get; set; } = 10;
        }
        
        public class EmailServiceMockSettings
        {
            public bool SimulateFailures { get; set; } = false;
            public double FailureRate { get; set; } = 0.02; // 2%
            public int DeliveryDelayMs { get; set; } = 200;
        }
    }
    
    public class LoggingSettings
    {
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = false;
        public bool EnableStructuredLogging { get; set; } = true;
        public string LogFilePath { get; set; } = "logs/commerce-tests.log";
    }
    
    public class ExternalServiceSettings
    {
        public string PaymentGatewayBaseUrl { get; set; } = "https://api.test-payment-gateway.com";
        public string WalletServiceBaseUrl { get; set; } = "https://api.test-wallet.com";
        public string NotificationServiceBaseUrl { get; set; } = "https://api.test-notifications.com";
        public string EmailServiceBaseUrl { get; set; } = "https://api.test-email.com";
        public int HttpTimeoutSeconds { get; set; } = 30;
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
    }
    
    /// <summary>
    /// Creates a default test configuration
    /// </summary>
    public static TestConfiguration CreateDefault()
    {
        return new TestConfiguration();
    }
    
    /// <summary>
    /// Creates a configuration for performance testing
    /// </summary>
    public static TestConfiguration CreateForPerformanceTesting()
    {
        var config = CreateDefault();
        config.Performance.StressTestOperations = 5000;
        config.Performance.ConcurrencyLevel = 50;
        config.Performance.LargeDatasetSize = 50000;
        config.Performance.BenchmarkIterations = 1000;
        config.Logging.MinimumLevel = LogLevel.Warning; // Reduce logging overhead
        return config;
    }
    
    /// <summary>
    /// Creates a configuration for integration testing
    /// </summary>
    public static TestConfiguration CreateForIntegrationTesting()
    {
        var config = CreateDefault();
        config.Database.UseInMemoryDatabase = false;
        config.Database.ConnectionString = "Server=194.62.17.5,1433;Database=CoreAxisDb;User Id=coreaxis_user;Password=YOUR_SECURE_PASSWORD;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        config.Mocks.PaymentService.SimulateFailures = true;
        config.Mocks.WalletService.SimulateInsufficientBalance = true;
        config.ExternalServices.RetryAttempts = 1; // Faster test execution
        return config;
    }
    
    /// <summary>
    /// Creates a configuration for stress testing
    /// </summary>
    public static TestConfiguration CreateForStressTesting()
    {
        var config = CreateDefault();
        config.Performance.StressTestOperations = 10000;
        config.Performance.ConcurrencyLevel = 100;
        config.Performance.DefaultTimeout = 120000; // 2 minutes
        config.Mocks.PaymentService.SimulateFailures = true;
        config.Mocks.PaymentService.FailureRate = 0.1; // 10% failure rate
        config.Logging.MinimumLevel = LogLevel.Error; // Minimal logging
        return config;
    }
    
    /// <summary>
    /// Loads configuration from appsettings.test.json
    /// </summary>
    public static TestConfiguration LoadFromFile(string filePath = "appsettings.test.json")
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(filePath, optional: true)
            .AddEnvironmentVariables("COMMERCE_TEST_")
            .Build();
        
        var testConfig = new TestConfiguration();
        configuration.Bind(testConfig);
        
        return testConfig;
    }
    
    /// <summary>
    /// Saves configuration to JSON file
    /// </summary>
    public void SaveToFile(string filePath = "appsettings.test.json")
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(filePath, json);
    }
    
    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (Performance.DefaultTimeout <= 0)
            throw new InvalidOperationException("Performance.DefaultTimeout must be greater than 0");
        
        if (Performance.ConcurrencyLevel <= 0)
            throw new InvalidOperationException("Performance.ConcurrencyLevel must be greater than 0");
        
        if (Performance.StressTestOperations <= 0)
            throw new InvalidOperationException("Performance.StressTestOperations must be greater than 0");
        
        if (Mocks.PaymentService.FailureRate < 0 || Mocks.PaymentService.FailureRate > 1)
            throw new InvalidOperationException("Mocks.PaymentService.FailureRate must be between 0 and 1");
        
        if (Mocks.WalletService.DefaultBalance < 0)
            throw new InvalidOperationException("Mocks.WalletService.DefaultBalance must be non-negative");
        
        if (!Database.UseInMemoryDatabase && string.IsNullOrWhiteSpace(Database.ConnectionString))
            throw new InvalidOperationException("Database.ConnectionString is required when not using in-memory database");
    }
}

/// <summary>
/// Test environment configuration
/// </summary>
public static class TestEnvironment
{
    private static TestConfiguration? _current;
    
    /// <summary>
    /// Gets the current test configuration
    /// </summary>
    public static TestConfiguration Current
    {
        get
        {
            if (_current == null)
            {
                _current = DetermineConfiguration();
                _current.Validate();
            }
            return _current;
        }
        set
        {
            _current = value;
            _current?.Validate();
        }
    }
    
    /// <summary>
    /// Determines the appropriate configuration based on environment
    /// </summary>
    private static TestConfiguration DetermineConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("COMMERCE_TEST_ENVIRONMENT");
        
        return environment?.ToLowerInvariant() switch
        {
            "performance" => TestConfiguration.CreateForPerformanceTesting(),
            "integration" => TestConfiguration.CreateForIntegrationTesting(),
            "stress" => TestConfiguration.CreateForStressTesting(),
            _ => TestConfiguration.LoadFromFile()
        };
    }
    
    /// <summary>
    /// Resets the configuration (useful for testing)
    /// </summary>
    public static void Reset()
    {
        _current = null;
    }
    
    /// <summary>
    /// Checks if running in CI/CD environment
    /// </summary>
    public static bool IsRunningInCI => 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_PIPELINES")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL"));
    
    /// <summary>
    /// Checks if running in debug mode
    /// </summary>
    public static bool IsDebugMode =>
#if DEBUG
        true;
#else
        false;
#endif
    
    /// <summary>
    /// Gets the test data directory
    /// </summary>
    public static string TestDataDirectory
    {
        get
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var testDataPath = Path.Combine(baseDirectory, "TestData");
            
            if (!Directory.Exists(testDataPath))
                Directory.CreateDirectory(testDataPath);
            
            return testDataPath;
        }
    }
    
    /// <summary>
    /// Gets the test output directory
    /// </summary>
    public static string TestOutputDirectory
    {
        get
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var outputPath = Path.Combine(baseDirectory, "TestOutput");
            
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
            
            return outputPath;
        }
    }
}

/// <summary>
/// Test categories for organizing tests
/// </summary>
public static class TestCategories
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string Performance = "Performance";
    public const string Stress = "Stress";
    public const string Regression = "Regression";
    public const string Smoke = "Smoke";
    public const string EndToEnd = "EndToEnd";
    public const string Security = "Security";
    public const string Compatibility = "Compatibility";
    public const string LongRunning = "LongRunning";
}

/// <summary>
/// Test traits for xUnit
/// </summary>
public static class TestTraits
{
    public const string Category = "Category";
    public const string Priority = "Priority";
    public const string Owner = "Owner";
    public const string Feature = "Feature";
    public const string Requirement = "Requirement";
    public const string Bug = "Bug";
}

/// <summary>
/// Test priorities
/// </summary>
public static class TestPriorities
{
    public const string Critical = "Critical";
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";
}

/// <summary>
/// Commerce module features for test organization
/// </summary>
public static class CommerceFeatures
{
    public const string Inventory = "Inventory";
    public const string Orders = "Orders";
    public const string Payments = "Payments";
    public const string Subscriptions = "Subscriptions";
    public const string Discounts = "Discounts";
    public const string Refunds = "Refunds";
    public const string Reconciliation = "Reconciliation";
    public const string Pricing = "Pricing";
    public const string Notifications = "Notifications";
    public const string Audit = "Audit";
}