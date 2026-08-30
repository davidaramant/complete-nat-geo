using System.CommandLine;
using System.CommandLine.Parsing;

namespace CompleteNatGeo.PostgresBuilder.Arguments;

static class ArgumentParser
{
	public static Invocation Parse(string[] args)
	{
		var sqlitePath = new Option<DirectoryInfo>("--sqlite-path")
		{
			Required = true,
			Description = "Path to the SQLite database",
		};

		var convert = new Command("convert", "Convert database content");
		var connectionString = new Option<string>("--postgres-connection-string")
		{
			Required = true,
			Description = "Postgres connection string",
		};
		convert.Options.Add(connectionString);

		var pages = new Command("pages", "Convert page images and metadata");
		var imagesPath = new Option<DirectoryInfo>("--images-path")
		{
			Required = true,
			Description = "Path to the converted JPGs",
		};
		pages.Options.Add(imagesPath);

		var metadata = new Command("metadata", "Convert metadata only");

		convert.Subcommands.Add(pages);
		convert.Subcommands.Add(metadata);

		var verifyMappings = new Command("verify-mappings", "Verify legacy mappings");

		var root = new RootCommand("Convert the SQLite database into Postgres");
		root.Options.Add(sqlitePath);
		root.Subcommands.Add(verifyMappings);
		root.Subcommands.Add(convert);

		ParseResult parseResult = root.Parse(args);
		if (parseResult.Errors.Count > 0)
		{
			foreach (ParseError error in parseResult.Errors)
			{
				Console.Error.WriteLine(error.Message);
			}

			throw new ArgumentException("Invalid command-line arguments.");
		}

		DirectoryInfo parsedSqlitePath = parseResult.GetValue(sqlitePath)!;
		return parseResult.CommandResult.Command switch
		{
			var command when command == verifyMappings => new VerifyMappingsInvocation(parsedSqlitePath.FullName),

			var command when command == pages => new ConvertPagesInvocation(
				parsedSqlitePath.FullName,
				parseResult.GetValue(connectionString)!,
				parseResult.GetValue(imagesPath)!.FullName
			),

			var command when command == metadata => new ConvertMetadataInvocation(
				parsedSqlitePath.FullName,
				parseResult.GetValue(connectionString)!
			),

			_ => throw new ArgumentException("Specify a command: verify-mappings or convert."),
		};
	}
}
