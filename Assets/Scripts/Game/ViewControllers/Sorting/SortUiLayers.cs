using System;
using UnityEngine;
namespace AllSorted
{
    public sealed class SortUiLayers : AllSortedController
    {
        public CanvasGroup[] Hosts=new CanvasGroup[7];
        public bool GameplayBlocked { get; private set; }

        /// <summary>Validates authored semantic hosts; never invents a second UI root.</summary>
        public void Initialize()
        {
            if(Hosts==null || Hosts.Length!=7) throw new InvalidOperationException("Seven UI hosts are required.");
            for(int i=0;i<7;i++) {
                if(!Hosts[i]) throw new InvalidOperationException("Missing UI host "+i);
                var canvas=Hosts[i].GetComponent<Canvas>();
                if(!canvas || canvas.sortingOrder!=i*100) throw new InvalidOperationException("Invalid UI host order "+i);
            }
            Hosts[4].interactable=false;Hosts[4].blocksRaycasts=false;
            Hosts[5].interactable=false;Hosts[5].blocksRaycasts=false;SetModal(false);
        }

        /// <summary>Owns this feature's one Replace-modal gate and its matching world-input gate.</summary>
        public void SetModal(bool value)
        {
            GameplayBlocked=value;
            for(int i=0;i<2;i++) if(Hosts[i]) {Hosts[i].interactable=!value;Hosts[i].blocksRaycasts=!value;}
        }
    }
}
