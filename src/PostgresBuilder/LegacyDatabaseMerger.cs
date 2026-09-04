using Dapper;
using Microsoft.Data.Sqlite;

namespace CompleteNatGeo.PostgresBuilder;

public static class LegacyDatabaseMerger
{
	private static readonly IReadOnlyDictionary<string, string[]> RequiredColumns = new Dictionary<string, string[]>(
		StringComparer.Ordinal
	)
	{
		["issues"] =
		[
			"id",
			"display_name",
			"search_time",
			"page_count",
			"numbered_page_count",
			"numbered_page_offset",
			"numbered_page_start_value",
			"page_exceptions",
		],
		["locations"] =
		[
			"id",
			"display_name",
			"lat",
			"lng",
			"kms_per_lng_degree",
			"bounds_ne_lat",
			"bounds_ne_lng",
			"bounds_sw_lat",
			"bounds_sw_lng",
			"geo_provider",
		],
		["ads"] = ["id", "display_name", "search_time", "start_page_offset", "issue_id"],
		["articles"] = ["id", "display_name", "search_time", "start_page_offset", "summary", "page_count", "issue_id"],
		["departments"] = ["id", "display_name", "search_time", "start_page_offset", "summary", "issue_id"],
		["maps"] =
		[
			"id",
			"display_name",
			"search_time",
			"start_page_offset",
			"summary",
			"size",
			"scale",
			"filename",
			"issue_id",
		],
		["contributors"] =
		[
			"id",
			"display_name",
			"search_time",
			"start_page_offset",
			"article_id",
			"department_id",
			"map_id",
		],
		["ad_subjects"] = ["id", "display_name", "search_time", "start_page_offset", "ad_id"],
		["article_subjects"] = ["id", "display_name", "search_time", "start_page_offset", "article_id"],
		["department_subjects"] = ["id", "display_name", "search_time", "start_page_offset", "department_id"],
		["map_subjects"] = ["id", "display_name", "search_time", "start_page_offset", "map_id"],
		["geolinks"] = ["id", "location_id", "article_id", "department_id", "map_id"],
		["links"] = ["id", "display_name", "search_time", "start_page_offset", "article_id", "department_id", "map_id"],
		["photos"] = ["id", "start_page_offset", "article_id", "department_id", "map_id"],
		["photo_subjects"] = ["id", "display_name", "search_time", "start_page_offset", "photo_id"],
		["trivia_questions"] =
		[
			"id",
			"ng_id",
			"prompt",
			"answer1",
			"answer2",
			"answer3",
			"answer4",
			"link_text",
			"difficulty",
			"category",
			"search_time",
			"start_page_offset",
		],
		["trivia_rankings"] = ["id", "display_name", "min_value", "max_value"],
	};

