# Environment Variables Setup

این پروژه از متغیرهای محیطی (Environment Variables) برای تنظیمات امنیتی استفاده می‌کند.

## تنظیم فایل .env

فایل `.env` در root directory پروژه قرار دارد و شامل متغیرهای محیطی مورد نیاز است:

```bash
# Shahkar Service Configuration
SHAHKAR_TOKEN=your_actual_shahkar_token_here

# Civil Registry Service Configuration
CIVIL_REGISTRY_TOKEN=your_actual_civil_registry_token_here

# Magfa SMS Service Configuration
MAGFA_USERNAME=your_magfa_username_here
MAGFA_PASSWORD=your_magfa_password_here
MAGFA_DOMAIN=your_magfa_domain_here
MAGFA_FROM=your_magfa_sender_number_here

# Other environment variables
# DATABASE_CONNECTION_STRING=
# JWT_SECRET_KEY=
# REDIS_CONNECTION_STRING=
```

## نحوه استفاده

### 1. تنظیم توکن‌های سرویس‌ها

توکن‌های Shahkar و Civil Registry در فایل `.env` تنظیم شده و از طریق configuration خوانده می‌شوند:

**ShahkarService:**
```csharp
_token = _configuration["Shahkar:Token"] ?? throw new InvalidOperationException("Shahkar token is not configured");
```

**CivilRegistryService:**
```csharp
_token = _configuration["CivilRegistry:Token"] ?? throw new InvalidOperationException("Civil Registry token is not configured");
```

**MagfaSmsService:**
```csharp
_username = _configuration["Magfa:Username"] ?? throw new InvalidOperationException("Magfa username is not configured");
_password = _configuration["Magfa:Password"] ?? throw new InvalidOperationException("Magfa password is not configured");
_domain = _configuration["Magfa:Domain"] ?? throw new InvalidOperationException("Magfa domain is not configured");
_from = _configuration["Magfa:From"] ?? throw new InvalidOperationException("Magfa sender number is not configured");
```

### 2. بارگذاری متغیرهای محیطی

در `Program.cs` فایل `.env` بارگذاری می‌شود:

```csharp
// Load environment variables from .env file if it exists
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../.env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();
```

### 3. تنظیمات appsettings.json

در فایل `appsettings.json` مرجع به environment variable تعریف شده:

```json
{
  "Shahkar": {
    "Token": "${SHAHKAR_TOKEN}",
    "BaseUrl": "https://api.shahkar.ir"
  },
  "CivilRegistry": {
    "Token": "${CIVIL_REGISTRY_TOKEN}",
    "BaseUrl": "https://api.civilregistry.ir"
  },
  "Magfa": {
    "Username": "${MAGFA_USERNAME}",
    "Password": "${MAGFA_PASSWORD}",
    "Domain": "${MAGFA_DOMAIN}",
    "From": "${MAGFA_FROM}",
    "BaseUrl": "https://sms.magfa.com/api/http/sms/v2"
  }
}
```

## امنیت

- فایل `.env` در `.gitignore` قرار دارد تا از commit شدن جلوگیری شود
- هرگز توکن‌های واقعی را در کد یا فایل‌های configuration commit نکنید
- برای production از Azure Key Vault یا سایر سرویس‌های مدیریت secrets استفاده کنید

## تست

برای تست کردن تنظیمات:

```bash
# بارگذاری متغیرهای محیطی
source .env

# نمایش توکن
echo $SHAHKAR_TOKEN
```