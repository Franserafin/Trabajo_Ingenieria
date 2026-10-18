using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using ISII.Web.Domain.CouponAggregate;
using ISII.Web.Extensions;

namespace ISII.Web.CouponFeatures.GetById;

public sealed class GetCouponByIdRequest
{
  public const string Route = "/coupons/{CouponId}";
  public int CouponId { get; init; }
}

public class GetCouponByIdEndpoint(IMediator mediator)
  : Endpoint<GetCouponByIdRequest,
             Results<Ok<CouponRecord>,
                     NotFound,
                     ProblemHttpResult>,
             GetCouponByIdMapper>
{
  public override void Configure()
  {
    Get(GetCouponByIdRequest.Route);
    AllowAnonymous();

    Summary(s =>
    {
      s.Summary = "Get a coupon by ID";
      s.Description = "Retrieves a specific coupon by its unique numeric identifier.";
      s.ExampleRequest = new GetCouponByIdRequest { CouponId = 1 };
      s.ResponseExamples[200] = new CouponRecord(
        1,
        "WELCOME10",
        10.00m,
        20.00m,
        new DateTimeOffset(2030, 12, 31, 23, 59, 59, TimeSpan.Zero),
        100,
        0,
        true);

      s.Responses[200] = "Coupon found and returned successfully";
      s.Responses[404] = "Coupon with specified ID not found";
    });

    Tags("Coupons");

    Description(builder => builder
      .Accepts<GetCouponByIdRequest>()
      .Produces<CouponRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<CouponRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(GetCouponByIdRequest request, CancellationToken ct)
  {
    var result = await mediator.Send(new GetCouponQuery(CouponId.From(request.CouponId)), ct);

    return result.ToGetByIdResult(Map.FromEntity);
  }
}

public sealed class GetCouponByIdValidator : Validator<GetCouponByIdRequest>
{
  public GetCouponByIdValidator()
  {
    RuleFor(x => x.CouponId)
      .GreaterThan(0)
      .WithMessage("Coupon ID must be greater than 0");
  }
}

public sealed class GetCouponByIdMapper
  : Mapper<GetCouponByIdRequest, CouponRecord, CouponDto>
{
  public override CouponRecord FromEntity(CouponDto e) =>
    new(
      e.Id.Value,
      e.Code,
      e.DiscountPercentage,
      e.MinimumAmount,
      e.ExpirationDate,
      e.MaxUsages,
      e.TimesUsed,
      e.IsActive);
}
