using CliWrap;
using Scripts;
using static Bullseye.Targets;

var config = Config.LoadFromEnv();

Target("format", FormatAsync);
Target("clean", CleanAsync);
Target("build", BuildSolutionAsync);

Target("verify-schema", () => VerifySchemaAsync(config));

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
				.Add("--sqlite-path")
				.Add(config.SqlitePath)
				.Add("verify-mappings")
		)
		.WithStandardOutputPipe(PipeTarget.ToStream(stdOut))
		.ExecuteAsync();
}
