using Microsoft.Data.Sqlite;

namespace CompleteNatGeo.PostgresBuilder.Tests;

public sealed class LegacyDatabaseMergerTests
{
	[Fact]
	public async Task MergeAsync_CopiesPhotoSubjectsWithoutMatchingPhotosWithNullPhotoId()
	{
		string directory = CreateTemporaryDirectory();
		try
		{
			string basePath = Path.Combine(directory, "base.sqlite3");
			string sourcePath = Path.Combine(directory, "source.sqlite3");
			string outputPath = Path.Combine(directory, "merged.sqlite3");
			await CreateDatabaseAsync(basePath);
			await CreateDatabaseAsync(sourcePath);
			await ExecuteInDatabaseAsync(
				sourcePath,
				"""
				INSERT INTO photo_subjects VALUES (1, 'Dangling photo subject', 201001, 1, 999);
				INSERT INTO photo_subjects VALUES (2, 'Null photo subject', 201001, 2, NULL);
				"""
			);

			await LegacyDatabaseMerger.MergeAsync(basePath, [sourcePath], outputPath);

			await using var output = OpenConnection(outputPath, SqliteOpenMode.ReadOnly);
			await output.OpenAsync();
			Assert.Equal(2, await CountAsync(output, "photo_subjects"));
			Assert.Equal(2, await CountAsync(output, "SELECT COUNT(*) FROM photo_subjects WHERE photo_id IS NULL;"));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public async Task MergeAsync_CopiesTheContentGraphAndReusesLocations()
	{
		string directory = CreateTemporaryDirectory();
		try
		{
			string basePath = Path.Combine(directory, "base.sqlite3");
			string sourcePath = Path.Combine(directory, "source.sqlite3");
			string secondSourcePath = Path.Combine(directory, "second-source.sqlite3");
			string outputPath = Path.Combine(directory, "merged.sqlite3");
			await CreateDatabaseAsync(basePath);
			await CreateDatabaseAsync(sourcePath);
			await CreateDatabaseAsync(secondSourcePath);
			await InsertBaseDataAsync(basePath);
			await InsertContentGraphAsync(sourcePath);
			await InsertIssueAsync(secondSourcePath, 50, 201101);

			await LegacyDatabaseMerger.MergeAsync(basePath, [sourcePath, secondSourcePath], outputPath);

			await using (var output = OpenConnection(outputPath, SqliteOpenMode.ReadOnly))
			{
				await output.OpenAsync();
				Assert.Equal(3, await CountAsync(output, "issues"));
				Assert.Equal(1, await CountAsync(output, "locations"));
				Assert.Equal(1, await CountAsync(output, "ads"));
				Assert.Equal(1, await CountAsync(output, "articles"));
				Assert.Equal(1, await CountAsync(output, "departments"));
				Assert.Equal(1, await CountAsync(output, "maps"));
				Assert.Equal(3, await CountAsync(output, "contributors"));
				Assert.Equal(3, await CountAsync(output, "geolinks"));
				Assert.Equal(3, await CountAsync(output, "links"));
				Assert.Equal(3, await CountAsync(output, "photos"));
				Assert.Equal(3, await CountAsync(output, "photo_subjects"));
				Assert.Equal(1, await CountAsync(output, "trivia_questions"));
				Assert.Equal(1, await CountAsync(output, "trivia_rankings"));
				Assert.Equal(
					3,
					await CountAsync(
						output,
						"SELECT COUNT(*) FROM photo_subjects AS subject JOIN photos AS photo ON photo.id = subject.photo_id;"
					)
				);
				Assert.Equal(
					0,
					await CountAsync(
						output,
						"SELECT COUNT(*) FROM pragma_table_info('photos') WHERE name = 'src_photo_id';"
					)
				);
			}

			await using (var source = OpenConnection(sourcePath, SqliteOpenMode.ReadOnly))
			{
				await source.OpenAsync();
				Assert.Equal(1, await CountAsync(source, "issues"));
				Assert.Equal(3, await CountAsync(source, "photos"));
			}
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public async Task MergeAsync_CopiesTriviaFromTheOnlyTriviaBearingSource()
	{
		string directory = CreateTemporaryDirectory();
		try
		{
			string basePath = Path.Combine(directory, "base.sqlite3");
			string sourcePath = Path.Combine(directory, "source.sqlite3");
			string outputPath = Path.Combine(directory, "merged.sqlite3");
			await CreateDatabaseAsync(basePath);
			await CreateDatabaseAsync(sourcePath);
			await InsertTriviaAsync(sourcePath, 7);

			await LegacyDatabaseMerger.MergeAsync(basePath, [sourcePath], outputPath);

			await using (var output = OpenConnection(outputPath, SqliteOpenMode.ReadOnly))
			{
				await output.OpenAsync();
				Assert.Equal(1, await CountAsync(output, "trivia_questions"));
				Assert.Equal(1, await CountAsync(output, "trivia_rankings"));
			}
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public async Task MergeAsync_RejectsDuplicateIssuesWithoutCreatingOutput()
	{
		string directory = CreateTemporaryDirectory();
		try
		{
			string basePath = Path.Combine(directory, "base.sqlite3");
			string sourcePath = Path.Combine(directory, "source.sqlite3");
			string outputPath = Path.Combine(directory, "merged.sqlite3");
			await CreateDatabaseAsync(basePath);
			await CreateDatabaseAsync(sourcePath);
			await InsertIssueAsync(basePath, 1, 200901);
			await InsertIssueAsync(sourcePath, 2, 200901);

			InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
				LegacyDatabaseMerger.MergeAsync(basePath, [sourcePath], outputPath)
			);

			Assert.Contains("200901", exception.Message, StringComparison.Ordinal);
			Assert.False(File.Exists(outputPath));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public async Task MergeAsync_RejectsTriviaInMoreThanOneDatabaseWithoutCreatingOutput()
	{
		string directory = CreateTemporaryDirectory();
		try
		{
			string basePath = Path.Combine(directory, "base.sqlite3");
			string sourcePath = Path.Combine(directory, "source.sqlite3");
			string outputPath = Path.Combine(directory, "merged.sqlite3");
			await CreateDatabaseAsync(basePath);
			await CreateDatabaseAsync(sourcePath);
			await InsertTriviaAsync(basePath, 1);
			await InsertTriviaAsync(sourcePath, 2);

			InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
				LegacyDatabaseMerger.MergeAsync(basePath, [sourcePath], outputPath)
			);

			Assert.Contains("Trivia", exception.Message, StringComparison.Ordinal);
			Assert.False(File.Exists(outputPath));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static string CreateTemporaryDirectory()
	{
		string directory = Path.Combine(Path.GetTempPath(), "CompleteNatGeo", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		return directory;
	}

	private static SqliteConnection OpenConnection(string path, SqliteOpenMode mode)
	{
		return new SqliteConnection(
			new SqliteConnectionStringBuilder
			{
				DataSource = path,
				Mode = mode,
				Pooling = false,
			}.ToString()
		);
	}

	private static async Task CreateDatabaseAsync(string path)
	{
		await using var connection = OpenConnection(path, SqliteOpenMode.ReadWriteCreate);
		await connection.OpenAsync();
		await ExecuteAsync(
			connection,
			"""
			CREATE TABLE issues (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, page_count INTEGER NOT NULL, numbered_page_count INTEGER NOT NULL, numbered_page_offset INTEGER NOT NULL, numbered_page_start_value INTEGER NOT NULL, page_exceptions TEXT NOT NULL);
			CREATE TABLE locations (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, lat REAL NOT NULL, lng REAL NOT NULL, kms_per_lng_degree REAL NOT NULL, bounds_ne_lat REAL NOT NULL, bounds_ne_lng REAL NOT NULL, bounds_sw_lat REAL NOT NULL, bounds_sw_lng REAL NOT NULL, geo_provider TEXT NOT NULL);
			CREATE TABLE ads (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, issue_id INTEGER NOT NULL);
			CREATE TABLE articles (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, summary TEXT NOT NULL, page_count INTEGER NOT NULL, issue_id INTEGER NOT NULL);
			CREATE TABLE departments (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, summary TEXT NOT NULL, issue_id INTEGER NOT NULL);
			CREATE TABLE maps (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, summary TEXT NOT NULL, size TEXT NOT NULL, scale TEXT NOT NULL, filename TEXT NOT NULL, issue_id INTEGER NOT NULL);
			CREATE TABLE contributors (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, article_id INTEGER, department_id INTEGER, map_id INTEGER);
			CREATE TABLE ad_subjects (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, ad_id INTEGER NOT NULL);
			CREATE TABLE article_subjects (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, article_id INTEGER NOT NULL);
			CREATE TABLE department_subjects (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, department_id INTEGER NOT NULL);
			CREATE TABLE map_subjects (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, map_id INTEGER NOT NULL);
			CREATE TABLE geolinks (id INTEGER PRIMARY KEY, location_id INTEGER NOT NULL, article_id INTEGER, department_id INTEGER, map_id INTEGER);
			CREATE TABLE links (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, article_id INTEGER, department_id INTEGER, map_id INTEGER);
			CREATE TABLE photos (id INTEGER PRIMARY KEY, start_page_offset INTEGER, article_id INTEGER, department_id INTEGER, map_id INTEGER);
			CREATE TABLE photo_subjects (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER, photo_id INTEGER);
			CREATE TABLE trivia_questions (id INTEGER PRIMARY KEY, ng_id TEXT NOT NULL, prompt TEXT NOT NULL, answer1 TEXT NOT NULL, answer2 TEXT NOT NULL, answer3 TEXT NOT NULL, answer4 TEXT NOT NULL, link_text TEXT NOT NULL, difficulty TEXT NOT NULL, category TEXT NOT NULL, search_time INTEGER NOT NULL, start_page_offset INTEGER NOT NULL);
			CREATE TABLE trivia_rankings (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL, min_value INTEGER NOT NULL, max_value INTEGER NOT NULL);
			"""
		);
	}

	private static async Task InsertBaseDataAsync(string path)
	{
		await InsertIssueAsync(path, 1, 200901);
		await using var connection = OpenConnection(path, SqliteOpenMode.ReadWrite);
		await connection.OpenAsync();
		await ExecuteAsync(
			connection,
			"""
			INSERT INTO locations VALUES (1, 'Shared', 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 'legacy');
			INSERT INTO trivia_questions VALUES (1, 'base-question', 'Prompt', 'A', 'B', 'C', 'D', 'More', 'Easy', 'Nature', 200901, 1);
			INSERT INTO trivia_rankings VALUES (1, 'Explorer', 0, 10);
			"""
		);
	}

	private static async Task ExecuteInDatabaseAsync(string path, string sql)
	{
		await using var connection = OpenConnection(path, SqliteOpenMode.ReadWrite);
		await connection.OpenAsync();
		await ExecuteAsync(connection, sql);
	}

	private static async Task InsertContentGraphAsync(string path)
	{
		await InsertIssueAsync(path, 42, 201001);
		await using var connection = OpenConnection(path, SqliteOpenMode.ReadWrite);
		await connection.OpenAsync();
		await ExecuteAsync(
			connection,
			"""
			INSERT INTO locations VALUES (2, 'Shared', 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 'legacy');
			INSERT INTO ads VALUES (10, 'Ad', 201001, 1, 42);
			INSERT INTO articles VALUES (20, 'Article', 201001, 2, 'Summary', 4, 42);
			INSERT INTO departments VALUES (30, 'Department', 201001, NULL, 'Summary', 42);
			INSERT INTO maps VALUES (40, 'Map', 201001, NULL, 'Summary', 'Large', '1:1', 'map.jpg', 42);
			INSERT INTO contributors VALUES (1, 'Article contributor', 201001, 1, 20, NULL, NULL);
			INSERT INTO contributors VALUES (2, 'Department contributor', 201001, 1, NULL, 30, NULL);
			INSERT INTO contributors VALUES (3, 'Map contributor', 201001, 1, NULL, NULL, 40);
			INSERT INTO ad_subjects VALUES (1, 'Ad subject', 201001, 1, 10);
			INSERT INTO article_subjects VALUES (1, 'Article subject', 201001, 1, 20);
			INSERT INTO department_subjects VALUES (1, 'Department subject', 201001, 1, 30);
			INSERT INTO map_subjects VALUES (1, 'Map subject', 201001, 1, 40);
			INSERT INTO geolinks VALUES (1, 2, 20, NULL, NULL);
			INSERT INTO geolinks VALUES (2, 2, NULL, 30, NULL);
			INSERT INTO geolinks VALUES (3, 2, NULL, NULL, 40);
			INSERT INTO links VALUES (1, 'Article link', 201001, 1, 20, NULL, NULL);
			INSERT INTO links VALUES (2, 'Department link', 201001, 1, NULL, 30, NULL);
			INSERT INTO links VALUES (3, 'Map link', 201001, 1, NULL, NULL, 40);
			INSERT INTO photos VALUES (90, 1, 20, NULL, NULL);
			INSERT INTO photos VALUES (91, 2, NULL, 30, NULL);
			INSERT INTO photos VALUES (92, 3, NULL, NULL, 40);
			INSERT INTO photo_subjects VALUES (1, 'Article photo subject', 201001, 1, 90);
			INSERT INTO photo_subjects VALUES (2, 'Department photo subject', 201001, 2, 91);
			INSERT INTO photo_subjects VALUES (3, 'Map photo subject', 201001, 3, 92);
			"""
		);
	}

	private static async Task InsertIssueAsync(string path, long id, long searchTime)
	{
		await using var connection = OpenConnection(path, SqliteOpenMode.ReadWrite);
		await connection.OpenAsync();
		await ExecuteAsync(
			connection,
			$"INSERT INTO issues VALUES ({id}, 'Issue {searchTime}', {searchTime}, 10, 10, 0, 1, '');"
		);
	}

	private static async Task InsertTriviaAsync(string path, long id)
	{
		await using var connection = OpenConnection(path, SqliteOpenMode.ReadWrite);
		await connection.OpenAsync();
		await ExecuteAsync(
			connection,
			$"""
			INSERT INTO trivia_questions VALUES ({id}, 'question-{id}', 'Prompt', 'A', 'B', 'C', 'D', 'More', 'Easy', 'Nature', {id}, 1);
			INSERT INTO trivia_rankings VALUES ({id}, 'Rank {id}', 0, 10);
			"""
		);
	}

	private static async Task<long> CountAsync(SqliteConnection connection, string tableOrSql)
	{
		string sql = tableOrSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
			? tableOrSql
			: $"SELECT COUNT(*) FROM {tableOrSql};";
		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		return Convert.ToInt64(await command.ExecuteScalarAsync());
	}

	private static async Task ExecuteAsync(SqliteConnection connection, string sql)
	{
		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		await command.ExecuteNonQueryAsync();
	}
}
