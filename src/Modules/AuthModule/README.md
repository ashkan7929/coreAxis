# AuthModule

The AuthModule provides comprehensive authentication and authorization functionality for the CoreAxis application. It implements a sophisticated role-based access control (RBAC) system with granular permissions, security features, and comprehensive audit logging.

## 🚀 Key Features

### 🔐 Authentication & Security
- **JWT Token-Based Authentication**: Secure, stateless authentication with configurable expiration
- **Password Security**: PBKDF2 hashing with SHA-256 and 10,000 iterations
- **Account Lockout Protection**: Automatic account locking after 5 failed login attempts (30-minute lockout)
- **IP Address Tracking**: Records login attempts with IP addresses for security monitoring
- **User Agent Logging**: Tracks browser/device information for security analysis

### 👥 User Management
- **User Registration**: Complete user registration with validation
- **Profile Management**: Update user information including username, email, and phone number
- **Password Management**: Secure password change functionality
- **Account Status Control**: Activate/deactivate user accounts
- **Account Unlocking**: Manual account unlock capability for administrators
- **Login History**: Track last login time and IP address

### 🛡️ Role-Based Access Control (RBAC)
- **Hierarchical Role System**: Create and manage user roles with descriptions
- **System Roles Protection**: Special system roles that cannot be deleted or deactivated
- **Role Assignment**: Assign multiple roles to users
- **Permission Inheritance**: Users inherit permissions from assigned roles
- **Direct Permission Assignment**: Assign permissions directly to users (bypassing roles)

### 🔑 Granular Permission System
- **Page-Action Based Permissions**: Permissions based on Page + Action combinations
- **Dynamic Permission Creation**: Create permissions for any page-action combination
- **Permission Management**: Full CRUD operations for permissions
- **Flexible Authorization**: Support for both role-based and direct user permissions

### 📊 Comprehensive Audit Logging
- **Access Log Tracking**: Complete audit trail of all authentication attempts
- **Success/Failure Logging**: Track both successful and failed operations
- **Resource Access Logging**: Log access to specific resources
- **Error Message Capture**: Store detailed error information for failed attempts
- **Additional Data Support**: Store custom metadata with log entries
- **Timestamp Precision**: UTC timestamp for all logged events

## 🏗️ Architecture

The AuthModule follows **Clean Architecture** principles with clear separation of concerns and dependency inversion:

### 🎯 Domain Layer (Core Business Logic)
**Contains the core business entities and rules, independent of external concerns**

**Entities:**
- **User**: Core user entity with authentication and profile management
- **Role**: Role definition with hierarchical support
- **Permission**: Granular permission system based on Page + Action
- **Page**: Application page/module definitions
- **Action**: Available actions (CRUD operations)
- **AccessLog**: Comprehensive audit logging entity

**Value Objects & Domain Services:**
- Password validation and hashing logic
- Permission name generation algorithms
- Account lockout business rules
- Role hierarchy validation

**Repository Interfaces:**
- `IUserRepository`: User data access contract
- `IRoleRepository`: Role management contract
- `IPermissionRepository`: Permission system contract
- `IAccessLogRepository`: Audit logging contract

### 🔄 Application Layer (Use Cases & Business Logic)
**Orchestrates domain entities to fulfill specific use cases**

#### 📝 Commands (Write Operations)
**CQRS pattern implementation for state-changing operations**

**User Management Commands:**
- `CreateUserCommand`: Register new user with validation
- `UpdateUserCommand`: Modify user profile information
- `DeleteUserCommand`: Soft delete user account
- `ChangePasswordCommand`: Secure password change with validation
- `LoginCommand`: Authenticate user and generate JWT token
- `AssignRoleToUserCommand`: Grant role to user
- `RemoveRoleFromUserCommand`: Revoke role from user

**Role Management Commands:**
- `CreateRoleCommand`: Create new role with permissions
- `UpdateRoleCommand`: Modify role details and permissions
- `DeleteRoleCommand`: Remove role (with system role protection)
- `AddPermissionToRoleCommand`: Grant permission to role
- `RemovePermissionFromRoleCommand`: Revoke permission from role

**Permission Management Commands:**
- `CreatePermissionCommand`: Create new page-action permission
- `UpdatePermissionCommand`: Modify permission details
- `DeletePermissionCommand`: Remove permission from system

#### 🔍 Queries (Read Operations)
**Optimized read operations with projection and filtering**

**User Queries:**
- `GetUserByIdQuery`: Retrieve user with roles and permissions
- `GetAllUsersQuery`: Paginated user list with filtering
- `GetUserPermissionsQuery`: User's effective permissions (role + direct)

**Role Queries:**
- `GetRoleByIdQuery`: Role details with permissions
- `GetAllRolesQuery`: Complete role list with metadata
- `GetRolePermissionsQuery`: Permissions assigned to specific role

**Permission Queries:**
- `GetPermissionByIdQuery`: Permission details with page/action info
- `GetAllPermissionsQuery`: Complete permission catalog
- `GetPermissionsByActionQuery`: Permissions for specific action
- `GetPermissionsByPageQuery`: Permissions for specific page

#### 🛠️ Application Services
**Cross-cutting application concerns and external integrations**

**IJwtTokenService:**
- `GenerateTokenAsync(user, roles, permissions)`: Create JWT with claims
- `ValidateTokenAsync(token)`: Verify token integrity and expiration
- `RefreshTokenAsync(refreshToken)`: Generate new access token
- `RevokeTokenAsync(token)`: Invalidate specific token

**IPasswordHasher:**
- `HashPassword(password, salt)`: PBKDF2 password hashing
- `VerifyPassword(password, hash, salt)`: Password verification
- `GenerateSalt()`: Cryptographically secure salt generation
- `ValidatePasswordStrength(password)`: Password policy enforcement

**IEmailService (Interface):**
- Email confirmation and notification support
- Password reset email functionality
- Account lockout notifications

