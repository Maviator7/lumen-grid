using UnityEngine;
using Michi.Core;

namespace Michi.Runtime
{
    public sealed partial class NetworkGame
    {
        public int DiagnosticDistrict { get; private set; } = -1;
        public DataInspection Diagnostic { get; private set; }
        public PeakTrial Trial { get; private set; }
        public bool TrialPrompt => Trial != null;
        public bool FeedbackModal => TrialPrompt || RetrofitPrompt;
        float nextDiagnostic;
        NetworkSimulation diagnosticCity;

        bool CanOpenFeedback => HasSession && Simulation.Continuous && Simulation.Map.Generated &&
            !TitleScreen && !Tutorial.Active && !GuidePrompt && !ResetPrompt && !MapPrompt &&
            !SavePrompt && !ExpansionPrompt && !CommunicationPrompt && !FeedbackModal;

        public void ActivateDiagnostic()
        {
            if (!CanOpenFeedback) return;
            if (DiagnosticDistrict >= 0) { ClearDiagnostic(); return; }
            int selected = View.SelectedNode;
            if (selected >= 0 && Simulation.Nodes[selected].Kind == NetworkNodeKind.District)
            {
                SelectDiagnostic(selected);
                return;
            }
            int fallback = -1;
            foreach (var district in Simulation.Nodes)
            {
                if (district.Kind != NetworkNodeKind.District) continue;
                if (fallback < 0) fallback = district.Id;
                if (Simulation.InspectDistrictData(district.Id).Block == DataBlock.None) continue;
                SelectDiagnostic(district.Id);
                return;
            }
            if (fallback >= 0) SelectDiagnostic(fallback);
        }

        void SelectDiagnostic(int district)
        {
            DiagnosticDistrict = district;
            diagnosticCity = Simulation;
            SelectTool(4);
            View.SelectedNode = district;
            View.KeepDiagnosticNodeVisible(district);
            SelectedLink = -1;
            nextDiagnostic = 0;
            UpdateFeedback();
        }

        public void InspectDiagnosticBlock()
        {
            var info = Diagnostic;
            if (!CanOpenFeedback || info == null || info.Block == DataBlock.None) return;
            SelectTool(4);
            if (info.Link >= 0 && Simulation.Links.ContainsKey(info.Link))
            {
                View.SelectedNode = -1;
                SelectedLink = info.Link;
            }
            else if (info.Node >= 0 && info.Node < Simulation.Nodes.Length)
            {
                View.SelectedNode = info.Node;
                SelectedLink = -1;
            }
        }

        public void FocusTrialDistrict(int district)
        {
            if (!TrialPrompt || !Trial.Complete || district < 0 || district >= Simulation.Nodes.Length ||
                Simulation.Nodes[district].Kind != NetworkNodeKind.District) return;
            CloseFeedbackModal();
            SelectDiagnostic(district);
            View.FocusDiagnosticNode(district);
        }

        public void CycleDiagnostic(int direction)
        {
            if (!CanOpenFeedback || DiagnosticDistrict < 0) return;
            var nodes = Simulation.Nodes;
            for (int i = 1; i < nodes.Length; i++)
            {
                int candidate = (DiagnosticDistrict + direction * i + nodes.Length) % nodes.Length;
                if (nodes[candidate].Kind != NetworkNodeKind.District) continue;
                SelectDiagnostic(candidate);
                return;
            }
        }

        void ClearDiagnostic()
        {
            DiagnosticDistrict = -1;
            Diagnostic = null;
            diagnosticCity = null;
            if (View != null) View.DataHighlight = null;
        }

        void UpdateFeedback()
        {
            if (TrialPrompt && !Trial.Complete) Trial.Advance(240);
            if (DiagnosticDistrict < 0) return;
            if (diagnosticCity != Simulation || DiagnosticDistrict >= Simulation.Nodes.Length)
            {
                ClearDiagnostic();
                return;
            }
            if (Time.unscaledTime < nextDiagnostic) return;
            nextDiagnostic = Time.unscaledTime + .25f;
            Diagnostic = Simulation.InspectDistrictData(DiagnosticDistrict);
            View.DataHighlight = Diagnostic;
        }

        void BlockFeedbackInput()
        {
            pointer.Cancel();
            View.PendingNode = -1;
            View.StopRotation();
        }

        public void OpenPeakTrial()
        {
            if (!CanOpenFeedback) return;
            Trial = Simulation.BeginPeakTrial();
            BlockFeedbackInput();
        }

        public void CloseFeedbackModal()
        {
            Trial = null;
            RetrofitNode = -1;
            ignoreConfirmFrame = Time.frameCount + 1;
            pointer.Cancel();
        }
    }
}
