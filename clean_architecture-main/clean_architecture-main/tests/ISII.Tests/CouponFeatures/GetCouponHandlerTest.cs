using Ardalis.Result;
using Ardalis.SharedKernel;
using Ardalis.Specification;
using ISII.Web.CouponFeatures.GetById;
using ISII.Web.Domain.CouponAggregate;
using NSubstitute;

namespace ISII.Tests.CouponFeatures;

public class GetCouponHandlerTest
{
    private readonly IReadRepository<Coupon> _repository = Substitute.For<IReadRepository<Coupon>>();
    private readonly GetCouponHandler _handler;

    public GetCouponHandlerTest()
    {
        _handler = new GetCouponHandler(_repository);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessWithDto_WhenCouponExists()
    {
        var couponId = CouponId.From(42);
        var expiration = DateTimeOffset.UtcNow.AddDays(30);
        var coupon = Coupon.Create(couponId, "PROMO42", 25m, 100m, expiration, 200);

        _repository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Coupon>>(),
            Arg.Any<CancellationToken>())
            .Returns(coupon);

        var query = new GetCouponQuery(couponId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(couponId, result.Value.Id);
        Assert.Equal("PROMO42", result.Value.Code);
        Assert.Equal(25m, result.Value.DiscountPercentage);
        Assert.Equal(100m, result.Value.MinimumAmount);
        Assert.Equal(expiration, result.Value.ExpirationDate);
        Assert.Equal(200, result.Value.MaxUsages);
        Assert.Equal(0, result.Value.TimesUsed);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCouponDoesNotExist()
    {
        var couponId = CouponId.From(999);

        _repository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Coupon>>(),
            Arg.Any<CancellationToken>())
            .Returns((Coupon?)null);

        var query = new GetCouponQuery(couponId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
