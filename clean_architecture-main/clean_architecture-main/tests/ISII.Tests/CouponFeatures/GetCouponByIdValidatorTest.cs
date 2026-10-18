using ISII.Web.CouponFeatures.GetById;

namespace ISII.Tests.CouponFeatures;

public class GetCouponByIdValidatorTest
{
    private readonly GetCouponByIdValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenCouponIdIsPositive()
    {
        var request = new GetCouponByIdRequest { CouponId = 1 };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_ShouldFail_WhenCouponIdIsZeroOrNegative(int invalidId)
    {
        var request = new GetCouponByIdRequest { CouponId = invalidId };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetCouponByIdRequest.CouponId));
    }
}
