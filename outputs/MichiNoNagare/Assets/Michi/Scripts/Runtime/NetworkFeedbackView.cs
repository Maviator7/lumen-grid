using System.Collections.Generic;
using UnityEngine;
using Michi.Core;

namespace Michi.Runtime
{
    public sealed partial class NetworkCityView
    {
        readonly HashSet<int> highlightedLinks = new HashSet<int>();
        DataInspection dataHighlight;
        Transform relayRetrofitRoot;

        public DataInspection DataHighlight
        {
            get => dataHighlight;
            set
            {
                dataHighlight = value;
                highlightedLinks.Clear();
                if (value == null) return;
                for (int i = 1; i < value.Route.Count; i++)
                    highlightedLinks.Add(NetworkSimulation.EdgeId(value.Route[i - 1], value.Route[i]));
            }
        }

        public void KeepDiagnosticNodeVisible(int node)
        {
            var point = Camera.WorldToViewportPoint(NodePoint(node));
            if (point.z > 0 && point.x > .28f && point.x < .77f && point.y > .28f && point.y < .82f) return;
            FocusDiagnosticNode(node);
        }

        public void FocusDiagnosticNode(int node)
        {
            var target = NodePoint(node);
            PanOffset = new Vector3(target.x - 1, 0, target.z - 3);
            UpdateCamera();
        }

        void StyleDiagnosticLink(LinkVisual visual, bool design)
        {
            bool route = highlightedLinks.Contains(visual.Link.Id);
            bool blocked = dataHighlight != null && dataHighlight.Link == visual.Link.Id;
            float strength = .15f + (float)(visual.Link.PowerLoad / 16) * 1.6f;
            if (dataHighlight != null) strength *= .45f;
            visual.Power.sharedMaterial.SetColor("_Color", Amber * strength);
            visual.Data.widthMultiplier = blocked || route ? .14f : .085f;
            visual.Data.sharedMaterial.SetColor("_Color", blocked ? Amber * 2.3f : Cyan *
                (route ? 1.75f : dataHighlight != null ? .22f : design ? .6f : .5f));
        }

        void ClearFeedbackView()
        {
            DataHighlight = null;
            relayRetrofitRoot = null;
        }

        public void RefreshRelayRetrofits()
        {
            if (relayRetrofitRoot == null) relayRetrofitRoot = Group("Relay retrofits", city);
            else Clear(relayRetrofitRoot);
            foreach (var node in sim.Nodes)
            {
                if (node.Kind != NetworkNodeKind.Relay) continue;
                var profile = sim.GetRelayMode(node.Id);
                if (profile == RelayMode.Shared) continue;
                var p = NodePoint(node.Id);
                if (profile == RelayMode.Power)
                {
                    var points = new Vector3[73];
                    for (int i = 0; i < points.Length; i++)
                    {
                        float t = i / 72f, angle = t * Mathf.PI * 6;
                        points[i] = p + new Vector3(Mathf.Cos(angle) * .36f, .17f + t * .46f, Mathf.Sin(angle) * .36f - .16f);
                    }
                    Line(relayRetrofitRoot, "Power winding", points, .065f, Amber * .9f);
                }
                else
                {
                    for (int i = -1; i < 2; i++)
                    {
                        float h = i == 0 ? .56f : .39f;
                        Box(relayRetrofitRoot, "Data fin", p.x + i * .2f, p.y + .19f + h / 2, p.z - .23f, .12f, h, .27f, Metal(Gunmetal));
                        Line(relayRetrofitRoot, "Fin light", new[] {
                            new Vector3(p.x + i * .2f, p.y + .19f, p.z - .37f),
                            new Vector3(p.x + i * .2f, p.y + .19f + h, p.z - .37f)
                        }, .032f, Cyan * 1.25f);
                    }
                }
            }
        }
    }
}
