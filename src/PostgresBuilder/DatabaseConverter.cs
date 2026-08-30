using Microsoft.Data.Sqlite;

namespace CompleteNatGeo.PostgresBuilder;

public static class DatabaseConverter
{
	public static async Task ConvertPagesAsync(
		SqliteConnection connection,
		string imagesPath,
		string postgresConnectionString
	)
	{
		await Task.CompletedTask;
	}

	public static Task ConvertMetadataAsync(SqliteConnection connection, string postgresConnectionString)
	{
		return Task.CompletedTask;
	}
}
