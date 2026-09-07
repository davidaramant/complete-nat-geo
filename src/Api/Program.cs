using Api;
using CompleteNatGeo.Data;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

Env.Load(Path.Combine(Repo.GetRoot(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CompleteNatGeoContext>(options =>
	options.UseNpgsql(GetPostgresConnectionString(builder.Configuration))
);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IImageContext, ImageContext>();

// TODO: Check this stuff when deployment becomes real
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
	});
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	// Serve images from the local folder specified in ImagesPath
	var imagesPath = builder.Configuration["IMAGES_PATH"];
	if (!string.IsNullOrWhiteSpace(imagesPath) && Directory.Exists(imagesPath))
	{
		app.UseStaticFiles(
			new StaticFileOptions { FileProvider = new PhysicalFileProvider(imagesPath), RequestPath = "/images" }
		);
	}
}

app.UseHttpsRedirection();
app.MapHealthChecks("/health");

var api = app.MapGroup("/api/v1");

api.MapGet(
		"/decades",
		async (CompleteNatGeoContext context, IImageContext imageContext) =>
		{
			var decades = await context
				.Issues.GroupBy(i => i.Decade)
				.OrderByDescending(g => g.Key)
				.Select(g => new
				{
					Decade = g.Key,
					FileName = g.OrderBy(i => i.ReleaseDate)
						.Select(i => i.Pages.Where(p => p.SortOrder == 0).Select(p => p.FileName).First())
						.First(),
					CoverImageUrl = g.OrderBy(i => i.ReleaseDate)
						.Select(i => i.Pages.Where(p => p.SortOrder == 0).Select(p => p.FileName).First())
						.First(),
				})
				.ToListAsync();

			return decades.Select(d => new DecadeSummaryDto(
				Decade: d.Decade,
				CoverImageUrl: imageContext.GetUrl(GetDecadeDate(d.Decade, context), d.CoverImageUrl)
			));
		}
	)
	.WithName("GetDecades");

api.MapGet(
		"/decades/{decade:int}",
		async (CompleteNatGeoContext context, IImageContext imageContext, [FromRoute] int decade) =>
		{
			var issues = await context
				.Issues.Where(i => i.Decade == decade)
				.OrderBy(i => i.ReleaseDate)
				.Select(i => new
				{
					i.Id,
					i.ReleaseDate,
					FileName = i.Pages.Where(p => p.SortOrder == 0).Select(p => p.FileName).First(),
					PageCount = i.Pages.Count,
				})
				.ToListAsync();

			if (issues.Count == 0)
			{
				return Results.NotFound();
			}

			var availableDecades = await context.Issues.Select(i => i.Decade).Distinct().OrderBy(d => d).ToListAsync();

			var decadeIndex = availableDecades.IndexOf(decade);
			var previousDecade = decadeIndex > 0 ? availableDecades[decadeIndex - 1] : (int?)null;
			var nextDecade =
				decadeIndex >= 0 && decadeIndex < availableDecades.Count - 1
					? availableDecades[decadeIndex + 1]
					: (int?)null;

			return Results.Ok(
				new DecadeDetailDto(
					Decade: decade,
					PreviousDecade: previousDecade,
					NextDecade: nextDecade,
					Issues: issues
						.Select(i => new IssueSummaryDto(
							Id: i.Id,
							ReleaseDate: i.ReleaseDate,
							PageCount: i.PageCount,
							CoverImageUrl: imageContext.GetUrl(i.ReleaseDate, i.FileName)
						))
						.ToList()
				)
			);
		}
	)
	.WithName("GetDecade");

