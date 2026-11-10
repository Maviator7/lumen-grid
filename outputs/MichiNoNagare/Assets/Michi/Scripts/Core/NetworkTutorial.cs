namespace Michi.Core
{
    public sealed class NetworkTutorial
    {
        public int Step {get;private set;} = -1;
        public bool Active {get{return Step>=0;}}
        public bool HubInspected {get;private set;}
        public double HubPowerBefore {get;private set;}
        public double HubCapacityBefore {get;private set;}
        public bool CanAdvance {get{return Step==0||Step==4||Step==13||(Step==5&&HubInspected);}}
        public bool ShowsHubDetails {get{return HubInspected&&Step>=5&&Step<=11;}}
        public void Begin(NetworkSimulation sim){HubInspected=false;HubPowerBefore=HubCapacityBefore=0;sim.LoadInitial();sim.BeginEdit();sim.Remove(NetworkSimulation.EdgeId(0,2));sim.EndEdit();Step=0;}
        public void Skip(){Step=-1;}
        public void Next(){if(Step==0)Step=1;else if(Step==4)Step=5;else if(Step==5&&HubInspected)Step=6;else if(Step==13)Skip();}
        public bool AllowsTool(int tool){return !Active||((Step==1||Step==6)&&tool==1)||(Step==9&&tool==2);}
        public bool AllowsNode(int id){return !Active||id==TargetNode;}
        public bool AllowsLink(int id){return !Active||(Step==10&&id==NetworkSimulation.EdgeId(2,4));}
        public bool AllowsLaunch {get{return !Active||Step==11;}}
        public int TargetNode {get{return Step==2?0:Step==3?2:Step==5?4:Step==7?2:Step==8?5:-1;}}
        public int TargetTool {get{return Step==1||Step==6?1:Step==9?2:-1;}}
        public void Tick(NetworkSimulation sim,int tool,int pending,int selected)
        {
            switch(Step){
                case 1:if(tool==1)Step=2;break;
                case 2:if(pending==0)Step=3;break;
                case 3:if(Is(sim,0,2,LinkKind.PowerOnly))Step=4;else if(pending<0)Step=2;break;
                case 5:if(selected==4&&!HubInspected){HubInspected=true;HubPowerBefore=sim.Nodes[4].PowerLoad;HubCapacityBefore=sim.Nodes[4].Capacity;}break;
                case 6:if(tool==1)Step=7;break;
                case 7:if(pending==2)Step=8;break;
                case 8:if(Is(sim,2,5,LinkKind.PowerOnly))Step=9;else if(pending<0)Step=7;break;
                case 9:if(tool==2)Step=10;break;
                case 10:if(Is(sim,2,4,LinkKind.DataOnly))Step=11;break;
                case 11:if(sim.Phase==RoundPhase.Running)Step=12;break;
                case 12:if(sim.Phase==RoundPhase.Result)Step=sim.Passed?13:11;break;
            }
        }
        static bool Is(NetworkSimulation s,int a,int b,LinkKind kind){GridLink link;return s.Links.TryGetValue(NetworkSimulation.EdgeId(a,b),out link)&&link.Kind==kind;}
    }
}
