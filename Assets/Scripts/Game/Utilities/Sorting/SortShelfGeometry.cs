using UnityEngine;
namespace AllSorted
{
    /// <summary>Shared three-slot geometry for the existing five-stage campaign; no gameplay state.</summary>
    public static class SortShelfGeometry
    {
        public const float SlotPitch=.94f,Width=3.55f,Height=1.74f,Depth=1.54f;
        public const float SupportY=-.487f;
        public const float ConnectedRowPitch=1.6f,ConnectedCellBottom=-.65f,ConnectedCellTop=.95f;
        public static readonly Vector3 RearOffset=new Vector3(0,.34f,.85f);
        public static Vector3 SourceCenter(float x,float y,Vector2 center)
        {return new Vector3((x-center.x)*1.25f,(y-center.y)*2.1f,0);}
        public static Vector3 ConnectedCenter(float x,float y,Vector2 center,int level)
        {return new Vector3((x-center.x)*(Width/3.13f),(y-center.y)*ConnectedRowPitch/(level==1?1.5f:1f),0);}
        public static Vector3 LocalSlot(int slot,bool rear=false)
        {return new Vector3((slot-1)*SlotPitch,0,-.5f)+(rear?RearOffset:Vector3.zero);}
        public static Vector3 SlotWorld(Vector3 shelfCenter,int slot)
        {return shelfCenter+LocalSlot(slot);}
    }
}
