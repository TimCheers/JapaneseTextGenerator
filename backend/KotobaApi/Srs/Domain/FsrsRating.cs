namespace KotobaApi.Srs.Domain;

/// <summary>FSRS review grades. Numeric values intentionally match the reference model.</summary>
public enum FsrsRating
{
    Again = 1,
    Hard = 2,
    Good = 3,
    Easy = 4,
}

internal static class FsrsRatingExtensions
{
    public static int ToReferenceValue(this FsrsRating rating)
    {
        if (rating is < FsrsRating.Again or > FsrsRating.Easy)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be Again, Hard, Good, or Easy.");
        }

        return (int)rating;
    }
}
