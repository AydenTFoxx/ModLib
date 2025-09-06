using System.Collections.Generic;

namespace Martyr.Slugcat.Features;

public abstract class Feature
{
    private static readonly List<Feature> Features = [];

    protected Feature()
    {
        Features.Add(this);
    }

    public static void ApplyFeatures()
    {
        foreach (Feature feature in Features)
        {
            feature.ApplyHooks();
        }
    }

    public static void RemoveFeatures()
    {
        foreach (Feature feature in Features)
        {
            feature.RemoveHooks();
        }
    }

    public abstract void ApplyHooks();

    public abstract void RemoveHooks();
}