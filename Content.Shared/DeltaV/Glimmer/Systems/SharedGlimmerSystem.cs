using System.Diagnostics.CodeAnalysis;
using Content.Shared.DeltaV.CCVars;
using Content.Shared.DeltaV.Glimmer.Components;
using Robust.Shared.Configuration;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeltaV.Glimmer
{
    /// <summary>
    /// This handles setting / reading the value of glimmer.
    /// </summary>
    public abstract class SharedGlimmerSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        protected ISawmill _sawmill = default!;
        public bool Enabled { get; private set; }
        protected Dictionary<MapId, EntityUid> _noosphereList = new();

        [ValidatePrototypeId<EntityPrototype>]
        protected const string NoospherePrototype = "BaseNoosphere";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);


            Enabled = _cfg.GetCVar(DCCVars.GlimmerEnabled);
            _cfg.OnValueChanged(DCCVars.GlimmerEnabled, value => Enabled = value, true);
            _sawmill = Logger.GetSawmill("glimmer");
        }

        private void Reset(RoundRestartCleanupEvent args)
        {
            _noosphereList.Clear();
        }

        public bool TryGetNoosphere(MapId mapId, out EntityUid noosphere)
        {
            return _noosphereList.TryGetValue(mapId, out noosphere);
        }

        public bool TryGetNoosphereEntity(EntityUid target, out EntityUid noosphere)
        {
            noosphere = EntityUid.Invalid;

            if (!TryComp<TransformComponent>(target, out var transform))
                return false;

            return _noosphereList.TryGetValue(transform.MapID, out noosphere);
        }

        public bool UpdateGlimmer(EntityUid noosphere, int amount)
        {
            if (!TryComp<NoosphereComponent>(noosphere, out var noosphereComponent))
                return false;

            noosphereComponent.Glimmer += amount;
            return true;
        }

        public bool SetGlimmer(EntityUid noosphere, int amount)
        {
            if (!TryComp<NoosphereComponent>(noosphere, out var noosphereComponent))
                return false;

            noosphereComponent.Glimmer = amount;
            return true;
        }

        /// <summary>
        /// I'm lazy, okay?
        /// </summary>
        public int GetGlimmer(EntityUid noosphere)
        {
            if (!TryComp<NoosphereComponent>(noosphere, out var noosphereComponent))
                return 0;

            return noosphereComponent.Glimmer;
        }

        /// <summary>
        /// Return an abstracted range of a glimmer count.
        /// </summary>
        /// <param name="glimmer">What glimmer count to check. Uses the current glimmer by default.</param>
        public GlimmerTier GetGlimmerTier(int glimmer)
        {
            return (glimmer) switch
            {
                <= 49 => GlimmerTier.Minimal,
                <= 99 => GlimmerTier.Low,
                <= 299 => GlimmerTier.Moderate,
                <= 499 => GlimmerTier.High,
                <= 899 => GlimmerTier.Dangerous,
                _ => GlimmerTier.Critical,
            };
        }
    }
}
