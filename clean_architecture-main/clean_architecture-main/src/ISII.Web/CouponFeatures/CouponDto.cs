using ISII.Web.Domain.CouponAggregate;

namespace ISII.Web.CouponFeatures;

public record CouponDto(
    CouponId Id,
    string Code,
    decimal DiscountPercentage,
    decimal MinimumAmount,
    DateTimeOffset ExpirationDate,
    int MaxUsages,
    int TimesUsed,
    bool IsActive);
