using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    public static class PrototypeHudLayout
    {
        public const string BarPath="Assets/Sprites/UI/BottomToolbarBackground.png";
        public const string ButtonPath="Assets/Sprites/UI/BottomFunctionButton.png";

        /// <summary>Applies the selected HUD artwork without rebuilding the board or rebinding gameplay.</summary>
        public static void Apply(SortGameViewController view)
        {
            var pages=view.Layers.Hosts[0].transform;var safe=pages.Find("SafeArea");
            if(!safe) throw new InvalidOperationException("HUD SafeArea is missing.");
            foreach(string name in new[]{"Brand","BrandCaption","SectionCaption","MatchCount","ProgressTrack","Status","Instruction","Footer"}) {
                var old=safe.Find(name);if(old) old.gameObject.SetActive(false);
            }
            var level=safe.Find("LevelPill") as RectTransform;
            Place(level,new Vector2(0,1),new Vector2(0,1),new Vector2(0,1),new Vector2(36,-52),new Vector2(130,42));
            Place((RectTransform)view.PauseButton.transform,new Vector2(1,1),new Vector2(1,1),new Vector2(1,1),new Vector2(-36,-41),new Vector2(64,64));
            var clock=safe.Find("ClockPill") as RectTransform;
            clock.gameObject.SetActive(true);
            Place(clock,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-41),new Vector2(174,64));

            var barSprite=AssetDatabase.LoadAssetAtPath<Sprite>(BarPath);var buttonSprite=AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);
            if(!barSprite || !buttonSprite) throw new InvalidOperationException("Selected toolbar sprites are missing.");
            var bar=FindOrCreate("BottomToolbar",pages);bar.SetAsFirstSibling();
            Place(bar,Vector2.zero,new Vector2(1,0),new Vector2(.5f,0),Vector2.zero,new Vector2(0,156));
            var background=GetImage(bar);background.sprite=barSprite;background.type=Image.Type.Sliced;background.color=Color.white;background.raycastTarget=false;

            var row=FindOrCreate("BottomButtons",safe);
            Place(row,Vector2.zero,new Vector2(1,0),new Vector2(.5f,.5f),new Vector2(0,126),new Vector2(0,128));
            for(int i=0;i<4;i++) {
                var rect=FindOrCreate("FunctionButton_"+(i+1),row);float anchor=(i+.5f)/4f;
                Place(rect,new Vector2(anchor,.5f),new Vector2(anchor,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(128,128));
                var image=GetImage(rect);image.sprite=buttonSprite;image.type=Image.Type.Simple;image.preserveAspect=true;image.color=Color.white;image.raycastTarget=true;
                var button=rect.GetComponent<Button>();if(!button) button=rect.gameObject.AddComponent<Button>();
                button.targetGraphic=image;button.transition=Selectable.Transition.None;button.interactable=false;
            }
            if(AssetDatabase.LoadAssetAtPath<Sprite>(PrototypeBoosterArtwork.SpriteRoot+"Vacuum.png")) PrototypeBoosterArtwork.ApplyTo(view);
            ApplyLevelTitleStyle(view.LevelLabel);
            if(System.IO.File.Exists(LevelHudArtwork.ArtRoot+"Normal.png"))LevelHudArtwork.ApplyTo(view);
        }

        /// <summary>Authors the reference's white rounded title and purple outline without changing its binding.</summary>
        public static void ApplyLevelTitleStyle(Text title)
        {
            var roundedFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Fredoka/Fredoka-Bold.ttf");
            if(!title || !roundedFont)throw new InvalidOperationException("Level title or rounded font is missing.");
            title.font=roundedFont;title.fontStyle=FontStyle.Normal;title.fontSize=26;title.color=Color.white;
            var outline=title.GetComponent<Outline>();if(!outline)outline=title.gameObject.AddComponent<Outline>();
            outline.effectColor=new Color(0.43f,0.20f,0.88f,1);outline.effectDistance=new Vector2(2,-2);outline.useGraphicAlpha=true;
        }

        /// <summary>Reuses only feature-owned rects so applying the layout twice does not duplicate controls.</summary>
        private static RectTransform FindOrCreate(string name,Transform parent)
        {
            var found=parent.Find(name) as RectTransform;if(found) return found;
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;
        }

        /// <summary>Gets or adds the explicitly owned decorative image component.</summary>
        private static Image GetImage(RectTransform rect)
        {
            var image=rect.GetComponent<Image>();return image?image:rect.gameObject.AddComponent<Image>();
        }

        /// <summary>Stores deterministic anchors and offsets for the requested top and bottom placement.</summary>
        private static void Place(RectTransform rect,Vector2 min,Vector2 max,Vector2 pivot,Vector2 position,Vector2 size)
        {
            if(!rect) throw new InvalidOperationException("Required HUD rect is missing.");
            rect.anchorMin=min;rect.anchorMax=max;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;
        }
    }
}
