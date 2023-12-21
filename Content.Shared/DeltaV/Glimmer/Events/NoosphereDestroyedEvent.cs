using Robust.Shared.Map;

namespace Content.Shared.DeltaV.Glimmer.Events;

public sealed class NoosphereDestroyedEvent : EntityEventArgs
{
    public MapId Noosphere { get; }
    public EntityUid Entity { get; }
    public int Glimmer { get; }

    public NoosphereDestroyedEvent(MapId noosphere, EntityUid entity, int glimmer)
    {
        Noosphere = noosphere;
        Entity = entity;
        Glimmer = glimmer;
    }
}
