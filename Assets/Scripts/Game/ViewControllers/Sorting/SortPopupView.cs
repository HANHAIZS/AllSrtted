using System;
using GameFoundation.UI;
using UnityEngine.UI;
namespace AllSorted
{
    public sealed partial class SortPopupView : AllSortedController,IUiPopupView
    {
        public string Id;
        public Text Title,Body,PrimaryCaption,SecondaryCaption;
        public Button Primary,Secondary;
        private bool logicallyOpen;
        private Action primaryAction,secondaryAction,closed;
        public string PopupId { get { return Id; } }
        public bool IsOpen { get { return logicallyOpen && gameObject.activeSelf; } }

        /// <summary>Binds each cached instance once; static authored text is otherwise preserved.</summary>
        public void Configure(Action first,Action second,Action onClosed)
        {
            primaryAction=first;secondaryAction=second;closed=onClosed;
            Primary.onClick.AddListener(InvokePrimary);Secondary.onClick.AddListener(InvokeSecondary);
        }

        /// <summary>Updates explicitly dynamic modal content owned by this feature.</summary>
        public void Present(string title,string body,string first,string second)
        {
            Title.text=title;Body.text=body;PrimaryCaption.text=first;SecondaryCaption.text=second;
            Secondary.gameObject.SetActive(!string.IsNullOrEmpty(second));
        }

        /// <summary>Opens a prepared modal with unscaled visual motion through the Foundation router.</summary>
        public void Open(object arguments) { BeginOpenMotion(); }

        /// <summary>Closes logically once, notifies the owner, and lets noninteractive visuals fade before reuse.</summary>
        public void Close() { if(!logicallyOpen)return;logicallyOpen=false;BeginCloseMotion();if(closed!=null) closed(); }

        /// <summary>Rejects hidden or gated callbacks before forwarding the primary intent.</summary>
        private void InvokePrimary() { if(IsOpen && Primary.IsInteractable() && primaryAction!=null) primaryAction(); }

        /// <summary>Rejects hidden or gated callbacks before forwarding the secondary intent.</summary>
        private void InvokeSecondary() { if(IsOpen && Secondary.IsInteractable() && secondaryAction!=null) secondaryAction(); }
    }
}
