using CompleteNatGeo.PostgresBuilder;
using CompleteNatGeo.PostgresBuilder.Arguments;
using Microsoft.Data.Sqlite;

Invocation invocation = ArgumentParser.Parse(args);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

await using var connection = new SqliteConnection(invocation.SqliteConnectionString);
connection.Open();

switch (invocation)
{
	case VerifyMappingsInvocation:
		await LegacySchemaVerification.VerifyMappingsAsync(connection);
		break;

	case ConvertPagesInvocation pages:
		await DatabaseConverter.ConvertPagesAsync(connection, pages.ImagesPath, pages.PostgresConnectionString);
		await DatabaseConverter.ConvertMetadataAsync(connection, pages.PostgresConnectionString);
		break;

	case ConvertMetadataInvocation metadata:
		await DatabaseConverter.ConvertMetadataAsync(connection, metadata.PostgresConnectionString);
		break;
}
