namespace Michi.Core
{
    public sealed class ResetConfirmation
    {
        public bool Pending {get;private set;}
        public void Request(){Pending=true;}
        public void Cancel(){Pending=false;}
        public bool Confirm(NetworkSimulation simulation)
        {
            if(!Pending)return false;
            Pending=false;simulation.LoadInitial();return true;
        }
    }
}
