using ISII.Web.Domain.CouponAggregate;

namespace ISII.Tests.CouponAggregate;

public class CouponTest
{
    [Fact]
    public void Constructor_ShouldAssignProvidedValuesAndInitializeDefaults()
    {
        var id = CouponId.New;
        const string code = "summer2026";
        const decimal discountPercentage = 20.00m;
        const decimal minimumAmount = 50.00m;
        var expirationDate = DateTimeOffset.UtcNow.AddMonths(1);
        const int maxUsages = 100;

        var coupon = new Coupon(id, code, discountPercentage, minimumAmount, expirationDate, maxUsages);

        Assert.Equal(id, coupon.Id);
        Assert.Equal("SUMMER2026", coupon.Code);
        Assert.Equal(discountPercentage, coupon.DiscountPercentage);
        Assert.Equal(minimumAmount, coupon.MinimumAmount);
        Assert.Equal(expirationDate, coupon.ExpirationDate);
        Assert.Equal(maxUsages, coupon.MaxUsages);
        Assert.Equal(0, coupon.TimesUsed);
        Assert.True(coupon.IsActive);
    }

    [Fact]
    public void Create_ShouldReturnCouponWithProvidedValues()
    {
        var id = CouponId.From(10);
        var expirationDate = DateTimeOffset.UtcNow.AddDays(15);

        var coupon = Coupon.Create(id, "PROMO15", 15m, 30m, expirationDate, 50);

        Assert.NotNull(coupon);
        Assert.Equal(id, coupon.Id);
        Assert.Equal("PROMO15", coupon.Code);
        Assert.Equal(15m, coupon.DiscountPercentage);
        Assert.Equal(30m, coupon.MinimumAmount);
        Assert.Equal(expirationDate, coupon.ExpirationDate);
        Assert.Equal(50, coupon.MaxUsages);
        Assert.Equal(0, coupon.TimesUsed);
        Assert.True(coupon.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ShouldThrow_WhenCodeIsEmptyOrWhitespace(string? invalidCode)
    {
        var expirationDate = DateTimeOffset.UtcNow.AddDays(10);

        Assert.ThrowsAny<ArgumentException>(() =>
            new Coupon(CouponId.New, invalidCode!, 10m, 20m, expirationDate, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.01)]
    public void Constructor_ShouldThrow_WhenDiscountPercentageIsOutOfRange(decimal invalidDiscount)
    {
        var expirationDate = DateTimeOffset.UtcNow.AddDays(10);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Coupon(CouponId.New, "DISCOUNT", invalidDiscount, 20m, expirationDate, 10));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenMinimumAmountIsNegative()
    {
        var expirationDate = DateTimeOffset.UtcNow.AddDays(10);

        Assert.Throws<ArgumentException>(() =>
            new Coupon(CouponId.New, "DISCOUNT", 10m, -1m, expirationDate, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_ShouldThrow_WhenMaxUsagesIsZeroOrNegative(int invalidMaxUsages)
    {
        var expirationDate = DateTimeOffset.UtcNow.AddDays(10);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Coupon(CouponId.New, "DISCOUNT", 10m, 20m, expirationDate, invalidMaxUsages));
    }

    [Fact]
    public void CanBeAppliedTo_ShouldReturnTrue_WhenConditionsMet()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 10);

        var canApply = coupon.CanBeAppliedTo(100m);

        Assert.True(canApply);
    }

    [Fact]
    public void CanBeAppliedTo_ShouldReturnFalse_WhenInactive()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 10);
        coupon.Deactivate();

        var canApply = coupon.CanBeAppliedTo(100m);

        Assert.False(canApply);
    }

    [Fact]
    public void CanBeAppliedTo_ShouldReturnFalse_WhenExpired()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 10);
        var pastReferenceDate = DateTimeOffset.UtcNow.AddDays(6);

        var canApply = coupon.CanBeAppliedTo(100m, now: pastReferenceDate);

        Assert.False(canApply);
    }

    [Fact]
    public void CanBeAppliedTo_ShouldReturnFalse_WhenOrderAmountIsBelowMinimum()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 10);

        var canApply = coupon.CanBeAppliedTo(49.99m);

        Assert.False(canApply);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnCorrectCalculatedDiscount()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 10);

        var discount = coupon.CalculateDiscount(150m);

        Assert.Equal(30.00m, discount);
    }

    [Fact]
    public void CalculateDiscount_ShouldThrow_WhenCouponCannotBeApplied()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 100m, DateTimeOffset.UtcNow.AddDays(5), 10);

        Assert.Throws<InvalidOperationException>(() => coupon.CalculateDiscount(50m));
    }

    [Fact]
    public void RecordUsage_ShouldIncrementTimesUsed()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 5);

        coupon.RecordUsage(100m);

        Assert.Equal(1, coupon.TimesUsed);
        Assert.True(coupon.IsActive);
    }

    [Fact]
    public void RecordUsage_ShouldDeactivateCoupon_WhenMaxUsagesReached()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 1);

        coupon.RecordUsage(100m);

        Assert.Equal(1, coupon.TimesUsed);
        Assert.False(coupon.IsActive);
        Assert.False(coupon.CanBeAppliedTo(100m));
    }

    [Fact]
    public void RecordUsage_ShouldThrow_WhenMaxUsagesAlreadyReached()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 1);
        coupon.RecordUsage(100m);

        Assert.Throws<InvalidOperationException>(() => coupon.RecordUsage(100m));
    }

    [Fact]
    public void Deactivate_ShouldMakeCouponInactive()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 10);

        coupon.Deactivate();

        Assert.False(coupon.IsActive);
    }

    [Fact]
    public void Activate_ShouldMakeCouponActive_WhenNotExpired()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 10);
        coupon.Deactivate();

        coupon.Activate();

        Assert.True(coupon.IsActive);
    }

    [Fact]
    public void Activate_ShouldThrow_WhenCouponIsExpired()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 10);
        coupon.Deactivate();
        var simulatedFuture = DateTimeOffset.UtcNow.AddDays(10);

        Assert.Throws<InvalidOperationException>(() => coupon.Activate(now: simulatedFuture));
    }

    [Fact]
    public void UpdateExpirationDate_ShouldUpdateDate_WhenInFuture()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 10);
        var newDate = DateTimeOffset.UtcNow.AddDays(20);

        coupon.UpdateExpirationDate(newDate);

        Assert.Equal(newDate, coupon.ExpirationDate);
    }

    [Fact]
    public void UpdateExpirationDate_ShouldThrow_WhenInPast()
    {
        var coupon = new Coupon(CouponId.New, "SAVE20", 20m, 50m, DateTimeOffset.UtcNow.AddDays(5), 10);
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);

        Assert.Throws<ArgumentException>(() => coupon.UpdateExpirationDate(pastDate));
    }
}
