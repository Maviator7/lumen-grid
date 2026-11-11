using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkGame
    {
        public int HoverNode {get;private set;}=-1;
        public int PreviewA {get;private set;}=-1;
        public int PreviewB {get;private set;}=-1;
        public NetworkEditPreview EditPreview {get;private set;}
        void UpdatePlanningPreview()
        {
            PreviewA=PreviewB=HoverNode=-1;EditPreview=null;
            var mouse=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            bool blocked=TitleScreen||MapPrompt||ResetPrompt||GuidePrompt||ExpansionPrompt||SavePrompt||CommunicationPrompt||pointer.Dragging||NetworkHud.IsUI(mouse,Tutorial.ShowsHubDetails||View.SelectedNode>=0||SelectedLink>=0);
            if(!blocked)HoverNode=View.PickNode(mouse);
            int from=-1,to=-1;
            if(!blocked&&Simulation.CanEdit&&Tool<=2){
                from=View.PendingNode;
                if(from>=0&&HoverNode>=0&&HoverNode!=from&&Tutorial.AllowsNode(HoverNode))to=HoverNode;
                if(from<0&&HoverNode<0){int id=View.PickLink(mouse);if(id>=0&&Tutorial.AllowsLink(id)){var link=Simulation.Links[id];from=link.A;to=link.B;}}
                if(to>=0){PreviewA=from;PreviewB=to;EditPreview=Simulation.PreviewConnect(from,to,Kind);}
            }
            View.PreviewConnection(from,to,Input.mousePosition,EditPreview!=null&&EditPreview.Error!=NetworkEditError.None?new Color(1,.17f,.12f):Tool==1?new Color(1,.48f,.1f):new Color(.1f,.7f,1));
        }
    }
    public sealed partial class NetworkHud
    {
        string PreviewReason(NetworkEditError error)
        {
            switch(error){case NetworkEditError.None:return "クリックで確定";case NetworkEditError.TooLong:return "距離が長すぎます（上限11.5）";case NetworkEditError.Obstacle:return "建設不可エリアを横切っています";case NetworkEditError.BridgeBudget:return "橋の枠が足りません";case NetworkEditError.CableBudget:return game.Simulation.ModernRules?"建設資材が足りません（専用は資材×1.75）":"回線長の枠が足りません";case NetworkEditError.DedicatedBudget:return "専用回線の枠が足りません";default:return "現在は接続できません";}
        }
        static string Delta(double value){return value.ToString("+0.0;-0.0;0.0");}
        static string Delta(int value){return value.ToString("+0;-0;0");}
        void DrawPlanningPreview(float width,float bottom)
        {
            var p=game.EditPreview;var s=game.Simulation;float x=width/2-350,y=bottom-114;Color c=p.Error==NetworkEditError.None?Cyan:Red;
            Frame(new Rect(x,y,700,100),c,true);
            Text(x+16,y+9,668,25,(p.Existing?"種類変更 / ":"新しい回線 / ")+PreviewReason(p.Error),body,c);
            Text(x+16,y+39,668,24,(s.ModernRules?"追加／回収資材 "+Delta(p.AddedCost)+" / 長さ "+Delta(p.AddedCable):"追加回線長 "+Delta(p.AddedCable))+" / 橋 "+Delta(p.BridgeDelta)+" / 専用枠 "+Delta(p.DedicatedDelta),body);
            Text(x+16,y+70,668,23,"確定後："+(s.ModernRules?"建設資材 ":"回線長 ")+(s.CableCost+p.AddedCost).ToString("0.0")+" / "+s.CableCostLimit.ToString("0.0")+"  橋 "+(s.BridgeCount+p.BridgeDelta)+" / "+s.AvailableBridges+"  専用枠 "+(s.DedicatedCount+p.DedicatedDelta)+" / "+s.AvailableDedicated,tiny);
        }
    }
}
