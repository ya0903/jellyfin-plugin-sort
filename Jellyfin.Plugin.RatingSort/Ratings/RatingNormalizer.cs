namespace Jellyfin.Plugin.RatingSort.Ratings;

internal static class RatingNormalizer
{
    public static float? GetCommunityRating0To10(MdbListTitleResponse data, string source)
    {
        var score = GetScore0To100(data, source);
        if (!score.HasValue)
        {
            return null;
        }

        var value = (float)Math.Round(Clamp(score.Value, 0, 100) / 10.0, 1, MidpointRounding.AwayFromZero);
        return value > 0 ? value : null;
    }

    public static float? GetCriticRating0To100(MdbListTitleResponse data, string source)
    {
        var score = GetScore0To100(data, source);
        if (!score.HasValue)
        {
            return null;
        }

        var value = (float)Math.Round(Clamp(score.Value, 0, 100), 0, MidpointRounding.AwayFromZero);
        return value > 0 ? value : null;
    }

    private static double? GetScore0To100(MdbListTitleResponse data, string source)
    {
        var rating = data.Ratings.FirstOrDefault(r => string.Equals(r.Source, source, StringComparison.OrdinalIgnoreCase));
        var score = rating?.Score ?? NormalizeValueToScore(rating?.Value);
        if (!score.HasValue || double.IsNaN(score.Value) || double.IsInfinity(score.Value) || score.Value <= 0)
        {
            return null;
        }

        return score.Value;
    }

    private static double? NormalizeValueToScore(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value <= 0)
        {
            return null;
        }

        return value.Value <= 10 ? value.Value * 10 : value.Value <= 100 ? value.Value : null;
    }

    private static double Clamp(double value, double min, double max)
    {
        return value < min ? min : value > max ? max : value;
    }
}
