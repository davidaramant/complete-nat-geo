using ShellProgressBar;

namespace CompleteNatGeo.ThumbnailGenerator;

public sealed class ThumbnailService
{
	private readonly DockerRunner _dockerRunner;

	public ThumbnailService(DockerRunner dockerRunner)
	{
		_dockerRunner = dockerRunner;
	}

	public async Task GenerateThumbnailsAsync(
		DirectoryInfo rootDirectory,
		string imageName = DockerRunner.DefaultImageName,
		int? maxDegreeOfParallelism = null,
		CancellationToken cancellationToken = default
	)
	{
		if (!rootDirectory.Exists)
		{
			throw new DirectoryNotFoundException($"Image directory does not exist: {rootDirectory.FullName}");
		}

		var allJpgFiles = Directory.EnumerateFiles(rootDirectory.FullName, "*.*", SearchOption.AllDirectories)
			.Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
			.ToList();

		if (allJpgFiles.Count == 0)
		{
			Console.WriteLine($"No JPG files found in {rootDirectory.FullName}.");
			return;
		}

		var directoriesWithJpgs = allJpgFiles
			.GroupBy(Path.GetDirectoryName)
			.Where(g => g.Key is not null)
			.Select(g => new DirectoryBatch(g.Key!, g.Count()))
			.ToList();

		Console.WriteLine($"Found {allJpgFiles.Count:N0} JPG files across {directoriesWithJpgs.Count:N0} directories.");

		var options = new ProgressBarOptions { DisplayTimeInRealTime = false };
		using var progress = new ProgressBar(allJpgFiles.Count, "Generating thumbnails...", options);

		var parallelOptions = new ParallelOptions
		{
			CancellationToken = cancellationToken,
			MaxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount,
		};

		await Parallel.ForEachAsync(
			directoriesWithJpgs,
			parallelOptions,
			async (batch, ct) =>
			{
				await _dockerRunner.ConvertDirectoryAsync(batch.DirectoryPath, imageName, ct);
				for (var i = 0; i < batch.FileCount; i++)
				{
					progress.Tick();
				}
			}
		);
	}

	public sealed record DirectoryBatch(string DirectoryPath, int FileCount);
}
