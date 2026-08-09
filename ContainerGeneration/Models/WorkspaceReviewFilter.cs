namespace VIBN_Tools.ContainerGeneration.Models;

public enum WorkspaceReviewFilter
{
    All,
    NeedsReview,
    Changed,
    ManuallyEdited,
    Unchecked,
    Invalid
}

public sealed record WorkspaceReviewFilterOption(
    WorkspaceReviewFilter Value,
    string DisplayName);
