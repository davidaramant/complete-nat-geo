namespace CompleteNatGeo.ThumbnailGenerator.Tests;

public sealed class ThumbnailServiceTests
{
	private static readonly byte[] ValidJpgBytes = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgICAgMCAgIDAwMDBAYEBAQEBAgGBgUGCQgKCgkICQkKDA8MCgsOCwkJDRENDg8QEBEQCgwSExIQEw8QEBD/2wBDAQMDAwQDBAgEBAgQCwkLEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBD/wAARCAAKAAoDAREAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAn/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFgEBAQEAAAAAAAAAAAAAAAAAAAYJ/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8Anu1TQ4AAD//Z"
	);

	[Test]
	public async Task ShouldGenerateThumbnailsRecursivelyAndOverwriteExisting()
	{
		var tempRoot = Path.Combine(Path.GetTempPath(), "natgeo_thumb_test_" + Guid.NewGuid());
		try
		{
			var subDir1 = Path.Combine(tempRoot, "1970s", "1970-01");
			var subDir2 = Path.Combine(tempRoot, "1980s", "1980-05");
			Directory.CreateDirectory(subDir1);
			Directory.CreateDirectory(subDir2);

			var file1 = Path.Combine(subDir1, "page001.jpg");
			var file2 = Path.Combine(subDir1, "page002.jpeg");
			var file3 = Path.Combine(subDir2, "cover image.JPG");

			await File.WriteAllBytesAsync(file1, ValidJpgBytes);
			await File.WriteAllBytesAsync(file2, ValidJpgBytes);
			await File.WriteAllBytesAsync(file3, ValidJpgBytes);

			// Pre-create an old thumbnail to verify overwriting
			var thumb1 = Path.Combine(subDir1, "page001.thumb.webp");
			await File.WriteAllTextAsync(thumb1, "old-dummy-content");

			var dockerRunner = new DockerRunner();
			var dockerfilePath = FindDockerfilePath();
			await dockerRunner.BuildImageAsync(dockerfilePath);

			var service = new ThumbnailService(dockerRunner);
			await service.GenerateThumbnailsAsync(new DirectoryInfo(tempRoot));

			var thumb2 = Path.Combine(subDir1, "page002.thumb.webp");
			var thumb3 = Path.Combine(subDir2, "cover image.thumb.webp");

			await Assert.That(File.Exists(thumb1)).IsTrue();
			await Assert.That(File.Exists(thumb2)).IsTrue();
			await Assert.That(File.Exists(thumb3)).IsTrue();

			// Verify old content was overwritten with valid WebP binary data
			var thumb1Bytes = await File.ReadAllBytesAsync(thumb1);
			await Assert.That(thumb1Bytes.Length).IsGreaterThan(10);
			// WebP RIFF header check: 'R' 'I' 'F' 'F' ... 'W' 'E' 'B' 'P'
			await Assert.That(thumb1Bytes[0]).IsEqualTo((byte)'R');
			await Assert.That(thumb1Bytes[1]).IsEqualTo((byte)'I');
			await Assert.That(thumb1Bytes[2]).IsEqualTo((byte)'F');
			await Assert.That(thumb1Bytes[3]).IsEqualTo((byte)'F');
			await Assert.That(thumb1Bytes[8]).IsEqualTo((byte)'W');
			await Assert.That(thumb1Bytes[9]).IsEqualTo((byte)'E');
			await Assert.That(thumb1Bytes[10]).IsEqualTo((byte)'B');
			await Assert.That(thumb1Bytes[11]).IsEqualTo((byte)'P');
		}
		finally
		{
			if (Directory.Exists(tempRoot))
			{
				Directory.Delete(tempRoot, recursive: true);
			}
		}
	}

	private static string FindDockerfilePath()
	{
		string[] searchPaths =
		[
			Path.Combine(AppContext.BaseDirectory, "imagemagick.Dockerfile"),
			Path.Combine(Directory.GetCurrentDirectory(), "ThumbnailGenerator", "imagemagick.Dockerfile"),
			Path.Combine(
				AppContext.BaseDirectory,
				"..",
				"..",
				"..",
				"..",
				"ThumbnailGenerator",
				"imagemagick.Dockerfile"
			),
		];

		foreach (var path in searchPaths)
		{
			var fullPath = Path.GetFullPath(path);
			if (File.Exists(fullPath))
			{
				return fullPath;
			}
		}

		throw new FileNotFoundException("Could not find imagemagick.Dockerfile");
	}
}
