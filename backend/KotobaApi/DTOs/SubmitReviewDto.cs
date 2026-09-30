using KotobaApi.Srs.Domain;

public record SubmitReviewDto(Guid WordId, Guid EventId, FsrsRating Rating, ReviewSource Source);