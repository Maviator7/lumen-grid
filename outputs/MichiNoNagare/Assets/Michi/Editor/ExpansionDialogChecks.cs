using System;
using Michi.Core;
namespace Michi.Checks
{
    public static class ExpansionDialogChecks
    {
        static int checks;
        static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
        public static string Run()
        {
            checks=0;var city=new NetworkSimulation(CityMap.Generate(664020296),true,1);var dialog=new ExpansionConfirmation();
            int nodes=city.Nodes.Length,links=city.Links.Count;double time=city.Time;
            Check(dialog.Request(city)&&dialog.Pending,"Unmet conditions must still open a visible dialog");
            Check(city.Nodes.Length==nodes&&city.Links.Count==links&&city.Time==time,"Opening condition details preserves the city");
            Check(!dialog.Confirm(city,true)&&dialog.Pending&&city.ExpansionCount==0,"An unready city cannot expand; explanation remains visible");
            dialog.Cancel();Check(!dialog.Pending&&!dialog.Confirm(city,true),"Cancel closes without expansion");
            Check(!dialog.Request(new NetworkSimulation())&&!dialog.Pending,"Fixed tutorial cannot open city expansion");
            city.LoadSolution();for(int i=0;i<12000&&!city.ExpansionReady;i++)city.Step(1.0/60);
            Check(city.ExpansionReady,"Reference setup becomes ready");
            Check(!dialog.Confirm(city,true)&&city.ExpansionCount==0,"Ready city also requires an explicit open dialog");
            Check(dialog.Request(city)&&dialog.Pending,"Ready city opens selection");
            city.BeginEdit();city.Remove(NetworkSimulation.EdgeId(0,2));city.EndEdit();for(int i=0;i<120;i++)city.Step(1.0/60);
            Check(!city.ExpansionReady&&!dialog.Confirm(city,true)&&dialog.Pending,"Condition loss cannot silently close selection or bypass requirements");
            dialog.Cancel();city.LoadSolution();for(int i=0;i<12000&&!city.ExpansionReady;i++)city.Step(1.0/60);
            dialog.Request(city);Check(dialog.Confirm(city,false)&&city.ExpansionCount==1&&!dialog.Pending,"Explicit available choice expands exactly once");
            Check(!dialog.Confirm(city,false)&&city.ExpansionCount==1,"Repeated confirmation cannot expand again");
            return "PASS: "+checks+" expansion dialog checks";
        }
#if EXPANSION_DIALOG_STANDALONE
        public static void Main(){Console.WriteLine(Run());}
#endif
    }
}
