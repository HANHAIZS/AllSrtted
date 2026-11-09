using QFramework;
using UnityEngine;

namespace AllSorted
{
    public abstract class AllSortedController : MonoBehaviour, IController
    {
        /// <summary>
        /// Returns the single project-owned QFramework architecture.
        /// </summary>
        public IArchitecture GetArchitecture()
        {
            return AllSortedApp.Interface;
        }
    }
}
