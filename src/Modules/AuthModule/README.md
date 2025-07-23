# AuthModule

The AuthModule provides comprehensive authentication and authorization functionality for the CoreAxis application. It implements a role-based access control (RBAC) system with multi-tenancy support.

## Features

- **User Management**: User registration, authentication, and profile management
- **Role-Based Access Control**: Hierarchical role and permission system
- **Multi-Tenancy**: Tenant-isolated authentication and authorization
- **JWT Authentication**: Secure token-based authentication
- **Access Logging**: Comprehensive audit trail for security monitoring
- **Password Security**: Secure password hashing using PBKDF2

## Architecture

The module follows Clean Architecture principles with the following layers:

### Domain Layer
- **Entities**: Core business entities (User, Role, Permission, Page, Action, AccessLog)
- **Events**: Domain events for user registration and authentication
- **Repositories**: Repository interfaces for data access
- **Value Objects**: Immutable objects representing domain concepts

### Application Layer
- **Commands**: CQRS command handlers for write operations
- **Queries**: CQRS query handlers for read operations
- **DTOs**: Data transfer objects for API communication
- **Services**: Application service interfaces

### Infrastructure Layer
- **Data**: Entity Framework DbContext and configurations
- **Repositories**: Repository implementations
- **Services**: Service implementations (JWT, Password Hashing)

### API Layer
- **Controllers**: REST API endpoints
- **Authentication**: JWT authentication configuration

## Key Entities

### User
- Represents system users with authentication credentials
- Supports multi-tenancy and role assignments
- Tracks login attempts and account status

### Role
- Defines user roles with associated permissions
- Supports both tenant-specific and system-wide roles
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

## API Endpoints

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User authentication
- `POST /api/auth/change-password` - Password change

### User Management
- `GET /api/users/{id}` - Get user by ID
- `GET /api/users` - Get users by tenant (paginated)
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user
- `POST /api/users/{id}/roles` - Assign role to user
- `DELETE /api/users/{id}/roles/{roleId}` - Remove role from user

### Role Management
- `POST /api/roles` - Create role
- `GET /api/roles/{id}` - Get role by ID
- `GET /api/roles` - Get roles by tenant
- `PUT /api/roles/{id}` - Update role
- `DELETE /api/roles/{id}` - Delete role
- `POST /api/roles/{id}/permissions/{permissionId}` - Add permission to role
- `DELETE /api/roles/{id}/permissions/{permissionId}` - Remove permission from role

### Permission Management
- `GET /api/permissions` - Get all permissions
- `GET /api/permissions/{id}` - Get permission by ID
- `POST /api/permissions` - Create permission
- `PUT /api/permissions/{id}` - Update permission
- `DELETE /api/permissions/{id}` - Delete permission
- `GET /api/permissions/by-page/{pageId}` - Get permissions by page
- `GET /api/permissions/by-action/{actionId}` - Get permissions by action

## Configuration

### JWT Settings
Add the following configuration to your `appsettings.json`:

```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "CoreAxis",
    "Audience": "CoreAxis-Users",
    "ExpirationMinutes": 60
  }
}
```

### Database Connection
Ensure your connection string is configured:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string-here"
  }
}
```

## Usage

### Registration in Startup/Program.cs

```csharp
services.AddAuthModuleApi(configuration);
```

### Database Migration

The module uses Entity Framework Core. Run migrations to create the database schema:

```bash
dotnet ef migrations add InitialCreate --project CoreAxis.Modules.AuthModule.Infrastructure
dotnet ef database update --project CoreAxis.Modules.AuthModule.Infrastructure
```

## Security Features

- **Password Hashing**: Uses PBKDF2 with SHA-256 and 10,000 iterations
- **JWT Tokens**: Secure token-based authentication with configurable expiration
- **Account Lockout**: Tracks failed login attempts for security monitoring
- **Multi-Tenancy**: Complete tenant isolation for data security
- **Audit Logging**: Comprehensive access logging for compliance

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