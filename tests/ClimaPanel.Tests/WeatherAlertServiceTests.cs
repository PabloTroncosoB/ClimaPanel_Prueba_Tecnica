using ClimaPanel.Web.Common;
using ClimaPanel.Web.Data;
using ClimaPanel.Web.Models;
using ClimaPanel.Web.Models.ViewModels;
using ClimaPanel.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClimaPanel.Tests;

public sealed class WeatherAlertServiceTests
{
    private async Task<(AppDbContext db, WeatherAlertService service, FavoriteCity city)> SetupAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var service = new WeatherAlertService(db);

        // Create a test favorite city
        var city = new FavoriteCity
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserId = "ana",
            LocationId = 123,
            Name = "Santiago",
            Country = "Chile",
            CountryCode = "CL",
            Latitude = -33.4,
            Longitude = -70.6,
            Timezone = "America/Santiago",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.FavoriteCities.Add(city);
        await db.SaveChangesAsync();

        return (db, service, city);
    }

    [Fact]
    public async Task Can_create_alert_with_valid_inputs()
    {
        var (db, service, city) = await SetupAsync();

        var input = new CreateWeatherAlertInput
        {
            FavoriteId = city.Id,
            Metric = WeatherMetric.TemperatureC,
            Operator = ThresholdOperator.GreaterThanOrEqual,
            Threshold = 30
        };

        var created = await service.CreateAsync("ana", input, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(city.Id, created.FavoriteId);
        Assert.Equal(WeatherMetric.TemperatureC, created.Metric);
        Assert.Equal(ThresholdOperator.GreaterThanOrEqual, created.Operator);
        Assert.Equal(30, created.Threshold);
        Assert.True(created.IsEnabled);
        Assert.False(created.IsTriggered);
    }

    [Fact]
    public async Task Cannot_create_more_than_5_active_alerts_per_city()
    {
        var (db, service, city) = await SetupAsync();

        // Create 5 alerts
        for (int i = 0; i < 5; i++)
        {
            await service.CreateAsync("ana", new CreateWeatherAlertInput
            {
                FavoriteId = city.Id,
                Metric = WeatherMetric.TemperatureC,
                Operator = ThresholdOperator.GreaterThanOrEqual,
                Threshold = 20 + i
            }, CancellationToken.None);
        }

        // Try to create a 6th alert
        var ex = await Assert.ThrowsAsync<UserMessageException>(async () =>
        {
            await service.CreateAsync("ana", new CreateWeatherAlertInput
            {
                FavoriteId = city.Id,
                Metric = WeatherMetric.HumidityPercent,
                Operator = ThresholdOperator.LessThanOrEqual,
                Threshold = 40
            }, CancellationToken.None);
        });

        Assert.Contains("No puede crear más de 5 alertas", ex.Message);
    }

    [Theory]
    [InlineData(WeatherMetric.TemperatureC, -81)]
    [InlineData(WeatherMetric.TemperatureC, 81)]
    [InlineData(WeatherMetric.HumidityPercent, -1)]
    [InlineData(WeatherMetric.HumidityPercent, 101)]
    [InlineData(WeatherMetric.PrecipitationMm, -1)]
    [InlineData(WeatherMetric.PrecipitationMm, 501)]
    [InlineData(WeatherMetric.WindSpeedKmh, -1)]
    [InlineData(WeatherMetric.WindSpeedKmh, 301)]
    public async Task Cannot_create_alert_with_threshold_out_of_range(WeatherMetric metric, double threshold)
    {
        var (db, service, city) = await SetupAsync();

        var ex = await Assert.ThrowsAsync<UserMessageException>(async () =>
        {
            await service.CreateAsync("ana", new CreateWeatherAlertInput
            {
                FavoriteId = city.Id,
                Metric = metric,
                Operator = ThresholdOperator.GreaterThanOrEqual,
                Threshold = threshold
            }, CancellationToken.None);
        });

        Assert.Contains("El valor debe estar entre", ex.Message);
    }

    [Fact]
    public async Task Can_list_alerts_for_favorite()
    {
        var (db, service, city) = await SetupAsync();

        // Create 3 alerts
        for (int i = 0; i < 3; i++)
        {
            await service.CreateAsync("ana", new CreateWeatherAlertInput
            {
                FavoriteId = city.Id,
                Metric = WeatherMetric.TemperatureC,
                Operator = ThresholdOperator.GreaterThanOrEqual,
                Threshold = 25 + i
            }, CancellationToken.None);
        }

        var alerts = await service.ListAsync("ana", city.Id, CancellationToken.None);

        Assert.Equal(3, alerts.Count);
    }

    [Fact]
    public async Task Can_toggle_alert_enabled_status()
    {
        var (db, service, city) = await SetupAsync();

        var created = await service.CreateAsync("ana", new CreateWeatherAlertInput
        {
            FavoriteId = city.Id,
            Metric = WeatherMetric.TemperatureC,
            Operator = ThresholdOperator.GreaterThanOrEqual,
            Threshold = 30
        }, CancellationToken.None);

        Assert.True(created.IsEnabled);

        await service.ToggleAsync("ana", city.Id, created.Id, CancellationToken.None);

        var alerts = await service.ListAsync("ana", city.Id, CancellationToken.None);
        var toggled = alerts.First();

        Assert.False(toggled.IsEnabled);
    }

    [Fact]
    public async Task Can_delete_alert()
    {
        var (db, service, city) = await SetupAsync();

        var created = await service.CreateAsync("ana", new CreateWeatherAlertInput
        {
            FavoriteId = city.Id,
            Metric = WeatherMetric.TemperatureC,
            Operator = ThresholdOperator.GreaterThanOrEqual,
            Threshold = 30
        }, CancellationToken.None);

        await service.DeleteAsync("ana", city.Id, created.Id, CancellationToken.None);

        var alerts = await service.ListAsync("ana", city.Id, CancellationToken.None);

        Assert.Empty(alerts);
    }

    [Fact]
    public async Task Evaluate_triggers_when_condition_met_temperature_greater_or_equal()
    {
        var (db, service, city) = await SetupAsync();

        await service.CreateAsync("ana", new CreateWeatherAlertInput
        {
            FavoriteId = city.Id,
            Metric = WeatherMetric.TemperatureC,
            Operator = ThresholdOperator.GreaterThanOrEqual,
            Threshold = 25
        }, CancellationToken.None);

        var weather = new WeatherCard(
            Source: "LIVE",
            FetchedAtUtc: DateTime.UtcNow,
            TemperatureC: 28,
            HumidityPercent: 60,
            PrecipitationMm: 0,
            WindSpeedKmh: 10,
            Daily: []);

        var result = await service.EvaluateAsync("ana", city.Id, weather, CancellationToken.None);

        Assert.Equal(1, result.Evaluated);
        Assert.Equal(1, result.Triggered);

        var alerts = await service.ListAsync("ana", city.Id, CancellationToken.None);
        var alert = alerts.First();

        Assert.True(alert.IsTriggered);
        Assert.NotNull(alert.LastTriggeredAtUtc);
    }

    [Fact]
    public async Task Evaluate_does_not_trigger_when_condition_not_met()
    {
        var (db, service, city) = await SetupAsync();

        await service.CreateAsync("ana", new CreateWeatherAlertInput
        {
            FavoriteId = city.Id,
            Metric = WeatherMetric.TemperatureC,
            Operator = ThresholdOperator.GreaterThanOrEqual,
            Threshold = 25
        }, CancellationToken.None);

        var weather = new WeatherCard(
            Source: "LIVE",
            FetchedAtUtc: DateTime.UtcNow,
            TemperatureC: 20,
            HumidityPercent: 60,
            PrecipitationMm: 0,
            WindSpeedKmh: 10,
            Daily: []);

        var result = await service.EvaluateAsync("ana", city.Id, weather, CancellationToken.None);

        Assert.Equal(1, result.Evaluated);
        Assert.Equal(0, result.Triggered);

        var alerts = await service.ListAsync("ana", city.Id, CancellationToken.None);
        var alert = alerts.First();

        Assert.False(alert.IsTriggered);
    }

    [Fact]
    public async Task Evaluate_triggers_humidity_less_or_equal()
    {
        var (db, service, city) = await SetupAsync();

        await service.CreateAsync("ana", new CreateWeatherAlertInput
        {
            FavoriteId = city.Id,
            Metric = WeatherMetric.HumidityPercent,
            Operator = ThresholdOperator.LessThanOrEqual,
            Threshold = 50
        }, CancellationToken.None);

        var weather = new WeatherCard(
            Source: "LIVE",
            FetchedAtUtc: DateTime.UtcNow,
            TemperatureC: 25,
            HumidityPercent: 40,
            PrecipitationMm: 0,
            WindSpeedKmh: 10,
            Daily: []);

        var result = await service.EvaluateAsync("ana", city.Id, weather, CancellationToken.None);

        Assert.Equal(1, result.Triggered);

        var alerts = await service.ListAsync("ana", city.Id, CancellationToken.None);
        Assert.True(alerts.First().IsTriggered);
    }

    [Fact]
    public async Task Cannot_list_alerts_for_other_user()
    {
        var (db, service, city) = await SetupAsync();

        await service.CreateAsync("ana", new CreateWeatherAlertInput
        {
            FavoriteId = city.Id,
            Metric = WeatherMetric.TemperatureC,
            Operator = ThresholdOperator.GreaterThanOrEqual,
            Threshold = 30
        }, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<UserMessageException>(async () =>
        {
            await service.ListAsync("bruno", city.Id, CancellationToken.None);
        });

        Assert.Contains("No se encontró", ex.Message);
    }

    [Fact]
    public async Task Cannot_delete_alert_of_other_user()
    {
        var (db, service, city) = await SetupAsync();

        var created = await service.CreateAsync("ana", new CreateWeatherAlertInput
        {
            FavoriteId = city.Id,
            Metric = WeatherMetric.TemperatureC,
            Operator = ThresholdOperator.GreaterThanOrEqual,
            Threshold = 30
        }, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<UserMessageException>(async () =>
        {
            await service.DeleteAsync("bruno", city.Id, created.Id, CancellationToken.None);
        });

        Assert.Contains("No se encontró", ex.Message);
    }

    [Fact]
    public async Task Disabled_alerts_are_not_evaluated()
    {
        var (db, service, city) = await SetupAsync();

        var created = await service.CreateAsync("ana", new CreateWeatherAlertInput
        {
            FavoriteId = city.Id,
            Metric = WeatherMetric.TemperatureC,
            Operator = ThresholdOperator.GreaterThanOrEqual,
            Threshold = 25
        }, CancellationToken.None);

        // Disable the alert
        await service.ToggleAsync("ana", city.Id, created.Id, CancellationToken.None);

        var weather = new WeatherCard(
            Source: "LIVE",
            FetchedAtUtc: DateTime.UtcNow,
            TemperatureC: 30,
            HumidityPercent: 60,
            PrecipitationMm: 0,
            WindSpeedKmh: 10,
            Daily: []);

        var result = await service.EvaluateAsync("ana", city.Id, weather, CancellationToken.None);

        // 0 alerts evaluated (because it's disabled)
        Assert.Equal(0, result.Evaluated);
    }
}
