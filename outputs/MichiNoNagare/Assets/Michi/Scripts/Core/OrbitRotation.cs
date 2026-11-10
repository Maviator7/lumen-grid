using System;
namespace Michi.Core
{
    // Exact integration of an exponential follower with a linearly moving target.
    // Angles stay unwrapped so button turns across 360 keep their intended direction.
    public sealed class OrbitRotation
    {
        const double Response=.10,DegreesPerSecond=80;
        double target;
        public double Angle {get;private set;}
        public OrbitRotation(double angle){Snap(angle);}
        public void Snap(double angle){Angle=target=angle;}
        public void Turn(double degrees){target+=degrees;}
        public bool Step(double axis,double dt)
        {
            if(dt<=0||double.IsNaN(dt)||double.IsInfinity(dt))return false;
            axis=Math.Max(-1,Math.Min(1,axis));double velocity=axis*DegreesPerSecond,before=Angle,oldTarget=target;
            target+=velocity*dt;double decay=Math.Exp(-dt/Response);
            Angle=target-velocity*Response+(Angle-oldTarget+velocity*Response)*decay;
            if(axis==0&&Math.Abs(Angle-target)<.00001)Angle=target;
            return Angle!=before;
        }
    }
}
