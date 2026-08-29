namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record TriviaRanking
{
	public required int Id { get; init; }
	public required string DisplayName { get; init; }
	public required int MinValue { get; init; }
	public required int MaxValue { get; init; }
}
