namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Location(
	int Id,
	string DisplayName,
	double Lat,
	double Lng,
	double BoundsNeLat,
	double BoundsNeLng,
	double BoundsSwLat,
	double BoundsSwLng,
	string GeoProvider,
	double KmsPerLngDegree
);
