using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Services;
using Xunit;

namespace CarMarketplace.Tests.Services;

public class AIServicePriceEstimateTests
{
    [Fact]
    public async Task EstimatePriceAsync_WhenUserPriceMissing_ReturnsEstimateWithoutEvaluation()
    {
        var service = new AIService();

        var response = await service.EstimatePriceAsync(new PriceEstimateRequestDTO
        {
            Brand = "Toyota",
            Model = "Corolla",
            Year = 2020,
            Mileage = 85_000,
            Condition = "good",
            Transmission = "automatic",
            FuelType = "gasoline",
            Location = "Cairo"
        });

        Assert.True(response.EstimatedPrice > 0);
        Assert.True(response.MinPrice > 0);
        Assert.True(response.MaxPrice > 0);
        Assert.True(response.MinPrice <= response.EstimatedPrice);
        Assert.True(response.MaxPrice >= response.EstimatedPrice);
        Assert.InRange(response.ConfidenceScore, 0m, 1m);
        Assert.Null(response.PriceStatus);
        Assert.Null(response.PercentageDifference);
        Assert.NotNull(response.Insights);
    }

    [Fact]
    public async Task EstimatePriceAsync_WhenUserPriceProvided_ReturnsEvaluation()
    {
        var service = new AIService();
        var userPrice = 650_000m;

        var response = await service.EstimatePriceAsync(new PriceEstimateRequestDTO
        {
            Brand = "Toyota",
            Model = "Corolla",
            Year = 2020,
            Mileage = 85_000,
            Condition = "good",
            Transmission = "automatic",
            FuelType = "gasoline",
            Location = "Cairo",
            UserPrice = userPrice
        });

        Assert.True(response.EstimatedPrice > 0);
        Assert.NotNull(response.PriceStatus);
        Assert.NotNull(response.PercentageDifference);

        var expectedDifference = decimal.Round(
            ((userPrice - response.EstimatedPrice) / response.EstimatedPrice) * 100m,
            2);
        var expectedStatus = expectedDifference < -15m ? "low" : expectedDifference > 15m ? "high" : "normal";

        Assert.Equal(expectedDifference, response.PercentageDifference!.Value);
        Assert.Equal(expectedStatus, response.PriceStatus);
        Assert.NotEmpty(response.Insights);
    }
}

