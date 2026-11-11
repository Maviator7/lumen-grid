namespace Michi.Core
{
    public sealed partial class NetworkSimulation
    {
        // With unchanged demand and wiring, rates stay constant until a cell empties/fills.
        // Reuse the allocation between those boundaries; integrate only energy each fixed step.
        bool batteryAllocationValid;
        double batteryAllocationHorizon;
        double[] batteryPowerInputs,batteryExpectedEnergy;
        double PowerInput(int index)
        {
            var n=Nodes[index/4];switch(index%4){case 0:return n.PowerDemand;case 1:return n.PowerSupply;case 2:return n.Capacity;default:return (int)n.Kind;}
        }
        void RememberBatteryAllocation(double dt)
        {
            batteryAllocationHorizon=dt>0?dt:1.0/60;
            int count=Nodes.Length*4;if(batteryPowerInputs==null||batteryPowerInputs.Length!=count)batteryPowerInputs=new double[count];
            for(int i=0;i<count;i++)batteryPowerInputs[i]=PowerInput(i);
            if(batteryExpectedEnergy==null||batteryExpectedEnergy.Length!=batteries.Count)batteryExpectedEnergy=new double[batteries.Count];
            for(int i=0;i<batteries.Count;i++)batteryExpectedEnergy[i]=batteries[i].Energy;
            batteryAllocationValid=true;
        }
        bool CanReuseBatteryAllocation(double dt)
        {
            if(!batteryAllocationValid||batteryAllocationHorizon!=dt||batteryPowerInputs.Length!=Nodes.Length*4||batteryExpectedEnergy.Length!=batteries.Count)return false;
            for(int i=0;i<batteryPowerInputs.Length;i++)if(batteryPowerInputs[i]!=PowerInput(i))return false;
            // Newly charged energy may open a bypass around a reactor-route bottleneck.
            // While any district is short, reevaluate charging allocations on the next tick.
            bool shortage=false;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District&&n.PowerFraction<.999999){shortage=true;break;}
            foreach(var b in batteries)if(b.DischargeRate>0){shortage=true;break;}
            for(int i=0;i<batteries.Count;i++){var b=batteries[i];if(shortage&&b.ChargeRate>0)return false;if(b.Energy!=batteryExpectedEnergy[i]||b.Energy<b.DischargeRate*dt||b.Energy+b.ChargeRate*BatteryEfficiency*dt>BatteryCapacity)return false;}
            return true;
        }
        void StepBatteryPower(double dt)
        {
            if(!CanReuseBatteryAllocation(dt)){AllocatePower(dt);return;}
            IntegrateBatteryEnergy(dt);
            for(int i=0;i<batteries.Count;i++)batteryExpectedEnergy[i]=batteries[i].Energy;
        }
    }
}
