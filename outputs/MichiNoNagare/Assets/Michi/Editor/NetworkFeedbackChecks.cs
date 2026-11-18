using System;
using Michi.Core;

public static class NetworkFeedbackChecks
{
    static void Check(bool condition, string reason)
    {
        if (!condition) throw new Exception(reason);
    }

    public static void Run()
    {
        var city = new NetworkSimulation(CityMap.Generate(42), true, 1);
        city.LoadEmpty();
        var issue = city.InspectDistrictData(7);
        Check(issue.Block == DataBlock.Disconnected, "disconnected district must be explained");
        for (int i = 0; i < 3900; i++) city.Step(1.0 / 60);
        city.SetCommunicationPeaks(true);
        Check(city.RecentDistrictSuccess(7) == 0, "district recent success reports observed failed requests");
        double time = city.Time;
        int failures = city.Expired;
        var trial = city.BeginPeakTrial();
        while (!trial.Complete) trial.Advance(600);
        Check(trial.Districts.Count == 3, "one result per district");
        Check(trial.Districts[0].Disconnected, "failed trial distinguishes disconnected districts");
        Check(trial.Districts[0].PeakFailures < trial.Districts[0].DataPeakFailures + trial.Districts[0].PowerPeakFailures,
              "overlapping peaks do not double-count failures");
        var legacy = city.CaptureSave();
        foreach (var sample in legacy.Quality) sample.Failed = null;
        Check(NetworkSimulation.RestoreSave(legacy).RecentDistrictSuccess(7) == null,
              "legacy saves without district outcomes display unavailable");
        Check(NetworkSimulation.RestoreSave(city.CaptureSave()).RecentDistrictSuccess(7) == 0,
              "new saves retain district outcomes");
        Check(trial.Districts[0].Failed > 0 && trial.Districts[0].MinimumPower < .98, "trial detects requested load failures");
        Check(trial.Districts[0].DataPeakFailures > 0, "trial reaches and measures communication peak");
        Check(time == city.Time && failures == city.Expired, "trial must not advance live city");
        city.LoadSolution();
        city.InstallBattery(5);
        for (int i = 0; i < 240; i++) city.Step(1.0 / 60);
        var before = city.CaptureSave();
        trial = city.BeginPeakTrial();
        while (!trial.Complete) trial.Advance(600);
        var after = city.CaptureSave();
        Check(before.Time == after.Time && before.Batteries[0].Energy == after.Batteries[0].Energy &&
              before.StableSeconds == after.StableSeconds && before.Packets.Length == after.Packets.Length,
              "trial preserves active packets, storage and expansion clock");
        Check(city.InspectDistrictData(7).Route.Count > 1, "connected districts expose a route");
        Check(city.SetRelayMode(4, RelayMode.Power), "central hub can prioritize power");
        for (int i = 0; i < 360; i++) city.Step(1.0 / 60);
        bool localized = false;
        foreach (var district in city.Nodes)
        {
            if (district.Kind != NetworkNodeKind.District) continue;
            var trace = city.InspectDistrictData(district.Id);
            localized |= trace.Block == DataBlock.Relay && trace.Node == 4 && trace.ObservedRoute;
        }
        Check(localized, "measured delay at the central hub must identify the hub and used route");
        var choices = new System.Collections.Generic.HashSet<int>();
        foreach (var offer in new NetworkSimulation(CityMap.Generate(42), true, 1).ExpansionOffers)
            if (offer.Available) choices.Add(offer.Role);
        Check(choices.Count >= 3, "seed 42 must offer different demand roles across directions");
        Console.WriteLine("Network feedback checks passed.");
    }

#if NETWORK_FEEDBACK_STANDALONE
    public static void Main() => Run();
#endif
}
