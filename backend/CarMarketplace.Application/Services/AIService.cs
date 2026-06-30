using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace CarMarketplace.Application.Services;

/// <summary>
/// Service implementation for AI operations.
/// </summary>
public class AIService : IAIService
{
    private const string MlServiceBaseUrlEnvVar = "CAR_MARKETPLACE_ML_SERVICE_URL";
    private const string MlServiceBaseUrlAltEnvVar = "ML_SERVICE_URL";
    private const string DefaultMlServiceBaseUrl = "http://127.0.0.1:5000";

    private const decimal AbsoluteMinEgyptianPrice = 80_000m;
    private const decimal AbsoluteMaxEgyptianPrice = 6_000_000m;

    private static readonly HttpClient HttpClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, BrandBaseline> EgyptianBrandBaselines =
        new Dictionary<string, BrandBaseline>(StringComparer.OrdinalIgnoreCase)
        {
            ["toyota"] = new(450_000m, 900_000m),
            ["hyundai"] = new(300_000m, 700_000m),
            ["nissan"] = new(350_000m, 750_000m),
            ["bmw"] = new(800_000m, 2_000_000m),
            ["mercedes"] = new(1_000_000m, 2_800_000m),
            ["audi"] = new(900_000m, 2_200_000m),
            ["kia"] = new(320_000m, 760_000m),
            ["honda"] = new(420_000m, 920_000m),
            ["chevrolet"] = new(250_000m, 600_000m),
            ["peugeot"] = new(280_000m, 650_000m)
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="AIService"/> class.
    /// </summary>
    public AIService()
    {
    }

    /// <inheritdoc/>
    public async Task<PriceEstimateResponseDTO> EstimatePriceAsync(PriceEstimateRequestDTO request)
    {
        var normalizedRequest = NormalizeEstimateRequest(request);

        try
        {
            var brandBaseline = GetBrandBaseline(normalizedRequest.Brand);
            var insights = new List<string>();

            var mlPrediction = await TryPredictWithMlServiceAsync(
                normalizedRequest.Year,
                normalizedRequest.Mileage,
                NormalizeFuelType(normalizedRequest.FuelType),
                NormalizeTransmission(normalizedRequest.Transmission));

            decimal estimatedPrice;
            if (mlPrediction.HasValue)
            {
                estimatedPrice = NormalizeMlPriceToEgyptianMarket(mlPrediction.Value, brandBaseline);
                estimatedPrice *= GetConditionFactor(normalizedRequest.Condition);
                estimatedPrice = Math.Clamp(
                    estimatedPrice,
                    brandBaseline.MinPrice * 0.60m,
                    brandBaseline.MaxPrice * 1.40m);

                insights.Add("تم التسعير باستخدام نموذج تعلم آلي مع معايرة لسوق مصر.");
            }
            else
            {
                estimatedPrice = EstimateRuleBasedPrice(normalizedRequest, brandBaseline, insights);
                insights.Add("تم استخدام نموذج بديل لأن خدمة الذكاء الاصطناعي غير متاحة حالياً.");
            }

            estimatedPrice = RoundCurrency(ClampEgyptianPrice(estimatedPrice));
            var minPrice = RoundCurrency(ClampEgyptianPrice(estimatedPrice * 0.90m));
            var maxPrice = RoundCurrency(ClampEgyptianPrice(estimatedPrice * 1.10m));

            string? priceStatus = null;
            decimal? percentageDifference = null;
            if (normalizedRequest.UserPrice.HasValue && normalizedRequest.UserPrice.Value > 0)
            {
                var (status, difference, statusInsight) = BuildPriceStatus(normalizedRequest.UserPrice.Value, estimatedPrice);
                priceStatus = status;
                percentageDifference = difference;

                if (!string.IsNullOrWhiteSpace(statusInsight))
                {
                    insights.Add(statusInsight);
                }
            }

            AppendContextInsights(normalizedRequest, insights);

            return new PriceEstimateResponseDTO
            {
                EstimatedPrice = estimatedPrice,
                MinPrice = minPrice,
                MaxPrice = Math.Max(minPrice, maxPrice),
                ConfidenceScore = CalculateConfidenceScore(
                    usedMlModel: mlPrediction.HasValue,
                    brandMatched: IsKnownBrand(normalizedRequest.Brand),
                    mileage: normalizedRequest.Mileage),
                PriceStatus = priceStatus,
                PercentageDifference = percentageDifference,
                Insights = insights
                    .Where(insight => !string.IsNullOrWhiteSpace(insight))
                    .Distinct(StringComparer.Ordinal)
                    .ToList()
            };
        }
        catch
        {
            return BuildSafePriceEstimateResponse(normalizedRequest);
        }
    }

    private static PriceEstimateRequestDTO NormalizeEstimateRequest(PriceEstimateRequestDTO? request)
    {
        request ??= new PriceEstimateRequestDTO();

        var currentYear = DateTime.UtcNow.Year;
        var normalizedYear = request.Year == 0 ? currentYear - 5 : request.Year;
        normalizedYear = Math.Clamp(normalizedYear, 1900, currentYear);

        return new PriceEstimateRequestDTO
        {
            Brand = (request.Brand ?? string.Empty).Trim(),
            Model = (request.Model ?? string.Empty).Trim(),
            Year = normalizedYear,
            Mileage = Math.Max(0, request.Mileage),
            Condition = string.IsNullOrWhiteSpace(request.Condition) ? "good" : request.Condition.Trim(),
            Transmission = string.IsNullOrWhiteSpace(request.Transmission) ? "Manual" : request.Transmission.Trim(),
            FuelType = string.IsNullOrWhiteSpace(request.FuelType) ? "Gasoline" : request.FuelType.Trim(),
            Location = (request.Location ?? string.Empty).Trim(),
            UserPrice = request.UserPrice
        };
    }

    private static decimal EstimateRuleBasedPrice(
        PriceEstimateRequestDTO request,
        BrandBaseline baseline,
        List<string> insights)
    {
        var midpoint = (baseline.MinPrice + baseline.MaxPrice) / 2m;
        var yearFactor = GetYearFactor(request.Year);
        var mileageFactor = GetMileageFactor(request.Mileage);
        var transmissionFactor = GetTransmissionFactor(request.Transmission);
        var fuelFactor = GetFuelFactor(request.FuelType);
        var conditionFactor = GetConditionFactor(request.Condition);

        var estimated = midpoint
            * yearFactor
            * mileageFactor
            * transmissionFactor
            * fuelFactor
            * conditionFactor;

        estimated = Math.Clamp(estimated, baseline.MinPrice * 0.55m, baseline.MaxPrice * 1.35m);

        if (yearFactor >= 1.03m)
        {
            insights.Add("موديل حديث يرفع السعر");
        }

        if (mileageFactor <= 0.92m)
        {
            insights.Add("عداد الكيلومترات عالي نسبياً");
        }

        if (transmissionFactor > 1.00m)
        {
            insights.Add("الفتيس الأوتوماتيك غالباً يرفع السعر.");
        }

        return estimated;
    }

    private static (string Status, decimal PercentageDifference, string Insight) BuildPriceStatus(
        decimal listedPrice,
        decimal estimatedPrice)
    {
        if (listedPrice <= 0 || estimatedPrice <= 0)
        {
            return ("normal", 0m, string.Empty);
        }

        var percentageDifference = decimal.Round(
            ((listedPrice - estimatedPrice) / estimatedPrice) * 100m,
            2);

        if (percentageDifference < -15m)
        {
            return ("low", percentageDifference, "السعر أقل من السوق");
        }

        if (percentageDifference > 15m)
        {
            return ("high", percentageDifference, "السعر أعلى من المتوسط");
        }

        return ("normal", percentageDifference, "سعر مناسب مقارنة بالموديل والسنة");
    }

    private static void AppendContextInsights(PriceEstimateRequestDTO request, List<string> insights)
    {
        var age = DateTime.UtcNow.Year - request.Year;

        if (request.Mileage > 100_000)
        {
            insights.Add("عداد الكيلومترات عالي نسبياً");
        }
        else if (request.Mileage > 0 && request.Mileage < 60_000)
        {
            insights.Add("عداد الكيلومترات منخفض نسبياً مقارنة بالسوق.");
        }

        if (age <= 2)
        {
            insights.Add("موديل حديث يرفع السعر");
        }
        else if (age >= 12)
        {
            insights.Add("سنة الصنع قديمة نسبياً وتؤثر على السعر.");
        }

        if (!IsKnownBrand(request.Brand))
        {
            insights.Add("يفضل إدخال الماركة بشكل دقيق لتحسين دقة التقدير.");
        }

        if (insights.Count == 0)
        {
            insights.Add("تم تقدير السعر بناءً على بيانات السيارة المدخلة.");
        }
    }

    private static decimal CalculateConfidenceScore(bool usedMlModel, bool brandMatched, int mileage)
    {
        var score = usedMlModel ? 0.86m : 0.74m;

        if (!brandMatched)
        {
            score -= 0.06m;
        }

        if (mileage > 250_000)
        {
            score -= 0.05m;
        }

        if (mileage <= 0)
        {
            score -= 0.04m;
        }

        return decimal.Round(Math.Clamp(score, 0.55m, 0.95m), 2);
    }

    private static decimal NormalizeMlPriceToEgyptianMarket(decimal mlPrice, BrandBaseline baseline)
    {
        if (mlPrice <= 0)
        {
            return (baseline.MinPrice + baseline.MaxPrice) / 2m;
        }

        var normalized = mlPrice;

        // Existing ML dataset can output smaller values; scale to realistic EGP.
        if (normalized < 100_000m)
        {
            normalized *= 25m;
        }

        if (normalized < baseline.MinPrice * 0.45m || normalized > baseline.MaxPrice * 1.80m)
        {
            var midpoint = (baseline.MinPrice + baseline.MaxPrice) / 2m;
            normalized = (normalized * 0.35m) + (midpoint * 0.65m);
        }

        return Math.Clamp(normalized, baseline.MinPrice * 0.60m, baseline.MaxPrice * 1.40m);
    }

    private static BrandBaseline GetBrandBaseline(string? brand)
    {
        var normalized = NormalizeBrandKey(brand);
        if (!string.IsNullOrWhiteSpace(normalized)
            && EgyptianBrandBaselines.TryGetValue(normalized, out var baseline))
        {
            return baseline;
        }

        return new BrandBaseline(320_000m, 900_000m);
    }

    private static bool IsKnownBrand(string? brand)
    {
        var normalized = NormalizeBrandKey(brand);
        return !string.IsNullOrWhiteSpace(normalized) && EgyptianBrandBaselines.ContainsKey(normalized);
    }

    private static string NormalizeBrandKey(string? brand)
    {
        return (brand ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static decimal GetYearFactor(int year)
    {
        var age = Math.Max(0, DateTime.UtcNow.Year - year);
        return age switch
        {
            <= 1 => 1.10m,
            <= 3 => 1.03m,
            <= 5 => 0.98m,
            <= 8 => 0.90m,
            <= 12 => 0.80m,
            <= 18 => 0.68m,
            _ => 0.58m
        };
    }

    private static decimal GetMileageFactor(int mileage)
    {
        if (mileage <= 60_000)
        {
            return 1.05m;
        }

        if (mileage <= 100_000)
        {
            return 1.00m;
        }

        var extraMileage = mileage - 100_000;
        var penalty = Math.Min(0.35m, (extraMileage / 10_000m) * 0.012m);
        return Math.Max(0.65m, 1.00m - penalty);
    }

    private static decimal GetTransmissionFactor(string transmission)
    {
        return NormalizeTransmission(transmission).ToLowerInvariant() switch
        {
            "automatic" => 1.04m,
            "cvt" => 1.03m,
            "semiautomatic" => 1.01m,
            "manual" => 0.99m,
            _ => 1.00m
        };
    }

    private static decimal GetFuelFactor(string fuelType)
    {
        return NormalizeFuelType(fuelType).ToLowerInvariant() switch
        {
            "electric" => 1.08m,
            "hybrid" => 1.05m,
            "pluginhybrid" => 1.04m,
            "diesel" => 1.02m,
            "cng" => 0.96m,
            _ => 1.00m
        };
    }

    private static decimal GetConditionFactor(string condition)
    {
        return (condition ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "excellent" => 1.07m,
            "good" => 1.00m,
            "fair" => 0.92m,
            "poor" => 0.85m,
            _ => 1.00m
        };
    }

    private static decimal RoundCurrency(decimal value)
    {
        return decimal.Round(value, 2);
    }

    private static decimal ClampEgyptianPrice(decimal value)
    {
        return Math.Clamp(value, AbsoluteMinEgyptianPrice, AbsoluteMaxEgyptianPrice);
    }

    private static async Task<decimal?> TryPredictWithMlServiceAsync(
        int year,
        int mileage,
        string fuelType,
        string transmission)
    {
        try
        {
            var baseUrl = GetMlServiceBaseUrl();
            var uri = new Uri($"{baseUrl.TrimEnd('/')}/predict", UriKind.Absolute);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var request = new MlPricePredictionRequest
            {
                Year = year,
                Mileage = mileage,
                FuelType = fuelType,
                Transmission = transmission
            };

            using var response = await HttpClient.PostAsJsonAsync(uri, request, JsonOptions, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var mlResponse = await response.Content.ReadFromJsonAsync<MlPricePredictionResponse>(JsonOptions, cts.Token);
            if (mlResponse == null || mlResponse.PredictedPrice <= 0)
            {
                return null;
            }

            return mlResponse.PredictedPrice;
        }
        catch
        {
            return null;
        }
    }

    private static string GetMlServiceBaseUrl()
    {
        var baseUrl = Environment.GetEnvironmentVariable(MlServiceBaseUrlEnvVar);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = Environment.GetEnvironmentVariable(MlServiceBaseUrlAltEnvVar);
        }

        return string.IsNullOrWhiteSpace(baseUrl) ? DefaultMlServiceBaseUrl : baseUrl.Trim();
    }

    private static string NormalizeFuelType(string fuelType)
    {
        var normalized = (fuelType ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "gasoline" or "petrol" or "بنزين" => "Gasoline",
            "diesel" or "ديزل" => "Diesel",
            "electric" or "كهرباء" or "كهربائي" => "Electric",
            "hybrid" or "هايبرد" => "Hybrid",
            "pluginhybrid" or "plug-in-hybrid" or "plug in hybrid" or "plug inhybrid" => "PlugInHybrid",
            "cng" => "CNG",
            _ => "Gasoline"
        };
    }

    private static string NormalizeTransmission(string transmission)
    {
        var normalized = (transmission ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "automatic" or "اوتوماتيك" => "Automatic",
            "manual" or "manu" or "مانيوال" => "Manual",
            "cvt" => "CVT",
            "semiautomatic" or "semi-automatic" or "semi automatic" => "SemiAutomatic",
            _ => "Manual"
        };
    }

    private static PriceEstimateResponseDTO BuildSafePriceEstimateResponse(PriceEstimateRequestDTO request)
    {
        var baseline = GetBrandBaseline(request.Brand);
        var estimated = RoundCurrency((baseline.MinPrice + baseline.MaxPrice) / 2m);
        var minPrice = RoundCurrency(ClampEgyptianPrice(estimated * 0.90m));
        var maxPrice = RoundCurrency(ClampEgyptianPrice(estimated * 1.10m));

        string? priceStatus = null;
        decimal? percentageDifference = null;
        if (request.UserPrice.HasValue && request.UserPrice.Value > 0)
        {
            var (status, difference, _) = BuildPriceStatus(request.UserPrice.Value, estimated);
            priceStatus = status;
            percentageDifference = difference;
        }

        return new PriceEstimateResponseDTO
        {
            EstimatedPrice = estimated,
            MinPrice = minPrice,
            MaxPrice = Math.Max(minPrice, maxPrice),
            ConfidenceScore = 0.60m,
            PriceStatus = priceStatus,
            PercentageDifference = percentageDifference,
            Insights =
            {
                "تم استخدام تقدير آمن بسبب تعذر حساب السعر التفصيلي.",
                "يرجى مراجعة البيانات والمحاولة مرة أخرى."
            }
        };
    }

    private sealed record BrandBaseline(decimal MinPrice, decimal MaxPrice);

    private sealed class MlPricePredictionRequest
    {
        public int Year { get; init; }
        public int Mileage { get; init; }
        public string FuelType { get; init; } = string.Empty;
        public string Transmission { get; init; } = string.Empty;
    }

    private sealed class MlPricePredictionResponse
    {
        public decimal PredictedPrice { get; init; }
    }
}
