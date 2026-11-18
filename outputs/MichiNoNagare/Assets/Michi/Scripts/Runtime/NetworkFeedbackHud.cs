using System.Linq;
using UnityEngine;
using Michi.Core;

namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        Vector2 trialScroll;
        PeakTrial visibleTrial;

        string DataIssue(DataInspection info)
        {
            var s = game.Simulation;
            switch (info.Block)
            {
                case DataBlock.Disconnected: return "センターからの通信経路がありません";
                case DataBlock.Source: return RoleName(s.Nodes[info.Node]) + "で送信待ち";
                case DataBlock.TransitTime: return "移動時間が長く、通信期限に間に合いません";
                case DataBlock.DistrictPower: return "地区への電力が足りず、データを受信できません";
                case DataBlock.Link:
                    if (!s.Links.TryGetValue(info.Link, out var link)) return "回線を確認中";
                    return "回線で待機中\n送電 " + link.PowerLoad.ToString("0.0") + " / 16 MW";
                case DataBlock.Relay:
                    var hub = s.Nodes[info.Node];
                    return RoleName(hub) + "で処理待ち\n送電 " + hub.PowerLoad.ToString("0.0") + " / " +
                        s.RelayPowerCapacity(info.Node).ToString("0.#") + " MW・通信余力 " +
                        s.NodeDataRate(info.Node).ToString("0.0") + "件/s";
                default: return info.ObservedRoute ? "直近の運転で目立つ待ちを検出していません" : "運転を再開すると、実際に使った経路を観測できます";
            }
        }

        void DrawDiagnosticBrief(float x, float y, float w)
        {
            var info = game.Diagnostic;
            if (info == null) return;
            var node = game.Simulation.Nodes[game.DiagnosticDistrict];
            Text(x + 40, y, w - 80, 30, BaseRoleName(node), body, White);
            if (Button(new Rect(x, y, 32, 30), "<")) game.CycleDiagnostic(-1);
            if (Button(new Rect(x + w - 32, y, 32, 30), ">")) game.CycleDiagnostic(1);
            Color color = info.Block == DataBlock.None ? Cyan : Amber;
            Text(x, y + 40, w, 70, DataIssue(info), new GUIStyle(body){wordWrap=true}, color);
            if (info.Route.Count > 0)
                Text(x, y + 110, w, 25, info.ObservedRoute ? "青：観測した通信経路　橙：詰まり" : "青：接続から見込まれる通信経路", tiny, Muted);
            if (info.Block != DataBlock.None && Button(new Rect(x, y + 143, w, 35), "原因の詳細を開く"))
                game.InspectDiagnosticBlock();
        }

        static Rect diagnosticMarkerBounds;

        void DrawMapDiagnostic()
        {
            var info = game.Diagnostic;
            if (info == null || info.Block == DataBlock.None || game.Tool != 4 || !GUI.enabled) return;
            var s = game.Simulation;
            Vector3 world;
            string label;
            if (info.Link >= 0 && s.Links.TryGetValue(info.Link, out var link))
            {
                world = game.View.Sample(link, .5f);
                label = "回線で待機";
            }
            else
            {
                if (info.Node < 0 || info.Node >= s.Nodes.Length) return;
                world = game.View.NodePoint(info.Node);
                switch (info.Block)
                {
                    case DataBlock.Relay: label = BaseRoleName(s.Nodes[info.Node]) + "で処理待ち"; break;
                    case DataBlock.Source: label = BaseRoleName(s.Nodes[info.Node]) + "で送信待ち"; break;
                    case DataBlock.Disconnected: label = "通信経路なし"; break;
                    case DataBlock.DistrictPower: label = "電力不足"; break;
                    default: label = "通信期限に間に合わない"; break;
                }
            }
            var projected = game.View.Camera.WorldToScreenPoint(world);
            if (projected.z <= 0 || projected.x < 0 || projected.x > Screen.width ||
                projected.y < 0 || projected.y > Screen.height) return;
            var anchor = new Vector2(projected.x / scale, (Screen.height - projected.y) / scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            float w = Mathf.Clamp(body.CalcSize(new GUIContent("!  " + label + "  ›")).x + 26, 140, 310);
            float left = Mathf.Clamp(anchor.x - w / 2, guideBounds.xMax + 12, width - 302 - w);
            float top = Mathf.Clamp(anchor.y - 53, 137, height - FooterHeight - 48);
            var rect = new Rect(left, top, w, 39);
            if (rect.Overlaps(expansionNoticeBounds)) rect.y = expansionNoticeBounds.yMax + 8;
            Line(anchor, new Vector2(rect.center.x, rect.yMax), Amber, 2);
            diagnosticMarkerBounds = rect;
            if (PrimaryButton(rect, "!  " + label + "  ›", Amber)) game.InspectDiagnosticBlock();
        }

        string TrialDistrictIssue(DistrictTrialResult district)
        {
            string issue = district.Disconnected ? "通信経路なし" : "";
            if (district.MinimumPower < .98)
                issue += (issue.Length > 0 ? "・" : "") + "電力供給 最低 " + district.MinimumPower.ToString("P0");
            if (issue.Length == 0) issue = district.Failed > 0 ? "通信の失敗あり" : "供給は安定";
            return issue;
        }

        Rect FeedbackFrame(float width, float height)
        {
            float screenW = Screen.width / scale, screenH = Screen.height / scale;
            var rect = new Rect((screenW - width) / 2, (screenH - height) / 2, width, height);
            Fill(new Rect(0, 0, screenW, screenH), new Color(0, .012f, .025f, .87f));
            Frame(rect, Cyan, true);
            return rect;
        }

        void DrawPeakTrial()
        {
            var trial = game.Trial;
            if (visibleTrial != trial) { visibleTrial = trial; trialScroll = Vector2.zero; }
            var rect = FeedbackFrame(684, 530);
            float x = rect.x + 26, y = rect.y + 22, w = rect.width - 52;
            Text(x, y, w, 42, "需要ピークの試運転", headline, Cyan);
            Text(x, y + 55, w, 45, "現在の配線・蓄電残量から " + trial.Seconds.ToString("0") +
                " 秒分を試算します。\n街に戻ると、時刻と残量は試運転前のままです。", wrap);
            if (!trial.Complete)
            {
                Text(x, y + 132, w, 30, "供給を計算中  " + trial.Progress.ToString("P0"), body, Cyan);
                Meter(x, y + 172, w, (float)trial.Progress, Cyan);
            }
            else
            {
                int issues = trial.Districts.Count(d => d.Disconnected || d.MinimumPower < .98 || d.Failed > 0);
                int problems = issues + trial.UnderpoweredBoosts;
                Text(x, y + 110, w, 30, problems > 0
                    ? "供給に問題あり" + (issues > 0 ? "  /  " + issues + "地区" : "") +
                      (trial.UnderpoweredBoosts > 0 ? "  /  増強設備 最大 " + trial.UnderpoweredBoosts + "基" : "")
                    : trial.AverageLatency > 2.8 ? "平均遅延が目標を超えています" : "試運転中の供給は安定",
                    body, problems > 0 || trial.AverageLatency > 2.8 ? Amber : Cyan);
                string peaks = trial.SawPowerPeak && trial.SawDataPeak ? "電力・通信ピークを検証" :
                    trial.SawPowerPeak ? "電力ピークを検証" : trial.SawDataPeak ? "通信ピークを検証" : "通常需要で検証";
                Text(x, y + 144, w, 28, peaks + "  /  平均遅延 " + (trial.AverageLatency > 0 ? trial.AverageLatency.ToString("0.00") + "秒" : "受信なし"), body);
                var rows = trial.Districts.OrderByDescending(d => d.Disconnected || d.MinimumPower < .98 || d.Failed > 0).ThenByDescending(d => d.Disconnected)
                    .ThenByDescending(d => d.Failed).ThenBy(d => d.Node);
                trialScroll = GUI.BeginScrollView(new Rect(x, y + 183, w, 236), trialScroll,
                    new Rect(0, 0, w - 24, trial.Districts.Count * 86));
                float rowY = 0;
                int focus = -1;
                foreach (var row in rows)
                {
                    Color color = row.Disconnected || row.MinimumPower < .98 || row.Failed > 0 ? Amber : Cyan;
                    Text(4, rowY, w - 150, 26, BaseRoleName(game.Simulation.Nodes[row.Node]), body, White);
                    Text(4, rowY + 27, w - 150, 26, TrialDistrictIssue(row), body, color);
                    Text(4, rowY + 54, w - 30, 24, "受信 " + row.Delivered + "件  /  失敗 " + row.Failed +
                        "件（ピーク中 " + row.PeakFailures + "件）", tiny, Muted);
                    if (QuietButton(new Rect(w - 156, rowY + 9, 123, 35), "地図で見る ›")) focus = row.Node;
                    Fill(new Rect(0, rowY + 81, w - 24, 1), Muted * .34f);
                    rowY += 86;
                }
                GUI.EndScrollView();
                if (focus >= 0) { game.FocusTrialDistrict(focus); return; }
            }
            if (QuietButton(new Rect(x, y + 442, w, 42), trial.Complete ? "街に戻る / ESC" : "試運転を中止 / ESC")) game.CloseFeedbackModal();
        }
    }
}
