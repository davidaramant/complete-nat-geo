namespace Api;

public static class Repo
{
	public static string GetRoot()
	{
        var cwd = Directory.GetCurrentDirectory();
		var dir = new DirectoryInfo(cwd);
		do
		{
			if (File.Exists(Path.Combine(dir.FullName, "run.sh")))
			{
				return dir.FullName;
			}

			dir = dir.Parent;
		} while (dir is not null);

		return cwd;
	}
}
