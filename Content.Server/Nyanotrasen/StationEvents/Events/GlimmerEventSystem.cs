using Content.Server.DeltaV.Glimmer.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Psionics.Glimmer;
using Content.Server.Station.Components;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Map.Components;

namespace Content.Server.StationEvents.Events
{
    public sealed class GlimmerEventSystem : StationEventSystem<GlimmerEventComponent>
    {
        [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;

        protected override void Ended(EntityUid uid, GlimmerEventComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, component, gameRule, args);

            var glimmerBurned = RobustRandom.Next(component.GlimmerBurnLower, component.GlimmerBurnUpper);

            if (!TryGetRandomStation(out var station))
                return;
            var gridUid = StationSystem.GetLargestGrid(Comp<StationDataComponent>(station.Value));
            if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out _))
                return;

            EntityUid noosphere;

            if (TryComp<TransformComponent>(gridUid, out var gridXform))
                _glimmerSystem.TryGetNoosphere(gridXform.MapID, out noosphere);
            else
                return;

            _glimmerSystem.UpdateGlimmer(noosphere, -glimmerBurned);

            var reportEv = new GlimmerEventEndedEvent(component.SophicReport, glimmerBurned, noosphere);
            RaiseLocalEvent(reportEv);
        }
    }

    public sealed class GlimmerEventEndedEvent : EntityEventArgs
    {
        public string Message = "";
        public int GlimmerBurned = 0;
        public EntityUid Noosphere;

        public GlimmerEventEndedEvent(string message, int glimmerBurned, EntityUid noosphere)
        {
            Message = message;
            GlimmerBurned = glimmerBurned;
            Noosphere = noosphere;
        }
    }
}
