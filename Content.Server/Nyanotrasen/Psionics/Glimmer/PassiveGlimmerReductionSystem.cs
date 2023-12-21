using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.GameTicking;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Server.DeltaV.Glimmer.Systems;
using Content.Shared.DeltaV.CCVars;
using Content.Shared.DeltaV.Glimmer.Components;
using Content.Shared.DeltaV.Glimmer.Events;

namespace Content.Server.Psionics.Glimmer
{
    /// <summary>
    /// Handles the passive reduction of glimmer.
    /// </summary>
    public sealed class PassiveGlimmerReductionSystem : EntitySystem
    {
        [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly GlimmerMonitorCartridgeSystem _cartridgeSys = default!;

        /// List of glimmer values spaced by minute.
        public List<int> GlimmerValues = new();

        private List<EntityUid> _noospheres = new();

        public TimeSpan TargetUpdatePeriod = TimeSpan.FromSeconds(6);

        private int _updateIncrementor;
        public TimeSpan NextUpdateTime = default!;
        public TimeSpan LastUpdateTime = default!;

        private float _glimmerLostPerSecond;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
            SubscribeLocalEvent<NoosphereCreatedEvent>(OnNoosphereCreated);
            SubscribeLocalEvent<NoosphereDestroyedEvent>(OnNoosphereDestroyed);
            _cfg.OnValueChanged(DCCVars.GlimmerLostPerSecond, UpdatePassiveGlimmer, true);
        }

        private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
        {
            GlimmerValues.Clear();
            _noospheres.Clear();
        }

        private void OnNoosphereCreated(NoosphereCreatedEvent args)
        {
            _noospheres.Add(args.Entity);
        }

        private void OnNoosphereDestroyed(NoosphereDestroyedEvent args)
        {
            _noospheres.Remove(args.Entity);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var curTime = _timing.CurTime;
            if (NextUpdateTime > curTime)
                return;


            var delta = curTime - LastUpdateTime;
            var maxGlimmerLost = (int) Math.Round(delta.TotalSeconds * _glimmerLostPerSecond);


            // Loop over every noosphere, as we need to update all of them
            foreach (var noosphere in _noospheres)
            {
                if (!TryComp<NoosphereComponent>(noosphere, out var noosphereComponent))
                    continue;

                // It used to be 75% to lose one glimmer per ten seconds, but now it's 50% per six seconds.
                // // The probability is exactly the same over the same span of time. (0.25 ^ 3 == 0.5 ^ 6)
                // // This math is just easier to do for pausing's sake.
                var actualGlimmerLost = _random.Next(0, 1 + maxGlimmerLost);

                _glimmerSystem.UpdateGlimmer(noosphere, -actualGlimmerLost);

                _updateIncrementor++;

                // Since we normally update every 6 seconds, this works out to a minute.
                if (_updateIncrementor == 10)
                {
                    GlimmerValues.Add(noosphereComponent.Glimmer);

                    _updateIncrementor = 0;
                }
            }

            NextUpdateTime = curTime + TargetUpdatePeriod;
            LastUpdateTime = curTime;
        }

        private void UpdatePassiveGlimmer(float value) => _glimmerLostPerSecond = value;
    }
}
