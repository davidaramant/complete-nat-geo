using dotenv.net;

namespace Scripts;

public sealed record Config(
	string CombinedSqlitePath,
	string SeparateSqlitePath,
	string ImagesPath,
	string PostgresHost,
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
			CombinedSqlitePath: envVars["COMBINED_SQLITE_PATH"],
			SeparateSqlitePath: envVars["SEPARATE_SQLITE_PATH"],
			ImagesPath: envVars["IMAGES_PATH"],
			PostgresHost: envVars["POSTGRES_HOST"],
			PostgresUser: envVars["POSTGRES_USER"],
			PostgresPassword: envVars["POSTGRES_PASSWORD"],
			PostgresDbName: envVars["POSTGRES_DB"],
			PostgresPort: int.Parse(envVars["POSTGRES_PORT"])
		);
	}

	public string PostgresConnectionString =>
		$"Host={PostgresHost};Port={PostgresPort};Database={PostgresDbName};Username={PostgresUser};Password={PostgresPassword};";
}
