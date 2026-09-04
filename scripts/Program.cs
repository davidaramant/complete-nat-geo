using CliWrap;
using Scripts;
using static Bullseye.Targets;

var config = Config.LoadFromEnv();

Target("format", FormatAsync);
Target("clean", CleanAsync);
Target("build", BuildSolutionAsync);

Target("verify-schema", () => VerifySchemaAsync(config));
Target("merge", () => MergeLegacyDatabasesAsync(config));

await RunTargetsAndExitAsync(args);
return;

static Task FormatAsync() =>
	Cli.Wrap("dotnet").WithArguments("csharpier format .").WithWorkingDirectory(RepoPath.Root).ExecuteAsync();

static Task CleanAsync() => Cli.Wrap("dotnet").WithArguments(c => c.Add("clean").Add(RepoPath.Solution)).ExecuteAsync();

static Task BuildSolutionAsync() =>
	Cli.Wrap("dotnet")
		.WithArguments(c => c.Add("build").Add(RepoPath.Solution).Add("-c").Add("Release"))
		.ExecuteAsync();

static async Task VerifySchemaAsync(Config config)
{
	await using var stdOut = Console.OpenStandardOutput();

	await Cli.Wrap("dotnet")
		.WithArguments(c =>
			c.Add("run")
				.Add("--project")
				.Add(RepoPath.PostgresBuilderProject)
				.Add("-c")
				.Add("Release")
				.Add("--")
				.Add("verify-mappings")
				.Add("--sqlite-path")
				.Add(config.CompleteSqlitePath)
		)
		.WithStandardOutputPipe(PipeTarget.ToStream(stdOut))
		.ExecuteAsync();
}

static async Task MergeLegacyDatabasesAsync(Config config)
{
	await using var stdOut = Console.OpenStandardOutput();
	await using var stdErr = Console.OpenStandardError();

	var dbs = Directory.GetFiles(config.SeparateSqlitePath, "*.sqlite3").OrderBy(name => name).ToArray();

	await Cli.Wrap("dotnet")
		.WithArguments(c =>
		{
			c.Add("run")
				.Add("--project")
				.Add(RepoPath.PostgresBuilderProject)
				.Add("-c")
				.Add("Release")
				.Add("--")
				.Add("merge")
				.Add("--sqlite-path")
				.Add(dbs.First());

			foreach (var db in dbs[1..])
			{
				c.Add("--source-sqlite-path").Add(db);
			}

			c.Add("--output-sqlite-path").Add(config.CompleteSqlitePath);
		})
		.WithStandardOutputPipe(PipeTarget.ToStream(stdOut))
		.WithStandardErrorPipe(PipeTarget.ToStream(stdErr))
		.ExecuteAsync();
}
