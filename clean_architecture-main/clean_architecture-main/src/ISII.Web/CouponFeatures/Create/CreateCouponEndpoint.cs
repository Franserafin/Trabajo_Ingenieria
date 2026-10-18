using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using ISII.Web.Domain.CouponAggregate;
using ISII.Web.Domain.CouponAggregate.Specifications;

namespace ISII.Web.CouponFeatures.Create;

public sealed class CreateCouponRequest
{
  public CouponId Id { get; init; } = CouponId.New;
  public string Code { get; init; } = string.Empty;
  public decimal DiscountPercentage { get; init; }
  public decimal MinimumAmount { get; init; }
  public DateTimeOffset ExpirationDate { get; init; }
  public int MaxUsages { get; init; } = 1;
}

public class CreateCouponEndpoint(IRepository<Coupon> repository) :
  Endpoint<CreateCouponRequest,
           Results<Created<CouponRecord>, ValidationProblem, ProblemHttpResult>>
{
  private readonly IRepository<Coupon> _repository = repository;

  public override void Configure()
  {
    Post("/coupons");
    AllowAnonymous();

    Summary(s =>
    {
      s.Summary = "Create a new discount coupon";
      s.Description = "Creates a new coupon with code, discount percentage, minimum order amount, expiration date, and maximum allowed usages.";
      s.ExampleRequest = new CreateCouponRequest
      {
        Code = "SPRING2026",
        DiscountPercentage = 15.00m,
        MinimumAmount = 50.00m,
        ExpirationDate = DateTimeOffset.UtcNow.AddMonths(3),
        MaxUsages = 50
      };
      s.ResponseExamples[201] = new CouponRecord(
        1,
        "SPRING2026",
        15.00m,
        50.00m,
        DateTimeOffset.UtcNow.AddMonths(3),
        50,
        0,
        true);

      s.Responses[201] = "Coupon created successfully";
      s.Responses[400] = "Invalid request data";
      s.Responses[409] = "A coupon with this code already exists";
    });

    Tags("Coupons");

    Description(builder => builder
      .Accepts<CreateCouponRequest>()
      .Produces<CouponRecord>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(409));
  }

  public override async Task<Results<Created<CouponRecord>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateCouponRequest request, CancellationToken cancellationToken)
  {
    var existingCoupon = await _repository.FirstOrDefaultAsync(
      new CouponByCodeSpec(request.Code),
      cancellationToken);

    if (existingCoupon != null)
    {
      return TypedResults.Problem(
        detail: $"A coupon with code '{request.Code.Trim().ToUpperInvariant()}' already exists.",
        statusCode: StatusCodes.Status409Conflict,
        title: "Conflict");
    }

    var coupon = Coupon.Create(
      request.Id,
      request.Code,
      request.DiscountPercentage,
      request.MinimumAmount,
      request.ExpirationDate,
      request.MaxUsages);

    await _repository.AddAsync(coupon, cancellationToken);
    await _repository.SaveChangesAsync(cancellationToken);

    var response = new CouponRecord(
      coupon.Id.Value,
      coupon.Code,
      coupon.DiscountPercentage,
      coupon.MinimumAmount,
      coupon.ExpirationDate,
      coupon.MaxUsages,
      coupon.TimesUsed,
      coupon.IsActive);

    return TypedResults.Created($"/coupons/{coupon.Id.Value}", response);
  }
}

public sealed class CreateCouponValidator : Validator<CreateCouponRequest>
{
  public CreateCouponValidator()
  {
    RuleFor(x => x.Code)
      .NotEmpty()
      .WithMessage("Coupon code is required")
      .MinimumLength(3)
      .WithMessage("Coupon code must be at least 3 characters")
      .MaximumLength(20)
      .WithMessage("Coupon code must not exceed 20 characters")
      .Matches("^[a-zA-Z0-9_-]+$")
      .WithMessage("Coupon code must contain only letters, numbers, hyphens, or underscores");

    RuleFor(x => x.DiscountPercentage)
      .GreaterThan(0)
      .WithMessage("Discount percentage must be greater than zero")
      .LessThanOrEqualTo(100)
      .WithMessage("Discount percentage cannot exceed 100");

    RuleFor(x => x.MinimumAmount)
      .GreaterThanOrEqualTo(0)
      .WithMessage("Minimum amount cannot be negative");

    RuleFor(x => x.ExpirationDate)
      .GreaterThan(DateTimeOffset.UtcNow)
      .WithMessage("Expiration date must be in the future");

    RuleFor(x => x.MaxUsages)
      .GreaterThan(0)
      .WithMessage("Max usages must be greater than zero");
  }
}
