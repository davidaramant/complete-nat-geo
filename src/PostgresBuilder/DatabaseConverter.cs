using System.Text.Json;
using CompleteNatGeo.PostgresBuilder.LegacyModels;
using CompleteNatGeo.PostgresBuilder.LegacyModels.PageExceptionsModel;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CompleteNatGeo.PostgresBuilder;

public static class DatabaseConverter
{
	public static async Task ConvertPagesAsync(SqliteConnection connection, string imagesPath)
	{
		await Task.CompletedTask;
	}

	public static Task ConvertMetadataAsync(SqliteConnection connection, string imagesPath)
	{
		return Task.CompletedTask;
	}
}
