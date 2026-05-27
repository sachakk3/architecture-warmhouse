var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/temperature", (string? location) =>
{
	if (string.IsNullOrWhiteSpace(location))
	{
		return Results.BadRequest(new { error = "Query parameter 'location' is required." });
	}

	var value = Math.Round(Random.Shared.NextDouble() * 80 - 30, 1);
	var timestamp = DateTime.UtcNow;

	return Results.Ok(new
	{
		location,
		value,
		temperature = value,
		unit = "C",
		status = "active",
		timestamp,
		description = $"Current temperature for {location}"
	});
});

app.MapGet("/temperature/{sensorId}", (string sensorId) =>
{
	if (string.IsNullOrWhiteSpace(sensorId))
	{
		return Results.BadRequest(new { error = "Path parameter 'sensorId' is required." });
	}

	var value = Math.Round(Random.Shared.NextDouble() * 80 - 30, 1);
	var timestamp = DateTime.UtcNow;

	return Results.Ok(new
	{
		sensor_id = sensorId,
		location = $"sensor-{sensorId}",
		value,
		temperature = value,
		unit = "C",
		status = "active",
		timestamp,
		description = $"Current temperature for sensor {sensorId}"
	});
});

app.Run();
