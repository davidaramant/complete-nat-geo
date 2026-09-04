namespace CompleteNatGeo.PostgresBuilder.Arguments;

abstract record Invocation;

sealed record HelpInvocation : Invocation;

abstract record LegacySqliteInvocation(string SqlitePath) : Invocation
{
	public string SqliteConnectionString => $"Data Source={SqlitePath}";
}

sealed record VerifyMappingsInvocation(string SqlitePath) : LegacySqliteInvocation(SqlitePath);

sealed record ConvertPagesInvocation(string SqlitePath, string PostgresConnectionString, string ImagesPath)
	: LegacySqliteInvocation(SqlitePath);

sealed record ConvertMetadataInvocation(string SqlitePath, string PostgresConnectionString)
	: LegacySqliteInvocation(SqlitePath);

sealed record MergeInvocation(string BaseSqlitePath, IReadOnlyList<string> SourceSqlitePaths, string OutputSqlitePath)
	: Invocation;
