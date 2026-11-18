using System;
using Michi.Core;
using Michi.Runtime;
using UnityEngine;

namespace Michi.Checks
{
    public static class RelaySpecializationSaveChecks
    {
        static int checks;

        static void Check(bool ok, string message)
        {
            checks++;
            if (!ok) throw new Exception("RELAY SPECIALIZATION JSON: " + message);
        }

        static SavedRelaySpecialization Record(int id, RelayMode mode) =>
            new SavedRelaySpecialization { Node = id, Mode = mode };

        static void Reject(NetworkSave saved, SavedRelaySpecialization[] records, string message)
        {
            saved.RelaySpecializations = records;
            bool rejected = false;
            try { NetworkSimulation.RestoreSave(JsonUtility.FromJson<NetworkSave>(JsonUtility.ToJson(saved))); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, message);
        }

        public static string Run()
        {
            checks = 0;
            var city = new NetworkSimulation(CityMap.Generate(42), true, 1);
            city.LoadSolution();
            double basis = city.Nodes[4].Capacity;
            string oldJson = JsonUtility.ToJson(city.CaptureSave()).Replace("\"RelaySpecializations\":[],", "");
            Check(!oldJson.Contains("\"RelaySpecializations\""), "legacy fixture omits the field");
            var legacy = NetworkSimulation.RestoreSave(JsonUtility.FromJson<NetworkSave>(oldJson));
            Check(legacy.SpecializedRelayCount == 0 && legacy.RelayPowerCapacity(4) == basis, "old JSON restores shared modes");

            Check(city.SetRelayMode(4, RelayMode.Data) && city.SetRelayMode(6, RelayMode.Power), "configure both supported modes");
            for (int i = 0; i < 180; i++) city.Step(1.0 / 60);
            var saved = new SavedCity
            {
                Version = 1, SavedAt = DateTime.UtcNow.ToString("O"), Network = city.CaptureSave(),
                Angle = -18, Zoom = 1, Paused = true, Speed = 2, Tool = 4
            };
            var decoded = CitySaveStore.Decode(CitySaveStore.Encode(saved));
            var restored = NetworkSimulation.RestoreSave(decoded.Network);
            Check(decoded.Network.RelaySpecializations.Length == 2, "shared modes omitted from the record list");
            Check(restored.GetRelayMode(4) == RelayMode.Data && restored.GetRelayMode(6) == RelayMode.Power, "both types survive durable JSON");
            Check(restored.Nodes[4].Capacity == basis && restored.RelayPowerCapacity(4) == basis * .25, "capacity stored as the original basis");
            Check(Math.Abs(restored.Time - city.Time) < 1e-9 && Math.Abs(restored.NodeDataRate(4) - city.NodeDataRate(4)) < 1e-9,
                "clock and derived capacity match");
            decoded.Network.RelaySpecializations[0].Mode = RelayMode.Power;
            Check(restored.GetRelayMode(4) == RelayMode.Data, "restored modes have independent ownership");
            Check(restored.SetRelayMode(4, RelayMode.Shared) && city.GetRelayMode(4) == RelayMode.Data, "loaded city can change independently");

            Reject(saved.Network, new[] { Record(4, (RelayMode)42) }, "unknown mode rejected");
            Reject(saved.Network, new[] { Record(4, RelayMode.Power), Record(4, RelayMode.Data) }, "duplicate relay rejected");
            Reject(saved.Network, new[] { Record(100, RelayMode.Power) }, "out-of-range ID rejected");
            Reject(saved.Network, new[] { Record(-1, RelayMode.Power) }, "negative ID rejected");
            Reject(saved.Network, new[] { Record(0, RelayMode.Power) }, "non-relay ID rejected");
            Reject(saved.Network, new[] { Record(2, RelayMode.Data), Record(4, RelayMode.Power), Record(6, RelayMode.Data) }, "slot overflow rejected");
            return "PASS: " + checks + " relay specialization JSON checks";
        }
    }
}
