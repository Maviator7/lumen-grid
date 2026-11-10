using System;
namespace Michi.Core
{
    public sealed partial class NetworkSimulation
    {
        public const int CurrentRulesVersion=1;
        public readonly int RulesVersion;
        public bool ModernRules {get{return Continuous&&RulesVersion==CurrentRulesVersion;}}
        public double CableCost {get{double used=0;foreach(var l in Links.Values)used+=LinkCost(l.Length,l.Kind);return used;}}
        public double CableCostLimit {get{return AvailableCable*(ModernRules?1.15:1);}}
        public double LinkCost(double length,LinkKind kind){return length*(ModernRules&&kind!=LinkKind.Shared?1.75:1);}
        public bool HousingPeak {get{return ModernRules&&Time%90>=60;}}
        public double HousingChangeIn {get{return ModernRules?(HousingPeak?90-Time%90:60-Time%90):0;}}
        public double RequestDeadline(int district){return !ModernRules?Deadline:DistrictDeadline(Nodes[district].Role);}
        public static double DistrictDeadline(int role){return role==9?2.8:role==7?7:5;}
        public static double DistrictBasePower(int role){return role==7?7:3;}
        double DistrictPower(GridNode n){return n.Role==8&&HousingPeak?5:DistrictBasePower(n.Role);}
        void UpdateDistrictDemand()
        {
            if(!ModernRules)return;bool changed=false;
            foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District){double demand=DistrictPower(n);if(Math.Abs(n.PowerDemand-demand)>.00001){n.PowerDemand=demand;changed=true;}}
            if(changed){AllocatePower();foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District&&n.PowerFraction<.98)StableSeconds=0;}
        }
    }
}