#### 📨 Integration Events
**Domain events for cross-module communication**

**UserRegisteredIntegrationEvent:**
- Published when new user is successfully registered
- Contains user ID, email, and registration timestamp
- Enables other modules to react to user creation

**UserLoginIntegrationEvent:**
- Published on successful user authentication
- Includes login timestamp and IP address
- Supports security monitoring and analytics

#### 🎯 Command/Query Handlers
**MediatR-based request handling with cross-cutting concerns**

**Features:**
- **Validation**: FluentValidation integration for request validation
- **Authorization**: Permission-based access control
- **Logging**: Structured logging for all operations
- **Caching**: Query result caching for performance
- **Transactions**: Automatic transaction management
- **Error Handling**: Consistent exception handling and mapping

### 🗄️ Infrastructure Layer (External Concerns)
**Implements interfaces defined in Domain/Application layers**

#### 📊 Data Access
**Entity Framework Core implementation with optimizations**

**AuthDbContext:**
- Entity configurations and relationships
- Database seeding for initial data
- Soft delete global query filters
- Audit fields automatic population
- Performance optimizations (indexes, query splitting)

**Repository Implementations:**
- `UserRepository`: Optimized user queries with role/permission loading
- `RoleRepository`: Role management with permission eager loading
- `PermissionRepository`: Permission queries with page/action details
- `AccessLogRepository`: High-performance audit log storage

**Database Features:**
- **Migrations**: Automated schema versioning
- **Seeding**: Default roles, permissions, and admin user
- **Indexes**: Optimized for common query patterns
- **Constraints**: Database-level data integrity
- **Soft Deletes**: Preserve data for audit purposes

#### 🔧 External Services
**Third-party integrations and infrastructure services**

**JWT Token Service:**
- Microsoft.IdentityModel.Tokens integration
- Configurable token expiration and refresh
- Claims-based authorization support
- Token blacklisting for security

**Password Hashing Service:**
- PBKDF2 with SHA-256 implementation
- Configurable iteration count and salt size
- Timing attack protection
- Password strength validation

### 🌐 API Layer (Presentation)
**RESTful API endpoints with comprehensive documentation**

#### 🎮 Controllers
**Clean, focused controllers following REST principles**

**AuthController:**
- `POST /api/auth/register`: User registration
- `POST /api/auth/login`: User authentication
- `POST /api/auth/change-password`: Password change
- `POST /api/auth/refresh-token`: Token refresh
- `POST /api/auth/logout`: User logout

**UsersController:**
- `GET /api/users`: List users with pagination
- `GET /api/users/{id}`: Get user details
- `POST /api/users`: Create new user
- `PUT /api/users/{id}`: Update user
- `DELETE /api/users/{id}`: Delete user
- `POST /api/users/{id}/roles`: Assign role
- `DELETE /api/users/{id}/roles/{roleId}`: Remove role

**RolesController:**
- `GET /api/roles`: List all roles
- `GET /api/roles/{id}`: Get role details
- `POST /api/roles`: Create new role
- `PUT /api/roles/{id}`: Update role
- `DELETE /api/roles/{id}`: Delete role
- `POST /api/roles/{id}/permissions`: Add permission
- `DELETE /api/roles/{id}/permissions/{permissionId}`: Remove permission

**PermissionsController:**
- `GET /api/permissions`: List all permissions
- `GET /api/permissions/{id}`: Get permission details
- `POST /api/permissions`: Create new permission
- `PUT /api/permissions/{id}`: Update permission
- `DELETE /api/permissions/{id}`: Delete permission

#### 🛡️ Middleware & Filters
**Cross-cutting concerns and security**

**Authentication Middleware:**
- JWT token validation and claims extraction
- Automatic user context population
- Token expiration handling

**Authorization Filters:**
- Permission-based access control
- Role-based authorization
- Resource-specific permissions

**Exception Handling:**
- Global exception handling middleware
- Consistent error response format
- Security-aware error messages

**Audit Logging:**
- Automatic access log creation
- Request/response logging
- Performance metrics collection

#### 🔌 Dependency Injection
**Modular service registration with configuration**

**Service Registration:**
```csharp
services.AddAuthModuleApi(configuration);
```

**Includes:**
- Database context and repositories
- Application services and handlers
- Authentication and authorization
- MediatR pipeline behaviors
- Validation and mapping services
- Background services for cleanup

### 🔄 Cross-Cutting Concerns

#### 📊 Logging & Monitoring
- Structured logging with Serilog
- Performance metrics collection
- Security event monitoring
- Health checks for dependencies

#### ⚡ Performance
- Query optimization and caching
- Async/await throughout the stack
- Connection pooling and retry policies
- Background job processing

#### 🔒 Security
- Input validation and sanitization
- SQL injection prevention
- XSS protection
- Rate limiting and throttling
- Secure headers and CORS configuration

## Key Entities

### User
- Represents system users with authentication credentials
- Supports role assignments
- Tracks login attempts and account status

### Role
- Defines user roles with associated permissions
- Supports system-wide roles
- Hierarchical permission inheritance

### Permission
- Granular permissions based on Page and Action combinations
- Enables fine-grained access control
- Can be assigned directly to users or through roles

### Page
- Represents application pages/modules
- Used for organizing permissions

### Action
- Represents operations that can be performed
- Combined with Pages to create specific permissions

### AccessLog
- Comprehensive audit trail for authentication attempts
- Tracks successful and failed login attempts
- Supports security monitoring and compliance

## 🌐 API Endpoints

### 🔐 Authentication Endpoints

#### `POST /api/auth/register`
**User Registration**
- **Purpose**: Register a new user account
- **Request Body**: `CreateUserDto` (Username, Email, Password)
- **Response**: `UserDto` with created user information
- **Security**: Anonymous access allowed
- **Features**: Automatic password hashing, domain event publishing

