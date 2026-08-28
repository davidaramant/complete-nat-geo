using System.CommandLine;
using System.CommandLine.Parsing;
using CompleteNatGeo.PostgresBuilder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

var settings = ReadInputs();
var argument = ParseConversionModeArgument(args);

switch (argument)
{
	case Argument.NoInput:
		return 0;
	case Argument.Error:
		return 1;
}
bool convertOnlyMetadata = argument == Argument.ConvertOnlyMetadata;

Console.WriteLine("SQLite path: " + settings.SqlitePath);
Console.WriteLine("Images path: " + settings.ImagesPath);
Console.WriteLine("Converting: " + (convertOnlyMetadata ? "Metadata" : "Everything"));

await using var connection = new SqliteConnection(settings.SqliteConnectionString);
connection.Open();

if (!convertOnlyMetadata)
{
	Console.WriteLine("Converting pages...");
	await DatabaseConverter.ConvertPagesAsync(connection, settings.ImagesPath);
}

Console.WriteLine("Converting metadata...");
await DatabaseConverter.ConvertMetadataAsync(connection, settings.ImagesPath);

return 0;

static InputsConfig ReadInputs()
{
	var configuration = new ConfigurationBuilder()
		.SetBasePath(AppContext.BaseDirectory)
		.AddJsonFile("appsettings.local.json", optional: false)
		.Build();

	return configuration.GetRequiredSection("Inputs").Get<InputsConfig>()
		?? throw new InvalidOperationException("The Inputs configuration section is required.");
}

static Argument ParseConversionModeArgument(string[] args)
{
	Command convertPages = new("convert-pages");
	Command convertMetadata = new("convert-metadata");

	RootCommand rootCommand = new("Converts the SQLite database into a Postgres database")
	{
		convertPages,
		convertMetadata,
	};

	ParseResult parseResult = rootCommand.Parse(args);
	if (parseResult.Errors.Count == 0)
	{
		if (parseResult.GetResult(convertPages) is not null)
			return Argument.ConvertEverything;
		if (parseResult.GetResult(convertMetadata) is not null)
			return Argument.ConvertOnlyMetadata;
		return Argument.NoInput;
	}

	foreach (ParseError parseError in parseResult.Errors)
	{
		Console.Error.WriteLine(parseError.Message);
	}

	return Argument.Error;
}

enum Argument
{
	NoInput,
	Error,
	ConvertOnlyMetadata,
	ConvertEverything,
}
