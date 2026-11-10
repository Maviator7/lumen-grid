using System;
using Michi.Core;
namespace Michi.Checks
{
    public static class OrbitChecks
    {
        static int checks;
        static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("ORBIT: "+why);}
        public static string Run()
        {
            checks=0;var a=new OrbitRotation(-18);a.Step(1,1.0/60);Check(a.Angle>-18&&a.Angle<-17,"first frame eases instead of jumping 20 degrees");
            var b=new OrbitRotation(0);var c=new OrbitRotation(0);for(int i=0;i<30;i++)b.Step(1,1.0/30);for(int i=0;i<144;i++)c.Step(1,1.0/144);Check(Math.Abs(b.Angle-c.Angle)<1e-9,"30 and 144fps produce the same motion");Check(b.Angle>65&&b.Angle<80,"holding gives continuous rotation");
            double previous=b.Angle;for(int i=0;i<120;i++){b.Step(0,1.0/60);Check(b.Angle>=previous&&b.Angle<=80.000001,"release settles without overshoot");previous=b.Angle;}Check(Math.Abs(b.Angle-80)<.0001,"release stops at final target");
            a.Snap(350);a.Turn(20);a.Step(0,.016);Check(a.Angle>350&&a.Angle<370,"button turns across 360 smoothly in intended direction");for(int i=0;i<120;i++)a.Step(0,1.0/60);Check(Math.Abs(a.Angle-370)<.0001,"button lands exactly");
            a.Turn(-20);a.Snap(12);a.Step(0,1);Check(a.Angle==12,"restore/reset discards queued motion");a.Step(-1,.5);Check(a.Angle<12,"Q turns opposite E");a.Snap(12);a.Step(0,.5);Check(a.Angle==12,"neutral input stationary");
            return "PASS: "+checks+" orbit checks";
        }
#if ORBIT_STANDALONE
        public static void Main(){Console.WriteLine(Run());}
#endif
    }
}
