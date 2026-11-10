using System;
using Michi.Core;
public static class PointerChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception("POINTER FAIL: "+name);}
    public static string Run()
    {
        var p=new PointerGesture();p.Begin(100,100,8);p.Move(103,104);Check(!p.Dragging,"small hand jitter remains a click");Check(p.End(103,104),"short press selects on release");
        p.Begin(0,0,8);p.Move(9,0);Check(p.Dragging,"movement starts camera drag");p.Move(0,0);Check(p.Dragging,"returning to origin stays a drag");Check(!p.End(0,0),"drag cannot edit a cable on release");
        p.Begin(0,0,8);Check(!p.End(20,0),"release displacement is included");p.Begin(0,0,8);p.Cancel();Check(!p.End(0,0),"cancel cannot select");Check(!p.Active&&!p.Dragging,"gesture clears after release");
        p.Begin(0,0,16);p.Move(10,0);Check(!p.Dragging,"scaled threshold respects high resolution");p.Cancel();return "PASS: pointer gesture checks";
    }
#if POINTER_STANDALONE
    public static void Main(){Console.WriteLine(Run());}
#endif
}