#### `POST /api/auth/login`
**User Authentication**
- **Purpose**: Authenticate user and receive JWT token
- **Request Body**: `LoginDto` (Username, Password)
- **Response**: `LoginResultDto` with JWT token and user info
- **Security**: Anonymous access, IP address logging
- **Features**: Failed attempt tracking, account lockout protection

#### `POST /api/auth/change-password`
**Password Change**
- **Purpose**: Change authenticated user's password
- **Request Body**: `ChangePasswordDto` (CurrentPassword, NewPassword)
- **Response**: Success confirmation
- **Security**: Requires JWT authentication
- **Features**: Current password verification, secure password hashing

### 👥 User Management Endpoints

#### `GET /api/users/{id}`
**Get User by ID**
- **Purpose**: Retrieve specific user information
- **Parameters**: User ID (GUID)
- **Response**: `UserDto` with user details and assigned roles
- **Security**: Requires authentication

#### `GET /api/users`
**Get Users (Paginated)**
- **Purpose**: Retrieve list of users with pagination
- **Query Parameters**: Page, PageSize, Search filters
- **Response**: Paginated list of `UserDto`
- **Security**: Requires authentication

#### `PUT /api/users/{id}`
**Update User**
- **Purpose**: Update user profile information
- **Request Body**: `UpdateUserDto` (FirstName, LastName, Email, PhoneNumber, IsActive)
- **Response**: Updated `UserDto`
- **Security**: Requires authentication and appropriate permissions

#### `DELETE /api/users/{id}`
**Delete User**
- **Purpose**: Soft delete user account
- **Parameters**: User ID (GUID)
- **Response**: Success confirmation
- **Security**: Requires authentication and delete permissions

#### `POST /api/users/{id}/roles`
**Assign Role to User**
- **Purpose**: Assign a role to a specific user
- **Parameters**: User ID, Role ID in request body
- **Response**: Success confirmation
- **Security**: Requires authentication and role management permissions

#### `DELETE /api/users/{id}/roles/{roleId}`
**Remove Role from User**
- **Purpose**: Remove a role assignment from user
- **Parameters**: User ID, Role ID
- **Response**: Success confirmation
- **Security**: Requires authentication and role management permissions

### 🛡️ Role Management Endpoints

#### `POST /api/roles`
**Create Role**
- **Purpose**: Create a new role
- **Request Body**: `CreateRoleDto` (Name, Description, IsSystemRole)
- **Response**: Created `RoleDto`
- **Security**: Requires authentication and role creation permissions

#### `GET /api/roles/{id}`
**Get Role by ID**
- **Purpose**: Retrieve specific role information
- **Parameters**: Role ID (GUID)
- **Response**: `RoleDto` with role details and permissions
- **Security**: Requires authentication

#### `GET /api/roles`
**Get All Roles**
- **Purpose**: Retrieve list of all roles
- **Response**: List of `RoleDto`
- **Security**: Requires authentication

#### `PUT /api/roles/{id}`
**Update Role**
- **Purpose**: Update role information
- **Request Body**: `UpdateRoleDto` (Name, Description)
- **Response**: Updated `RoleDto`
- **Security**: Requires authentication and role management permissions
- **Note**: System roles cannot be deactivated

#### `DELETE /api/roles/{id}`
**Delete Role**
- **Purpose**: Soft delete role
- **Parameters**: Role ID (GUID)
- **Response**: Success confirmation
- **Security**: Requires authentication and role deletion permissions
- **Note**: System roles cannot be deleted

#### `POST /api/roles/{id}/permissions/{permissionId}`
**Add Permission to Role**
- **Purpose**: Assign a permission to a role
- **Parameters**: Role ID, Permission ID
- **Response**: Success confirmation
- **Security**: Requires authentication and permission management rights

#### `DELETE /api/roles/{id}/permissions/{permissionId}`
**Remove Permission from Role**
- **Purpose**: Remove permission from role
- **Parameters**: Role ID, Permission ID
- **Response**: Success confirmation
- **Security**: Requires authentication and permission management rights

### 🔑 Permission Management Endpoints

#### `GET /api/permissions`
**Get All Permissions**
- **Purpose**: Retrieve list of all permissions
- **Response**: List of `PermissionDto` with Page and Action details
- **Security**: Requires authentication

#### `GET /api/permissions/{id}`
**Get Permission by ID**
- **Purpose**: Retrieve specific permission information
- **Parameters**: Permission ID (GUID)
- **Response**: `PermissionDto` with full details
- **Security**: Requires authentication

#### `POST /api/permissions`
**Create Permission**
- **Purpose**: Create a new permission
- **Request Body**: `CreatePermissionDto` (PageId, ActionId, Description)
- **Response**: Created `PermissionDto`
- **Security**: Requires authentication and permission creation rights
- **Features**: Automatic name generation from Page + Action codes

#### `PUT /api/permissions/{id}`
**Update Permission**
- **Purpose**: Update permission details
- **Request Body**: `UpdatePermissionDto` (Description)
- **Response**: Updated `PermissionDto`
- **Security**: Requires authentication and permission management rights

#### `DELETE /api/permissions/{id}`
**Delete Permission**
- **Purpose**: Soft delete permission
- **Parameters**: Permission ID (GUID)
- **Response**: Success confirmation
- **Security**: Requires authentication and permission deletion rights

#### `GET /api/permissions/by-page/{pageId}`
**Get Permissions by Page**
- **Purpose**: Retrieve all permissions for a specific page
- **Parameters**: Page ID (GUID)
- **Response**: List of `PermissionDto` for the specified page
- **Security**: Requires authentication

#### `GET /api/permissions/by-action/{actionId}`
**Get Permissions by Action**
- **Purpose**: Retrieve all permissions for a specific action
- **Parameters**: Action ID (GUID)
- **Response**: List of `PermissionDto` for the specified action
- **Security**: Requires authentication

## ⚙️ Configuration

### 🔧 Complete Configuration Setup

#### JWT Authentication Settings
**Comprehensive JWT configuration for secure token-based authentication**

