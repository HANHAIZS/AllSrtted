using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace AllSorted.PlayTests
{
    public sealed class VictoryResultPlayModeTests
    {
        [UnityTearDown] public IEnumerator RestoreClock(){Time.timeScale=1;yield break;}
        [UnityTest] public IEnumerator RewardFlightCancelsOnCloseAndReusesWithoutDuplicateCredit()
        {
            var go=Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Popups/VictoryPopup"));var popup=go.GetComponent<SortPopupView>();var reward=go.GetComponent<SortVictoryRewardView>();int coins=1600,grants=0,advances=0;
            popup.Configure(()=>reward.Collect(),()=>{},()=>{});reward.Configure(()=>popup.IsOpen,()=>{grants++;coins+=30;return true;},()=>coins,()=>advances++);
            reward.Present(1,coins);popup.Open(null);yield return new WaitForSecondsRealtime(.3f);popup.Primary.onClick.Invoke();yield return new WaitForSecondsRealtime(.1f);popup.Close();yield return new WaitForSecondsRealtime(1.2f);Assert.AreEqual(0,grants);Assert.AreEqual(0,advances);
            reward.Present(1,coins);popup.Open(null);yield return new WaitForSecondsRealtime(.3f);Time.timeScale=0;popup.Primary.onClick.Invoke();popup.Primary.onClick.Invoke();yield return new WaitForSecondsRealtime(1.4f);
            Assert.AreEqual(1,grants);Assert.AreEqual(1,advances);Assert.AreEqual("1630",reward.Balance.text);foreach(var image in reward.FlyingCoins)Assert.IsFalse(image.gameObject.activeSelf);
            popup.Close();yield return new WaitForSecondsRealtime(.2f);Time.timeScale=1;Object.Destroy(go);
        }
        [UnityTest] public IEnumerator RibbonsEmitOnceAndGoldRemainsAlive()
        {
            var go=Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Popups/VictoryPopup"));var popup=go.GetComponent<SortPopupView>();popup.Configure(()=>{},()=>{},()=>{});popup.Open(null);yield return new WaitForSecondsRealtime(.5f);
            var root=go.transform.Find("PopupContent/VictoryLayout");var ribbons=root.Find("Celebration").GetComponentsInChildren<SortVictoryParticleGraphic>();Assert.AreEqual(1,ribbons.Length);Assert.IsFalse(ribbons[0].Source.main.loop);Assert.Greater(ribbons[0].Source.particleCount,0);
            yield return new WaitForSecondsRealtime(3.5f);Assert.AreEqual(0,ribbons[0].Source.particleCount);int gold=0;foreach(var g in root.Find("CoinSparkle").GetComponentsInChildren<SortVictoryParticleGraphic>())gold+=g.Source.particleCount;Assert.Greater(gold,0);
            popup.Close();yield return new WaitForSecondsRealtime(.2f);Object.Destroy(go);
        }
        [UnityTest] public IEnumerator RecoveryButtonsFitShortViewport()
        {
            var go=Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Popups/VictoryPopup"));var root=(RectTransform)go.transform;root.sizeDelta=new Vector2(720,960);var v=go.GetComponent<SortPopupView>();v.Configure(()=>{},()=>{},()=>{});v.Present("ALL SORTED!","NO ENERGY. WAIT OR TAP HOME TO REFILL.","Continue","HOME");v.Body.gameObject.SetActive(true);v.Open(null);yield return new WaitForSecondsRealtime(.3f);
            foreach(var t in new[]{v.Primary.transform,v.Secondary.transform}){var corners=new Vector3[4];((RectTransform)t).GetWorldCorners(corners);foreach(var c in corners)Assert.IsTrue(root.rect.Contains(root.InverseTransformPoint(c)),t.name+" outside short viewport");}
            v.Close();yield return new WaitForSecondsRealtime(.2f);Object.Destroy(go);
        }
        [UnityTest] public IEnumerator VictoryResourceHasContinueAndUnscaledNonBlockingEffects()
        {
            var prefab=Resources.Load<GameObject>("Prefabs/UI/Popups/VictoryPopup");Assert.IsNotNull(prefab,"Dedicated victory popup must load lazily");
            var go=Object.Instantiate(prefab);go.SetActive(false);var popup=go.GetComponent<SortPopupView>();int clicks=0;popup.Configure(()=>clicks++,()=>{},()=>{});popup.Present("ALL SORTED!","","Continue","");Time.timeScale=0;popup.Open(null);yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(popup.IsOpen);Assert.AreEqual("Continue",popup.PrimaryCaption.text);var systems=go.GetComponentsInChildren<ParticleSystem>(true);Assert.GreaterOrEqual(systems.Length,3);
            int particles=0;foreach(var ps in systems){Assert.IsTrue(ps.main.useUnscaledTime);particles+=ps.particleCount;}Assert.Greater(particles,0);
            foreach(var graphic in go.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))if(graphic.GetType().Name=="SortVictoryParticleGraphic")Assert.IsFalse(graphic.raycastTarget);
            popup.Primary.onClick.Invoke();Assert.AreEqual(1,clicks);popup.Close();popup.Primary.onClick.Invoke();Assert.AreEqual(1,clicks);yield return new WaitForSecondsRealtime(.2f);Assert.IsFalse(go.activeSelf);foreach(var ps in systems)Assert.AreEqual(0,ps.particleCount);Time.timeScale=1;Object.Destroy(go);
        }
    }
}
