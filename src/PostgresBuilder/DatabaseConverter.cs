using CompleteNatGeo.Data;
using CompleteNatGeo.PostgresBuilder.Utilities;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CompleteNatGeo.PostgresBuilder;

public static class DatabaseConverter
{
	public static async Task ConvertPagesAsync(
		SqliteConnection connection,
		string imagesPath,
		string postgresConnectionString
	)
	{
		await RecreateSchemaAsync(postgresConnectionString);
		await using var context = new CompleteNatGeoContext(postgresConnectionString);
		context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
		await context.Database.EnsureCreatedAsync();

		var legacyIssues = await connection.QueryAsync<LegacyModels.Issue>("SELECT * FROM issues order by search_time");
		foreach (var legacyIssue in legacyIssues)
		{
			var releaseDate = legacyIssue.SearchTime.ToDate();
			var decadeDir = $"{releaseDate.Year / 10}x";

			var pageImages = Directory
				.GetFiles(Path.Combine(imagesPath, decadeDir, legacyIssue.SearchTime.ToString()), "*.jpg")
				.Select(path => Path.GetFileNameWithoutExtension(path))
				.OrderBy(name => name)
				.ToArray();

			var issue = new Issue { ReleaseDate = releaseDate };

			foreach (var (pageImage, pageIndex) in pageImages.WithIndex())
			{
				issue.Pages.Add(
					new Page
					{
						FileName = pageImage,
						PageNumber = null,
						SortOrder = pageIndex,
					}
				);
			}

			context.Issues.Add(issue);
			await context.SaveChangesAsync();

			context.ChangeTracker.Clear();
		}
	}

	public static Task ConvertMetadataAsync(SqliteConnection connection, string postgresConnectionString)
	{
		return Task.CompletedTask;
	}

	private static async Task RecreateSchemaAsync(string postgresConnectionString)
	{
		await using var connection = new NpgsqlConnection(postgresConnectionString);
		await connection.OpenAsync();
		await using var command = connection.CreateCommand();
		command.CommandText = "DROP SCHEMA IF EXISTS \"CompleteNatGeo\" CASCADE; CREATE SCHEMA \"CompleteNatGeo\";";
		await command.ExecuteNonQueryAsync();
	}
}