```json
{
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-here-must-be-at-least-32-characters",
    "Issuer": "CoreAxis",
    "Audience": "CoreAxis-Users",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7,
    "ClockSkewMinutes": 5,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true,
    "RequireExpirationTime": true,
    "RequireSignedTokens": true
  }
}
```

**JWT Configuration Properties:**
- **SecretKey**: 256-bit secret key for token signing (minimum 32 characters)
- **Issuer**: Token issuer identifier (your application name)
- **Audience**: Intended token audience (your application users)
- **ExpirationMinutes**: Access token lifetime (recommended: 15-60 minutes)
- **RefreshTokenExpirationDays**: Refresh token lifetime (recommended: 7-30 days)
- **ClockSkewMinutes**: Allowed time drift for token validation (default: 5 minutes)
- **Validation Flags**: Security validation options (all should be true in production)

#### Database Connection Configuration
**Entity Framework Core database connection settings**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CoreAxis_Auth;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;",
    "AuthConnection": "Server=localhost;Database=CoreAxis_Auth;User Id=auth_user;Password=secure_password;MultipleActiveResultSets=true;Encrypt=true;"
  }
}
```

**Connection String Options:**
- **Server**: Database server address
- **Database**: Dedicated database name for Auth module
- **Authentication**: Windows Authentication (Trusted_Connection) or SQL Authentication
- **MultipleActiveResultSets**: Enable for complex queries (recommended: true)
- **Encrypt**: Enable encryption for production (recommended: true)
- **TrustServerCertificate**: For development with self-signed certificates

#### Security Configuration
**Advanced security settings for production environments**

```json
{
  "AuthModuleSettings": {
    "Security": {
      "PasswordPolicy": {
        "MinimumLength": 8,
        "RequireUppercase": true,
        "RequireLowercase": true,
        "RequireDigit": true,
        "RequireSpecialCharacter": true,
        "MaximumAge": 90,
        "PreventReuse": 5
      },
      "AccountLockout": {
        "MaxFailedAttempts": 5,
        "LockoutDurationMinutes": 30,
        "ResetFailedAttemptsAfterMinutes": 60
      },
      "SessionManagement": {
        "MaxConcurrentSessions": 3,
        "SessionTimeoutMinutes": 120,
        "ExtendSessionOnActivity": true
      }
    }
  }
}
```

**Security Policy Details:**

**Password Policy:**
- **MinimumLength**: Minimum password length (recommended: 8-12)
- **Character Requirements**: Enforce complexity rules
- **MaximumAge**: Force password change after days (0 = never)
- **PreventReuse**: Number of previous passwords to remember

**Account Lockout:**
- **MaxFailedAttempts**: Failed login attempts before lockout
- **LockoutDurationMinutes**: How long account stays locked
- **ResetFailedAttemptsAfterMinutes**: Reset counter after successful login

**Session Management:**
- **MaxConcurrentSessions**: Limit simultaneous user sessions
- **SessionTimeoutMinutes**: Automatic logout after inactivity
- **ExtendSessionOnActivity**: Refresh session on user activity

#### Email Service Configuration
**Email notifications for authentication events**

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@domain.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "CoreAxis Authentication",
    "Templates": {
      "WelcomeEmail": "Templates/WelcomeEmail.html",
      "PasswordReset": "Templates/PasswordReset.html",
      "AccountLocked": "Templates/AccountLocked.html",
      "EmailConfirmation": "Templates/EmailConfirmation.html"
    }
  }
}
```

#### Logging Configuration
**Structured logging for security and audit purposes**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "CoreAxis.Modules.AuthModule": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/auth-module-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341",
          "apiKey": "your-seq-api-key"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

#### Performance & Caching Configuration
**Optimization settings for high-performance scenarios**

```json
{
  "CachingSettings": {
    "DefaultExpirationMinutes": 30,
    "UserPermissionsCacheMinutes": 15,
    "RolePermissionsCacheMinutes": 60,
    "JwtBlacklistCacheHours": 24,
    "EnableDistributedCache": true,
    "RedisConnection": "localhost:6379"
  },
  "PerformanceSettings": {
    "EnableQuerySplitting": true,
    "DefaultPageSize": 20,
    "MaxPageSize": 100,
    "CommandTimeoutSeconds": 30,
    "EnableSensitiveDataLogging": false
  }
}
```

#### Environment-Specific Configuration

**Development Environment:**
```json
{
  "AuthModuleSettings": {
    "Environment": "Development",
    "EnableDetailedErrors": true,
    "EnableSensitiveDataLogging": true,
    "BypassEmailVerification": true,
    "SeedDefaultData": true,
    "CreateDefaultAdmin": {
      "Username": "admin",
      "Email": "admin@localhost",
      "Password": "Admin123!"
    }
  }
}
```

**Production Environment:**
```json
{
  "AuthModuleSettings": {
    "Environment": "Production",
    "EnableDetailedErrors": false,
    "EnableSensitiveDataLogging": false,
    "RequireHttps": true,
    "EnableRateLimiting": true,
    "RateLimitRequests": 100,
    "RateLimitWindowMinutes": 15,
    "EnableHealthChecks": true
  }
}
```

### 🔐 Environment Variables
**Secure configuration using environment variables**

```bash
# JWT Configuration
JWT_SECRET_KEY="your-production-secret-key-256-bits"
JWT_ISSUER="YourProductionApp"
JWT_AUDIENCE="YourProductionUsers"

# Database Configuration
DATABASE_CONNECTION="Server=prod-server;Database=CoreAxis_Auth;User Id=auth_user;Password=secure_password;"

# Email Configuration
SMTP_USERNAME="your-smtp-username"
SMTP_PASSWORD="your-smtp-password"

# Redis Cache
REDIS_CONNECTION="your-redis-connection-string"

# External Services
SEQ_API_KEY="your-seq-api-key"
```

### 📋 Configuration Validation
**Built-in validation ensures proper configuration**

