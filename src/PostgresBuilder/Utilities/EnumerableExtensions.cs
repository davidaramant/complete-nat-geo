namespace CompleteNatGeo.PostgresBuilder.Utilities;

public static class EnumerableExtensions
{
	public static IEnumerable<(T Value, int Index)> WithIndex<T>(this IEnumerable<T> source) =>
		source.Select((value, index) => (value, index));
}
