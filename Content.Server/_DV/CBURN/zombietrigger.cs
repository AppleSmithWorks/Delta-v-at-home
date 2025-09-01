using Content.Shared.Mobs.Systems;
using Content.Shared.Zombies;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using System.Collections.Generic;
using Content.Server.Chat.Systems;
using Robust.Shared.Maths;
using Content.Server.Station.Systems;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.AlertLevel;
using Robust.Shared.EntitySerialization;
using Content.Server.Maps;
using Content.Server.GameTicking.Events;
using Robust.Shared.Utility;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Player;
using Robust.Shared.Console;
using Robust.Shared.Configuration;

namespace Content.Server._DV.CBURN
{
    public static class CBurnCVars
    {
        public static readonly CVarDef<bool> CburnSpawnDisabled = 
            CVarDef.Create("cburn.spawn_disabled", false, CVar.SERVERONLY);
    }
    public sealed class ZombieCounterSystem : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        private readonly ResPath _mapPath = new("Maps/_DV/cburn_outpost.yml"); //map spawned when triggered
        private bool _outbreakTriggered = false;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundStartAttemptEvent>(OnRoundStart);
        }

        public void ToggleCburnSpawn()
        {
            var current = _cfg.GetCVar(CBurnCVars.CburnSpawnDisabled);
            _cfg.SetCVar(CBurnCVars.CburnSpawnDisabled, !current);
        }
        public bool IsCburnSpawnDisabled()
        {
            return _cfg.GetCVar(CBurnCVars.CburnSpawnDisabled);
        }
        private void OnRoundStart(RoundStartAttemptEvent args)
        {
            _outbreakTriggered = false;
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (_outbreakTriggered)
                return;
            if (_cfg.GetCVar(CBurnCVars.CburnSpawnDisabled)) 
                return;
            var infectedFraction = GetInfectedFraction(includeOffStation: true, includeDead: false);
            if (infectedFraction >= 0.65f)
            {
                _outbreakTriggered = true;
                var stations = _stationSystem.GetStationsSet();
                foreach (var station in stations)
                {
                    _alertLevel.SetLevel(station, "gamma", true, true, true, true);
                }
                _chatSystem.DispatchGlobalAnnouncement(
                    "ALERT: A MASS VIRAL OUTBREAK IS DETECTED ABOARD YOUR STATION. A CBURN SQUAD IS BEING DEPLOYED TO YOUR LOCATION.", //alert and alarm modifyer.
                    colorOverride: Color.FromHex("#ff1cfb"));
                if (_mapLoader.TryLoadMap(_mapPath, out var map, out _, new DeserializationOptions { InitializeMaps = true }))
                    _map.SetPaused(map.Value.Comp.MapId, false);
            }
        }
        private float GetInfectedFraction(bool includeOffStation = true, bool includeDead = false)
        {
            var healthyPlayers = GetHealthyHumans(includeOffStation);
            int zombieCount = 0;
            var zombieQuery = EntityQueryEnumerator<HumanoidAppearanceComponent, ZombieComponent>();
            while (zombieQuery.MoveNext(out var uid, out _, out _))
            {
                if (!includeDead && !_mobState.IsAlive(uid))
                    continue;
                zombieCount++;
            }
            int total = healthyPlayers.Count + zombieCount;
            if (total == 0)
                return 0f;
            return (float)zombieCount / total;
        }
        private List<EntityUid> GetHealthyHumans(bool includeOffStation = true)
        {
            var healthy = new List<EntityUid>();
            var playerQuery = AllEntityQuery<HumanoidAppearanceComponent, ActorComponent, TransformComponent>();
            var zombieQuery = GetEntityQuery<ZombieComponent>();
            while (playerQuery.MoveNext(out var uid, out var humanoid, out var actor, out var transform))
            {
                if (!_mobState.IsAlive(uid))
                    continue;
                if (zombieQuery.HasComponent(uid))
                    continue;
                healthy.Add(uid);
            }
            return healthy;
        }
    }
}
