using System;
using System.Collections.Generic;

namespace Michi.Core
{
    public enum DataBlock { None, Disconnected, Source, TransitTime, Relay, Link, DistrictPower }

    public sealed class DataInspection
    {
        public readonly DataBlock Block;
        public readonly int Node, Link;
        public readonly bool ObservedRoute;
        public readonly IReadOnlyList<int> Route;

        internal DataInspection(DataBlock block, int node, int link, bool observedRoute, List<int> route)
        {
            Block = block;
            Node = node;
            Link = link;
            ObservedRoute = observedRoute;
            Route = route == null ? Array.Empty<int>() : (IReadOnlyList<int>)route.AsReadOnly();
        }
    }

    public sealed partial class NetworkSimulation
    {
        struct DataObservation
        {
            public double Time;
            public DataBlock Block;
            public int Node, Link;
            public List<int> Route;
            public bool ObservedRoute;
        }

        readonly Dictionary<int, DataObservation> dataBlocks = new Dictionary<int, DataObservation>();
        readonly Dictionary<int, List<int>> lastDataRoutes = new Dictionary<int, List<int>>();

        void ClearDataObservations()
        {
            dataBlocks.Clear();
            lastDataRoutes.Clear();
        }

        void ObserveDataBlock(int district, DataBlock reason, int node, int link, List<int> path, bool observedRoute = false)
        {
            dataBlocks[district] = new DataObservation
            {
                Time = Time, Block = reason, Node = node, Link = link, Route = path, ObservedRoute = observedRoute
            };
        }

        void TrackPacketWait(DataPacket packet, GridLink link, bool nodeReady, double dt)
        {
            if (!Continuous) return;
            packet.WaitAtHop += dt;
            if (packet.WaitAtHop < .22) return;
            bool source = Nodes[packet.Node].Kind == NetworkNodeKind.DataCore;
            var reason = nodeReady ? DataBlock.Link : source ? DataBlock.Source : DataBlock.Relay;
            ObserveDataBlock(packet.Destination, reason, packet.Node, nodeReady ? link.Id : -1, packet.Path, true);
        }

        public DataInspection InspectDistrictData(int district)
        {
            if (district < 0 || district >= Nodes.Length || Nodes[district].Kind != NetworkNodeKind.District)
                throw new ArgumentOutOfRangeException(nameof(district));

            List<int> route;
            bool observed = lastDataRoutes.TryGetValue(district, out route);
            var paths = SourcePaths(Nodes[district], SignalKind.Data);
            if (paths.Count == 0)
                return new DataInspection(DataBlock.Disconnected, district, -1, false, null);
            if (!observed)
            {
                for (int i = Packets.Count - 1; i >= 0; i--)
                {
                    if (Packets[i].Destination != district) continue;
                    route = Packets[i].Path;
                    observed = true;
                    break;
                }
            }
            if (!observed) route = paths[0];
            if (Nodes[district].PowerFraction < .95)
                return new DataInspection(DataBlock.DistrictPower, district, -1, observed, route);
            DataObservation trace;
            if (dataBlocks.TryGetValue(district, out trace) && Time - trace.Time <= 6)
                return new DataInspection(trace.Block, trace.Node, trace.Link, trace.Route != null ? trace.ObservedRoute : observed, trace.Route ?? route);
            return new DataInspection(DataBlock.None, -1, -1, observed, route);
        }

        public PeakTrial BeginPeakTrial() => new PeakTrial(RestoreSave(CaptureSave()));
    }

    public sealed class DistrictTrialResult
    {
        public readonly int Node;
        public readonly bool Disconnected;
        public double MinimumPower { get; internal set; } = 1;
        public int Delivered { get; internal set; }
        public int Failed { get; internal set; }
        public int DataPeakFailures { get; internal set; }
        public int PowerPeakFailures { get; internal set; }
        public int PeakFailures { get; internal set; }
        internal int LastDelivered, LastFailed;

        internal DistrictTrialResult(GridNode node, bool disconnected)
        {
            Node = node.Id;
            Disconnected = disconnected;
            MinimumPower = node.PowerFraction;
            LastDelivered = node.Delivered;
            LastFailed = node.Failed;
        }
    }

    public sealed class PeakTrial
    {
        const double Tick = 1.0 / 60;
        readonly NetworkSimulation copy;
        readonly List<DistrictTrialResult> results = new List<DistrictTrialResult>();
        readonly int totalSteps;
        readonly int startDelivered;
        readonly double startDelay;
        int steps;
        public IReadOnlyList<DistrictTrialResult> Districts { get; }
        public bool Complete => steps >= totalSteps;
        public double Progress => (double)steps / totalSteps;
        public double Seconds => totalSteps * Tick;
        public bool SawPowerPeak { get; private set; }
        public bool SawDataPeak { get; private set; }
        public int UnderpoweredBoosts { get; private set; }
        public double AverageLatency
        {
            get
            {
                int delivered = copy.Successful - startDelivered;
                return delivered > 0 ? Math.Max(0, (copy.AverageLatency * copy.Successful - startDelay) / delivered) : 0;
            }
        }

        internal PeakTrial(NetworkSimulation copy)
        {
            this.copy = copy;
            startDelivered = copy.Successful;
            startDelay = copy.AverageLatency * copy.Successful;
            foreach (var node in copy.Nodes)
                if (node.Kind == NetworkNodeKind.District) results.Add(new DistrictTrialResult(node, !copy.DistrictDataConnected(node.Id)));
            Districts = results.AsReadOnly();
            // Include the next complete peak of each enabled demand cycle and time for its last requests.
            double powerWindow = copy.ModernRules ? copy.HousingChangeIn + (copy.HousingPeak ? 90 : 30) : 0;
            double dataWindow = copy.CommunicationPeaksEnabled ? copy.DataPeakChangeIn + (copy.DataPeakActive ? 120 : 30) : 0;
            if (copy.HasResearchDistrict)
                dataWindow = Math.Max(dataWindow, copy.ResearchBurstChangeIn + (copy.ResearchBurstActive ? 120 : 30));
            totalSteps = (int)Math.Ceiling((Math.Max(23, Math.Max(powerWindow, dataWindow)) + 7) / Tick);
        }

        public void Advance(int stepBudget)
        {
            while (stepBudget-- > 0 && !Complete)
            {
                copy.Step(Tick);
                steps++;
                SawPowerPeak |= copy.HousingPeak;
                bool dataPeak = copy.DataPeakActive || copy.ResearchBurstActive;
                SawDataPeak |= dataPeak;
                UnderpoweredBoosts = Math.Max(UnderpoweredBoosts, copy.UnderpoweredCenterCount);
                foreach (var result in results)
                {
                    var district = copy.Nodes[result.Node];
                    result.MinimumPower = Math.Min(result.MinimumPower, district.PowerFraction);
                    int failures = district.Failed - result.LastFailed;
                    result.Failed += failures;
                    if (dataPeak || copy.HousingPeak) result.PeakFailures += failures;
                    if (dataPeak) result.DataPeakFailures += failures;
                    if (copy.HousingPeak) result.PowerPeakFailures += failures;
                    result.Delivered += district.Delivered - result.LastDelivered;
                    result.LastDelivered = district.Delivered;
                    result.LastFailed = district.Failed;
                }
            }
        }
    }
}
