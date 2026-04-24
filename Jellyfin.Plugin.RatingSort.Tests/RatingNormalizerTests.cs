using Jellyfin.Plugin.RatingSort.Ratings;

namespace Jellyfin.Plugin.RatingSort.Tests;

public sealed class RatingNormalizerTests
{
    [Fact]
    public void ImdbCommunityRatingUsesTenPointScale()
    {
        var data = new MdbListTitleResponse
        {
            Ratings =
            [
                new MdbListRating { Source = "imdb", Value = 8.3, Score = 83 }
            ]
        };

        Assert.Equal(8.3f, RatingNormalizer.GetCommunityRating0To10(data, "imdb"));
    }

    [Fact]
    public void LetterboxdCriticRatingUsesHundredPointScale()
    {
        var data = new MdbListTitleResponse
        {
            Ratings =
            [
                new MdbListRating { Source = "letterboxd", Value = 4.4, Score = 88 }
            ]
        };

        Assert.Equal(88f, RatingNormalizer.GetCriticRating0To100(data, "letterboxd"));
    }

    [Fact]
    public void MissingOrInvalidRatingsReturnNull()
    {
        var data = new MdbListTitleResponse
        {
            Ratings =
            [
                new MdbListRating { Source = "imdb", Value = 0, Score = 0 },
                new MdbListRating { Source = "letterboxd", Value = double.NaN }
            ]
        };

        Assert.Null(RatingNormalizer.GetCommunityRating0To10(data, "imdb"));
        Assert.Null(RatingNormalizer.GetCriticRating0To100(data, "letterboxd"));
        Assert.Null(RatingNormalizer.GetCommunityRating0To10(data, "missing"));
    }

    [Fact]
    public void ValueFallbackNormalizesSmallNumbersToScore()
    {
        var data = new MdbListTitleResponse
        {
            Ratings =
            [
                new MdbListRating { Source = "letterboxd", Value = 4.2 }
            ]
        };

        Assert.Equal(42f, RatingNormalizer.GetCriticRating0To100(data, "letterboxd"));
    }
}
