using CompleteNatGeo.PostgresBuilder;
using CompleteNatGeo.PostgresBuilder.Arguments;
using Microsoft.Data.Sqlite;

Invocation invocation = ArgumentParser.Parse(args);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

switch (invocation)
{
	case HelpInvocation:
		break;

	case MergeInvocation merge:
		await LegacyDatabaseMerger.MergeAsync(merge.BaseSqlitePath, merge.SourceSqlitePaths, merge.OutputSqlitePath);
		break;

	case LegacySqliteInvocation legacy:
		await using (var connection = new SqliteConnection(legacy.SqliteConnectionString))
		{
			await connection.OpenAsync();
			switch (legacy)
			{
				case VerifyMappingsInvocation:
					await LegacySchemaVerification.VerifyMappingsAsync(connection);
					break;

				case ConvertPagesInvocation pages:
					await DatabaseConverter.ConvertPagesAsync(
						connection,
						pages.ImagesPath,
						pages.PostgresConnectionString
					);
					await DatabaseConverter.ConvertMetadataAsync(connection, pages.PostgresConnectionString);
					break;

				case ConvertMetadataInvocation metadata:
					await DatabaseConverter.ConvertMetadataAsync(connection, metadata.PostgresConnectionString);
					break;
			}
		}
		break;
}
