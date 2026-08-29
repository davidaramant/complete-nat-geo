using System.Text.Json;

namespace CompleteNatGeo.PostgresBuilder.LegacyModels.PageExceptionsModel;

public sealed record PageExceptions(
	string Basename,
	Correction[] Corrections,
	PageRun[] PageRuns,
	LargePage[] LargePages
)
{
	private static readonly JsonSerializerOptions SerializeOptions = new(JsonSerializerDefaults.Strict)
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	};

	public static PageExceptions Deserialize(string json) =>
		JsonSerializer.Deserialize<PageExceptions>(json, SerializeOptions)
		?? throw new ArgumentException("Could not deserialize PageExceptions");
}
