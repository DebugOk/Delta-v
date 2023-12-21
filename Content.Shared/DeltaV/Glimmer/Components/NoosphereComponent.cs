using Robust.Shared.Map;

namespace Content.Shared.DeltaV.Glimmer.Components;

[RegisterComponent]
[Access(typeof(SharedGlimmerSystem))]
public sealed partial class NoosphereComponent : Component
{
    public int Glimmer { get; set; }

    public MapId MapId { get; set; }
}
