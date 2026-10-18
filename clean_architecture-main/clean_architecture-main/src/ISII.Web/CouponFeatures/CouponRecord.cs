namespace ISII.Web.CouponFeatures;

public record CouponRecord(
    int Id,
    string Code,
    decimal DiscountPercentage,
    decimal MinimumAmount,
    DateTimeOffset ExpirationDate,
    int MaxUsages,
    int TimesUsed,
    bool IsActive);
