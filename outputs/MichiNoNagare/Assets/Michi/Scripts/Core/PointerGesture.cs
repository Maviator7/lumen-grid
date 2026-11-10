namespace Michi.Core
{
    public sealed class PointerGesture
    {
        public bool Active {get;private set;}
        public bool Dragging {get;private set;}
        double startX,startY,limit;
        public void Begin(double x,double y,double threshold){startX=x;startY=y;limit=threshold*threshold;Active=true;Dragging=false;}
        public void Move(double x,double y){if(Active&&(x-startX)*(x-startX)+(y-startY)*(y-startY)>limit)Dragging=true;}
        public bool End(double x,double y){Move(x,y);bool click=Active&&!Dragging;Cancel();return click;}
        public void Cancel(){Active=Dragging=false;}
    }
}