api.MapGet(
		"/issues/{id:int}",
		async (CompleteNatGeoContext context, IImageContext imageContext, [FromRoute] int id) =>
		{
			var issue = await context
				.Issues.Where(i => i.Id == id)
				.Select(i => new
				{
					i.Id,
					i.ReleaseDate,
					i.Decade,
					Pages = i
						.Pages.OrderBy(p => p.SortOrder)
						.Select(p => new
						{
							Id = p.Id,
							SortOrder = p.SortOrder,
							PageNumber = p.PageNumber,
							FileName = p.FileName,
						})
						.ToArray(),
				})
				.SingleOrDefaultAsync();

			if (issue is null)
			{
				return Results.NotFound();
			}

			var orderedIssueIds = await context
				.Issues.OrderBy(i => i.ReleaseDate)
				.Select(i => new { i.Id, i.ReleaseDate })
				.ToListAsync();

			var issueIndex = orderedIssueIds.FindIndex(i => i.Id == issue.Id);
			var previousIssueId = issueIndex > 0 ? orderedIssueIds[issueIndex - 1].Id : (int?)null;
			var nextIssueId = issueIndex < orderedIssueIds.Count - 1 ? orderedIssueIds[issueIndex + 1].Id : (int?)null;

			return Results.Ok(
				new IssueDetailDto(
					Id: issue.Id,
					ReleaseDate: issue.ReleaseDate,
					Decade: issue.Decade,
					PreviousIssueId: previousIssueId,
					NextIssueId: nextIssueId,
					Pages: issue
						.Pages.Select(p => new PageDto(
							Id: p.Id,
							SortOrder: p.SortOrder,
							PageNumber: p.PageNumber,
							ImageUrl: imageContext.GetUrl(issue.ReleaseDate, p.FileName)
						))
						.ToList()
				)
			);
		}
	)
	.WithName("GetIssue");

api.MapGet(
		"/pages/{id:int}",
		async (CompleteNatGeoContext context, IImageContext imageContext, [FromRoute] int id) =>
		{
			var page = await context
				.Pages.Where(p => p.Id == id)
				.Select(p => new
				{
					p.Id,
					p.IssueId,
					p.SortOrder,
					p.PageNumber,
					p.FileName,
					ReleaseDate = context.Issues.Where(i => i.Id == p.IssueId).Select(i => i.ReleaseDate).First(),
					Decade = context.Issues.Where(i => i.Id == p.IssueId).Select(i => i.Decade).First(),
				})
				.SingleOrDefaultAsync();

			if (page is null)
			{
				return Results.NotFound();
			}

			var orderedPages = await context
				.Pages.Where(p => p.IssueId == page.IssueId)
				.OrderBy(i => i.SortOrder)
				.Select(i => i.Id)
				.ToListAsync();

			var currentIndex = orderedPages.IndexOf(page.Id);
			var previousPageId = currentIndex > 0 ? orderedPages[currentIndex - 1] : (int?)null;
			var nextPageId = currentIndex < orderedPages.Count - 1 ? orderedPages[currentIndex + 1] : (int?)null;

			return Results.Ok(
				new PageDetailDto(
					Id: page.Id,
					IssueId: page.IssueId,
					Decade: page.Decade,
					PageNumber: page.PageNumber,
					ReleaseDate: page.ReleaseDate,
					PreviousPageId: previousPageId,
					NextPageId: nextPageId,
					ImageUrl: imageContext.GetUrl(page.ReleaseDate, page.FileName)
				)
			);
		}
	)
	.WithName("GetPage");

app.Run();

static DateOnly GetDecadeDate(int decade, CompleteNatGeoContext context)
{
	return context.Issues.Where(i => i.Decade == decade).OrderBy(i => i.ReleaseDate).Select(i => i.ReleaseDate).First();
}

static string GetPostgresConnectionString(IConfigurationManager config)
{
	var host = config["POSTGRES_HOST"] ?? throw new ArgumentException("Host");
	var user = config["POSTGRES_USER"] ?? throw new ArgumentException("User");
	var password = config["POSTGRES_PASSWORD"] ?? throw new ArgumentException("Password");
	var dbName = config["POSTGRES_DB"] ?? throw new ArgumentException("DB");
	var port = int.Parse(config["POSTGRES_PORT"] ?? throw new ArgumentException("Port"));

	return $"Host={host};Port={port};Database={dbName};Username={user};Password={password};";
}
