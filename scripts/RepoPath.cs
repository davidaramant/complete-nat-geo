namespace Scripts;

public static class RepoPath
{
	static RepoPath()
	{
		Root = FindRoot();

		static string FindRoot()
		{
			var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
			do
			{
				if (File.Exists(Path.Combine(dir.FullName, "run.sh")))
				{
					return dir.FullName;
				}

				dir = dir.Parent;
			} while (dir is not null);

			throw new InvalidOperationException("Could not find root directory");
		}
	}

	public static string Root { get; }

	public static string Source => Path.Combine(Root, "src");
	public static string Solution => Path.Combine(Source, "CompleteNatGeo.slnx");

	public static string PostgresBuilderProject => Path.Combine(Source, "PostgresBuilder", "PostgresBuilder.csproj");
}
