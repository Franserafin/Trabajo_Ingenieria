using Vogen;

namespace ISII.Web.Domain.CouponAggregate;

[ValueObject<int>]
public readonly partial struct CouponId
{
  /// <summary>
  /// Represents a new Coupon that has not yet been persisted to the database.
  /// EF Core will assign the actual identity value on SaveChanges.
  /// </summary>
  public static CouponId New => From(0);

  private static Validation Validate(int value)
      => value >= 0 ? Validation.Ok : Validation.Invalid("CouponId must be non-negative.");
}
