using UnityEngine;
using UnityEngine.Rendering;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        public Mesh ContactShadowMesh;
        public Material ContactShadowMaterial;
        public Material CabinetBackdropShadowMaterial;
        private GameObject cabinetBackdrop;
        private Transform contactRoot;
        private float contactEntranceAlpha=1;
        private Color contactShadowColor;
        private static readonly int ContactColor=Shader.PropertyToID("_BaseColor");
        private static readonly int CabinetCells=Shader.PropertyToID("_CabinetCellRects");
        private static readonly int CabinetCellCount=Shader.PropertyToID("_CabinetCellCount");

        private void ConfigureCabinetAO()
        {
            var cells=new Vector4[16];int count=Mathf.Min(model.Board.ShelfCount,cells.Length);
            for(int i=0;i<count;i++) {
                var center=SourceShelfCenter(i);
                cells[i]=new Vector4(center.x-1.64f,center.y+SortShelfGeometry.SupportY,center.x+1.64f,center.y+.84f);
            }
            Shader.SetGlobalVectorArray(CabinetCells,cells);Shader.SetGlobalInt(CabinetCellCount,count);
            ConfigureCabinetBackdrop(cells,count);
        }
        private void ConfigureContact(PieceVisual visual,int id,int shelf,int slot,bool behind)
        {
            if(!sourceConnectedArtwork || !ContactShadowMesh || !ContactShadowMaterial)return;
            if(!visual.Contact) {
                if(!contactRoot){contactShadowColor=ContactShadowMaterial.GetColor(ContactColor);contactRoot=new GameObject("Product Contact Shadows").transform;contactRoot.SetParent(transform,false);}
                var go=new GameObject("Contact_"+id,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(contactRoot,false);
                go.GetComponent<MeshFilter>().sharedMesh=ContactShadowMesh;
                visual.Contact=go.transform;visual.ContactRenderer=go.GetComponent<MeshRenderer>();
                visual.ContactRenderer.sharedMaterial=ContactShadowMaterial;visual.ContactRenderer.shadowCastingMode=ShadowCastingMode.Off;visual.ContactRenderer.receiveShadows=false;
                visual.ContactBlock=new MaterialPropertyBlock();
            }
            var floor=StageSlotWorld(shelf,slot)+(behind?new Vector3(0,0,SortShelfGeometry.RearOffset.z):Vector3.zero);
            // With the approved elevated front view, a genuine horizontal floor
            // ellipse is visible; it remains below the item and never follows a drag.
            visual.Contact.position=floor+new Vector3(0,SortShelfGeometry.SupportY+.004f,.10f);
            visual.Contact.rotation=Quaternion.Euler(90,0,0);
            float width=Mathf.Clamp(visual.Scale.x*.98f,.28f,1.05f);
            visual.ContactSize=new Vector3(width,width*.75f,1);
            UpdateContact(visual,false);
        }
        private void UpdateContact(PieceVisual visual,bool held)
        {
            if(!visual.Contact)return;
            float fade=visual.Dying?Mathf.Clamp01(visual.Life/(visual.Vacuum?.28f:.22f)):1;
            var color=contactShadowColor;color.a*=fade*contactEntranceAlpha*(held?.40f:1);
            visual.Contact.localScale=visual.ContactSize*(held?1.18f:1);
            visual.ContactBlock.SetColor(ContactColor,color);visual.ContactRenderer.SetPropertyBlock(visual.ContactBlock);
        }
        private void SetContactEntrance(float alpha)
        {
            contactEntranceAlpha=alpha;
            foreach(var pair in pieces)UpdateContact(pair.Value,dragging && pair.Key==draggedId);
        }
        private void DestroyPieceVisual(PieceVisual visual)
        {
            if(visual.Contact){visual.Contact.gameObject.SetActive(false);Destroy(visual.Contact.gameObject);}
            if(visual.Transform)Destroy(visual.Transform.gameObject);
        }
        private void ConfigureCabinetBackdrop(Vector4[] cells,int count)
        {
            if(!CabinetBackdropShadowMaterial || !ContactShadowMesh || count==0)return;
            float left=float.MaxValue,right=float.MinValue,bottom=left,top=right;
            for(int i=0;i<count;i++){left=Mathf.Min(left,cells[i].x-.135f);right=Mathf.Max(right,cells[i].z+.135f);bottom=Mathf.Min(bottom,cells[i].y-.163f);top=Mathf.Max(top,cells[i].w+.11f);}
            if(!cabinetBackdrop){
                cabinetBackdrop=new GameObject("Cabinet Backdrop Shadow",typeof(MeshFilter),typeof(MeshRenderer));cabinetBackdrop.transform.SetParent(transform,false);
                cabinetBackdrop.GetComponent<MeshFilter>().sharedMesh=ContactShadowMesh;
                var renderer=cabinetBackdrop.GetComponent<MeshRenderer>();renderer.sharedMaterial=CabinetBackdropShadowMaterial;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            cabinetBackdrop.SetActive(true);
            cabinetBackdrop.transform.position=new Vector3((left+right)*.5f+.22f,(bottom+top)*.5f-.33f,1.6f);
            cabinetBackdrop.transform.localScale=new Vector3(right-left+1,top-bottom+1,1);
        }
        private void DisposeContacts()
        {
            if(contactRoot)Destroy(contactRoot.gameObject);contactRoot=null;
            if(cabinetBackdrop)Destroy(cabinetBackdrop);cabinetBackdrop=null;
            Shader.SetGlobalInt(CabinetCellCount,0);
        }
    }
}
