using System;
namespace Michi.Core
{
    public enum DistrictDemandProfile { Standard, HeavyIndustry, Research }

    public sealed partial class NetworkSimulation
    {
        public const double HeavyIndustryPower=9,ResearchPower=3,ResearchBaseData=2,ResearchPeakData=5;
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
        static double DistrictBasePower(int role,DistrictDemandProfile profile)
        {
            if(profile==DistrictDemandProfile.HeavyIndustry)return HeavyIndustryPower;
            if(profile==DistrictDemandProfile.Research)return ResearchPower;
            return DistrictBasePower(role);
        }
        double DistrictPower(GridNode n){return n.DemandProfile==DistrictDemandProfile.Standard&&n.Role==8&&HousingPeak?5:DistrictBasePower(n.Role,n.DemandProfile);}
        static readonly int[] ExpansionRoleSalts = { 103, 407, 809, 1201 };

        int ExpansionRole(ExpansionDirection direction)
        {
            int index = (int)direction - 1;
            int seed = unchecked(Map.Seed + ExpansionCount * 7919);
            int role = new CityRandom(unchecked(seed + ExpansionRoleSalts[index])).Int(3);
            if (!ModernRules || ExpansionCount != 0 || index < 2) return 7 + role;
            // Some seeds produce the same contract in every direction on the first choice.
            foreach (int salt in ExpansionRoleSalts)
                if (new CityRandom(unchecked(seed + salt)).Int(3) != role) return 7 + role;
            return 7 + (role + index - 1) % 3;
        }

        void ConfigureExpansionDemand(ExpansionOffer offer)
        {
            if(ModernRules)offer.DemandProfile=offer.Role==7?DistrictDemandProfile.HeavyIndustry:offer.Role==8?DistrictDemandProfile.Research:DistrictDemandProfile.Standard;
            RefreshExpansionDemand(offer);
        }
        void RefreshExpansionDemand(ExpansionOffer offer)
        {
            offer.Demand=ModernRules?DistrictBasePower(offer.Role,offer.DemandProfile):offer.Role==7?6:offer.Role==8?3:4;
            offer.Title=DirectionName(offer.Direction)+"の"+ExpansionDistrictName(offer.Role,offer.DemandProfile,true);
        }
        static string ExpansionDistrictName(int role,DistrictDemandProfile profile,bool full)
        {
            if(profile==DistrictDemandProfile.HeavyIndustry)return full?"重工業地区":"重工業区";
            if(profile==DistrictDemandProfile.Research)return full?"研究地区":"研究区";
            return role==7?(full?"工業地区":"工業区"):role==8?(full?"住宅地区":"居住区"):(full?"医療地区":"医療区");
        }
        void UpdateDistrictDemand()
        {
            if(!ModernRules)return;bool changed=false;
            foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District){double demand=DistrictPower(n);if(Math.Abs(n.PowerDemand-demand)>.00001){n.PowerDemand=demand;changed=true;}}
            if(changed){AllocatePower();foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District&&n.PowerFraction<.98)StableSeconds=0;}
        }
    }
}
