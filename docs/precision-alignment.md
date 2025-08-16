# Precision Alignment بین Wallet و ProductOrder Modules

## خلاصه

این مستند تحلیل و مقایسه دقت اعشاری (decimal precision) بین ماژول‌های Wallet و ProductOrder را ارائه می‌دهد.

## تحلیل Precision در هر ماژول

### WalletModule

**نوع داده:** `decimal` مستقیم
**Database Precision:** `decimal(18,6)` - 18 رقم کل، 6 رقم اعشار

```csharp
// Entity: Wallet.cs
public decimal Balance { get; private set; }

// Configuration: WalletConfiguration.cs
builder.Property(w => w.Balance)
    .HasPrecision(18, 6)
    .HasColumnType("decimal(18,6)");
```

**مثال:** `123456789012.123456` (حداکثر 12 رقم صحیح + 6 رقم اعشار)

### ProductOrderModule

**نوع داده:** `Money` Value Object
**Database Precision:** `decimal(18,8)` - 18 رقم کل، 8 رقم اعشار

```csharp
// Value Object: Money.cs
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
}

// Configuration: OrderConfiguration.cs
builder.OwnsOne(o => o.LockedPrice, lp =>
{
    lp.Property(x => x.Amount)
        .HasColumnName("LockedPriceAmount")
        .HasPrecision(18, 8);
});
```

**مثال:** `1234567890.12345678` (حداکثر 10 رقم صحیح + 8 رقم اعشار)

## مقایسه و تحلیل

| جنبه | WalletModule | ProductOrderModule |
|------|-------------|-------------------|
| **نوع داده** | `decimal` | `Money` Value Object |
| **Database Precision** | `decimal(18,6)` | `decimal(18,8)` |
| **رقم اعشار** | 6 | 8 |
| **رقم صحیح** | 12 | 10 |
| **حداکثر مقدار** | 999,999,999,999.999999 | 9,999,999,999.99999999 |

## مزایا و معایب

### WalletModule (18,6)
**مزایا:**
- رقم‌های صحیح بیشتر (12 رقم) برای مبالغ بزرگ
- مناسب برای موجودی‌های کلان

**معایب:**
- دقت کمتر در اعشار (6 رقم)
- ممکن است برای ارزهای دیجیتال کافی نباشد

### ProductOrderModule (18,8)
**مزایا:**
- دقت بالاتر در اعشار (8 رقم)
- مناسب برای قیمت‌گذاری دقیق ارزهای دیجیتال
- پشتیبانی از محاسبات پیچیده

**معایب:**
- رقم‌های صحیح کمتر (10 رقم)
- محدودیت در مبالغ بسیار بزرگ

## توصیه‌ها

### 1. سازگاری در تبادل داده

هنگام تبادل داده بین دو ماژول، باید دقت کرد:

```csharp
// تبدیل از ProductOrder به Wallet
public static decimal ToWalletAmount(Money money)
{
    // Round to 6 decimal places to match Wallet precision
    return Math.Round(money.Amount, 6, MidpointRounding.AwayFromZero);
}

// تبدیل از Wallet به ProductOrder
public static Money ToProductOrderMoney(decimal amount, string currency)
{
    // Wallet amount already has 6 decimals, safe to convert
    return Money.Create(amount, currency);
}
```

### 2. Validation در API Boundaries

```csharp
// در ProductOrder API
public class PlaceOrderRequest
{
    [Range(0.000001, 9999999999.99999999)]
    public decimal Amount { get; set; }
}

// در Wallet API
public class UpdateBalanceRequest
{
    [Range(0.000001, 999999999999.999999)]
    public decimal Amount { get; set; }
}
```

### 3. تست‌های Precision

```csharp
[Fact]
public void PrecisionAlignment_WalletToProductOrder_ShouldMaintainAccuracy()
{
    // Test conversion from Wallet (18,6) to ProductOrder (18,8)
    var walletAmount = 123.123456m; // 6 decimals
    var money = Money.Create(walletAmount, "USD");
    
    Assert.Equal(walletAmount, money.Amount);
}

[Fact]
public void PrecisionAlignment_ProductOrderToWallet_ShouldRoundCorrectly()
{
    // Test conversion from ProductOrder (18,8) to Wallet (18,6)
    var productOrderMoney = Money.Create(123.12345678m, "USD"); // 8 decimals
    var walletAmount = Math.Round(productOrderMoney.Amount, 6);
    
    Assert.Equal(123.123457m, walletAmount); // Rounded to 6 decimals
}
```

## نتیجه‌گیری

1. **هر دو ماژول از `decimal(18,x)` استفاده می‌کنند** که سازگاری خوبی دارد
2. **تفاوت در تعداد ارقام اعشار** نیاز به مدیریت دقیق در تبادل داده دارد
3. **ProductOrderModule دقت بالاتری** برای محاسبات قیمت دارد
4. **WalletModule ظرفیت بیشتری** برای مبالغ بزرگ دارد
5. **تست‌های مناسب** برای اطمینان از صحت تبدیلات ضروری است

## اقدامات آینده

- [ ] ایجاد Converter Classes برای تبادل امن داده
- [ ] افزودن Validation Rules در API Boundaries
- [ ] ایجاد Integration Tests برای سناریوهای مختلف
- [ ] بررسی نیاز به یکسان‌سازی precision در نسخه‌های آینده