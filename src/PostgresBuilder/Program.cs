using CompleteNatGeo.PostgresBuilder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
	.SetBasePath(AppContext.BaseDirectory)
	.AddJsonFile("appsettings.local.json", optional: false)
	.Build();

var settings =
	configuration.GetRequiredSection("Inputs").Get<InputsConfig>()
	?? throw new InvalidOperationException("The Inputs configuration section is required.");

Console.WriteLine("SQLite path: " + settings.SqlitePath);
Console.WriteLine("Images path: " + settings.ImagesPath);

await using var connection = new SqliteConnection(settings.SqliteConnectionString);
connection.Open();

await DatabaseConverter.ConvertAsync(connection, settings.ImagesPath);

Console.WriteLine("Connected to SQLite database.");
