using System.CommandLine;
using System.CommandLine.Parsing;
using ShellProgressBar;

Option<DirectoryInfo> cngOption = new("--cng-path")
{
	Description = "The base input path containing all the CNG files.",
};
Option<DirectoryInfo> outputOption = new("--output-path") { Description = "The output directory" };

RootCommand rootCommand = new("Sample app for System.CommandLine");
rootCommand.Options.Add(cngOption);
rootCommand.Options.Add(outputOption);

ParseResult parseResult = rootCommand.Parse(args);
if (
	parseResult.Errors.Count == 0
	&& parseResult.GetValue(cngOption) is { } inputPath
	&& parseResult.GetValue(outputOption) is { } outputPath
)
{
	ConvertCngs(inputPath, outputPath);
	return 0;
}
foreach (ParseError parseError in parseResult.Errors)
{
	Console.Error.WriteLine(parseError.Message);
}

return 1;

static void ConvertCngs(DirectoryInfo inputPath, DirectoryInfo outputPath)
{
	if (outputPath.Exists)
	{
		outputPath.Delete(recursive: true);
		outputPath.Create();
	}

	int totalCngs = Directory.GetFileSystemEntries(inputPath.FullName, "*.cng", SearchOption.AllDirectories).Length;
	var decades = Directory.GetDirectories(inputPath.FullName, "*", SearchOption.TopDirectoryOnly);
	Console.WriteLine($"Found {totalCngs:N0} CNG files in {decades.Length} decades.");

	var options = new ProgressBarOptions { DisplayTimeInRealTime = false };
	using var progress = new ProgressBar(totalCngs, "Converting CNG files...", options);

	Parallel.ForEach(
		decades,
		decadePath =>
		{
			const int bufferSize = 64 * 1024;
			var buffer = new byte[bufferSize];

			foreach (var issuePath in Directory.GetDirectories(decadePath))
			{
				var relativePath = Path.GetRelativePath(inputPath.FullName, issuePath);
				Directory.CreateDirectory(Path.Combine(outputPath.FullName, relativePath));

				foreach (var pagePath in Directory.GetFiles(issuePath, "*.cng"))
				{
					using var input = new FileStream(
						pagePath,
						FileMode.Open,
						FileAccess.Read,
						FileShare.Read,
						bufferSize,
						FileOptions.SequentialScan
					);
					using var output = File.Create(
						Path.Combine(
							outputPath.FullName,
							relativePath,
							Path.GetFileNameWithoutExtension(pagePath) + ".jpg"
						)
					);

					int bytesRead;
					while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
					{
						for (var index = 0; index < bytesRead; index++)
						{
							buffer[index] ^= 0xEF;
						}

						output.Write(buffer, 0, bytesRead);
					}

					progress.Tick();
				}
			}
		}
	);
}
