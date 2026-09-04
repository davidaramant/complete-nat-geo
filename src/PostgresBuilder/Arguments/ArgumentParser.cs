using System.CommandLine;
using System.CommandLine.Parsing;

namespace CompleteNatGeo.PostgresBuilder.Arguments;

static class ArgumentParser
{
	public static Invocation Parse(string[] args)
	{
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
		var pagesSqlitePath = CreateSqlitePathOption("Path to the SQLite database");
		pages.Options.Add(imagesPath);
		pages.Options.Add(pagesSqlitePath);

		var metadata = new Command("metadata", "Convert metadata only");
		var metadataSqlitePath = CreateSqlitePathOption("Path to the SQLite database");
		metadata.Options.Add(metadataSqlitePath);

		convert.Subcommands.Add(pages);
		convert.Subcommands.Add(metadata);

		var verifyMappings = new Command("verify-mappings", "Verify legacy mappings");
		var verifyMappingsSqlitePath = CreateSqlitePathOption("Path to the SQLite database");
		verifyMappings.Options.Add(verifyMappingsSqlitePath);
		var merge = new Command("merge", "Create a merged SQLite database");
		var mergeSqlitePath = CreateSqlitePathOption("Path to the base SQLite database");
		var sourceSqlitePaths = new Option<FileInfo[]>("--source-sqlite-path")
		{
			Required = true,
			AllowMultipleArgumentsPerToken = true,
			Description = "Path to an additional SQLite database; specify once for each source",
		};
		var outputSqlitePath = new Option<FileInfo>("--output-sqlite-path")
		{
			Required = true,
			Description = "Path for the new merged SQLite database",
		};
		merge.Options.Add(mergeSqlitePath);
		merge.Options.Add(sourceSqlitePaths);
		merge.Options.Add(outputSqlitePath);

		var root = new RootCommand("Convert the SQLite database into Postgres");
		root.Subcommands.Add(verifyMappings);
		root.Subcommands.Add(convert);
		root.Subcommands.Add(merge);

		ParseResult parseResult = root.Parse(args);
		if (args.Any(argument => argument is "--help" or "-h"))
		{
			parseResult.Invoke();
			return new HelpInvocation();
		}

		if (parseResult.Errors.Count > 0)
		{
			foreach (ParseError error in parseResult.Errors)
			{
				Console.Error.WriteLine(error.Message);
			}

			throw new ArgumentException("Invalid command-line arguments.");
		}

		return parseResult.CommandResult.Command switch
		{
			var command when command == merge => new MergeInvocation(
				parseResult.GetValue(mergeSqlitePath)!.FullName,
				parseResult.GetValue(sourceSqlitePaths)!.Select(path => path.FullName).ToList(),
				parseResult.GetValue(outputSqlitePath)!.FullName
			),

			var command when command == verifyMappings => new VerifyMappingsInvocation(
				parseResult.GetValue(verifyMappingsSqlitePath)!.FullName
			),

			var command when command == pages => new ConvertPagesInvocation(
				parseResult.GetValue(pagesSqlitePath)!.FullName,
				parseResult.GetValue(connectionString)!,
				parseResult.GetValue(imagesPath)!.FullName
			),

			var command when command == metadata => new ConvertMetadataInvocation(
				parseResult.GetValue(metadataSqlitePath)!.FullName,
				parseResult.GetValue(connectionString)!
			),

			_ => throw new ArgumentException("Specify a command: verify-mappings, merge, or convert."),
		};

		static Option<FileInfo> CreateSqlitePathOption(string description) =>
			new("--sqlite-path") { Required = true, Description = description };
	}
}
