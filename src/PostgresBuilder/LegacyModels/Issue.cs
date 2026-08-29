using System.Text.RegularExpressions;

namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Issue
{
	public required int Id { get; init; }
	public required string DisplayName { get; init; }
	public required int SearchTime { get; init; }
	public required int PageCount { get; init; }
	public required int NumberedPageOffset { get; init; }
	public required int NumberedPageCount { get; init; }
	public required int NumberedPageStartValue { get; init; }
	public required string PageExceptions { get; init; }

	public string PageExceptionsAsJson
	{
		get
		{
			var ngFormat = PageExceptions;
			if (ngFormat.StartsWith(";"))
			{
				ngFormat = ngFormat.Substring(1, ngFormat.Length - 1);
			}

			ngFormat = Regex.Replace(ngFormat, @"#:(\w+)=>?", @"""$1"": ");

			return $"{{{ngFormat.Replace('@', ',').Replace(';', ',')}}}";
		}
	}
}
