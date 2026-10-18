using Ardalis.GuardClauses;

namespace ISII.Web.Domain.CouponAggregate;

public class Coupon : EntityBase<Coupon, CouponId>, IAggregateRoot
{
  // Private constructor for EF Core
  private Coupon() { }

  public Coupon(
    CouponId id,
    string code,
    decimal discountPercentage,
    decimal minimumAmount,
    DateTimeOffset expirationDate,
    int maxUsages)
  {
    Guard.Against.NullOrWhiteSpace(code, nameof(code), "Coupon code cannot be empty.");
    Guard.Against.Negative(minimumAmount, nameof(minimumAmount), "Minimum amount cannot be negative.");

    if (discountPercentage <= 0 || discountPercentage > 100)
    {
      throw new ArgumentOutOfRangeException(nameof(discountPercentage), "Discount percentage must be between 1 and 100.");
    }

    if (maxUsages <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(maxUsages), "Max usages must be greater than zero.");
    }

    Id = id;
    Code = code.Trim().ToUpperInvariant();
    DiscountPercentage = discountPercentage;
    MinimumAmount = minimumAmount;
    ExpirationDate = expirationDate;
    MaxUsages = maxUsages;
    TimesUsed = 0;
    IsActive = true;
  }

  // Factory method for creating new coupons
  public static Coupon Create(
    CouponId id,
    string code,
    decimal discountPercentage,
    decimal minimumAmount,
    DateTimeOffset expirationDate,
    int maxUsages) =>
    new(id, code, discountPercentage, minimumAmount, expirationDate, maxUsages);

  public string Code { get; private set; } = string.Empty;
  public decimal DiscountPercentage { get; private set; }
  public decimal MinimumAmount { get; private set; }
  public DateTimeOffset ExpirationDate { get; private set; }
  public int MaxUsages { get; private set; }
  public int TimesUsed { get; private set; }
  public bool IsActive { get; private set; } = true;

  public bool IsExpired(DateTimeOffset? now = null) =>
    (now ?? DateTimeOffset.UtcNow) > ExpirationDate;

  public bool CanBeAppliedTo(decimal orderAmount, DateTimeOffset? now = null)
  {
    if (!IsActive)
    {
      return false;
    }

    if (IsExpired(now))
    {
      return false;
    }

    if (orderAmount < MinimumAmount)
    {
      return false;
    }

    if (TimesUsed >= MaxUsages)
    {
      return false;
    }

    return true;
  }

  public decimal CalculateDiscount(decimal orderAmount, DateTimeOffset? now = null)
  {
    if (!CanBeAppliedTo(orderAmount, now))
    {
      throw new InvalidOperationException("Coupon cannot be applied to this order amount or is no longer valid.");
    }

    return Math.Round(orderAmount * (DiscountPercentage / 100m), 2);
  }

  public void RecordUsage(decimal orderAmount, DateTimeOffset? now = null)
  {
    if (!CanBeAppliedTo(orderAmount, now))
    {
      throw new InvalidOperationException("Coupon cannot be used because it is invalid, expired, or maximum usages reached.");
    }

    TimesUsed++;

    if (TimesUsed >= MaxUsages)
    {
      IsActive = false;
    }
  }

  public void Deactivate()
  {
    IsActive = false;
  }

  public void Activate(DateTimeOffset? now = null)
  {
    if (IsExpired(now))
    {
      throw new InvalidOperationException("Cannot activate an expired coupon.");
    }

    IsActive = true;
  }

  public void UpdateExpirationDate(DateTimeOffset newExpirationDate, DateTimeOffset? now = null)
  {
    if (newExpirationDate <= (now ?? DateTimeOffset.UtcNow))
    {
      throw new ArgumentException("New expiration date must be in the future.", nameof(newExpirationDate));
    }

    ExpirationDate = newExpirationDate;
  }
}
