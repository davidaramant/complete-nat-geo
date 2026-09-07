using System.Diagnostics;

namespace CompleteNatGeo.ThumbnailGenerator;

public sealed class DockerRunner
{
	public const string DefaultImageName = "complete-nat-geo-imagemagick";

	private readonly string _dockerPath;

	public DockerRunner()
	{
		_dockerPath = ResolveDockerExecutable();
	}

	public async Task BuildImageAsync(
		string dockerfilePath,
		string imageName = DefaultImageName,
		CancellationToken cancellationToken = default
	)
	{
		if (!File.Exists(dockerfilePath))
		{
			throw new FileNotFoundException($"Dockerfile not found at {dockerfilePath}", dockerfilePath);
		}

		var contextDir = Path.GetDirectoryName(dockerfilePath) ?? Directory.GetCurrentDirectory();
		var args = new[] { "build", "-t", imageName, "-f", dockerfilePath, contextDir };

		var (exitCode, output, error) = await RunCommandAsync(args, cancellationToken);
		if (exitCode != 0)
		{
			throw new InvalidOperationException(
				$"Failed to build Docker image '{imageName}'. Exit code: {exitCode}.\nError: {error}\nOutput: {output}"
			);
		}
	}

	public async Task ConvertDirectoryAsync(
		string directoryPath,
		string imageName = DefaultImageName,
		CancellationToken cancellationToken = default
	)
	{
		if (!Directory.Exists(directoryPath))
		{
			throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
		}

		// Run sh inside container with /data mounted to directoryPath
		const string conversionScript =
			"for f in *; do if [ -f \"$f\" ]; then case \"$f\" in *.[jJ][pP][gG]|*.[jJ][pP][eE][gG]) magick \"$f\" -resize 360x520 \"${f%.*}.thumb.webp\" || exit 1 ;; esac; fi; done; exit 0";
		var args = new[]
		{
			"run",
			"--rm",
			"--entrypoint",
			"sh",
			"-v",
			$"{directoryPath}:/data",
			imageName,
			"-c",
			conversionScript,
		};

		var (exitCode, output, error) = await RunCommandAsync(args, cancellationToken);
		if (exitCode != 0)
		{
			throw new InvalidOperationException(
				$"ImageMagick conversion failed for directory '{directoryPath}'. Exit code: {exitCode}.\nError: {error}\nOutput: {output}"
			);
		}
	}

	public async Task<(int ExitCode, string Output, string Error)> RunCommandAsync(
		IEnumerable<string> arguments,
		CancellationToken cancellationToken = default
	)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = _dockerPath,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		foreach (var arg in arguments)
		{
			startInfo.ArgumentList.Add(arg);
		}

		var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
		startInfo.Environment["PATH"] =
			$"/Applications/Docker.app/Contents/Resources/bin:/usr/local/bin:/opt/homebrew/bin:{currentPath}";

		using var process = new Process { StartInfo = startInfo };

		process.Start();

		var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
		var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

		await process.WaitForExitAsync(cancellationToken);

		var output = await outputTask;
		var error = await errorTask;

		return (process.ExitCode, output, error);
	}

	private static string ResolveDockerExecutable()
	{
		if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
		{
			string[] candidates =
			[
				"/usr/local/bin/docker",
				"/opt/homebrew/bin/docker",
				$"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.docker/bin/docker",
				"/Applications/Docker.app/Contents/Resources/bin/docker",
				"/usr/bin/docker",
			];

			foreach (var candidate in candidates)
			{
				if (File.Exists(candidate))
				{
					return candidate;
				}
			}
		}

		return "docker";
	}
}
