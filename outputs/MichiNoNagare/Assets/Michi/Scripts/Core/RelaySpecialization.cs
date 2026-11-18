using System;
using System.Collections.Generic;

namespace Michi.Core
{
    public enum RelayMode { Shared, Power, Data }

    [Serializable]
    public sealed class SavedRelaySpecialization
    {
        public int Node;
        public RelayMode Mode;
    }

    public sealed partial class NetworkSimulation
    {
        readonly Dictionary<int, RelayMode> relayModes = new Dictionary<int, RelayMode>();

        public int SpecializedRelayCount => relayModes.Count;
        public int RelaySpecializationLimit => Continuous && Map.Generated ? 2 + ExpansionCount : 0;

        bool IsRelay(int node) => node >= 0 && node < Nodes.Length && Nodes[node].Kind == NetworkNodeKind.Relay;

        public RelayMode GetRelayMode(int node) => relayModes.TryGetValue(node, out var mode) ? mode : RelayMode.Shared;

        public bool CanSetRelayMode(int node, RelayMode mode, out string reason)
        {
            reason = null;
            if (!CanEdit || RelaySpecializationLimit == 0) reason = "この都市では中継を改装できません。";
            else if (!IsRelay(node) || mode < RelayMode.Shared || mode > RelayMode.Data) reason = "改装する中継と型を選んでください。";
            else
            {
                var current = GetRelayMode(node);
                if (current == mode) reason = "現在と同じ型です。";
                else if (current == RelayMode.Shared && SpecializedRelayCount >= RelaySpecializationLimit)
                    reason = "特化枠が埋まっています。他の中継を共用型に戻すと枠が空きます。";
            }
            return reason == null;
        }

        public bool SetRelayMode(int node, RelayMode mode)
        {
            if (!CanSetRelayMode(node, mode, out _)) return false;
            if (mode == RelayMode.Shared) relayModes.Remove(node);
            else relayModes[node] = mode;
            batteryAllocationValid = false;
            AllocatePower();
            foreach (var n in Nodes)
                nodeCredit[n.Id] = Math.Min(nodeCredit[n.Id], DataCreditLimit(NodeDataRate(n.Id)));
            ClearDataObservations();
            StableSeconds = 0;
            offerCache = null;
            return true;
        }

        public double RelayPowerCapacity(int node) => RelayPowerCapacity(node, GetRelayMode(node));

        public double RelayPowerCapacity(int node, RelayMode mode) =>
            Nodes[node].Capacity * (mode == RelayMode.Power ? 1.75 : mode == RelayMode.Data ? .25 : 1);

        public double RelayDataCapacity(int node, RelayMode mode) =>
            Nodes[node].Capacity * (mode == RelayMode.Data ? 1.75 : mode == RelayMode.Power ? .25 : 1);

        public double NodeDataRate(int node)
        {
            var n = Nodes[node];
            var mode = GetRelayMode(node);
            if (mode == RelayMode.Shared) return Math.Max(0, n.Capacity - n.PowerLoad);
            return RelayDataCapacity(node, mode) * Math.Max(0, 1 - n.PowerLoad / RelayPowerCapacity(node, mode));
        }

        static double DataCreditLimit(double rate) => Math.Max(1, rate * .5);

        void CopyRelayModesTo(NetworkSimulation copy)
        {
            foreach (var entry in relayModes) copy.relayModes.Add(entry.Key, entry.Value);
        }

        SavedRelaySpecialization[] CaptureRelayModes()
        {
            var records = new List<SavedRelaySpecialization>(relayModes.Count);
            foreach (var entry in relayModes)
                records.Add(new SavedRelaySpecialization { Node = entry.Key, Mode = entry.Value });
            records.Sort((a, b) => a.Node.CompareTo(b.Node));
            return records.ToArray();
        }

        void RestoreRelayModes(SavedRelaySpecialization[] records)
        {
            if (records == null) return;
            RequireSave(records.Length <= RelaySpecializationLimit);
            foreach (var record in records)
            {
                RequireSave(record != null && IsRelay(record.Node) &&
                    (record.Mode == RelayMode.Power || record.Mode == RelayMode.Data) && !relayModes.ContainsKey(record.Node));
                relayModes.Add(record.Node, record.Mode);
            }
        }
    }
}