**Startup Validation:**
- JWT secret key length and complexity
- Database connection availability
- Required configuration sections
- Email service connectivity (if enabled)
- Cache service availability (if enabled)

**Runtime Health Checks:**
- Database connectivity
- External service availability
- Cache performance metrics
- JWT token validation performance

### 🚀 Quick Start Configuration
**Minimal configuration for getting started**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CoreAxis_Auth;Trusted_Connection=true;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-at-least-32-characters-long",
    "Issuer": "CoreAxis",
    "Audience": "CoreAxis-Users",
    "ExpirationMinutes": 60
  },
  "AuthModuleSettings": {
    "SeedDefaultData": true,
    "CreateDefaultAdmin": {
      "Username": "admin",
      "Email": "admin@localhost",
      "Password": "Admin123!"
    }
  }
}
```

## 🚀 Usage & Integration

### 📦 Module Registration
**Complete setup in your application startup**

#### .NET 6+ Program.cs Setup
```csharp
using CoreAxis.Modules.AuthModule.API;
using CoreAxis.Modules.AuthModule.Application;
using CoreAxis.Modules.AuthModule.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register AuthModule with all dependencies
builder.Services.AddAuthModuleApi(builder.Configuration);
builder.Services.AddAuthModuleApplication();
builder.Services.AddAuthModuleInfrastructure(builder.Configuration);

// Add authentication and authorization
builder.Services.AddAuthentication()
    .AddJwtBearer();
builder.Services.AddAuthorization();

// Add Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthentication();  // Must be before UseAuthorization
app.UseAuthorization();

// Map AuthModule controllers
app.MapControllers();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.MigrateAsync();
    
    // Seed default data if configured
    var seeder = scope.ServiceProvider.GetRequiredService<IAuthDataSeeder>();
    await seeder.SeedAsync();
}

app.Run();
```

#### Legacy Startup.cs Setup
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register AuthModule
        services.AddAuthModuleApi(Configuration);
        services.AddAuthModuleApplication();
        services.AddAuthModuleInfrastructure(Configuration);
        
        // Add MVC and API controllers
        services.AddControllers();
        
        // Add authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSettings = Configuration.GetSection("JwtSettings").Get<JwtSettings>();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                };
            });
            
        services.AddAuthorization();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

### 🗄️ Database Setup & Migrations
**Complete database initialization and migration guide**

#### Initial Database Setup
```bash
# Navigate to the solution root
cd /path/to/your/solution

# Install Entity Framework CLI tools (if not already installed)
dotnet tool install --global dotnet-ef

# Verify EF tools installation
dotnet ef --version
```

#### Create Initial Migration
```bash
# Create the initial migration for AuthModule
dotnet ef migrations add InitialAuthModuleCreate \
    --project src/Modules/AuthModule/Infrastructure \
    --startup-project src/API \
    --context AuthDbContext \
    --output-dir Data/Migrations

# Review the generated migration files
ls src/Modules/AuthModule/Infrastructure/Data/Migrations/
```

#### Apply Database Migrations
```bash
# Update database with migrations
dotnet ef database update \
    --project src/Modules/AuthModule/Infrastructure \
    --startup-project src/API \
    --context AuthDbContext

# Verify database creation
dotnet ef database list \
    --project src/Modules/AuthModule/Infrastructure \
    --startup-project src/API
```

#### Advanced Migration Commands
```bash
# Create a specific migration
dotnet ef migrations add AddUserProfileFields \
    --project src/Modules/AuthModule/Infrastructure \
    --startup-project src/API \
    --context AuthDbContext

# Remove last migration (if not applied)
dotnet ef migrations remove \
    --project src/Modules/AuthModule/Infrastructure \
    --startup-project src/API \
    --context AuthDbContext

# Generate SQL script for migrations
dotnet ef migrations script \
    --project src/Modules/AuthModule/Infrastructure \
    --startup-project src/API \
    --context AuthDbContext \
    --output auth-migration.sql

# Update to specific migration
dotnet ef database update 20231201000000_InitialAuthModuleCreate \
    --project src/Modules/AuthModule/Infrastructure \
    --startup-project src/API \
    --context AuthDbContext

# Drop database (development only)
dotnet ef database drop \
    --project src/Modules/AuthModule/Infrastructure \
    --startup-project src/API \
    --context AuthDbContext
```

### 🔧 Development Setup
**Quick development environment setup**

#### 1. Clone and Setup
```bash
# Clone the repository
git clone <your-repo-url>
cd coreAxis

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build
```

#### 2. Configure Development Settings
```bash
# Copy development settings template
cp src/API/appsettings.Development.json.template src/API/appsettings.Development.json

# Edit configuration
nano src/API/appsettings.Development.json
```

#### 3. Initialize Database
```bash
# Run migrations
dotnet ef database update --project src/Modules/AuthModule/Infrastructure --startup-project src/API

# Seed development data
dotnet run --project src/API -- --seed-data
```

#### 4. Run the Application
```bash
# Start the API
dotnet run --project src/API

# Or with hot reload
dotnet watch run --project src/API
```

### 🧪 Testing Setup
**Comprehensive testing configuration**

#### Unit Tests
```bash
# Run all AuthModule unit tests
dotnet test tests/Modules/AuthModule/AuthModule.UnitTests/

# Run with coverage
dotnet test tests/Modules/AuthModule/AuthModule.UnitTests/ \
    --collect:"XPlat Code Coverage" \
    --results-directory ./coverage

# Generate coverage report
reportgenerator \
    -reports:"coverage/**/coverage.cobertura.xml" \
    -targetdir:"coverage/report" \
    -reporttypes:Html
```

#### Integration Tests
```bash
# Run integration tests
dotnet test tests/Modules/AuthModule/AuthModule.IntegrationTests/

# Run with specific test category
dotnet test tests/Modules/AuthModule/AuthModule.IntegrationTests/ \
    --filter Category=Authentication
