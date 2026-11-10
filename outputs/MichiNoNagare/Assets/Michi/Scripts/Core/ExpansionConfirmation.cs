namespace Michi.Core
{
    // Opening the dialog is distinct from permission to commit an expansion.
    public sealed class ExpansionConfirmation
    {
        public bool Pending {get;private set;}
        public bool Request(NetworkSimulation simulation)
        {
            if(simulation==null||!simulation.Continuous)return false;
            Pending=true;return true;
        }
        public void Cancel(){Pending=false;}
        public bool Confirm(NetworkSimulation simulation,bool north)
        {
            if(!Pending||!simulation.Expand(north))return false;
            Pending=false;return true;
        }
    }
}
