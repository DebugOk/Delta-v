using Content.Server.DeltaV.Glimmer.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.Psionics.Glimmer;
using Content.Server.Station.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Map.Components;

namespace Content.Server.StationEvents.Events;

internal sealed class FreeProberRule : StationEventSystem<FreeProberRuleComponent>
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private static readonly string ProberPrototype = "GlimmerProber";
    private static readonly int SpawnDirections = 4;

    protected override void Started(EntityUid uid, FreeProberRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        List<EntityUid> PossibleSpawns = new();

        if (!TryGetRandomStation(out var station))
            return;

        var stationData = Comp<StationDataComponent>(station.Value);

        // find a station grid
        EntityUid? gridUid;
        gridUid = StationSystem.GetLargestGrid(stationData);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out _))
        {
            Sawmill.Warning("Chosen station has no grids, cannot spawn free prober!");
            return;
        }

        EntityUid noosphere;

        if (TryComp<TransformComponent>(gridUid, out var gridXform))
            _glimmerSystem.TryGetNoosphere(gridXform.MapID, out noosphere);
        else
            return;

        var query = EntityQueryEnumerator<GlimmerSourceComponent>();
        while (query.MoveNext(out var glimmerSource, out var glimmerSourceComponent))
        {
            if (glimmerSourceComponent.AddToGlimmer && glimmerSourceComponent.Active)
            {

                if (!TryComp<TransformComponent>(glimmerSource, out var xform) || xform.GridUid != gridUid)
                    continue;
                PossibleSpawns.Add(glimmerSource);
            }
        }

        if (PossibleSpawns.Count == 0 || _glimmerSystem.GetGlimmer(noosphere) >= 500 || _robustRandom.Prob(0.25f))
        {
            var queryBattery = EntityQueryEnumerator<PowerNetworkBatteryComponent>();
            while (query.MoveNext(out var battery, out var _))
            {
                PossibleSpawns.Add(battery);
            }
        }

        if (PossibleSpawns.Count > 0)
        {
            _robustRandom.Shuffle(PossibleSpawns);

            foreach (var source in PossibleSpawns)
            {
                var xform = Transform(source);

                if (_stationSystem.GetOwningStation(source, xform) == null)
                    continue;

                var coordinates = xform.Coordinates;
                gridUid = xform.GridUid;
                if (!_mapManager.TryGetGrid(gridUid, out var grid))
                    continue;

                var tileIndices = grid.TileIndicesFor(coordinates);

                for (var i = 0; i < SpawnDirections; i++)
                {
                    var direction = (DirectionFlag) (1 << i);
                    var offsetIndices = tileIndices.Offset(direction.AsDir());

                    // This doesn't check against the prober's mask/layer, because it hasn't spawned yet...
                    if (!_anchorable.TileFree(grid, offsetIndices))
                        continue;

                    Spawn(ProberPrototype, grid.GridTileToLocal(offsetIndices));
                    return;
                }
            }
        }
    }
}
