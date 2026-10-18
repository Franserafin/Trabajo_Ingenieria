namespace ISII.Web.Domain.CouponAggregate.Specifications;

public class CouponByCodeSpec : Specification<Coupon>
{
  public CouponByCodeSpec(string code) =>
    Query.Where(coupon => coupon.Code == code.ToUpper());
}
