using System.CommandLine;
using System.CommandLine.Parsing;
using CompleteNatGeo.ThumbnailGenerator;

Option<DirectoryInfo> imagePathOption = new("--image-path") { Description = "The image directory" };

RootCommand rootCommand = new("Thumbnail generator");
rootCommand.Options.Add(imagePathOption);

ParseResult parseResult = rootCommand.Parse(args);
if (parseResult.Errors.Count == 0 && parseResult.GetValue(imagePathOption) is { } imagePath)
{
	await CreateThumbnailsAsync(imagePath);
	return 0;
}
foreach (ParseError parseError in parseResult.Errors)
{
	Console.Error.WriteLine(parseError.Message);
}

return 1;

static async Task CreateThumbnailsAsync(DirectoryInfo imagePath)
{
	var dockerfilePath = FindDockerfilePath();
	Console.WriteLine($"Building ImageMagick Docker image from {dockerfilePath}...");

	var dockerRunner = new DockerRunner();
	await dockerRunner.BuildImageAsync(dockerfilePath);

	var thumbnailService = new ThumbnailService(dockerRunner);
	await thumbnailService.GenerateThumbnailsAsync(imagePath);
}

static string FindDockerfilePath()
{
	string[] searchPaths =
	[
		Path.Combine(AppContext.BaseDirectory, "imagemagick.Dockerfile"),
		Path.Combine(Directory.GetCurrentDirectory(), "imagemagick.Dockerfile"),
		Path.Combine(Directory.GetCurrentDirectory(), "ThumbnailGenerator", "imagemagick.Dockerfile"),
		Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "imagemagick.Dockerfile"),
	];

	foreach (var path in searchPaths)
	{
		var fullPath = Path.GetFullPath(path);
		if (File.Exists(fullPath))
		{
			return fullPath;
		}
	}

	throw new FileNotFoundException("Could not find imagemagick.Dockerfile in output or source directories.");
}