	public static async Task MergeAsync(
		string baseSqlitePath,
		IReadOnlyList<string> sourceSqlitePaths,
		string outputSqlitePath,
		CancellationToken cancellationToken = default
	)
	{
		var inputPaths = ValidatePaths(baseSqlitePath, sourceSqlitePaths, outputSqlitePath);
		var inventories = new List<DatabaseInventory>(inputPaths.Count);
		foreach (var inputPath in inputPaths)
		{
			inventories.Add(await InspectDatabaseAsync(inputPath, cancellationToken));
		}

		ValidateUniqueIssues(inventories);
		DatabaseInventory? triviaProvider = ValidateTriviaOwnership(inventories);

		string outputPath = Path.GetFullPath(outputSqlitePath);
		string stagingPath = outputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			await CloneDatabaseAsync(inputPaths[0], stagingPath, cancellationToken);
			await MergeSourcesAsync(stagingPath, inputPaths.Skip(1), triviaProvider?.Path, cancellationToken);
			File.Move(stagingPath, outputPath);
		}
		catch
		{
			if (File.Exists(stagingPath))
			{
				File.Delete(stagingPath);
			}

			throw;
		}
	}

	private static List<string> ValidatePaths(
		string baseSqlitePath,
		IReadOnlyList<string> sourceSqlitePaths,
		string outputSqlitePath
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(baseSqlitePath);
		ArgumentNullException.ThrowIfNull(sourceSqlitePaths);
		ArgumentException.ThrowIfNullOrWhiteSpace(outputSqlitePath);
		if (sourceSqlitePaths.Count == 0)
		{
			throw new ArgumentException("Specify at least one --source-sqlite-path option.", nameof(sourceSqlitePaths));
		}

		var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
		var inputPaths = new List<string> { Path.GetFullPath(baseSqlitePath) };
		inputPaths.AddRange(sourceSqlitePaths.Select(Path.GetFullPath));
		string outputPath = Path.GetFullPath(outputSqlitePath);

		if (inputPaths.Any(path => !File.Exists(path)))
		{
			string missingPath = inputPaths.First(path => !File.Exists(path));
			throw new FileNotFoundException("SQLite input database was not found.", missingPath);
		}

		if (inputPaths.Distinct(pathComparer).Count() != inputPaths.Count)
		{
			throw new ArgumentException("Each SQLite input database must have a distinct path.");
		}

		if (inputPaths.Contains(outputPath, pathComparer))
		{
			throw new ArgumentException("The output database cannot also be an input database.");
		}

		if (File.Exists(outputPath))
		{
			File.Delete(outputPath);
		}

		string? outputDirectory = Path.GetDirectoryName(outputPath);
		if (string.IsNullOrEmpty(outputDirectory) || !Directory.Exists(outputDirectory))
		{
			throw new DirectoryNotFoundException($"The output directory does not exist: {outputDirectory}");
		}

		return inputPaths;
	}

	private static async Task<DatabaseInventory> InspectDatabaseAsync(
		string databasePath,
		CancellationToken cancellationToken
	)
	{
		await using var connection = CreateReadOnlyConnection(databasePath);
		await connection.OpenAsync(cancellationToken);
		await ValidateSchemaAsync(connection, databasePath, cancellationToken);
		await ValidateRelationshipsAsync(connection, databasePath, cancellationToken);

		var duplicateSearchTimes = (
			await connection.QueryAsync<long>(
				new CommandDefinition(
					"SELECT search_time FROM issues GROUP BY search_time HAVING COUNT(*) > 1;",
					cancellationToken: cancellationToken
				)
			)
		).ToList();
		if (duplicateSearchTimes.Count > 0)
		{
			throw new InvalidDataException(
				$"Database '{databasePath}' contains duplicate issue search_time values: "
					+ string.Join(", ", duplicateSearchTimes)
			);
		}

		long nullIssueSearchTimeCount = await connection.ExecuteScalarAsync<long>(
			new CommandDefinition(
				"SELECT COUNT(*) FROM issues WHERE search_time IS NULL;",
				cancellationToken: cancellationToken
			)
		);
		if (nullIssueSearchTimeCount > 0)
		{
			throw new InvalidDataException($"Database '{databasePath}' contains issues without search_time.");
		}

		var issueSearchTimes = (
			await connection.QueryAsync<long>(
				new CommandDefinition("SELECT search_time FROM issues;", cancellationToken: cancellationToken)
			)
		).ToList();
		bool hasTrivia =
			await connection.ExecuteScalarAsync<long>(
				new CommandDefinition(
					"SELECT (SELECT COUNT(*) FROM trivia_questions) + (SELECT COUNT(*) FROM trivia_rankings);",
					cancellationToken: cancellationToken
				)
			) > 0;

		return new DatabaseInventory(databasePath, issueSearchTimes, hasTrivia);
	}

	private static async Task ValidateSchemaAsync(
		SqliteConnection connection,
		string databasePath,
		CancellationToken cancellationToken
	)
	{
		var errors = new List<string>();
		foreach ((string table, string[] requiredColumns) in RequiredColumns)
		{
			var columns = new HashSet<string>(StringComparer.Ordinal);
			await using var command = connection.CreateCommand();
			command.CommandText = $"PRAGMA table_info({table});";
			await using var reader = await command.ExecuteReaderAsync(cancellationToken);
			while (await reader.ReadAsync(cancellationToken))
			{
				columns.Add(reader.GetString(1));
			}

			var missingColumns = requiredColumns.Where(column => !columns.Contains(column)).ToList();
			if (missingColumns.Count > 0)
			{
				errors.Add($"{table}: {string.Join(", ", missingColumns)}");
			}
		}

		if (errors.Count > 0)
		{
			throw new InvalidDataException(
				$"Database '{databasePath}' does not match the required legacy schema: " + string.Join("; ", errors)
			);
		}
	}

	private static async Task ValidateRelationshipsAsync(
		SqliteConnection connection,
		string databasePath,
		CancellationToken cancellationToken
	)
	{
		await ThrowForInvalidRowAsync(connection, "ads", "issue_id", "issues", databasePath, cancellationToken);
		await ThrowForInvalidRowAsync(connection, "articles", "issue_id", "issues", databasePath, cancellationToken);
		await ThrowForInvalidRowAsync(connection, "departments", "issue_id", "issues", databasePath, cancellationToken);
		await ThrowForInvalidRowAsync(connection, "maps", "issue_id", "issues", databasePath, cancellationToken);
		await ThrowForInvalidRowAsync(connection, "ad_subjects", "ad_id", "ads", databasePath, cancellationToken);
		await ThrowForInvalidRowAsync(
			connection,
			"article_subjects",
			"article_id",
			"articles",
			databasePath,
			cancellationToken
		);
		await ThrowForInvalidRowAsync(
			connection,
			"department_subjects",
			"department_id",
			"departments",
			databasePath,
			cancellationToken
		);
		await ThrowForInvalidRowAsync(connection, "map_subjects", "map_id", "maps", databasePath, cancellationToken);
		await ThrowForInvalidRowAsync(
			connection,
			"geolinks",
			"location_id",
			"locations",
			databasePath,
			cancellationToken
		);

		foreach (string table in new[] { "contributors", "geolinks", "links", "photos" })
		{
			await ThrowForInvalidVariantAsync(connection, table, databasePath, cancellationToken);
		}
	}

	private static async Task ThrowForInvalidRowAsync(
		SqliteConnection connection,
		string childTable,
		string childColumn,
		string parentTable,
		string databasePath,
		CancellationToken cancellationToken
	)
	{
		string sql = $"""
			SELECT child.id
			FROM {childTable} AS child
			LEFT JOIN {parentTable} AS parent ON parent.id = child.{childColumn}
			WHERE child.{childColumn} IS NULL OR parent.id IS NULL
			LIMIT 1;
			""";
		long? id = await connection.QuerySingleOrDefaultAsync<long?>(
			new CommandDefinition(sql, cancellationToken: cancellationToken)
		);
		if (id is not null)
		{
			throw new InvalidDataException(
				$"Database '{databasePath}' contains {childTable} row {id} with an invalid {childColumn}."
			);
		}
	}

	private static async Task ThrowForInvalidVariantAsync(
		SqliteConnection connection,
		string table,
		string databasePath,
		CancellationToken cancellationToken
	)
	{
		string sql = $"""
			SELECT id
			FROM {table}
			WHERE (article_id IS NOT NULL) + (department_id IS NOT NULL) + (map_id IS NOT NULL) <> 1
			LIMIT 1;
			""";
		long? invalidVariantId = await connection.QuerySingleOrDefaultAsync<long?>(
			new CommandDefinition(sql, cancellationToken: cancellationToken)
		);
		if (invalidVariantId is not null)
		{
			throw new InvalidDataException(
				$"Database '{databasePath}' contains {table} row {invalidVariantId} without exactly one parent."
			);
		}

		await ThrowForInvalidOptionalRowAsync(
			connection,
			table,
			"article_id",
			"articles",
			databasePath,
			cancellationToken
		);
		await ThrowForInvalidOptionalRowAsync(
			connection,
			table,
			"department_id",
			"departments",
			databasePath,
			cancellationToken
		);
		await ThrowForInvalidOptionalRowAsync(connection, table, "map_id", "maps", databasePath, cancellationToken);
	}

	private static async Task ThrowForInvalidOptionalRowAsync(
		SqliteConnection connection,
		string childTable,
		string childColumn,
		string parentTable,
		string databasePath,
		CancellationToken cancellationToken
	)
	{
		string sql = $"""
			SELECT child.id
			FROM {childTable} AS child
			LEFT JOIN {parentTable} AS parent ON parent.id = child.{childColumn}
			WHERE child.{childColumn} IS NOT NULL AND parent.id IS NULL
			LIMIT 1;
			""";
		long? id = await connection.QuerySingleOrDefaultAsync<long?>(
			new CommandDefinition(sql, cancellationToken: cancellationToken)
		);
		if (id is not null)
		{
			throw new InvalidDataException(
				$"Database '{databasePath}' contains {childTable} row {id} with an invalid {childColumn}."
			);
		}
	}

	private static void ValidateUniqueIssues(IEnumerable<DatabaseInventory> inventories)
	{
		var issueOrigins = new Dictionary<long, string>();
		foreach (var inventory in inventories)
		{
			foreach (long searchTime in inventory.IssueSearchTimes)
			{
				if (!issueOrigins.TryAdd(searchTime, inventory.Path))
				{
					throw new InvalidDataException(
						$"Issue search_time {searchTime} occurs in both '{issueOrigins[searchTime]}' and '{inventory.Path}'."
					);
				}
			}
		}
	}

	private static DatabaseInventory? ValidateTriviaOwnership(IEnumerable<DatabaseInventory> inventories)
	{
		var providers = inventories.Where(inventory => inventory.HasTrivia).ToList();
		if (providers.Count > 1)
		{
			throw new InvalidDataException(
				"Trivia items occur in more than one database: "
					+ string.Join(", ", providers.Select(provider => provider.Path))
			);
		}

		return providers.SingleOrDefault();
	}

	private static async Task CloneDatabaseAsync(
		string basePath,
		string stagingPath,
		CancellationToken cancellationToken
	)
	{
		await using var source = CreateReadOnlyConnection(basePath);
		await using var destination = CreateReadWriteConnection(stagingPath);
		await source.OpenAsync(cancellationToken);
		await destination.OpenAsync(cancellationToken);
		source.BackupDatabase(destination);
	}

	private static async Task MergeSourcesAsync(
		string stagingPath,
		IEnumerable<string> sourcePaths,
		string? triviaProviderPath,
		CancellationToken cancellationToken
	)
	{
		await using var connection = CreateReadWriteConnection(stagingPath);
		await connection.OpenAsync(cancellationToken);
		await connection.ExecuteAsync(
			new CommandDefinition("PRAGMA foreign_keys = ON;", cancellationToken: cancellationToken)
		);

		foreach (string sourcePath in sourcePaths)
		{
			await AttachSourceAsync(connection, sourcePath, cancellationToken);
			try
			{
				await using (
					var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken)
				)
				{
					await connection.ExecuteAsync(
						new CommandDefinition(
							CreateMapsSql,
							transaction: transaction,
							cancellationToken: cancellationToken
						)
					);
					await connection.ExecuteAsync(
						new CommandDefinition(
							MergeCoreSql,
							transaction: transaction,
							cancellationToken: cancellationToken
						)
					);
					if (string.Equals(sourcePath, triviaProviderPath, StringComparison.Ordinal))
					{
						await connection.ExecuteAsync(
							new CommandDefinition(
								CopyTriviaSql,
								transaction: transaction,
								cancellationToken: cancellationToken
							)
						);
					}

					await AssertMapCountAsync(connection, transaction, "issue_map", "src.issues", cancellationToken);
					await AssertMapCountAsync(
						connection,
						transaction,
						"location_map",
						"src.locations",
						cancellationToken
					);
					await AssertMapCountAsync(connection, transaction, "ad_map", "src.ads", cancellationToken);
					await AssertMapCountAsync(
						connection,
						transaction,
						"article_map",
						"src.articles",
						cancellationToken
					);
					await AssertMapCountAsync(
						connection,
						transaction,
						"department_map",
						"src.departments",
						cancellationToken
					);
					await AssertMapCountAsync(connection, transaction, "map_map", "src.maps", cancellationToken);
					await AssertMapCountAsync(connection, transaction, "photo_map", "src.photos", cancellationToken);
					await transaction.CommitAsync(cancellationToken);
				}
			}
			finally
			{
				await connection.ExecuteAsync(new CommandDefinition(DropMapsSql, cancellationToken: cancellationToken));
				await connection.ExecuteAsync(
					new CommandDefinition("DETACH DATABASE src;", cancellationToken: cancellationToken)
				);
			}
		}

		long? foreignKeyViolation = await connection.QuerySingleOrDefaultAsync<long?>(
			new CommandDefinition(
				"SELECT rowid FROM pragma_foreign_key_check LIMIT 1;",
				cancellationToken: cancellationToken
			)
		);
		if (foreignKeyViolation is not null)
		{
			throw new InvalidDataException("The merged database failed SQLite foreign-key validation.");
		}
	}

	private static async Task AttachSourceAsync(
		SqliteConnection connection,
		string sourcePath,
		CancellationToken cancellationToken
	)
	{
		await connection.ExecuteAsync(
			new CommandDefinition(
				"ATTACH DATABASE $sourcePath AS src;",
				new { sourcePath },
				cancellationToken: cancellationToken
			)
		);
	}

	private static async Task AssertMapCountAsync(
		SqliteConnection connection,
		SqliteTransaction transaction,
		string mapTable,
		string sourceTable,
		CancellationToken cancellationToken
	)
	{
		long mapCount = await connection.ExecuteScalarAsync<long>(
			new CommandDefinition(
				$"SELECT COUNT(*) FROM {mapTable};",
				transaction: transaction,
				cancellationToken: cancellationToken
			)
		);
		long sourceCount = await connection.ExecuteScalarAsync<long>(
			new CommandDefinition(
				$"SELECT COUNT(*) FROM {sourceTable};",
				transaction: transaction,
				cancellationToken: cancellationToken
			)
		);
		if (mapCount != sourceCount)
		{
			throw new InvalidDataException(
				$"Could not map every {sourceTable} row while merging: mapped {mapCount:N0} of {sourceCount:N0}."
			);
		}
	}

	private static SqliteConnection CreateReadOnlyConnection(string path)
	{
		return new SqliteConnection(
			new SqliteConnectionStringBuilder
			{
				DataSource = path,
				Mode = SqliteOpenMode.ReadOnly,
				Pooling = false,
			}.ToString()
		);
	}

	private static SqliteConnection CreateReadWriteConnection(string path)
	{
		return new SqliteConnection(
			new SqliteConnectionStringBuilder
			{
				DataSource = path,
				Mode = SqliteOpenMode.ReadWriteCreate,
				Pooling = false,
			}.ToString()
		);
	}

	private sealed record DatabaseInventory(string Path, IReadOnlyList<long> IssueSearchTimes, bool HasTrivia);

	private const string CreateMapsSql = """
		CREATE TEMP TABLE issue_map (source_id INTEGER PRIMARY KEY, target_id INTEGER NOT NULL);
		CREATE TEMP TABLE location_map (source_id INTEGER PRIMARY KEY, target_id INTEGER NOT NULL);
		CREATE TEMP TABLE ad_map (source_id INTEGER PRIMARY KEY, target_id INTEGER NOT NULL);
		CREATE TEMP TABLE article_map (source_id INTEGER PRIMARY KEY, target_id INTEGER NOT NULL);
		CREATE TEMP TABLE department_map (source_id INTEGER PRIMARY KEY, target_id INTEGER NOT NULL);
		CREATE TEMP TABLE map_map (source_id INTEGER PRIMARY KEY, target_id INTEGER NOT NULL);
		CREATE TEMP TABLE photo_map (source_id INTEGER PRIMARY KEY, target_id INTEGER NOT NULL);
		""";

	private const string DropMapsSql = """
		DROP TABLE IF EXISTS issue_map;
		DROP TABLE IF EXISTS location_map;
		DROP TABLE IF EXISTS ad_map;
		DROP TABLE IF EXISTS article_map;
		DROP TABLE IF EXISTS department_map;
		DROP TABLE IF EXISTS map_map;
		DROP TABLE IF EXISTS photo_map;
		""";

	private const string MergeCoreSql = """
		INSERT INTO main.issues (display_name, search_time, page_count, numbered_page_count, numbered_page_offset, numbered_page_start_value, page_exceptions)
		SELECT display_name, search_time, page_count, numbered_page_count, numbered_page_offset, numbered_page_start_value, page_exceptions
		FROM src.issues;

		INSERT INTO issue_map (source_id, target_id)
		SELECT source.id, target.id
		FROM src.issues AS source
		JOIN main.issues AS target ON target.search_time = source.search_time;

		INSERT INTO main.locations (display_name, lat, lng, kms_per_lng_degree, bounds_ne_lat, bounds_ne_lng, bounds_sw_lat, bounds_sw_lng, geo_provider)
		SELECT source.display_name, source.lat, source.lng, source.kms_per_lng_degree, source.bounds_ne_lat, source.bounds_ne_lng, source.bounds_sw_lat, source.bounds_sw_lng, source.geo_provider
		FROM src.locations AS source
		WHERE source.id = (
			SELECT MIN(same_location.id)
			FROM src.locations AS same_location
			WHERE same_location.lat IS source.lat
				AND same_location.lng IS source.lng
				AND same_location.display_name IS source.display_name
		)
		AND NOT EXISTS (
			SELECT 1 FROM main.locations AS target
			WHERE target.lat IS source.lat
				AND target.lng IS source.lng
				AND target.display_name IS source.display_name
		);

		INSERT INTO location_map (source_id, target_id)
		SELECT source.id, MIN(target.id)
		FROM src.locations AS source
		JOIN main.locations AS target
			ON target.lat IS source.lat
			AND target.lng IS source.lng
			AND target.display_name IS source.display_name
		GROUP BY source.id;

		INSERT INTO main.ads (display_name, search_time, start_page_offset, issue_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, issue_map.target_id
		FROM src.ads AS source
		JOIN issue_map ON issue_map.source_id = source.issue_id;

		INSERT INTO ad_map (source_id, target_id)
		SELECT source.id, MIN(target.id)
		FROM src.ads AS source
		JOIN issue_map ON issue_map.source_id = source.issue_id
		JOIN main.ads AS target ON target.issue_id = issue_map.target_id
			AND target.search_time = source.search_time
			AND target.start_page_offset IS source.start_page_offset
			AND target.display_name IS source.display_name
		GROUP BY source.id;

		INSERT INTO main.articles (display_name, search_time, start_page_offset, summary, page_count, issue_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, source.summary, source.page_count, issue_map.target_id
		FROM src.articles AS source
		JOIN issue_map ON issue_map.source_id = source.issue_id;

		INSERT INTO article_map (source_id, target_id)
		SELECT source.id, MIN(target.id)
		FROM src.articles AS source
		JOIN issue_map ON issue_map.source_id = source.issue_id
		JOIN main.articles AS target ON target.issue_id = issue_map.target_id
			AND target.search_time = source.search_time
			AND target.start_page_offset IS source.start_page_offset
			AND target.display_name IS source.display_name
		GROUP BY source.id;

		INSERT INTO main.departments (display_name, search_time, start_page_offset, summary, issue_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, source.summary, issue_map.target_id
		FROM src.departments AS source
		JOIN issue_map ON issue_map.source_id = source.issue_id;

		INSERT INTO department_map (source_id, target_id)
		SELECT source.id, MIN(target.id)
		FROM src.departments AS source
		JOIN issue_map ON issue_map.source_id = source.issue_id
		JOIN main.departments AS target ON target.issue_id = issue_map.target_id
			AND target.search_time = source.search_time
			AND target.start_page_offset IS source.start_page_offset
			AND target.display_name IS source.display_name
		GROUP BY source.id;

		INSERT INTO main.maps (display_name, search_time, start_page_offset, summary, size, scale, filename, issue_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, source.summary, source.size, source.scale, source.filename, issue_map.target_id
		FROM src.maps AS source
		JOIN issue_map ON issue_map.source_id = source.issue_id;

		INSERT INTO map_map (source_id, target_id)
		SELECT source.id, MIN(target.id)
		FROM src.maps AS source
		JOIN issue_map ON issue_map.source_id = source.issue_id
		JOIN main.maps AS target ON target.issue_id = issue_map.target_id
			AND target.search_time = source.search_time
			AND target.start_page_offset IS source.start_page_offset
			AND target.display_name IS source.display_name
		GROUP BY source.id;

		INSERT INTO main.contributors (display_name, search_time, start_page_offset, article_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, article_map.target_id
		FROM src.contributors AS source
		JOIN article_map ON article_map.source_id = source.article_id;

		INSERT INTO main.contributors (display_name, search_time, start_page_offset, department_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, department_map.target_id
		FROM src.contributors AS source
		JOIN department_map ON department_map.source_id = source.department_id;

		INSERT INTO main.contributors (display_name, search_time, start_page_offset, map_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, map_map.target_id
		FROM src.contributors AS source
		JOIN map_map ON map_map.source_id = source.map_id;

		INSERT INTO main.ad_subjects (display_name, search_time, start_page_offset, ad_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, ad_map.target_id
		FROM src.ad_subjects AS source
		JOIN ad_map ON ad_map.source_id = source.ad_id;

		INSERT INTO main.article_subjects (display_name, search_time, start_page_offset, article_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, article_map.target_id
		FROM src.article_subjects AS source
		JOIN article_map ON article_map.source_id = source.article_id;

		INSERT INTO main.department_subjects (display_name, search_time, start_page_offset, department_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, department_map.target_id
		FROM src.department_subjects AS source
		JOIN department_map ON department_map.source_id = source.department_id;

		INSERT INTO main.map_subjects (display_name, search_time, start_page_offset, map_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, map_map.target_id
		FROM src.map_subjects AS source
		JOIN map_map ON map_map.source_id = source.map_id;

		INSERT INTO main.geolinks (location_id, article_id)
		SELECT location_map.target_id, article_map.target_id
		FROM src.geolinks AS source
		JOIN location_map ON location_map.source_id = source.location_id
		JOIN article_map ON article_map.source_id = source.article_id;

		INSERT INTO main.geolinks (location_id, department_id)
		SELECT location_map.target_id, department_map.target_id
		FROM src.geolinks AS source
		JOIN location_map ON location_map.source_id = source.location_id
		JOIN department_map ON department_map.source_id = source.department_id;

		INSERT INTO main.geolinks (location_id, map_id)
		SELECT location_map.target_id, map_map.target_id
		FROM src.geolinks AS source
		JOIN location_map ON location_map.source_id = source.location_id
		JOIN map_map ON map_map.source_id = source.map_id;

		INSERT INTO main.links (display_name, search_time, start_page_offset, article_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, article_map.target_id
		FROM src.links AS source
		JOIN article_map ON article_map.source_id = source.article_id;

		INSERT INTO main.links (display_name, search_time, start_page_offset, department_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, department_map.target_id
		FROM src.links AS source
		JOIN department_map ON department_map.source_id = source.department_id;

		INSERT INTO main.links (display_name, search_time, start_page_offset, map_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, map_map.target_id
		FROM src.links AS source
		JOIN map_map ON map_map.source_id = source.map_id;

		INSERT INTO main.photos (start_page_offset, article_id)
		SELECT source.start_page_offset, article_map.target_id
		FROM src.photos AS source
		JOIN article_map ON article_map.source_id = source.article_id;

		INSERT INTO main.photos (start_page_offset, department_id)
		SELECT source.start_page_offset, department_map.target_id
		FROM src.photos AS source
		JOIN department_map ON department_map.source_id = source.department_id;

		INSERT INTO main.photos (start_page_offset, map_id)
		SELECT source.start_page_offset, map_map.target_id
		FROM src.photos AS source
		JOIN map_map ON map_map.source_id = source.map_id;

		INSERT INTO photo_map (source_id, target_id)
		SELECT source.id, MIN(target.id)
		FROM src.photos AS source
		LEFT JOIN article_map ON article_map.source_id = source.article_id
		LEFT JOIN department_map ON department_map.source_id = source.department_id
		LEFT JOIN map_map ON map_map.source_id = source.map_id
		JOIN main.photos AS target ON target.start_page_offset IS source.start_page_offset
			AND target.article_id IS article_map.target_id
			AND target.department_id IS department_map.target_id
			AND target.map_id IS map_map.target_id
		GROUP BY source.id;

		INSERT INTO main.photo_subjects (display_name, search_time, start_page_offset, photo_id)
		SELECT source.display_name, source.search_time, source.start_page_offset, photo_map.target_id
		FROM src.photo_subjects AS source
		LEFT JOIN photo_map ON photo_map.source_id = source.photo_id;
		""";

	private const string CopyTriviaSql = """
		INSERT INTO main.trivia_questions (ng_id, prompt, answer1, answer2, answer3, answer4, link_text, difficulty, category, search_time, start_page_offset)
		SELECT ng_id, prompt, answer1, answer2, answer3, answer4, link_text, difficulty, category, search_time, start_page_offset
		FROM src.trivia_questions;

		INSERT INTO main.trivia_rankings (display_name, min_value, max_value)
		SELECT display_name, min_value, max_value
		FROM src.trivia_rankings;
		""";
}
