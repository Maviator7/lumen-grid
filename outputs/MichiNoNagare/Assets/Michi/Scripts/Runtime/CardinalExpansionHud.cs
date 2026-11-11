using System.Collections.Generic;
using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        void DrawCardinalExpansionPicker()
        {
            float width=Screen.width/scale,height=Screen.height/scale,x=width/2-530,y=height/2-373;
            Fill(new Rect(0,0,width,height),new Color(0,.012f,.025f,.91f));Frame(new Rect(x,y,1060,746),Cyan,true);
            Text(x+24,y+16,1012,38,"EXPAND / 街の形を選ぶ",headline,Cyan);
            Text(x+24,y+62,1012,28,"1–4で選択 / 地形・供給条件・資材を比較。通信は通常→ピーク。街と配線は保持します。",body,White);
            var offers=game.Simulation.ExpansionOffers;
            for(int i=0;i<offers.Length;i++)DrawExpansionCard(new Rect(x+24+(i%2)*514,y+102+(i/2)*288,498,274),offers[i],i);
            if(Button(new Rect(x+24,y+694,1012,32),"今の街へ戻る  /  ESC"))game.Expansion.Cancel();
        }
        void DrawExpansionCard(Rect r,ExpansionOffer offer,int index)
        {
            Color accent=offer.Terrain==SectorTerrain.Riverbank?Cyan:Amber;
            Frame(r,accent,offer.Available);
            Text(r.x+14,r.y+10,470,30,(index+1)+" / "+offer.Title,new GUIStyle(headline){fontSize=21},White);
            Text(r.x+14,r.y+47,470,25,NetworkSimulation.TerrainName(offer.Terrain)+(offer.Terrain==SectorTerrain.Riverbank?"  /  川越えを設計":"  /  建設不可範囲あり"),body,accent);
            DrawExpansionMap(new Rect(r.x+14,r.y+82,146,104),offer);
            Text(r.x+14,r.y+190,150,20,"橙＝建設不可範囲",new GUIStyle(tiny){fontSize=11},Amber);
            Text(r.x+14,r.y+211,150,18,"白＝新しい区画",new GUIStyle(tiny){fontSize=11},White);
            var s=game.Simulation;
            string resources=(s.ModernRules?"資材 +":"回線長 +")+(offer.CableGrant*(s.ModernRules?1.15:1)).ToString("0.0");
            string rate=NetworkSimulation.DistrictDataRate(offer.Role).ToString("0.0");if(s.CommunicationPeaksEnabled&&NetworkSimulation.DistrictPeakDataRate(offer.Role)>NetworkSimulation.DistrictDataRate(offer.Role))rate+="→"+NetworkSimulation.DistrictPeakDataRate(offer.Role).ToString("0.0");
            string text="電力 "+offer.Demand.ToString("0")+" MW / 通信 "+rate+" 件/s"+
                "\n期限 "+(s.ModernRules?NetworkSimulation.DistrictDeadline(offer.Role):5).ToString("0.0")+"秒"+(s.ModernRules&&offer.Role==8?" / 住宅ピーク 5 MW":"")+
                "\n"+resources+" / 専用 +2本"+"\n川越え +1本 / "+(offer.Utilities?"発電・データ併設":"既存設備から供給");
            if(offer.Role<7)text="候補地を確保できませんでした。";
            Text(r.x+174,r.y+79,308,105,text,new GUIStyle(tiny){fontSize=14,wordWrap=true},White);
            Text(r.x+174,r.y+190,308,38,offer.Available?NetworkSimulation.TerrainHint(offer.Terrain):offer.UnavailableReason,new GUIStyle(tiny){fontSize=14,wordWrap=true},offer.Available?Muted:Red);
            bool enabled=GUI.enabled;GUI.enabled=enabled&&offer.Available;
            if(Button(new Rect(r.x+14,r.y+234,470,32),offer.Available?NetworkSimulation.DirectionName(offer.Direction)+"へ開放  >":"この方向は開放できません",offer.Available,accent))game.ConfirmExpansion(offer.Direction);
            GUI.enabled=enabled;
        }
        void DrawExpansionMap(Rect r,ExpansionOffer offer)
        {
            Fill(r,new Color(.015f,.06f,.09f,.9f));var points=new List<CityPoint>();
            if(offer.WestRoute!=null)points.AddRange(offer.WestRoute);if(offer.EastRoute!=null)points.AddRange(offer.EastRoute);if(offer.DistrictRoute!=null)points.AddRange(offer.DistrictRoute);
            if(offer.DistrictPoint!=null)points.Add(offer.DistrictPoint);if(points.Count==0)return;
            double minX=points[0].X,maxX=minX,minZ=points[0].Z,maxZ=minZ;
            foreach(var p in points){minX=System.Math.Min(minX,p.X);maxX=System.Math.Max(maxX,p.X);minZ=System.Math.Min(minZ,p.Z);maxZ=System.Math.Max(maxZ,p.Z);}
            foreach(var b in offer.Obstacles){minX=System.Math.Min(minX,b.X-b.Width/2);maxX=System.Math.Max(maxX,b.X+b.Width/2);minZ=System.Math.Min(minZ,b.Z-b.Depth/2);maxZ=System.Math.Max(maxZ,b.Z+b.Depth/2);}
            float zoom=Mathf.Min((r.width-18)/(float)System.Math.Max(1,maxX-minX),(r.height-18)/(float)System.Math.Max(1,maxZ-minZ));
            System.Func<double,double,Vector2> project=(px,pz)=>new Vector2(r.center.x+(float)(px-(minX+maxX)/2)*zoom,r.center.y-(float)(pz-(minZ+maxZ)/2)*zoom);
            double river=game.Simulation.Map.RiverX;if(river>minX&&river<maxX){var center=project(river,(minZ+maxZ)/2);Fill(new Rect(center.x-(float)game.Simulation.Map.RiverWidth*zoom/2,r.y,(float)game.Simulation.Map.RiverWidth*zoom,r.height),new Color(.05f,.31f,.43f));}
            foreach(var b in offer.Obstacles){var p=project(b.X,b.Z);Fill(new Rect(p.x-(float)b.Width*zoom/2,p.y-(float)b.Depth*zoom/2,(float)b.Width*zoom,(float)b.Depth*zoom),new Color(.7f,.32f,.11f,.8f));}
            foreach(var p in points){var dot=project(p.X,p.Z);Fill(new Rect(dot.x-2,dot.y-2,4,4),Cyan);}
            if(offer.DistrictPoint!=null){var p=project(offer.DistrictPoint.X,offer.DistrictPoint.Z);Fill(new Rect(p.x-4,p.y-4,8,8),White);}
        }
    }
}
