using Microsoft.Data.Sqlite;

namespace CompleteNatGeo.PostgresBuilder;

public static class DatabaseConverter
{
	public static Task ConvertPagesAsync(SqliteConnection connection, string imagesPath)
	{
		return Task.CompletedTask;
	}

	public static Task ConvertMetadataAsync(SqliteConnection connection, string imagesPath)
	{
		return Task.CompletedTask;
	}
}
