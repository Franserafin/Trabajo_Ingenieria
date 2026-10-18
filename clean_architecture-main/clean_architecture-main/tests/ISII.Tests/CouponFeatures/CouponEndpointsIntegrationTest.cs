using System.Net;
using System.Net.Http.Json;
using ISII.Web.CouponFeatures;
using ISII.Web.CouponFeatures.Create;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ISII.Tests.CouponFeatures;

public class CouponEndpointsIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CouponEndpointsIntegrationTest(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCouponById_ShouldReturnSeedCoupon_WhenIdIsOne()
    {
        var response = await _client.GetAsync("/coupons/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var coupon = await response.Content.ReadFromJsonAsync<CouponRecord>();
        Assert.NotNull(coupon);
        Assert.Equal(1, coupon.Id);
        Assert.Equal("WELCOME10", coupon.Code);
        Assert.Equal(10.00m, coupon.DiscountPercentage);
        Assert.True(coupon.IsActive);
    }

    [Fact]
    public async Task GetCouponById_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        var response = await _client.GetAsync("/coupons/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCoupon_ShouldReturnCreated_WhenRequestIsValid()
    {
        var uniqueCode = $"TEST_{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var request = new CreateCouponRequest
        {
            Code = uniqueCode,
            DiscountPercentage = 15m,
            MinimumAmount = 30m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(45),
            MaxUsages = 50
        };

        var postResponse = await _client.PostAsJsonAsync("/coupons", request);

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        Assert.NotNull(postResponse.Headers.Location);

        var created = await postResponse.Content.ReadFromJsonAsync<CouponRecord>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal(uniqueCode, created.Code);
        Assert.Equal(15m, created.DiscountPercentage);
        Assert.Equal(30m, created.MinimumAmount);
        Assert.Equal(50, created.MaxUsages);
        Assert.Equal(0, created.TimesUsed);
        Assert.True(created.IsActive);

        // Verify retrieval through GET endpoint
        var getResponse = await _client.GetAsync($"/coupons/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<CouponRecord>();
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(uniqueCode, retrieved.Code);
    }

    [Fact]
    public async Task CreateCoupon_ShouldReturnBadRequest_WhenValidationFails()
    {
        var invalidRequest = new CreateCouponRequest
        {
            Code = "!",
            DiscountPercentage = -10m,
            MinimumAmount = -5m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(-1),
            MaxUsages = 0
        };

        var response = await _client.PostAsJsonAsync("/coupons", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCoupon_ShouldReturnConflict_WhenCodeAlreadyExists()
    {
        // WELCOME10 is already seeded
        var duplicateRequest = new CreateCouponRequest
        {
            Code = "welcome10",
            DiscountPercentage = 20m,
            MinimumAmount = 10m,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
            MaxUsages = 10
        };

        var response = await _client.PostAsJsonAsync("/coupons", duplicateRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
