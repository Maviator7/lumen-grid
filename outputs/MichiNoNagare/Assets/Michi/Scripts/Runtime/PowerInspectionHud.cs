using System.Text;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        string PowerStateTitle(PowerInspection info,GridNode node)
        {
            switch(info.State){case PowerUse.InUse:return node.Kind==NetworkNodeKind.Reactor?"供給中":"送電中";case PowerUse.Standby:return "接続済み / 待機中";case PowerUse.NoSource:return "発電所への経路なし";case PowerUse.NoDestination:return "送電先への経路なし";default:return "接続先に電力不足";}
        }
        string PowerStatus(PowerInspection info,GridNode node)
        {
            switch(info.State){
                case PowerUse.InUse:return node.Kind==NetworkNodeKind.Reactor?"需要に合わせて自動で供給中。":"電力がこの中継を通過しています。";
                case PowerUse.Standby:return (node.Kind==NetworkNodeKind.Reactor?"接続済み・発電所は待機中。":"接続済み・現在送電なし。")+"\n接続先の区画は別経路から供給中。";
                case PowerUse.NoSource:return node.Kind==NetworkNodeKind.Reactor?"発電容量が0 MWです。":"発電所までの送電経路がありません。\n共用か送電専用でつないでください。";
                case PowerUse.NoDestination:return (node.Kind==NetworkNodeKind.Reactor?"":"発電所には接続済み。\n")+"需要地区への送電経路がありません。";
                default:return "接続済み・現在送電なし。\n接続先に電力不足があります。発電容量と経路を確認してください。";
            }
        }
        string PowerSources(PowerInspection info)
        {
            if(info.Sources.Count==0)return "現在の供給元：なし";
            var text=new StringBuilder(info.SourcesPassThroughSelection?"実際の供給元：":"接続先の区画の供給元：");
            for(int i=0;i<info.Sources.Count&&i<2;i++){var source=info.Sources[i];text.Append('\n').Append(RoleName(game.Simulation.Nodes[source.Source])).Append("  ").Append(source.Amount.ToString("0.0")).Append(" MW");}
            if(info.Sources.Count>2)text.Append("\nほか ").Append(info.Sources.Count-2).Append(" 供給設備");
            return text.ToString();
        }
        string RelayPowerDetails(GridNode node)
        {
            var info=game.Simulation.InspectPower(node.Id);
            return PowerStatus(info,node)+"\n"+PowerSources(info)+"\n近い経路を優先。通過電力の分だけ通信余力が減ります。";
        }
        string ReactorPowerDetails(GridNode node)
        {
            var info=game.Simulation.InspectPower(node.Id);
            return PowerStatus(info,node)+(info.State==PowerUse.Standby?"\n"+PowerSources(info):"")+"\n近い発電所の経路を優先し、容量が足りなければ他の発電所が補います。";
        }
    }
}
