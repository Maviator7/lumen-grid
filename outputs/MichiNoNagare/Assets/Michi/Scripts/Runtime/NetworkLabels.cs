using System.Collections.Generic;
using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        static readonly Color RelayLabel=NetworkCityView.RGB(0xd3dfe9),IndustryLabel=NetworkCityView.RGB(0xc7a6ff),HousingLabel=NetworkCityView.RGB(0xffb5d1),MedicalLabel=NetworkCityView.RGB(0x87efbe);
        static Color FacilityColor(GridNode node)
        {
            if(node.Kind==NetworkNodeKind.Reactor)return Amber;
            if(node.Kind==NetworkNodeKind.DataCore)return Cyan;
            if(node.Kind==NetworkNodeKind.Relay)return RelayLabel;
            return node.Role==7?IndustryLabel:node.Role==8?HousingLabel:MedicalLabel;
        }
        void DrawFacilityLegend(float x,float y)
        {
            Text(x,y,58,24,"施設色",tiny);
            string[] labels={"発電","データ","中継","工業","住宅","医療"};
            Color[] colors={Amber,Cyan,RelayLabel,IndustryLabel,HousingLabel,MedicalLabel};
            for(int i=0;i<labels.Length;i++){float left=x+62+i*82;Fill(new Rect(left,y+8,5,9),colors[i]);Text(left+12,y,68,24,labels[i],tiny,colors[i]);}
        }
        bool LabelFocused(GridNode n){return n.Id==game.View.SelectedNode||n.Id==game.View.PendingNode||n.Id==game.HoverNode||(game.Tutorial.Active&&n.Id==game.Tutorial.TargetNode);}
        int LabelPriority(GridNode n){return LabelFocused(n)?0:DistrictIssue(n).Length>0?1:n.Kind==NetworkNodeKind.Relay?3:2;}
        void NodeLabels()
        {
            float width=Screen.width/scale,height=Screen.height/scale;
            var safe=new Rect(26,137,width-52,height-FooterHeight-159);
            var used=new List<Rect>{guideBounds};
            if(expansionNoticeBounds.width>0)used.Add(expansionNoticeBounds);
            if(game.Tutorial.ShowsHubDetails||game.View.SelectedNode>=0||game.SelectedLink>=0)used.Add(new Rect(width-295,143,280,height-FooterHeight-165));
            var nodes=new List<GridNode>(game.Simulation.Nodes);nodes.Sort((a,b)=>{int p=LabelPriority(a).CompareTo(LabelPriority(b));return p!=0?p:a.Id.CompareTo(b.Id);});
            foreach(var n in nodes){
                var world=game.View.Camera.WorldToScreenPoint(game.View.NodePoint(n.Id));if(world.z<0)continue;
                var point=new Vector2(world.x/scale,(Screen.height-world.y)/scale);if(point.x<0||point.x>width||point.y<124||point.y>height-FooterHeight-6)continue;
                string issue=DistrictIssue(n),name=RoleName(n),detail="";bool focused=LabelFocused(n);
                if(issue.Length>0)detail="! "+issue;
                else if(focused&&game.Simulation.ModernRules&&n.Kind==NetworkNodeKind.District)detail="電力 "+n.PowerDemand+" MW / 通信期限 "+game.Simulation.RequestDeadline(n.Id).ToString("0.0")+"秒";
                else if(focused)detail=game.Simulation.Continuous&&n.Kind==NetworkNodeKind.DataCore?(game.Simulation.DataSourceConnected(n.Id)?"送信 "+n.DataOutputRate.ToString("0.0")+" / "+n.DataSupply.ToString("0.0")+" 件/s":"未接続 / 出力 "+n.DataSupply.ToString("0.0")+" 件/s"):Purpose(n);
                float w=Mathf.Clamp(Mathf.Max(body.CalcSize(new GUIContent(name)).x,tiny.CalcSize(new GUIContent(detail)).x)+10,72,250),h=detail.Length>0?60:32;
                var offsets=new[]{new Vector2(-w/2,-h-15),new Vector2(30,-h/2),new Vector2(-w-30,-h/2),new Vector2(-w/2,20),new Vector2(-w/2,-h-55),new Vector2(30,-h-35),new Vector2(-w-30,-h-35),new Vector2(-w/2,62)};
                Rect placed=Rect.zero;int slot=-1;
                for(int i=0;i<offsets.Length;i++){
                    var r=new Rect(point+offsets[i],new Vector2(w,h));if(!safe.Contains(r.min)||!safe.Contains(r.max))continue;
                    bool collision=false;var padded=new Rect(r.x-5,r.y-4,r.width+10,r.height+8);foreach(var old in used)if(padded.Overlaps(old)){collision=true;break;}
                    if(!collision){placed=r;slot=i;used.Add(r);break;}
                }
                // A crowded overview omits ordinary names; selecting or hovering gives them priority.
                if(slot<0)continue;
                Color c=FacilityColor(n);
                if(slot>0)Line(point,new Vector2(Mathf.Clamp(point.x,placed.x,placed.xMax),Mathf.Clamp(point.y,placed.y,placed.yMax)),new Color(c.r,c.g,c.b,.35f));
                Text(placed.x+1.5f,placed.y+1.5f,w,32,name,body,new Color(0,0,0,.9f));Text(placed.x,placed.y,w,32,name,body,c);
                if(detail.Length>0){Text(placed.x+1.5f,placed.y+33.5f,w,26,detail,tiny,new Color(0,0,0,.9f));Text(placed.x,placed.y+32,w,26,detail,tiny,issue.Length>0?Amber:Muted);}
            }
        }
    }
}