```

#### API Testing with Swagger
```bash
# Start the application
dotnet run --project src/API

# Open Swagger UI
open https://localhost:5001/swagger

# Test authentication flow:
# 1. POST /api/auth/register - Create test user
# 2. POST /api/auth/login - Get JWT token
# 3. Click "Authorize" in Swagger UI
# 4. Enter: Bearer <your-jwt-token>
# 5. Test protected endpoints
```

### 🔌 Client Integration Examples
**How to integrate with different client applications**

#### JavaScript/TypeScript Client
```typescript
// auth-service.ts
export class AuthService {
    private baseUrl = 'https://localhost:5001/api';
    private token: string | null = null;

    async login(username: string, password: string): Promise<boolean> {
        try {
            const response = await fetch(`${this.baseUrl}/auth/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ username, password }),
            });

            if (response.ok) {
                const data = await response.json();
                this.token = data.accessToken;
                localStorage.setItem('auth_token', this.token);
                return true;
            }
            return false;
        } catch (error) {
            console.error('Login failed:', error);
            return false;
        }
    }

    async register(userData: RegisterRequest): Promise<boolean> {
        try {
            const response = await fetch(`${this.baseUrl}/auth/register`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(userData),
            });
            return response.ok;
        } catch (error) {
            console.error('Registration failed:', error);
            return false;
        }
    }

    async makeAuthenticatedRequest(url: string, options: RequestInit = {}): Promise<Response> {
        const token = this.token || localStorage.getItem('auth_token');
        
        return fetch(url, {
            ...options,
            headers: {
                ...options.headers,
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json',
            },
        });
    }

    logout(): void {
        this.token = null;
        localStorage.removeItem('auth_token');
    }
}

// Usage example
const authService = new AuthService();

// Login
const loginSuccess = await authService.login('john.doe', 'password123');
if (loginSuccess) {
    console.log('Login successful');
}

// Make authenticated request
const response = await authService.makeAuthenticatedRequest('/api/users/profile');
const userProfile = await response.json();
```

#### C# Client
```csharp
// AuthApiClient.cs
public class AuthApiClient
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var loginRequest = new { Username = username, Password = password };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            _accessToken = result?.AccessToken;
            
            // Set default authorization header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            
            return true;
        }
        
        return false;
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var response = await _httpClient.GetAsync("/api/users/current");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserDto>();
        }
        
        return null;
    }

    public async Task<List<RoleDto>> GetUserRolesAsync(int userId)
    {
        var response = await _httpClient.GetAsync($"/api/users/{userId}/roles");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<RoleDto>>() ?? new List<RoleDto>();
        }
        
        return new List<RoleDto>();
    }

    public void Logout()
    {
        _accessToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
}

// Dependency injection setup
services.AddHttpClient<AuthApiClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:5001");
});
```

### 🔒 Security Best Practices
**Implementation guidelines for secure usage**

#### JWT Token Handling
```csharp
// Secure token storage and validation
public class SecureTokenHandler
{
    public void StoreToken(string token)
    {
        // Store in secure HTTP-only cookie (recommended)
        Response.Cookies.Append("auth_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });
        
        // Avoid storing in localStorage for sensitive applications
        // localStorage is vulnerable to XSS attacks
    }
    
    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters();
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return validatedToken != null;
        }
        catch
        {
            return false;
        }
    }
}
```

#### Permission-Based Authorization
```csharp
// Custom authorization attribute
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute, IAuthorizationRequirement
{
    public string Page { get; }
    public string Action { get; }
    
    public RequirePermissionAttribute(string page, string action)
    {
        Page = page;
        Action = action;
    }
}

// Usage in controllers
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [RequirePermission("Products", "Read")]
    public async Task<IActionResult> GetProducts()
    {
        // Implementation
    }
    
    [HttpPost]
    [RequirePermission("Products", "Create")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        // Implementation
    }
    
    [HttpDelete("{id}")]
    [RequirePermission("Products", "Delete")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        // Implementation
    }
}
```

### 📊 Monitoring & Observability
**Production monitoring setup**

#### Health Checks
```csharp
// Add to Program.cs
builder.Services.AddHealthChecks()
    .AddDbContext<AuthDbContext>()
    .AddCheck<JwtTokenHealthCheck>("jwt-token")
    .AddCheck<AuthServiceHealthCheck>("auth-service");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

#### Metrics Collection
```csharp
// Custom metrics for authentication events
public class AuthMetrics
{
    private static readonly Counter LoginAttempts = Metrics
        .CreateCounter("auth_login_attempts_total", "Total login attempts", "result");
    
    private static readonly Histogram LoginDuration = Metrics
        .CreateHistogram("auth_login_duration_seconds", "Login duration");
    
    public static void RecordLoginAttempt(bool success)
    {
        LoginAttempts.WithLabels(success ? "success" : "failure").Inc();
    }
    
    public static void RecordLoginDuration(double seconds)
    {
        LoginDuration.Observe(seconds);
    }
}
```

## 📊 Data Models & Entities

### 🏗️ Core Entities

#### 👤 User Entity
**Primary user account entity with comprehensive security features**

**Properties:**
- **Id**: Unique identifier (GUID)
- **Username**: Unique username (required, indexed)
- **Email**: User email address (required, indexed)
- **FirstName**: User's first name (optional)
- **LastName**: User's last name (optional)
- **PhoneNumber**: Contact phone number (optional)
- **PasswordHash**: Securely hashed password using PBKDF2
- **IsActive**: Account activation status (default: true)
- **EmailConfirmed**: Email verification status (default: false)
- **CreatedAt**: Account creation timestamp (UTC)
- **LastLoginAt**: Last successful login timestamp (nullable)
- **LastLoginIp**: IP address of last successful login (nullable)
- **FailedLoginAttempts**: Counter for consecutive failed logins (default: 0)
- **LockedUntil**: Account lockout expiration time (nullable)

**Navigation Properties:**
- **UserRoles**: Collection of assigned roles (many-to-many)
- **UserPermissions**: Collection of direct permissions (many-to-many)
- **AccessLogs**: Collection of audit log entries (one-to-many)

**Key Methods:**
- `UpdatePassword(newPasswordHash)`: Securely update user password
- `UpdateProfile(firstName, lastName, email, phoneNumber)`: Update profile information
- `RecordSuccessfulLogin(ipAddress)`: Log successful authentication
- `RecordFailedLogin()`: Increment failed login counter
- `Activate()` / `Deactivate()`: Control account status
- `UnlockAccount()`: Remove account lockout

#### 🛡️ Role Entity
**Role definition for RBAC system**

**Properties:**
- **Id**: Unique identifier (GUID)
- **Name**: Role name (required, unique)
- **Description**: Detailed role description (optional)
- **IsActive**: Role activation status (default: true)
- **IsSystemRole**: Indicates protected system role (default: false)
- **CreatedAt**: Role creation timestamp (UTC)

**Navigation Properties:**
- **UserRoles**: Collection of users with this role (many-to-many)
- **RolePermissions**: Collection of assigned permissions (many-to-many)

**Key Methods:**
- `UpdateDetails(name, description)`: Update role information
- `Activate()` / `Deactivate()`: Control role status (system roles protected)

**Business Rules:**
- System roles cannot be deactivated or deleted
- Role names must be unique across the system
- Deactivating a role affects all users with that role

#### 🔑 Permission Entity
**Granular permission definition based on Page + Action combinations**

**Properties:**
- **Id**: Unique identifier (GUID)
- **Name**: Auto-generated permission name (Page.Code + Action.Code)
- **Description**: Human-readable permission description
- **PageId**: Reference to associated page (required)
- **ActionId**: Reference to associated action (required)
- **IsActive**: Permission activation status (default: true)
- **CreatedAt**: Permission creation timestamp (UTC)

**Navigation Properties:**
- **Page**: Associated page entity (many-to-one)
- **Action**: Associated action entity (many-to-one)
- **RolePermissions**: Roles with this permission (many-to-many)
- **UserPermissions**: Users with direct permission (many-to-many)

**Key Methods:**
- `SetName()`: Auto-generate name from Page + Action codes
- `UpdateDescription(description)`: Update permission description
- `Activate()` / `Deactivate()`: Control permission status

**Business Rules:**
- Permission names are automatically generated and unique
- Page + Action combination must be unique
- Deactivating affects all roles and users with this permission

#### 📄 Page Entity
**Represents application pages/modules for permission system**

**Properties:**
- **Id**: Unique identifier (GUID)
- **Name**: Human-readable page name (required)
- **Code**: Unique page code for permission generation (required)
- **Description**: Detailed page description (optional)
- **IsActive**: Page activation status (default: true)
- **CreatedAt**: Page creation timestamp (UTC)

**Navigation Properties:**
- **Permissions**: Collection of permissions for this page (one-to-many)

**Business Rules:**
- Page codes must be unique and follow naming conventions
- Used in permission name generation (Page.Code + Action.Code)
- Deactivating affects all related permissions

#### ⚡ Action Entity
**Represents specific actions that can be performed on pages**

**Properties:**
- **Id**: Unique identifier (GUID)
- **Name**: Human-readable action name (required)
- **Code**: Unique action code for permission generation (required)
- **Description**: Detailed action description (optional)
- **IsActive**: Action activation status (default: true)
- **CreatedAt**: Action creation timestamp (UTC)

**Navigation Properties:**
- **Permissions**: Collection of permissions using this action (one-to-many)

**Business Rules:**
- Action codes must be unique (e.g., "CREATE", "READ", "UPDATE", "DELETE")
- Used in permission name generation (Page.Code + Action.Code)
- Deactivating affects all related permissions

#### 📋 AccessLog Entity
**Comprehensive audit logging for security and compliance**

**Properties:**
- **Id**: Unique identifier (GUID)
- **UserId**: User identifier (nullable for failed attempts)
- **Username**: Username for tracking (nullable)
- **Action**: Performed action (e.g., "LOGIN", "LOGOUT", "CREATE_USER")
- **Resource**: Accessed resource or endpoint (nullable)
- **IpAddress**: Client IP address (required)
- **UserAgent**: Client browser/device information (nullable)
- **IsSuccess**: Operation success status (required)
- **ErrorMessage**: Detailed error information for failures (nullable)
- **Timestamp**: Event timestamp in UTC (auto-generated)
- **AdditionalData**: Custom JSON metadata (nullable)

**Navigation Properties:**
- **User**: Associated user entity (many-to-one, nullable)

**Static Factory Methods:**
- `CreateLoginAttempt(username, ipAddress, isSuccess, ...)`: Create login log entry
- `CreateResourceAccess(userId, action, resource, ...)`: Create resource access log
- `CreateSecurityEvent(action, ipAddress, ...)`: Create security-related log

**Business Rules:**
- All authentication attempts are logged (success and failure)
- Failed attempts include error details for security analysis
- Successful operations include user context when available
- Timestamps are always stored in UTC for consistency

### 🔗 Relationship Entities

#### UserRole (Many-to-Many)
**Links users to their assigned roles**
- **UserId**: User identifier
- **RoleId**: Role identifier
- **AssignedAt**: Assignment timestamp
- **AssignedBy**: User who made the assignment (optional)

#### RolePermission (Many-to-Many)
**Links roles to their granted permissions**
- **RoleId**: Role identifier
- **PermissionId**: Permission identifier
- **GrantedAt**: Grant timestamp
- **GrantedBy**: User who granted permission (optional)

#### UserPermission (Many-to-Many)
**Direct permission assignments to users (bypassing roles)**
- **UserId**: User identifier
- **PermissionId**: Permission identifier
- **GrantedAt**: Grant timestamp
- **GrantedBy**: User who granted permission (optional)
- **ExpiresAt**: Permission expiration (optional)

### 📋 Data Transfer Objects (DTOs)

#### UserDto
**User information for API responses**
- Complete user profile with roles
- Excludes sensitive information (password hash)
- Includes account status and login history

#### CreateUserDto / UpdateUserDto
**User creation and modification requests**
- Validation attributes for data integrity
- Separate DTOs for different operations

#### RoleDto / PermissionDto
**Role and permission information**
- Includes related entity details
- Hierarchical permission structure

#### AccessLogDto
**Audit log information for reporting**
- Formatted timestamps and user-friendly action names
- Filtered sensitive information

## Security Features

- **Password Hashing**: Uses PBKDF2 with SHA-256 and 10,000 iterations
- **JWT Tokens**: Secure token-based authentication with configurable expiration
- **Account Lockout**: Tracks failed login attempts for security monitoring
- **Audit Logging**: Comprehensive access logging for compliance

## 🛠️ Troubleshooting & FAQ

### Database Issues

#### Migration Errors
```bash
# Error: "No migrations configuration type was found"
# Solution: Ensure you're in the correct directory and specify the context
dotnet ef migrations add InitialCreate \
    --project src/Modules/AuthModule/Infrastructure \
    --startup-project src/API \
    --context AuthDbContext

# Error: "Unable to create an object of type 'AuthDbContext'"
# Solution: Ensure connection string is configured in appsettings.json
```

#### Connection String Issues
```json
// SQL Server (LocalDB)
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CoreAxisAuth;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}

// PostgreSQL
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=CoreAxisAuth;Username=postgres;Password=yourpassword"
  }
}

// MySQL
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CoreAxisAuth;Uid=root;Pwd=yourpassword;"
  }
}
```

### Authentication Issues

#### JWT Token Problems
```csharp
// Issue: "The token is not yet valid"
// Solution: Check server time synchronization
services.Configure<JwtSettings>(options =>
{
    options.ClockSkew = TimeSpan.FromMinutes(5);
});

