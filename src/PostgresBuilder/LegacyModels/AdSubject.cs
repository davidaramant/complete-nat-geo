namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record AdSubject(int Id, string? DisplayName, int? SearchTime, int? StartPageOffset, int? AdId);
