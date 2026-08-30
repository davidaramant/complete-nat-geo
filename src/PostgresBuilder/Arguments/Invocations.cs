namespace CompleteNatGeo.PostgresBuilder.Arguments;

abstract record Invocation(string SqlitePath)
{
	public string SqliteConnectionString => $"Data Source={SqlitePath}";
}

sealed record VerifyMappingsInvocation(string SqlitePath) : Invocation(SqlitePath);

sealed record ConvertPagesInvocation(string SqlitePath, string PostgresConnectionString, string ImagesPath)
	: Invocation(SqlitePath);

sealed record ConvertMetadataInvocation(string SqlitePath, string PostgresConnectionString) : Invocation(SqlitePath);
