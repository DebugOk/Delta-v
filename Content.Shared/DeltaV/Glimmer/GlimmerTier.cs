using Robust.Shared.Serialization;

namespace Content.Shared.DeltaV.Glimmer;

[Serializable, NetSerializable]
public enum GlimmerTier : byte
{
    Minimal,
    Low,
    Moderate,
    High,
    Dangerous,
    Critical,
}
