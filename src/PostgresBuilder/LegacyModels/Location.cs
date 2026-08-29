namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Location
{
	public required int Id { get; init; }
	public required string DisplayName { get; init; }
	public double Lat { get; init; }
	public double Lng { get; init; }
	public double BoundsNeLat { get; init; }
	public double BoundsNeLng { get; init; }
	public double BoundsSwLat { get; init; }
	public double BoundsSwLng { get; init; }
	public required string GeoProvider { get; init; }
	public double KmsPerLngDegree { get; init; }
}
