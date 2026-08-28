using Microsoft.Data.Sqlite;

namespace CompleteNatGeo.PostgresBuilder;

public static class DatabaseConverter
{
	public static Task ConvertAsync(SqliteConnection connection, string imagesPath)
	{
		return Task.CompletedTask;
	}
}
