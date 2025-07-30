# Wallet Module

The Wallet Module provides comprehensive wallet and transaction management functionality for the CoreAxis application. It follows Clean Architecture principles with clear separation of concerns across Domain, Application, Infrastructure, and API layers.

## Features

- **Multi-Wallet Support**: Users can have multiple wallets of different types
- **Transaction Management**: Support for deposits, withdrawals, and transfers
- **Wallet Providers**: Integration with external wallet providers
- **Wallet Contracts**: Configurable limits and terms for wallet usage
- **Multi-Currency Support**: Handle different currencies per wallet
- **Transaction Types**: Flexible transaction categorization
- **Audit Trail**: Complete transaction history and tracking
- **Domain Events**: Event-driven architecture for wallet operations

## Architecture

### Domain Layer
- **Entities**: Core business entities (Wallet, Transaction, WalletType, etc.)
- **Events**: Domain events for wallet and transaction operations
- **Repositories**: Repository interfaces for data access

### Application Layer
- **Commands**: CQRS commands for write operations
- **Queries**: CQRS queries for read operations
- **Handlers**: Command and query handlers
- **DTOs**: Data transfer objects for API communication

### Infrastructure Layer
- **DbContext**: Entity Framework database context
- **Configurations**: Entity configurations for database mapping
- **Repositories**: Repository implementations

### API Layer
- **Controllers**: REST API controllers
- **Dependency Injection**: Service registration

## Key Entities

### Wallet
- Represents a user's wallet with balance and currency
- Linked to a specific wallet type
- Can be locked/unlocked for security
- Supports credit/debit operations

### Transaction
- Records all wallet operations (deposits, withdrawals, transfers)
- Maintains transaction status and audit information
- Links related transactions (e.g., transfer operations)

### WalletType
- Defines different types of wallets (e.g., Savings, Current, Crypto)

### WalletProvider
- External wallet service providers
- Configurable API endpoints and capabilities

### WalletContract
- Defines usage limits and terms between users and providers
- Daily/monthly limits with automatic reset functionality

## API Endpoints

### Wallet Management
- `POST /api/wallet` - Create new wallet
- `GET /api/wallet/{id}` - Get wallet details
- `GET /api/wallet/user/{userId}` - Get user wallets
- `GET /api/wallet/{id}/balance` - Get wallet balance

### Transaction Operations
- `POST /api/wallet/{id}/deposit` - Deposit to wallet
- `POST /api/wallet/{id}/withdraw` - Withdraw from wallet
- `POST /api/wallet/{id}/transfer` - Transfer between wallets
- `GET /api/wallet/{id}/transactions` - Get wallet transactions

### Transaction History
- `GET /api/transaction/{id}` - Get transaction details
- `GET /api/transaction` - Get transactions with filters
- `GET /api/transaction/user/{userId}` - Get user transactions

## Domain Events

- **WalletCreatedEvent**: Fired when a new wallet is created
- **WalletBalanceChangedEvent**: Fired when wallet balance changes
- **TransactionCreatedEvent**: Fired when a transaction is created
- **TransactionCompletedEvent**: Fired when a transaction completes
- **TransactionFailedEvent**: Fired when a transaction fails

## Usage

### Registration
```csharp
// In Program.cs or Startup.cs
services.AddWalletModuleApi(configuration);
```

### Creating a Wallet
```csharp
var createWalletDto = new CreateWalletDto
{
    UserId = userId,
    WalletTypeId = walletTypeId,
    Currency = "USD",
};

var wallet = await mediator.Send(new CreateWalletCommand
{
    UserId = createWalletDto.UserId,
    WalletTypeId = createWalletDto.WalletTypeId,
    Currency = createWalletDto.Currency,
});
```

### Making a Deposit
```csharp
var result = await mediator.Send(new DepositCommand
{
    WalletId = walletId,
    Amount = 100.00m,
    Description = "Initial deposit",
    Reference = "DEP001"
});
```

## Database Schema

The module uses the "wallet" schema with the following tables:
- `Wallets` - User wallets
- `WalletTypes` - Wallet type definitions
- `Transactions` - Transaction records
- `TransactionTypes` - Transaction type definitions
- `WalletProviders` - External provider configurations
- `WalletContracts` - User-provider agreements

## Security Considerations

- All API endpoints require authorization
- Wallet operations include balance validation
- Transaction limits enforced through wallet contracts
- Audit trail maintained for all operations
- Multi-tenancy support for data isolation

## Error Handling

- Insufficient balance validation
- Wallet lock status checking
- Transaction type validation
- Provider availability verification
- Comprehensive error messages and logging