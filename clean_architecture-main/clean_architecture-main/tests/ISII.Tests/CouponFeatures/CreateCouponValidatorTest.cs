using ISII.Web.CouponFeatures.Create;

namespace ISII.Tests.CouponFeatures;

public class CreateCouponValidatorTest
{
    private readonly CreateCouponValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        var request = new CreateCouponRequest
        {
            Code = "WELCOME20",
            DiscountPercentage = 20m,
            MinimumAmount = 50m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
            MaxUsages = 100
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenCodeIsEmpty(string? code)
    {
        var request = new CreateCouponRequest
        {
            Code = code!,
            DiscountPercentage = 20m,
            MinimumAmount = 50m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
            MaxUsages = 100
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponRequest.Code));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCodeIsTooShort()
    {
        var request = new CreateCouponRequest
        {
            Code = "AB",
            DiscountPercentage = 20m,
            MinimumAmount = 50m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
            MaxUsages = 100
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponRequest.Code));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCodeIsTooLong()
    {
        var request = new CreateCouponRequest
        {
            Code = new string('A', 25),
            DiscountPercentage = 20m,
            MinimumAmount = 50m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
            MaxUsages = 100
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponRequest.Code));
    }

    [Theory]
    [InlineData("CODE!")]
    [InlineData("CODE 20")]
    [InlineData("CODE@HOME")]
    public void Validate_ShouldFail_WhenCodeContainsInvalidCharacters(string invalidCode)
    {
        var request = new CreateCouponRequest
        {
            Code = invalidCode,
            DiscountPercentage = 20m,
            MinimumAmount = 50m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
            MaxUsages = 100
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponRequest.Code));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.5)]
    public void Validate_ShouldFail_WhenDiscountPercentageIsOutOfRange(decimal discount)
    {
        var request = new CreateCouponRequest
        {
            Code = "VALIDCODE",
            DiscountPercentage = discount,
            MinimumAmount = 50m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
            MaxUsages = 100
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponRequest.DiscountPercentage));
    }

    [Fact]
    public void Validate_ShouldFail_WhenMinimumAmountIsNegative()
    {
        var request = new CreateCouponRequest
        {
            Code = "VALIDCODE",
            DiscountPercentage = 20m,
            MinimumAmount = -1m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
            MaxUsages = 100
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponRequest.MinimumAmount));
    }

    [Fact]
    public void Validate_ShouldFail_WhenExpirationDateIsInThePast()
    {
        var request = new CreateCouponRequest
        {
            Code = "VALIDCODE",
            DiscountPercentage = 20m,
            MinimumAmount = 10m,
            ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(-10),
            MaxUsages = 100
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponRequest.ExpirationDate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenMaxUsagesIsZeroOrNegative(int maxUsages)
    {
        var request = new CreateCouponRequest
        {
            Code = "VALIDCODE",
            DiscountPercentage = 20m,
            MinimumAmount = 10m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(10),
            MaxUsages = maxUsages
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponRequest.MaxUsages));
    }
}
