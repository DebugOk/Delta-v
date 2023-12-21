using Content.Shared.DeltaV.Glimmer;
using Content.Shared.DeltaV.Glimmer.Components;
using Content.Shared.DeltaV.Glimmer.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.DeltaV.Glimmer.Systems;

public sealed class GlimmerSystem : SharedGlimmerSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapChangedEvent>(MapChangedEvent);
    }

    /// <summary>
    /// Control the creation and deletion of noospheres
    /// </summary>
    private void MapChangedEvent(MapChangedEvent args)
    {
        if (args.Map == MapId.Nullspace)
            return;

        if (args.Created)
        {
            if (_noosphereList.ContainsKey(args.Map))
                return;
            var noosphere = EntityManager.SpawnEntity(NoospherePrototype, MapCoordinates.Nullspace);
            var noosphereComponent = EnsureComp<NoosphereComponent>(noosphere);
            noosphereComponent.MapId = args.Map;

            _noosphereList.Add(args.Map, noosphere);
            _sawmill.Debug($"Created noosphere for {args.Map}, {noosphere}");

            RaiseLocalEvent(noosphere, new NoosphereCreatedEvent(args.Map, noosphere, noosphereComponent.Glimmer));
        }
        else if (args.Destroyed)
        {
            if (_noosphereList.Remove(args.Map, out var noosphere))
            {
                if (!TryComp<NoosphereComponent>(noosphere, out var noosphereComponent))
                    return;

                RaiseLocalEvent(noosphere, new NoosphereDestroyedEvent(args.Map, noosphere, noosphereComponent.Glimmer));
                EntityManager.QueueDeleteEntity(noosphere);
                _sawmill.Debug($"Destroyed noosphere for {args.Map}, {noosphere}");
            }
        }
    }
}
