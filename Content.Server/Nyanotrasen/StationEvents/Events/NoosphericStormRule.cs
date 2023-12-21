using Robust.Shared.Random;
using Content.Server.Abilities.Psionics;
using Content.Server.DeltaV.Glimmer.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Psionics;
using Content.Server.Station.Components;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Mobs.Systems;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Map.Components;

namespace Content.Server.StationEvents.Events;

internal sealed class NoosphericStormRule : StationEventSystem<NoosphericStormRuleComponent>
{
    [Dependency] private readonly PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    protected override void Started(EntityUid uid, NoosphericStormRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        EntityUid targetMap;

        if (!TryGetRandomStation(out var station))
            return;
        var stationGridUid = StationSystem.GetLargestGrid(Comp<StationDataComponent>(station.Value));
        if (stationGridUid == null || !TryComp<MapGridComponent>(stationGridUid, out _))
            return;

        if (TryComp<TransformComponent>(stationGridUid, out var gridXform) && gridXform.MapUid != null)
            targetMap = gridXform.MapUid.Value;
        else
            return;

        List<EntityUid> validList = new();

        var query = EntityManager.EntityQueryEnumerator<PotentialPsionicComponent>();
        while (query.MoveNext(out var potentialPsionic, out var potentialPsionicComponent))
        {
            if (_mobStateSystem.IsDead(potentialPsionic))
                continue;

            // Skip over those who are already psionic or those who are insulated.
            if (HasComp<PsionicComponent>(potentialPsionic) || HasComp<PsionicInsulationComponent>(potentialPsionic))
                continue;

            if (!TryComp<TransformComponent>(potentialPsionic, out var xform) || xform.MapUid != targetMap)
                continue;

            validList.Add(potentialPsionic);
        }

        // Give some targets psionic abilities.
        RobustRandom.Shuffle(validList);

        var toAwaken = RobustRandom.Next(1, component.MaxAwaken);

        foreach (var target in validList)
        {
            if (toAwaken-- == 0)
                break;

            _psionicAbilitiesSystem.AddPsionics(target);
        }

        // Increase glimmer.
        var baseGlimmerAdd = _robustRandom.Next(component.BaseGlimmerAddMin, component.BaseGlimmerAddMax);
        var glimmerSeverityMod = 1 + (component.GlimmerSeverityCoefficient * (GetSeverityModifier() - 1f));
        var glimmerAdded = (int) Math.Round(baseGlimmerAdd * glimmerSeverityMod);

        _glimmerSystem.TryGetNoosphere(gridXform.MapID, out var noosphere);
        _glimmerSystem.UpdateGlimmer(noosphere, glimmerAdded);
    }
}
