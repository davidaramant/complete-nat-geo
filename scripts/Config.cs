using dotenv.net;

namespace Scripts;

public sealed record Config(string CompleteSqlitePath, string SeparateSqlitePath, string ImagesPath, string PostgresConnectionString)
{
	public static Config LoadFromEnv()
	{
		var envVars = DotEnv.Read(options: new DotEnvOptions(envFilePaths: [Path.Combine(RepoPath.Root, ".env")]));

		var completeSqlitePath = envVars["CompleteSqlitePath"];
		var separateSqlitePath = envVars["SeparateSqlitePath"];
		var imagesPath = envVars["ImagesPath"];
		var postgresConnectionString = envVars["PostgresConnectionString"];

		return new Config(completeSqlitePath, separateSqlitePath, imagesPath, postgresConnectionString);
	}
}
