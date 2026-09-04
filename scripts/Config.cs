using dotenv.net;

namespace Scripts;

public sealed record Config(
	string CompleteSqlitePath,
	string SeparateSqlitePath,
	string ImagesPath,
	string PostgresUser,
	string PostgresPassword,
	string PostgresDbName,
	int PostgresPort
)
{
	public static Config LoadFromEnv()
	{
		var envVars = DotEnv.Read(options: new DotEnvOptions(envFilePaths: [Path.Combine(RepoPath.Root, ".env")]));

		return new Config(
			CompleteSqlitePath: envVars["CompleteSqlitePath"],
			SeparateSqlitePath: envVars["SeparateSqlitePath"],
			ImagesPath: envVars["ImagesPath"],
			PostgresUser: envVars["POSTGRES_USER"],
			PostgresPassword: envVars["POSTGRES_PASSWORD"],
			PostgresDbName: envVars["POSTGRES_DB"],
			PostgresPort: int.Parse(envVars["POSTGRES_PORT"])
		);
	}

	public string PostgresConnectionString =>
		$"Host=localhost;Port={PostgresPort};Database={PostgresDbName};Username={PostgresUser};Password={PostgresPassword};";
}
