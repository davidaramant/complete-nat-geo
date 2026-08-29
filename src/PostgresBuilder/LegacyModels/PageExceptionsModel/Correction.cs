namespace CompleteNatGeo.PostgresBuilder.LegacyModels.PageExceptionsModel;

public sealed record Correction(
	int SearchTime,
	string Op,
	string? Filename = null,
	string? NewFilename = null,
	int? Adjustment = null,
	int? OtherSearchTime = null,
	string? OtherFilename = null,
	int? PageCount = null,
	int? PageOffset = null,
	string? Adjust = null, // TODO: Is this another enum?
	int? StartValue = null
)
{
	public CorrectionOperation GetOperation()
	{
		return Op switch
		{
			"remove_image" => CorrectionOperation.RemoveImage,
			"move_image" => CorrectionOperation.MoveImage,
			"unnumbered_image" => CorrectionOperation.UnnumberedImage,
			"not_large_page" => CorrectionOperation.NotLargePage,
			"mark_large_page" => CorrectionOperation.MarkLargePage,
			"move_map" => CorrectionOperation.MoveMap,
			"add_new_image_before" => CorrectionOperation.AddNewImageBefore,
			"insert_image_from_other_issue" => CorrectionOperation.InsertImageFromOtherIssue,
			"remove_map" => CorrectionOperation.RemoveMap,
			"delete_missing_numbered_pages" => CorrectionOperation.DeleteMissingNumberedPages,
			_ => throw new InvalidOperationException("Unknown operation."),
		};
	}
}

public enum CorrectionOperation
{
	/// <summary>
	/// Used to mark new images found in the app bundle.
	/// </summary>
	AddNewImageBefore,

	/// <summary>
	/// Unused.
	/// </summary>
	DeleteMissingNumberedPages,

	/// <summary>
	/// Corrected manually.
	/// </summary>
	InsertImageFromOtherIssue,

	/// <summary>
	/// Marks a large page.
	/// </summary>
	MarkLargePage,

	/// <summary>
	/// Rearranges page order.
	/// </summary>
	MoveImage,

	/// <summary>
	/// Moves the map to where it was in the issue.
	/// </summary>
	MoveMap,

	/// <summary>
	/// The CNG app works on aspect ratio or something similar.
	/// </summary>
	NotLargePage,

	/// <summary>
	/// Corrected manually.
	/// </summary>
	RemoveImage,

	/// <summary>
	/// Corrected manually.
	/// </summary>
	RemoveMap,

	UnnumberedImage,
}
