using UnityEngine;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        public GameObject[] ConnectedShelfPrefabs;
        private bool sourceConnectedArtwork;

        private GameObject ConnectedCabinetPrefab()
        {
            int index=model.Board.Definition.number-1;
            if(ConnectedShelfPrefabs==null || index<0 || index>=ConnectedShelfPrefabs.Length)return null;
            return ConnectedShelfPrefabs[index];
        }
        private bool ValidConnectedCabinet(GameObject prefab)
        {
            if(!prefab || !prefab.activeSelf || prefab.transform.localScale!=Vector3.one || prefab.GetComponentsInChildren<Collider>(true).Length>0)return false;
            if(!VisibleShelfMesh(prefab.transform,"StaticBody") || !VisibleShelfMesh(prefab.transform,"BackPanel"))return false;
            var definition=model.Board.Definition;
            for(int shelf=0;shelf<definition.shelves.Length;shelf++) {
                var data=definition.shelves[shelf];
                var center=SortShelfGeometry.ConnectedCenter(data.x,data.y,sourceCenter,definition.number);
                for(int slot=0;slot<3;slot++)for(int layer=0;layer<2;layer++) {
                    var anchor=prefab.transform.Find("Anchors/Shelf_"+shelf+"/"+(layer==0?"Front":"Rear")+slot);
                    if(!anchor || (prefab.transform.InverseTransformPoint(anchor.position)-center-SortShelfGeometry.LocalSlot(slot,layer==1)).sqrMagnitude>.000001f)return false;
                }
            }
            return true;
        }
        private void ConfigureReferenceView(Transform art)
        {
            // Preserve the current authored/user-adjusted orientation for all stages.
            var back=art.Find("BackPanel").GetComponent<MeshRenderer>();
            var block=new MaterialPropertyBlock();block.SetFloat("_TopShadowStrength",.38f);back.SetPropertyBlock(block);
        }
        private void AddConnectedCabinet()
        {
            var art=Instantiate(ConnectedCabinetPrefab(),sourceShelves,false);
            ConfigureCabinetAO();
            ConfigureReferenceView(art.transform);
            art.name="ConnectedArtwork";art.transform.localPosition=Vector3.zero;art.transform.localRotation=Quaternion.identity;
        }
    }
}
