using UnityEngine;
namespace AllSorted
{
    public static class SortLevelTransitionTiming
    {
        public const float HomeFadeSeconds=.15f,ArrivalSeconds=.28f,LoadingThreshold=.25f;
        public static float HomeAlpha(float elapsed){return 1-Mathf.Clamp01(elapsed/HomeFadeSeconds);}
        public static bool ShowLoading(float elapsed,bool ready){return !ready && elapsed>=LoadingThreshold;}
        public static float ArrivalFraction(float elapsed,float readyAt){return Mathf.Clamp01((elapsed-Mathf.Max(HomeFadeSeconds,readyAt))/ArrivalSeconds);}
        public static float ArrivalProgress(float elapsed,float readyAt){float t=ArrivalFraction(elapsed,readyAt);return 1-Mathf.Pow(1-t,3);}
        public static float MerchandiseScale(float elapsed,float readyAt)
        {
            float seconds=Mathf.Max(0,elapsed-Mathf.Max(HomeFadeSeconds,readyAt));
            if(seconds<=.18f){float t=seconds/.18f;return 1.15f*(1-Mathf.Pow(1-t,3));}
            float settle=Mathf.Clamp01((seconds-.18f)/.10f);settle=settle*settle*(3-2*settle);
            return Mathf.Lerp(1.15f,1,settle);
        }
    }
}
