namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record AdSubject
{
	public required int Id { get; init; }
	public required string DisplayName { get; init; }
	public required int SearchTime { get; init; }
	public required int StartPageOffset { get; init; }
	public required int AdId { get; init; }
}
