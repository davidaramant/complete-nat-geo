using Dapper;
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
		var legacyIssues = await connection.QueryAsync<LegacyModels.Issue>(
			"SELECT * FROM issues order by search_time desc"
		);
		foreach (var legacyIssue in legacyIssues)
		{
			// TODO: expand to Page
		}

		await Task.CompletedTask;
	}

	public static Task ConvertMetadataAsync(SqliteConnection connection, string postgresConnectionString)
	{
		return Task.CompletedTask;
	}
}
