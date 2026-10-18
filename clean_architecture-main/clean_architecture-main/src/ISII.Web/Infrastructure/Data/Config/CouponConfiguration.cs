using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ISII.Web.Domain.CouponAggregate;

namespace ISII.Web.Infrastructure.Data.Config;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
  public void Configure(EntityTypeBuilder<Coupon> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenIntIdValueGenerator<AppDbContext, Coupon, CouponId>>()
      .HasVogenConversion()
      .HasSentinel(CouponId.New)
      .IsRequired();

    builder.Property(entity => entity.Code)
      .HasMaxLength(20)
      .IsRequired();

    builder.HasIndex(entity => entity.Code)
      .IsUnique();

    builder.Property(entity => entity.DiscountPercentage)
      .HasPrecision(5, 2)
      .IsRequired();

    builder.Property(entity => entity.MinimumAmount)
      .HasPrecision(18, 2)
      .IsRequired();

    builder.Property(entity => entity.ExpirationDate)
      .IsRequired();

    builder.Property(entity => entity.MaxUsages)
      .IsRequired();

    builder.Property(entity => entity.TimesUsed)
      .IsRequired();

    builder.Property(entity => entity.IsActive)
      .IsRequired();

    builder.HasData(
      new Coupon(
        CouponId.From(1),
        "WELCOME10",
        10.00m,
        20.00m,
        new DateTimeOffset(2030, 12, 31, 23, 59, 59, TimeSpan.Zero),
        100)
    );
  }
}