// Issue: "The signature is invalid"
// Solution: Ensure SecretKey is consistent across instances

// Issue: "The token is expired"
// Solution: Implement token refresh mechanism
```

#### Permission Denied Errors
```csharp
// Debug permission issues

// 1. Check user roles
var user = await _userService.GetUserByIdAsync(userId);
var roles = user.UserRoles.Select(ur => ur.Role.Name);

// 2. Check user permissions
var permissions = await _permissionService.GetUserPermissionsAsync(userId);

// 3. Verify permission exists
var permission = await _permissionService.GetPermissionByActionAsync("Users", "Read");
```

### Performance Issues

#### Slow Authentication
```csharp
// Optimize password hashing for development
services.Configure<PasswordHashingOptions>(options =>
{
    options.IterationCount = 10000; // Reduce for development
});

// Add caching for permissions
services.AddMemoryCache();
```

#### Database Performance
```sql
-- Add indexes for better performance
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_AccessLogs_UserId_Timestamp ON AccessLogs(UserId, Timestamp);
```

### Common Configuration Mistakes

#### Missing Dependencies
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="6.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="6.0.0" />
<PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.0.0" />
```

#### Incorrect Middleware Order
```csharp
// CORRECT order
app.UseAuthentication();  // ✅ Authentication first
app.UseAuthorization();   // ✅ Authorization second
```

