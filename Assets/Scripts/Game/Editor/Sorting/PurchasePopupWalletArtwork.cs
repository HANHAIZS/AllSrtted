using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    /// <summary>Adds only the upper-left balance bar, retaining the authored offer.</summary>
    public static class PurchasePopupWalletArtwork
    {
        [MenuItem("All Sorted/Apply Purchase Popup Coin Balance")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var root=PrefabUtility.LoadPrefabContents(BoosterPurchaseArtwork.Path);
            try{ApplyTo(root);PrefabUtility.SaveAsPrefabAsset(root,BoosterPurchaseArtwork.Path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        public static void ApplyTo(GameObject root)
        {
            var view=root.GetComponent<SortPopupView>();var content=root.transform.Find("PopupContent");
            var bar=Child(content,"PurchaseWallet");bar.anchorMin=bar.anchorMax=new Vector2(0,1);bar.pivot=new Vector2(0,.5f);bar.anchoredPosition=new Vector2(4,130);bar.sizeDelta=new Vector2(250,95);
            var image=Get<Image>(bar);image.sprite=CoinImageReplacementArtwork.TrySprite("Hud");image.color=Color.white;image.type=Image.Type.Simple;image.preserveAspect=false;image.raycastTarget=false;
            var label=Child(bar,"Value");label.anchorMin=new Vector2(.37f,.1f);label.anchorMax=new Vector2(.95f,.9f);label.offsetMin=label.offsetMax=Vector2.zero;
            var text=Get<Text>(label);text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/LilitaOne/LilitaOne-Regular.ttf");text.fontSize=39;text.text="1600";text.color=new Color(.53f,0,0);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=18;text.resizeTextMaxSize=39;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;view.PurchaseCoinBalance=text;
            var plus=Child(bar,"Plus");plus.anchorMin=plus.anchorMax=new Vector2(.31f,0);plus.pivot=new Vector2(.5f,.5f);plus.anchoredPosition=new Vector2(0,10);plus.sizeDelta=new Vector2(66,66);
            var hit=Get<Image>(plus);hit.color=Color.clear;hit.raycastTarget=true;var button=Get<Button>(plus);button.targetGraphic=hit;
            var art=Child(plus,"Art");art.anchorMin=Vector2.zero;art.anchorMax=Vector2.one;art.offsetMin=art.offsetMax=Vector2.zero;
            var badge=Get<Image>(art);badge.sprite=Sprite("Assets/Sprites/UI/Home/Wallet/PlusBadge.png");badge.color=Color.white;badge.preserveAspect=true;badge.raycastTarget=false;
            if(!plus.GetComponent<SortButtonPressFeedback>())plus.gameObject.AddComponent<SortButtonPressFeedback>();view.PurchaseCoinPlus=button;
        }
        private static Sprite Sprite(string path){foreach(var a in AssetDatabase.LoadAllAssetsAtPath(path))if(a is Sprite)return (Sprite)a;throw new InvalidOperationException("Missing wallet Sprite: "+path);}
        private static T Get<T>(RectTransform rect) where T:Component{var c=rect.GetComponent<T>();return c?c:rect.gameObject.AddComponent<T>();}
        private static RectTransform Child(Transform parent,string name){var c=parent.Find(name);if(c)return (RectTransform)c;var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;}
    }
}
