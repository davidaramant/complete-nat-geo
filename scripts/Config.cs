using dotenv.net;

namespace Scripts;

public sealed record Config(string SqlitePath, string ImagesPath, string PostgresConnectionString)
{
	public static Config LoadFromEnv()
	{
		var envVars = DotEnv.Read(options: new DotEnvOptions(envFilePaths: [Path.Combine(RepoPath.Root, ".env")]));

		var sqlitePath = envVars["SqlitePath"];
		var imagesPath = envVars["ImagesPath"];
		var postgresConnectionString = envVars["PostgresConnectionString"];

		return new Config(sqlitePath, imagesPath, postgresConnectionString);
	}
}
