using CliWrap;
using Scripts;
using static Bullseye.Targets;

Target("format", FormatAsync);
Target("clean", CleanAsync);
Target("build", BuildSolutionAsync);

await RunTargetsAndExitAsync(args);
return;

static Task FormatAsync() =>
	Cli.Wrap("dotnet").WithArguments("csharpier format .").WithWorkingDirectory(RepoPath.Root).ExecuteAsync();

static Task CleanAsync() => Cli.Wrap("dotnet").WithArguments(c => c.Add("clean").Add(RepoPath.Solution)).ExecuteAsync();

static Task BuildSolutionAsync() =>
	Cli.Wrap("dotnet")
		.WithArguments(c => c.Add("build").Add(RepoPath.Solution).Add("-c").Add("Release"))
		.ExecuteAsync();
