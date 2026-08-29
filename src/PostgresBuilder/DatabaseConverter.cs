using CompleteNatGeo.PostgresBuilder.LegacyModels;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CompleteNatGeo.PostgresBuilder;

public static class DatabaseConverter
{
	public static async Task VerifyMappingsAsync(SqliteConnection connection)
	{
		var issues = (await connection.QueryAsync<Issue>("SELECT * FROM issues")).ToList();

		await Console.Out.WriteLineAsync($"Issue count: {issues.Count:N0}");

		var ads = (await connection.QueryAsync<Ad>("SELECT * FROM ads")).ToList();

		await Console.Out.WriteLineAsync($"Ad count: {ads.Count:N0}");

		var adSubjects = (await connection.QueryAsync<AdSubject>("SELECT * FROM ad_subjects")).ToList();

		await Console.Out.WriteLineAsync($"AdSubject count: {adSubjects.Count:N0}");

		var articles = (await connection.QueryAsync<Article>("SELECT * FROM articles")).ToList();

		await Console.Out.WriteLineAsync($"Article count: {articles.Count:N0}");

		var articleSubjects = (await connection.QueryAsync<ArticleSubject>("SELECT * FROM article_subjects")).ToList();

		await Console.Out.WriteLineAsync($"ArticleSubject count: {articleSubjects.Count:N0}");

		var contributors = (await connection.QueryAsync<Contributor>("SELECT * FROM contributors")).ToList();

		await Console.Out.WriteLineAsync($"Contributor count: {contributors.Count:N0}");

		var departments = (await connection.QueryAsync<Department>("SELECT * FROM departments")).ToList();

		await Console.Out.WriteLineAsync($"Department count: {departments.Count:N0}");

		var departmentSubjects = (
			await connection.QueryAsync<DepartmentSubject>("SELECT * FROM department_subjects")
		).ToList();

		await Console.Out.WriteLineAsync($"DepartmentSubject count: {departmentSubjects.Count:N0}");

		var links = (await connection.QueryAsync<Link>("SELECT * FROM links")).ToList();

		await Console.Out.WriteLineAsync($"Link count: {links.Count:N0}");

		var geoLinks = (await connection.QueryAsync<GeoLink>("SELECT * FROM geolinks")).ToList();

		await Console.Out.WriteLineAsync($"GeoLink count: {geoLinks.Count:N0}");

		var locations = (await connection.QueryAsync<Location>("SELECT * FROM locations")).ToList();

		await Console.Out.WriteLineAsync($"Location count: {locations.Count:N0}");

		var maps = (await connection.QueryAsync<Map>("SELECT * FROM maps")).ToList();

		await Console.Out.WriteLineAsync($"Map count: {maps.Count:N0}");

		var mapSubjects = (await connection.QueryAsync<MapSubject>("SELECT * FROM map_subjects")).ToList();

		await Console.Out.WriteLineAsync($"MapSubject count: {mapSubjects.Count:N0}");

		var photos = (await connection.QueryAsync<Photo>("SELECT * FROM photos")).ToList();

		await Console.Out.WriteLineAsync($"Photo count: {photos.Count:N0}");

		var photoSubjects = (await connection.QueryAsync<PhotoSubject>("SELECT * FROM photo_subjects")).ToList();

		await Console.Out.WriteLineAsync($"PhotoSubject count: {photoSubjects.Count:N0}");

		var triviaQuestions = (await connection.QueryAsync<TriviaQuestion>("SELECT * FROM trivia_questions")).ToList();

		await Console.Out.WriteLineAsync($"TriviaQuestion count: {triviaQuestions.Count:N0}");

		var triviaRankings = (await connection.QueryAsync<TriviaRanking>("SELECT * FROM trivia_rankings")).ToList();

		await Console.Out.WriteLineAsync($"TriviaRanking count: {triviaRankings.Count:N0}");
	}

	public static async Task ConvertPagesAsync(SqliteConnection connection, string imagesPath)
	{
		await Task.CompletedTask;
	}

	public static Task ConvertMetadataAsync(SqliteConnection connection, string imagesPath)
	{
		return Task.CompletedTask;
	}
}
