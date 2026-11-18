using UnityEngine;
using Michi.Core;

namespace Michi.Runtime
{
    public sealed partial class NetworkGame
    {
        public int RetrofitNode { get; private set; } = -1;
        public bool RetrofitPrompt => RetrofitNode >= 0;
        public RelayMode RetrofitChoice { get; private set; }
        public double RetrofitPowerLoad { get; private set; }
        public double RetrofitDataRate { get; private set; }
        public double RetrofitCoverage { get; private set; }
        NetworkSimulation retrofitCity;

        public void OpenRelayRetrofit(int node)
        {
            if (!CanOpenFeedback || View.SelectedNode != node || Simulation.Nodes[node].Kind != NetworkNodeKind.Relay) return;
            RetrofitNode = node;
            retrofitCity = Simulation;
            ChooseRelayMode(Simulation.GetRelayMode(node));
            BlockFeedbackInput();
        }

        public bool CanApplyRetrofit(out string reason)
        {
            reason = "";
            if (!RetrofitPrompt || retrofitCity != Simulation || View.SelectedNode != RetrofitNode) return false;
            if (Simulation.GetRelayMode(RetrofitNode) == RetrofitChoice) return false;
            return Simulation.CanSetRelayMode(RetrofitNode, RetrofitChoice, out reason);
        }

        public void ChooseRelayMode(RelayMode mode)
        {
            if (!RetrofitPrompt || retrofitCity != Simulation) return;
            RetrofitChoice = mode;
            RetrofitPowerLoad = Simulation.Nodes[RetrofitNode].PowerLoad;
            RetrofitDataRate = Simulation.NodeDataRate(RetrofitNode);
            RetrofitCoverage = Simulation.PowerCoverage;
            if (!CanApplyRetrofit(out _)) return;

            var preview = NetworkSimulation.RestoreSave(Simulation.CaptureSave());
            if (!preview.SetRelayMode(RetrofitNode, mode)) return;
            RetrofitPowerLoad = preview.Nodes[RetrofitNode].PowerLoad;
            RetrofitDataRate = preview.NodeDataRate(RetrofitNode);
            RetrofitCoverage = preview.PowerCoverage;
        }

        public void ApplyRelayRetrofit()
        {
            if (!CanApplyRetrofit(out _) || !Simulation.SetRelayMode(RetrofitNode, RetrofitChoice)) return;
            CloseFeedbackModal();
            nextDiagnostic = 0;
            View.RefreshRelayRetrofits();
            if (SaveCity(false)) Notify("中継の型を変更しました。送電と通信の負荷を確認できます。");
        }
    }

    public sealed partial class NetworkHud
    {
        static string RelayModeText(RelayMode mode)
        {
            switch (mode)
            {
                case RelayMode.Power: return "送電型";
                case RelayMode.Data: return "通信型";
                default: return "共用型";
            }
        }

        void DrawRelayRetrofit()
        {
            var s = game.Simulation;
            int node = game.RetrofitNode;
            var rect = FeedbackFrame(718, 508);
            float x = rect.x + 26, y = rect.y + 22, w = rect.width - 52;
            Text(x, y, w, 44, BaseRoleName(s.Nodes[node]) + "の改装", headline, Cyan);
            Text(x, y + 47, w, 32, "現在 " + RelayModeText(s.GetRelayMode(node)) + "  /  特化枠 " +
                s.SpecializedRelayCount + " / " + s.RelaySpecializationLimit + " 台（拡張ごとに＋1）", body);
            float cardWidth = (w - 24) / 3;
            for (int i = 0; i < 3; i++)
            {
                var mode = (RelayMode)i;
                float left = x + i * (cardWidth + 12);
                string capacity = s.RelayPowerCapacity(node, mode).ToString("0.#") + " MW  /  " +
                    s.RelayDataCapacity(node, mode).ToString("0.#") + " 件/s";
                if (Button(new Rect(left, y + 98, cardWidth, 44), RelayModeText(mode), game.RetrofitChoice == mode,
                           mode == RelayMode.Power ? Amber : Cyan)) game.ChooseRelayMode(mode);
                Text(left + 8, y + 150, cardWidth - 16, 30, capacity, tiny, White);
            }
            Text(x, y + 192, w, 45, "上は送電と通信の上限。送電が増えると通信余力が減ります。\n送電上限は、併設した蓄電所の充電・放電にも適用されます。", wrap, Muted);
            bool valid = game.CanApplyRetrofit(out string reason);
            bool different = game.RetrofitChoice != s.GetRelayMode(node);
            if (valid)
            {
                Text(x, y + 251, w, 30, "現在の配線と需要で試算", body, Cyan);
                Text(x, y + 287, w, 32, "通過電力 " + s.Nodes[node].PowerLoad.ToString("0.0") + " → " +
                    game.RetrofitPowerLoad.ToString("0.0") + " MW  /  通信余力 " + s.NodeDataRate(node).ToString("0.0") +
                    " → " + game.RetrofitDataRate.ToString("0.0") + " 件/s", body);
                Text(x, y + 325, w, 30, "都市の電力供給 " + s.PowerCoverage.ToString("P0") + " → " +
                    game.RetrofitCoverage.ToString("P0"), body, game.RetrofitCoverage < s.PowerCoverage ? Amber : White);
            }
            else if (different)
                Text(x, y + 258, w, 60, reason, wrap, Amber);
            else Text(x, y + 258, w, 55, "型を選ぶと、現在の負荷での変更前後を確認できます。", wrap, Muted);
            GUI.enabled = valid;
            if (PrimaryButton(new Rect(x, y + 384, w, 43), "この型に改装する", Cyan, valid)) game.ApplyRelayRetrofit();
            GUI.enabled = true;
            if (QuietButton(new Rect(x, y + 440, w, 34), "戻る / ESC")) game.CloseFeedbackModal();
        }
    }
}
