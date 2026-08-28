namespace CompleteNatGeo.PostgresBuilder;

public sealed record InputsConfig(string SqlitePath, string ImagesPath)
{
	public string SqliteConnectionString => $"Data Source={SqlitePath}";
}
