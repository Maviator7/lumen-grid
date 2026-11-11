using System;
using System.Collections.Generic;
using Michi.Core;
namespace Michi.Checks
{
    public static class CardinalExpansionChecks
    {
        static int checks;
        static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("CARDINAL: "+message);}
        static void Stable(NetworkSimulation sim){for(int i=0;i<15000&&!sim.ExpansionReady;i++)sim.Step(1.0/60);Check(sim.ExpansionReady,"reference stabilizes seed="+sim.Map.Seed+" expansions="+sim.ExpansionCount+" "+sim.Summary());}
        static string Geometry(NetworkSimulation s){string text="";foreach(var n in s.Nodes)text+=n.Id+":"+n.X.ToString("R")+","+n.Z.ToString("R")+";";foreach(var b in s.AllObstacles)text+=b.X.ToString("R")+","+b.Z.ToString("R")+","+b.Width+","+b.Depth+";";foreach(var a in s.Sectors)text+=(int)a.Direction+":"+(int)a.Terrain+";";return text;}
        public static string Run()
        {
            checks=0;
            foreach(int seed in new[]{291026,664020296,42,-19}){
                var s=new NetworkSimulation(CityMap.Generate(seed),true,1);s.LoadSolution();Stable(s);
                double time=s.Time;string before=Geometry(s);int count=s.Nodes.Length,links=s.Links.Count;
                var offers=s.ExpansionOffers;Check(offers.Length==4,"four choices");Check(Geometry(s)==before&&s.Time==time&&s.Nodes.Length==count&&s.Links.Count==links,"preview never changes live city");
                Check(offers[2].Terrain!=offers[3].Terrain,"east and west have distinct terrain choices");
                for(int i=0;i<4;i++){
                    var direction=(ExpansionDirection)(i+1);Check(offers[i].Direction==direction,"stable cardinal offer order");Check(offers[i].Available,"advertised solvable choice seed="+seed+" direction="+direction+" reason="+offers[i].UnavailableReason);
                    var copy=NetworkSimulation.RestoreSave(s.CaptureSave());Check(copy.Expand(direction),"confirmed direction opens");var sector=copy.Sectors[copy.Sectors.Count-1];Check(sector.Direction==direction,"sector records exact direction");
                    for(int j=0;j<s.Nodes.Length;j++)Check(copy.Nodes[j].X==s.Nodes[j].X&&copy.Nodes[j].Z==s.Nodes[j].Z,"old sites never move");
                    foreach(var e in s.Links.Values)Check(copy.Links.ContainsKey(e.Id)&&!copy.IsBlocked(copy.Nodes[e.A],copy.Nodes[e.B]),"new terrain preserves old wiring");
                    if(i>=2){
                        double extreme=s.Nodes[0].X;foreach(var n in s.Nodes)extreme=i==2?Math.Max(extreme,n.X):Math.Min(extreme,n.X);
                        Check(i==2?copy.Nodes[sector.District].X>extreme:copy.Nodes[sector.District].X<extreme,"lateral district actually grows outward");
                        var labels=new HashSet<string>();for(int j=s.Nodes.Length;j<copy.Nodes.Length;j++)if(copy.Nodes[j].Kind==NetworkNodeKind.Relay)Check(labels.Add(copy.Nodes[j].Name),"lateral relay labels distinguish every added relay");
                        Check(sector.Obstacles.Count>0&&sector.DemandRoute.Count>0,"lateral terrain and alternative path are real geometry");
                        Check(copy.IsBlocked(copy.Nodes[sector.East],copy.Nodes[sector.District]),"terrain obstructs direct shortcut");
                    }
                    copy.LoadSolution();Check(copy.ReferenceValid,"all reference links obey restrictions and grants");Stable(copy);
                    string shape=Geometry(copy);var restored=NetworkSimulation.RestoreSave(copy.CaptureSave());Check(Geometry(restored)==shape&&restored.ExpansionCount==copy.ExpansionCount,"save preserves exact direction, layout and barriers");
                    var again=NetworkSimulation.RestoreSave(s.CaptureSave());Check(again.Expand(direction)&&Geometry(again)==shape,"seed reproduces identical expansion geometry");
                }
                foreach(var direction in new[]{ExpansionDirection.East,ExpansionDirection.North,ExpansionDirection.West,ExpansionDirection.South,ExpansionDirection.East,ExpansionDirection.West}){
                    Check(s.Expand(direction),"mixed direction sequence remains available seed="+seed+" "+direction);s.LoadSolution();Check(s.ReferenceValid,"mixed reference legal");Stable(s);s=NetworkSimulation.RestoreSave(s.CaptureSave());
                }
            }
            var tight=new NetworkSimulation(CityMap.Generate(1),true,1);tight.LoadSolution();Stable(tight);
            foreach(var offer in new[]{tight.ExpansionOffers[2],tight.ExpansionOffers[3]}){
                Check(offer.Available,"tight-map lateral offer remains usable");
                foreach(var route in new[]{offer.WestRoute,offer.EastRoute})foreach(var point in route)foreach(var site in tight.Nodes)Check(Math.Abs(point.X-site.X)>=1.55||Math.Abs(point.Z-site.Z)>=1.55,"new relay plinth does not overlap an existing site seed=1");
                foreach(var block in offer.Obstacles)Check(!(Math.Abs(block.X-(offer.DistrictPoint.X+.7))<(block.Width+3.2)/2&&Math.Abs(block.Z-(offer.DistrictPoint.Z+.2))<(block.Depth+3.2)/2),"new district footprint does not overlap terrain");
            }
            var isolation=new NetworkSimulation(CityMap.Generate(42),true,1);isolation.LoadSolution();Stable(isolation);isolation.Expand(ExpansionDirection.East);
            string originalShape=Geometry(isolation);var snapshot=isolation.CaptureSave();snapshot.Sectors[0].Obstacles[0].X+=100;Check(Geometry(isolation)==originalShape,"captured terrain is independent of live city");
            snapshot=isolation.CaptureSave();var isolatedCopy=NetworkSimulation.RestoreSave(snapshot);snapshot.Sectors[0].Obstacles[0].X+=100;Check(Geometry(isolatedCopy)==originalShape,"restored terrain is independent of input snapshot");
            var badBarrier=isolation.CaptureSave();badBarrier.Sectors[0].Obstacles[0].Width=-1;bool badRejected=false;try{NetworkSimulation.RestoreSave(badBarrier);}catch(ArgumentException){badRejected=true;}Check(badRejected,"invalid saved rectangle rejected");
            var farRoute=isolation.CaptureSave();var farSector=farRoute.Sectors[0];farSector.DemandRoute=new[]{new SavedEdge{A=farSector.East,B=2},new SavedEdge{A=2,B=farSector.District}};badRejected=false;try{NetworkSimulation.RestoreSave(farRoute);}catch(ArgumentException){badRejected=true;}Check(badRejected,"impossible saved route lengths rejected");
            var badRoute=isolation.CaptureSave();badRoute.Sectors[0].DemandRoute[0].A=0;badRejected=false;try{NetworkSimulation.RestoreSave(badRoute);}catch(ArgumentException){badRejected=true;}Check(badRejected,"broken demand-route chain rejected");
            var classic=new NetworkSimulation(CityMap.Generate(42),true);classic.LoadSolution();Stable(classic);foreach(var direction in new[]{ExpansionDirection.East,ExpansionDirection.West}){Check(classic.Expand(direction),"classic saves support lateral growth");classic.LoadSolution();Stable(classic);}
            var legacy=new NetworkSimulation(CityMap.Generate(291026),true);legacy.LoadSolution();Stable(legacy);legacy.Expand(false);legacy.LoadSolution();var old=legacy.CaptureSave();old.Sectors[0].Direction=ExpansionDirection.None;old.Sectors[0].Obstacles=null;old.Sectors[0].DemandRoute=null;
            var migrated=NetworkSimulation.RestoreSave(old);Check(migrated.Sectors[0].Direction==ExpansionDirection.South,"missing legacy direction infers original south bool");Check(migrated.Sectors[0].Obstacles.Count==0,"legacy map gains no hidden barriers");
            var invalid=migrated.CaptureSave();invalid.Sectors[0].Direction=(ExpansionDirection)99;bool rejected=false;try{NetworkSimulation.RestoreSave(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid saved directions rejected");
            return "PASS: "+checks+" cardinal expansion checks";
        }
#if CARDINAL_STANDALONE
        public static void Main(){Console.WriteLine(Run());}
#endif
    }
}