### Development Tips

#### Enable Detailed Logging
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "CoreAxis.Modules.AuthModule": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

#### Test Data Seeding
```csharp
// Create admin user for development
var adminUser = await _mediator.Send(new CreateUserCommand
{
    Username = "admin",
    Email = "admin@example.com",
    Password = "Admin123!"
});

// Create admin role
var adminRole = await _mediator.Send(new CreateRoleCommand
{
    Name = "Administrator",
    Description = "System administrator"
});

// Assign role
await _mediator.Send(new AssignRoleToUserCommand
{
    UserId = adminUser.Id,
    RoleId = adminRole.Id
});
```

### FAQ

**Q: How do I reset a user's password?**
```csharp
A: Use the ChangePasswordCommand:

var result = await _mediator.Send(new ChangePasswordCommand
{
    UserId = userId,
    CurrentPassword = "OldPassword123!",
    NewPassword = "NewPassword123!"
});
```

**Q: How do I add custom claims to JWT tokens?**
```csharp
A: Extend the JwtTokenService to include custom claims:

var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new(ClaimTypes.Name, user.Username),
    new("department", user.Department ?? ""),
    new("employee_id", user.EmployeeId ?? "")
};
```

**Q: How do I implement custom password policies?**
```csharp
A: Create a custom password validator:

public class CustomPasswordValidator : IPasswordValidator
{
    public ValidationResult Validate(string password)
    {
        var errors = new List<string>();
        
        if (password.Length < 12)
            errors.Add("Password must be at least 12 characters");
            
        if (!password.Any(char.IsUpper))
            errors.Add("Must contain uppercase letter");
            
        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
}
```

**Q: How do I implement role hierarchy?**
```csharp
A: Extend the Role entity with parent-child relationships:

public class Role : BaseEntity
{
    public int? ParentRoleId { get; private set; }
    public Role? ParentRole { get; private set; }
    public ICollection<Role> ChildRoles { get; private set; }
    
    public bool IsChildOf(Role parentRole)
    {
        var current = this.ParentRole;
        while (current != null)
        {
            if (current.Id == parentRole.Id) return true;
            current = current.ParentRole;
        }
        return false;
    }
}
```

## Dependencies

- CoreAxis.SharedKernel
- CoreAxis.BuildingBlocks
- CoreAxis.EventBus
- Entity Framework Core
- MediatR
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.IdentityModel.Tokens

## Future Enhancements

- Two-factor authentication (2FA)
- OAuth2/OpenID Connect integration
- Password policy enforcement
- Session management
- Advanced audit reporting
- Permission caching for performance