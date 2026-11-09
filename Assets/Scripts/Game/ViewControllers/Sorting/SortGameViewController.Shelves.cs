using UnityEngine;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        public GameObject ModularShelfPrefab;
        private bool sourceModularArtwork;

        private Vector3 SourceShelfCenter(int shelf)
        {
            var data=model.Board.Definition.shelves[shelf];
            return sourceConnectedArtwork
                ?SortShelfGeometry.ConnectedCenter(data.x,data.y,sourceCenter,model.Board.Definition.number)
                :SortShelfGeometry.SourceCenter(data.x,data.y,sourceCenter);
        }
        private static bool VisibleShelfMesh(Transform root,string name)
        {
            var part=root.Find(name);if(!part || !part.gameObject.activeSelf)return false;
            if(part.localPosition.sqrMagnitude>.000001f || part.localScale!=Vector3.one || Quaternion.Angle(part.localRotation,Quaternion.identity)>.001f)return false;
            var renderer=part.GetComponent<MeshRenderer>();var filter=part.GetComponent<MeshFilter>();
            if(!renderer || !renderer.enabled || !renderer.sharedMaterial || !filter || !filter.sharedMesh || filter.sharedMesh.vertexCount==0)return false;
            var mesh=filter.sharedMesh;var size=mesh.bounds.size;
            if(size.x<=.0001f || size.y<=.0001f || size.z<=.0001f)return false;
            for(int sub=0;sub<mesh.subMeshCount;sub++)
                if(mesh.GetTopology(sub)==MeshTopology.Triangles && mesh.GetIndexCount(sub)>=3)return true;
            return false;
        }
        private static bool ValidModularShelf(GameObject prefab)
        {
            if(!prefab || !prefab.activeSelf || prefab.transform.localScale!=Vector3.one || prefab.GetComponentsInChildren<Collider>(true).Length>0)return false;
            if(!VisibleShelfMesh(prefab.transform,"StaticBody") || !VisibleShelfMesh(prefab.transform,"BackPanel"))return false;
            for(int slot=0;slot<3;slot++)for(int layer=0;layer<2;layer++) {
                string name=(layer==0?"Front":"Rear")+slot;
                var anchor=prefab.transform.Find("Anchors/"+name);if(!anchor)anchor=prefab.transform.Find(name);
                if(!anchor || (prefab.transform.InverseTransformPoint(anchor.position)-SortShelfGeometry.LocalSlot(slot,layer==1)).sqrMagnitude>.000001f)return false;
            }
            return true;
        }
        private bool AddModularShelf(Transform root)
        {
            if(!sourceModularArtwork)return false;
            var art=Instantiate(ModularShelfPrefab,root,false);art.name="SourceShelfArtwork";
            art.transform.localPosition=Vector3.zero;art.transform.localRotation=Quaternion.identity;return true;
        }
        private void ConfigureSourceSupport(PieceVisual visual)
        {
            if((!sourceModularArtwork && !sourceConnectedArtwork) || CanUseSourceArtwork(visual.Kind))return;
            var mesh=visual.Transform.GetComponent<MeshFilter>().sharedMesh;
            var scale=StagePieceScale(visual.Kind);float bottom=float.MaxValue;
            // Read the real primitive surface once at creation, including its authored tilt.
            // Renderer bounds would overestimate the bottom of tilted spheres/capsules.
            foreach(var vertex in mesh.vertices)
                bottom=Mathf.Min(bottom,(visual.Transform.localRotation*Vector3.Scale(vertex,scale)).y);
            visual.FrontSupport=SortShelfGeometry.SupportY-bottom;
            visual.RearSupport=SortShelfGeometry.SupportY-bottom*.72f;
        }
    }
}
