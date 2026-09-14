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
				.Issues
				.GroupBy(i => i.Decade)
				.OrderByDescending(g => g.Key)
				.Select(g => new
				{
					Decade = g.Key,
					FirstYear = g.Min(i => i.ReleaseDate.Year),
					LastYear = g.Max(i => i.ReleaseDate.Year),
					FirstReleaseDate = g.Min(i => i.ReleaseDate),
					CoverFileName = g
						.OrderBy(i => i.ReleaseDate)
						.Select(i => i.Pages.Where(p => p.SortOrder == 0).Select(p => p.FileName).First())
						.First(),
				})
				.ToListAsync();

			return decades.Select(d => new DecadeSummaryDto(
				Decade: d.Decade,
				FirstYear: d.FirstYear,
				LastYear: d.LastYear,
				CoverImageUrl: imageContext.GetThumbnailUrl(d.FirstReleaseDate, d.CoverFileName)
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

			var previousDecade = await context
				.Issues.Where(i => i.Decade < decade)
				.Select(i => (int?)i.Decade)
				.MaxAsync();

			var nextDecade = await context
				.Issues.Where(i => i.Decade > decade)
				.Select(i => (int?)i.Decade)
				.MinAsync();

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
							CoverImageUrl: imageContext.GetThumbnailUrl(i.ReleaseDate, i.FileName)
						))
						.ToList()
				)
			);
		}
	)
	.WithName("GetDecade");

api.MapGet(
		"/years/{year:int}",
		async (CompleteNatGeoContext context, IImageContext imageContext, [FromRoute] int year) =>
		{
			var issues = await context
				.Issues.Where(i => i.ReleaseDate.Year == year)
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

			var previousYear = await context
				.Issues.Where(i => i.ReleaseDate.Year < year)
				.Select(i => (int?)i.ReleaseDate.Year)
				.MaxAsync();

			var nextYear = await context
				.Issues.Where(i => i.ReleaseDate.Year > year)
				.Select(i => (int?)i.ReleaseDate.Year)
				.MinAsync();

			return Results.Ok(
				new YearDetailDto(
					Year: year,
					PreviousYear: previousYear,
					NextYear: nextYear,
					Issues: issues
						.Select(i => new IssueSummaryDto(
							Id: i.Id,
							ReleaseDate: i.ReleaseDate,
							PageCount: i.PageCount,
							CoverImageUrl: imageContext.GetThumbnailUrl(i.ReleaseDate, i.FileName)
						))
						.ToList()
				)
			);
		}
	)
	.WithName("GetYear");

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
							p.Id,
							p.SortOrder,
							p.PageNumber,
							p.FileName,
						})
						.ToArray(),
				})
				.SingleOrDefaultAsync();

			if (issue is null)
			{
				return Results.NotFound();
			}

			var previousIssueId = await context
				.Issues.Where(i => i.ReleaseDate < issue.ReleaseDate)
				.Select(i => (int?)i.Id)
				.MaxAsync();

			var nextIssueId = await context
				.Issues.Where(i => i.ReleaseDate > issue.ReleaseDate)
				.Select(i => (int?)i.Id)
				.MinAsync();

			return Results.Ok(
				new IssueDetailDto(
					Id: issue.Id,
					ReleaseDate: issue.ReleaseDate,
					CoverImageUrl: imageContext.GetFullSizeUrl(
						issue.ReleaseDate,
						issue.Pages.Where(p => p.SortOrder == 0).Select(p => p.FileName).Single()
					),
					Decade: issue.Decade,
					PreviousIssueId: previousIssueId,
					NextIssueId: nextIssueId,
					Pages: issue
						.Pages.Select(p => new PageDto(
							Id: p.Id,
							SortOrder: p.SortOrder,
							PageNumber: p.PageNumber,
							ImageUrl: imageContext.GetThumbnailUrl(issue.ReleaseDate, p.FileName)
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
				.Pages
				.Where(p => p.Id == id)
				.Join(
					context.Issues,
					page => page.IssueId,
					issue => issue.Id,
					(page, issue) => new
					{
						page.Id,
						page.IssueId,
						page.SortOrder,
						page.PageNumber,
						page.FileName,
						issue.ReleaseDate,
		            				issue.Decade,
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
					ImageUrl: imageContext.GetFullSizeUrl(page.ReleaseDate, page.FileName)
				)
			);
		}
	)
	.WithName("GetPage");

app.Run();

static string GetPostgresConnectionString(IConfigurationManager config)
{
	var host = config["POSTGRES_HOST"] ?? throw new ArgumentException("Host");
	var user = config["POSTGRES_USER"] ?? throw new ArgumentException("User");
	var password = config["POSTGRES_PASSWORD"] ?? throw new ArgumentException("Password");
	var dbName = config["POSTGRES_DB"] ?? throw new ArgumentException("DB");
	var port = int.Parse(config["POSTGRES_PORT"] ?? throw new ArgumentException("Port"));

	return $"Host={host};Port={port};Database={dbName};Username={user};Password={password};";
}
