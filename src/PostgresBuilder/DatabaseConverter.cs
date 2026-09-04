using CompleteNatGeo.Data;
using CompleteNatGeo.PostgresBuilder.Utilities;
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
		await using var context = new CompleteNatGeoContext(postgresConnectionString);
		var legacyIssues = await connection.QueryAsync<LegacyModels.Issue>(
			"SELECT * FROM issues order by search_time desc"
		);
		var batch = new List<Page>();
		foreach (var legacyIssue in legacyIssues)
		{
			var releaseDate = legacyIssue.SearchTime.ToDate();
			var decadeDir = $"{releaseDate.Year - releaseDate.Year % 10}s";

			var pageImages = Directory
				.GetFiles(Path.Combine(imagesPath, decadeDir, legacyIssue.SearchTime.ToString(), "*.jpg"))
				.Select(path => Path.GetRelativePath(imagesPath, path))
				.OrderBy(name => name)
				.ToArray();

			foreach (var pageImage in pageImages)
			{
				batch.Add(
					new Page
					{
						IssueDate = releaseDate,
						FileName = pageImage,
						PageNumber = null,
						SortOrder = 0,
					}
				);
			}

			context.Pages.AddRange(batch);
			await context.SaveChangesAsync();

			context.ChangeTracker.Clear();
			batch.Clear();
		}
	}

	public static Task ConvertMetadataAsync(SqliteConnection connection, string postgresConnectionString)
	{
		return Task.CompletedTask;
	}
}
