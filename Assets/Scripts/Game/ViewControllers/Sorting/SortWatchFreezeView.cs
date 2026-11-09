using System;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted
{
 /// <summary>Scene-owned noninteractive watch flight, frost hit and pooled snow presentation.</summary>
 public sealed class SortWatchFreezeView : AllSortedController
 {
  public RectTransform Surface,Origin,Clock;
  public Image Watch,Glow,Wave,Flash,Frame;
  public Image FrozenSkin;
  public Image[] Snow;
  private float launch=-1,tail,thaw=-1,snowTime;private bool frozen,snowing;private Action impact;
  public bool IsLaunching {get{return launch>=0;}}
  public bool IsSnowing {get{return snowing;}}
  public bool IsVisible {get{return Frame.color.a>0 || IsLaunching || tail>0;}}
  public void Launch(Action hit){if(IsLaunching)return;Clear();impact=hit;launch=0;Watch.gameObject.SetActive(true);}
  public void SetSnow(bool enabled){snowing=enabled;}
  public void Sync(float freeze,bool playing,bool terminal)
  {
   if(terminal){Clear();return;}
   float dt=playing?Time.unscaledDeltaTime:0;
   if(launch>=0){launch+=dt;float t=Mathf.Clamp01(launch/.28f);float u=1-Mathf.Pow(1-t,3);Vector3 from=Surface.InverseTransformPoint(Origin.position),to=Surface.InverseTransformPoint(Clock.position)+Vector3.down*100;
    Watch.rectTransform.localPosition=Vector3.LerpUnclamped(from,to,u)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*65);Watch.rectTransform.localScale=Vector3.one*Mathf.Lerp(1,1.3f,u);Alpha(Watch,launch<.4f?1:0);
    Glow.rectTransform.position=Watch.rectTransform.position;Alpha(Glow,Mathf.Clamp01((launch-.18f)/.1f)*.8f);
    if(launch>=.32f){float pulse=Mathf.Clamp01((launch-.32f)/.08f);Wave.rectTransform.position=Vector3.Lerp(Watch.rectTransform.position,Clock.position,pulse);Wave.rectTransform.localScale=Vector3.one*Mathf.Lerp(.3f,.45f,pulse);Alpha(Wave,pulse*.8f);}
    if(launch>=.4f){launch=-1;tail=.4f;var hit=impact;impact=null;if(hit!=null)hit();freeze=FrozenSkin.gameObject.activeSelf?15:0;}
   }
   if(tail>0){tail=Mathf.Max(0,tail-dt);float t=1-tail/.4f;Wave.rectTransform.position=Clock.position;Flash.rectTransform.position=Clock.position;Wave.rectTransform.localScale=Vector3.one*Mathf.Lerp(.45f,2.8f,t);Alpha(Wave,(1-t)*.8f);Alpha(Flash,Mathf.Max(0,1-t*3));Alpha(Glow,(1-t)*.6f);Alpha(Watch,(1-t)*.7f);}
   bool active=freeze>0;
   if(active && !frozen){frozen=true;thaw=-1;snowTime=0;SetSnow(true);}
   if(!active && frozen){frozen=false;thaw=0;SetSnow(false);}
   if(FrozenSkin)FrozenSkin.gameObject.SetActive(active);
   if(active){snowTime+=dt;float a=Mathf.Clamp01(snowTime/.35f);Alpha(Frame,a*.9f);Frame.rectTransform.localScale=Vector3.one*Mathf.Lerp(1.14f,1,a);}
   if(thaw>=0){thaw+=dt;Alpha(Frame,Mathf.Max(0,1-thaw/2.5f)*.9f);if(thaw>=2.5f){thaw=-1;foreach(var s in Snow)s.gameObject.SetActive(false);}}
   if(active || thaw>=0){for(int i=0;i<Snow.Length;i++){var s=Snow[i];s.gameObject.SetActive(true);float phase=Mathf.Repeat(snowTime*(.13f+i%4*.012f)+i*.618034f,1);if(!active)phase=Mathf.Repeat(phase+thaw*.18f,1);float x=(Mathf.Repeat(i*.381966f,1)-.5f)*Surface.rect.width+Mathf.Sin(snowTime+i)*20;
     s.rectTransform.anchoredPosition=new Vector2(x,Surface.rect.height*(.55f-phase*1.15f));s.rectTransform.localRotation=Quaternion.Euler(0,0,snowTime*(i%2==0?12:-9)+i*25);float fade=active?Mathf.Clamp01(snowTime/.5f):Mathf.Clamp01(1-thaw/2.5f);Alpha(s,fade*(.3f+i%4*.12f));}}
  }
  public void HitSkin(){if(FrozenSkin)FrozenSkin.gameObject.SetActive(true);}
  public void Clear(){launch=-1;tail=0;thaw=-1;frozen=false;snowing=false;impact=null;snowTime=0;
   foreach(var i in new[]{Watch,Glow,Wave,Flash,Frame})if(i){Alpha(i,0);i.rectTransform.localScale=Vector3.one;}if(Snow!=null)foreach(var i in Snow)if(i)i.gameObject.SetActive(false);if(FrozenSkin)FrozenSkin.gameObject.SetActive(false);}
  private void OnDisable(){Clear();}
  private static void Alpha(Image i,float a){if(i){var c=i.color;c.a=a;i.color=c;}}
 }
}
