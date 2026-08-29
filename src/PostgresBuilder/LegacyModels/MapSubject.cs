namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record MapSubject(int Id, string? DisplayName, int? SearchTime, int? StartPageOffset, int? MapId);
