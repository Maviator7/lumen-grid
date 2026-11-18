using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Michi.Core;

namespace Michi.Checks
{
    public static class RelaySpecializationChecks
    {
        static int checks;

        static void Check(bool ok, string message)
        {
            checks++;
            if (!ok) throw new Exception("RELAY SPECIALIZATION: " + message);
        }

        static void Near(double actual, double expected, string message)
        {
            Check(Math.Abs(actual - expected) < 1e-7, message + " actual=" + actual + " expected=" + expected);
        }

        static NetworkSimulation Fixture()
        {
            var nodes = CityMap.Tutorial().CreateNodes();
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].X = 30 + i % 4;
                nodes[i].Z = 30 + i / 4;
                nodes[i].PowerDemand = 0;
            }
            nodes[3].Kind = NetworkNodeKind.Reactor;
            var edges = new[] { new[] { 0, 4 }, new[] { 1, 4 }, new[] { 3, 4 }, new[] { 4, 7 }, new[] { 4, 8 }, new[] { 4, 9 } };
            return new NetworkSimulation(new CityMap(42, true, nodes, edges, -100, 1, 500, 99, new CityObstacle[0]), true);
        }

        static void Advance(NetworkSimulation city, int ticks)
        {
            for (int i = 0; i < ticks; i++) city.Step(1.0 / 60);
        }

        static void VerifyCapacity()
        {
            var city = Fixture();
            foreach (int id in new[] { 2, 4, 5 })
            {
                double basis = city.Nodes[id].Capacity;
                Near(city.RelayPowerCapacity(id), basis, "relay " + id + " starts shared");
                Near(city.RelayPowerCapacity(id, RelayMode.Power), 1.75 * basis, "scaled power capacity");
                Near(city.RelayPowerCapacity(id, RelayMode.Data), .25 * basis, "scaled data-mode power capacity");
                Near(city.RelayDataCapacity(id, RelayMode.Power), .25 * basis, "scaled power-mode data capacity");
                Near(city.RelayDataCapacity(id, RelayMode.Data), 1.75 * basis, "scaled data capacity");
            }

            foreach (int id in new[] { 7, 8, 9 }) city.Nodes[id].PowerDemand = 10;
            city.ReturnToDesign();
            double original = city.Nodes[4].Capacity;
            Near(city.Nodes[4].PowerLoad, original, "shared aggregate power uses original capacity");
            Near(city.NodeDataRate(4), 0, "shared relay saturated by power");
            Check(city.SetRelayMode(4, RelayMode.Power), "central hub eligible for power mode");
            Near(city.Nodes[4].PowerLoad, 30, "multiple input and output links use the higher effective capacity");
            Near(city.NodeDataRate(4), .25 * original * (1 - 30 / (1.75 * original)), "mixed power-mode data capacity");
            Near(city.InspectPower(4).Throughput, 30, "power inspection reflects retrofit");
            Check(city.InspectPower(4).Sources.Count == 2, "boost aggregates both sources");
            city.BeginEdit();
            city.Remove(NetworkSimulation.EdgeId(3, 4));
            city.EndEdit();
            Near(city.Nodes[4].PowerLoad, 16, "single shared link retains 16MW limit");
            Check(city.Undo(), "wiring undo succeeds");
            Check(city.GetRelayMode(4) == RelayMode.Power, "wiring undo keeps specialization");
            Near(city.Nodes[4].PowerLoad, 30, "wiring undo restores second incoming power line");
            Check(city.SetRelayMode(4, RelayMode.Data), "specialization switches directly");
            Near(city.Nodes[4].PowerLoad, .25 * original, "data mode imposes lower electrical capacity");
            Near(city.NodeDataRate(4), 0, "fully used electrical capacity exhausts the data mode");
            foreach (int id in new[] { 7, 8, 9 }) city.Nodes[id].PowerDemand = id == 7 ? 2 : 0;
            city.ReturnToDesign();
            Near(city.NodeDataRate(4), 1.75 * original * (1 - 2 / (.25 * original)), "mixed data-mode data capacity");
            Check(city.SetRelayMode(4, RelayMode.Shared), "restore shared mode");
            Near(city.NodeDataRate(4), original - 2, "shared mode matches legacy subtraction");
            Near(city.NodeDataRate(0), city.Nodes[0].Capacity - city.Nodes[0].PowerLoad, "non-relay processing keeps legacy calculation");
            Near(city.Nodes[4].Capacity, original, "multiple mode changes keep the saved base capacity");
        }

        static void VerifyLiveChanges()
        {
            var city = new NetworkSimulation(CityMap.Generate(42), true, 1);
            city.LoadSolution();
            Advance(city, 61 * 60);
            var saved = city.CaptureSave();
            saved.Waiting = new[] { new SavedRequest { Destination = 7, Born = city.Time } };
            saved.NodeCredit[4] = 8;
            city = NetworkSimulation.RestoreSave(saved);
            var packet = city.Packets.First(p => p.Travelling && p.Progress > 0);
            var path = packet.Path;
            int index = packet.Index;
            double progress = packet.Progress, time = city.Time;
            var pending = city.PendingData.Single();
            Check(city.ExpansionReady, "stable live fixture");
            Check(!city.SetRelayMode(4, RelayMode.Shared) && city.ExpansionReady, "unchanged mode preserves stability");
            Check(city.SetRelayMode(4, RelayMode.Power) && city.StableSeconds == 0, "real mode change resets stability");
            Check(city.Packets.Contains(packet) && ReferenceEquals(packet.Path, path) && packet.Travelling &&
                packet.Index == index && packet.Progress == progress, "in-flight packet and path keep their progress");
            Check(ReferenceEquals(city.PendingData.Single(), pending), "pending request is kept");
            Near(city.Time, time, "retrofit does not advance the clock");
            var after = city.CaptureSave();
            double clamp = Math.Max(1, city.NodeDataRate(4) / 2);
            Near(after.NodeCredit[4], clamp, "stored node credits shrink to half a second of effective room");
            Check(city.SetRelayMode(4, RelayMode.Shared), "switch back with no elapsed time");
            Near(city.CaptureSave().NodeCredit[4], clamp, "larger room cannot grant credits");
            Advance(city, 1);
            Check(packet.Progress != progress || packet.Index != index, "next tick advances the kept packet");

            city = Fixture();
            city.Nodes[4].Capacity = 4;
            Check(city.InstallBattery(4), "install storage on same relay");
            Advance(city, 2);
            double energy = city.BatteryAt(4).Energy;
            Near(city.BatteryAt(4).ChargeRate, 2, "shared relay charges at full battery rate");
            Check(city.SetRelayMode(4, RelayMode.Data), "retrofit with a charging battery");
            Near(city.BatteryAt(4).ChargeRate, 1, "charge preview respects data-mode power limit");
            Near(city.BatteryAt(4).Energy, energy, "retrofit leaves energy intact");
            Advance(city, 2);
            energy += 2.0 / 60 * NetworkSimulation.BatteryEfficiency;
            Near(city.BatteryAt(4).Energy, energy, "next ticks, including cache reuse, charge at new rate");
            city.Nodes[0].PowerSupply = city.Nodes[3].PowerSupply = 0;
            Check(city.SetCenterBoost(1, true), "boosted center can use stored power");
            Near(city.Nodes[1].PowerReceived, 1, "discharge limited by converted relay");
            Near(city.Nodes[1].DataSupply, 9, "center output updates with partial power");
            Near(city.BatteryAt(4).Energy, energy, "power preview still leaves battery energy intact");
            Advance(city, 1);
            Near(city.BatteryAt(4).Energy, energy - 1.0 / 60, "energy integrates once at new discharge rate");
        }

        static void VerifySlotsAndExpansion()
        {
            var city = new NetworkSimulation(CityMap.Generate(42), true, 1);
            string reason;
            Check(city.RelaySpecializationLimit == 2, "main city starts with two slots");
            Check(!city.SetRelayMode(7, RelayMode.Power) && !city.SetRelayMode(-1, RelayMode.Power) &&
                !city.SetRelayMode(2, (RelayMode)100), "bad target and mode rejected");
            Check(!new NetworkSimulation().SetRelayMode(4, RelayMode.Power), "tutorial cannot specialize");
            Check(city.SetRelayMode(4, RelayMode.Data) && city.SetRelayMode(5, RelayMode.Power), "both modes consume slots");
            Check(!city.CanSetRelayMode(6, RelayMode.Data, out reason) && reason.Contains("共用型"), "cap explains how to free a slot");
            Check(city.SetRelayMode(4, RelayMode.Power) && city.SpecializedRelayCount == 2, "specialized switch uses no additional slot");
            Check(city.SetRelayMode(4, RelayMode.Shared) && city.SetRelayMode(6, RelayMode.Power), "return to shared releases a slot");
            city.LoadEmpty();
            Check(city.SpecializedRelayCount == 2, "wiring removal keeps both specializations");
            city.LoadInitial();
            Check(city.SpecializedRelayCount == 0, "city reset clears specializations");

            city.LoadSolution();
            var sharedOffers = city.ExpansionOffers;
            Check(sharedOffers.Any(o => o.Available), "shared reference has viable offers");
            Check(city.SetRelayMode(2, RelayMode.Data), "limit the reference's original power outlet");
            var cloneMethod = typeof(NetworkSimulation).GetMethod("PlanningClone", BindingFlags.Instance | BindingFlags.NonPublic);
            var clone = (NetworkSimulation)cloneMethod.Invoke(city, null);
            clone.LoadSolution();
            Check(clone.GetRelayMode(2) == RelayMode.Data && clone.PowerCoverage < .98, "planning clone and reference retain the chosen capacity");
            Check(clone.SetRelayMode(2, RelayMode.Shared) && city.GetRelayMode(2) == RelayMode.Data, "planning mode copies are independent");
            var changedOffers = city.ExpansionOffers;
            Check(!ReferenceEquals(sharedOffers, changedOffers), "mode change invalidates offer cache");
            Check(changedOffers.Count(o => o.Available) < sharedOffers.Count(o => o.Available), "candidate trials run with the restricted relay");
            Check(city.SetRelayMode(2, RelayMode.Shared), "restore shared allocation before growing");
            Advance(city, 61 * 60);
            Check(city.Expand(city.ExpansionOffers.First(o => o.Available).Direction), "shared expansion still viable");
            Check(city.RelaySpecializationLimit == 3, "expansion increases the slot limit");
            Check(city.Nodes.Skip(10).Where(n => n.Kind == NetworkNodeKind.Relay)
                .All(n => city.GetRelayMode(n.Id) == RelayMode.Shared), "new district relays start shared");
        }

        static NetworkSimulation PacketFixture(RelayMode mode, params int[] destinations)
        {
            var city = Fixture();
            foreach (var node in city.Nodes) node.DataDemand = 0;
            Check(city.SetRelayMode(4, mode), "configure relay for packet throughput");
            for (int i = 0; i < 60; i++)
            {
                foreach (int destination in destinations)
                    city.Packets.Add(new DataPacket { Destination = destination, Index = 1, Path = new List<int> { 1, 4, destination } });
            }
            return city;
        }

        static void VerifyPacketCaps()
        {
            var power = PacketFixture(RelayMode.Power, 7, 8);
            Advance(power, 60);
            Check(power.Nodes[4].Processed >= 4 && power.Nodes[4].Processed <= 6, "power relay throttles packet forwarding to its actual data rate");

            var data = PacketFixture(RelayMode.Data, 7);
            Advance(data, 60);
            var outgoing = data.Links[NetworkSimulation.EdgeId(4, 7)];
            Check(outgoing.Sent >= 16 && outgoing.Sent <= 17, "single data link keeps its 16 requests/s limit");

            data = PacketFixture(RelayMode.Data, 7, 8);
            Advance(data, 60);
            Check(data.Nodes[4].Processed >= 29 && data.Nodes[4].Processed <= 33, "multiple data links use specialized relay capacity in aggregate");
        }

        static void VerifyPeakChange()
        {
            var city = new NetworkSimulation(CityMap.Generate(42), true, 1);
            city.SetCommunicationPeaks(true);
            city.LoadSolution();
            Advance(city, 91 * 60);
            Check(city.DataPeakActive, "communication peak setup");
            double time = city.Time;
            Check(city.SetRelayMode(4, RelayMode.Data), "retrofit during peak");
            Near(city.Time, time, "peak retrofit keeps simulation clock");
            Check(city.DataPeakActive, "peak stays active during retrofit");
            Advance(city, 30 * 60);
            Check(!city.DataPeakActive && city.GetRelayMode(4) == RelayMode.Data, "post-peak processing keeps new mode");
        }

        public static string Run()
        {
            checks = 0;
            VerifyCapacity();
            VerifyLiveChanges();
            VerifySlotsAndExpansion();
            VerifyPacketCaps();
            VerifyPeakChange();
            return "PASS: " + checks + " relay specialization checks";
        }

#if RELAY_STANDALONE
        public static void Main() { Console.WriteLine(Run()); }
#endif
    }
}
