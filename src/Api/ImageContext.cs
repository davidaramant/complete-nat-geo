namespace Api;

internal interface IImageContext
{
	string GetUrl(DateOnly date, string fileName);
}

internal sealed class ImageContext : IImageContext
{
	private readonly string _imagesBaseUrl;

	public ImageContext(IConfiguration configuration)
	{
		_imagesBaseUrl =
			configuration["IMAGES_BASE_URL"]?.TrimEnd('/')
			?? throw new ArgumentException("IMAGES_BASE_URL is not configured");
	}

	public string GetUrl(DateOnly date, string fileName) =>
		$"{_imagesBaseUrl}/{date.Year / 10}x/{date:yyyyMMdd}/{fileName}.jpg";
}
