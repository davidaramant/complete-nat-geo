using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.local.json", optional: false)
    .Build();

var settings = configuration.GetRequiredSection("Inputs").Get<InputsConfig>()
    ?? throw new InvalidOperationException("The Inputs configuration section is required.");

Console.WriteLine(settings.SqlitePath);
Console.WriteLine(settings.ImagesPath);

public sealed class InputsConfig
{
    public required string SqlitePath { get; init; }
    public required string ImagesPath { get; init; }
}
