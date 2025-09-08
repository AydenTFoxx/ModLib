namespace Martyr.Slugcat.Features;

public interface IFeature
{
    void ApplyHooks();

    void RemoveHooks();
}

public static class FeatureManager
{
    private static readonly IFeature[] Features = [
        new PossessionFeature()
    ];

    public static void ApplyFeatures()
    {
        foreach (IFeature feature in Features)
        {
            MyLogger.LogDebug($"Adding feature: {feature}");

            feature.ApplyHooks();
        }
    }

    public static void RemoveFeatures()
    {
        foreach (IFeature feature in Features)
        {
            MyLogger.LogDebug($"Removing feature: {feature}");

            feature.RemoveHooks();
        }
    }
}