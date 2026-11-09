using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllSorted.Editor
{
    /// <summary>Read-only source catalog; never starts unsupported content as gameplay.</summary>
    public sealed class SortExpressLevelCatalogWindow : EditorWindow
    {
        private SortExpressLevelImporter.ImportReport report;
        private int selected=1,depth;
        private Vector2 scroll,previewScroll;
        private string raw,error;
        private SourceLevel level;
        private bool showRaw;
#pragma warning disable 0649
        [Serializable] private sealed class SourceLevel {public Shelf[] s;}
        [Serializable] private sealed class Shelf {public string id,gid;public Position pos;public int sicw;public Row[] si;}
        [Serializable] private sealed class Position {public float x,y;}
        [Serializable] private sealed class Row {public Cell[] rd;}
        [Serializable] private sealed class Cell {public int it;public string iid;}
#pragma warning restore 0649
        [MenuItem("All Sorted/Levels/Sort Express 配置目录")]
        public static void Open(){GetWindow<SortExpressLevelCatalogWindow>("Sort Express 原关卡").Show();}
        private void OnEnable(){Reload();}
        private static string Folder {get{return Path.Combine(Directory.GetParent(Application.dataPath).FullName,SortExpressLevelImporter.ArchiveAssetPath);}}
        private void Reload()
        {
            try {
                report=JsonUtility.FromJson<SortExpressLevelImporter.ImportReport>(File.ReadAllText(Path.Combine(Folder,SortExpressLevelImporter.ManifestFileName)));
                if(report==null || report.entries==null || report.entries.Length!=report.levelCount)throw new InvalidDataException("Invalid source manifest.");
                Select(selected);
            } catch(Exception e){report=null;level=null;error=e.Message;}
        }
        private void Select(int number)
        {
            selected=Mathf.Clamp(number,1,report.levelCount);depth=0;error=null;
            try {raw=File.ReadAllText(Path.Combine(Folder,report.entries[selected-1].sourceFileName));level=JsonUtility.FromJson<SourceLevel>(raw);if(level==null || level.s==null)throw new InvalidDataException("Missing source shelves.");}
            catch(Exception e){error=e.Message;raw=null;level=null;}
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sort Express! 2165 原始配置",EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("重新读取目录"))Reload();
            if(GUILayout.Button("从文件夹导入...")){SortExpressLevelImporter.ImportFromMenu();Reload();}
            EditorGUILayout.EndHorizontal();
            if(report==null){EditorGUILayout.HelpBox(error??"请先导入配置。",MessageType.Warning);return;}
            EditorGUILayout.LabelField(string.Format("共 {0} 关 · 固定 {1} · 混合 {2} · 生成 {3}",report.levelCount,report.fixedCount,report.mixedCount,report.generatedCount));
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("上一关",GUILayout.Width(65)))Select(selected-1);
            int requested=EditorGUILayout.IntField("原关卡编号",selected);if(requested!=selected)Select(requested);
            if(GUILayout.Button("下一关",GUILayout.Width(65)))Select(selected+1);
            EditorGUILayout.EndHorizontal();
            var entry=report.entries[selected-1];
            EditorGUILayout.HelpBox("运行支持：待验证。此页为源配置预览，不是游戏关卡运行；生成占位和特殊机制不作近似替换。",MessageType.Warning);
            EditorGUILayout.LabelField("结构分类",entry.classification);
            EditorGUILayout.LabelField("货架 / 显式商品 / 生成占位",entry.shelfCount+" / "+entry.fixedItemCount+" / "+entry.placeholderCount);
            EditorGUILayout.LabelField("tl 原值（未确定单位）",entry.rawTl.ToString(System.Globalization.CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("兼容性标记",string.Join(", ",entry.diagnostics));
            EditorGUILayout.SelectableLabel(entry.sourceFileName+"\nSHA256: "+entry.sha256,GUILayout.Height(35));
            if(!string.IsNullOrEmpty(error)){EditorGUILayout.HelpBox(error,MessageType.Error);return;}
            int max=0;foreach(var shelf in level.s)if(shelf.si!=null)max=Mathf.Max(max,shelf.si.Length-1);
            depth=EditorGUILayout.IntSlider("编码层索引（不预设前后序）",depth,0,max);
            scroll=EditorGUILayout.BeginScrollView(scroll);
            float sx,sy,cw,ch;PreviewSize(out sx,out sy,out cw,out ch);
            previewScroll=EditorGUILayout.BeginScrollView(previewScroll,GUILayout.Height(420));
            Rect area=GUILayoutUtility.GetRect(cw,ch,GUILayout.ExpandWidth(false));DrawLayout(area,sx,sy);EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField("灰：无商品编码 · 蓝：显式商品 · 黄：生成占位 · 红：其他类型。位置按源pos示意。",EditorStyles.wordWrappedLabel);
            showRaw=EditorGUILayout.Foldout(showRaw,"原始 JSON（只读）",true);
            if(showRaw)EditorGUILayout.SelectableLabel(raw,EditorStyles.textArea,GUILayout.Height(Mathf.Clamp(raw.Split('\n').Length*14,200,6000)));
            EditorGUILayout.EndScrollView();
        }
        private void PreviewSize(out float sx,out float sy,out float width,out float height)
        {
            float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue,maxWidth=40,dx=float.MaxValue,dy=float.MaxValue;
            foreach(var a in level.s){if(a.pos==null)continue;minX=Mathf.Min(minX,a.pos.x);maxX=Mathf.Max(maxX,a.pos.x);minY=Mathf.Min(minY,a.pos.y);maxY=Mathf.Max(maxY,a.pos.y);maxWidth=Mathf.Max(maxWidth,a.sicw*34);
                foreach(var b in level.s){if(b.pos==null)continue;float x=Mathf.Abs(a.pos.x-b.pos.x),y=Mathf.Abs(a.pos.y-b.pos.y);if(x>.001f)dx=Mathf.Min(dx,x);if(y>.001f)dy=Mathf.Min(dy,y);}}
            sx=dx==float.MaxValue?100:Mathf.Max(100,(maxWidth+12)/dx);sy=dy==float.MaxValue?100:Mathf.Max(100,58/dy);
            width=minX==float.MaxValue?300:Mathf.Max(300,(maxX-minX)*sx+maxWidth+30);height=minY==float.MaxValue?390:Mathf.Max(390,(maxY-minY)*sy+76);
        }
        private void DrawLayout(Rect area,float sx,float sy)
        {
            EditorGUI.DrawRect(area,new Color(.11f,.12f,.17f));
            if(level.s.Length==0)return;
            float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
            foreach(var s in level.s)if(s.pos!=null){minX=Mathf.Min(minX,s.pos.x);maxX=Mathf.Max(maxX,s.pos.x);minY=Mathf.Min(minY,s.pos.y);maxY=Mathf.Max(maxY,s.pos.y);}
            if(minX==float.MaxValue)return;
            
            foreach(var s in level.s){if(s.pos==null)continue;
                float cardWidth=Mathf.Max(40,s.sicw*34);
                float x=area.x+15+(s.pos.x-minX)*sx;
                float y=area.y+15+(maxY-s.pos.y)*sy;
                var rect=new Rect(x,y,cardWidth,46);EditorGUI.DrawRect(rect,new Color(.25f,.25f,.36f));
                GUI.Label(new Rect(x+2,y,cardWidth-4,16),"gid "+s.gid+" · width "+s.sicw,EditorStyles.miniLabel);
                if(s.si==null || depth>=s.si.Length || s.si[depth].rd==null)continue;
                var cells=s.si[depth].rd;float cw=(cardWidth-4)/Mathf.Max(1,cells.Length);
                for(int c=0;c<cells.Length;c++){var cell=cells[c];var r=new Rect(x+2+c*cw,y+18,cw-2,24);
                    EditorGUI.DrawRect(r,cell.it==0?Color.gray:cell.it==1?new Color(.15f,.5f,.8f):cell.it==-1?new Color(.65f,.45f,.1f):new Color(.7f,.2f,.2f));
                    GUI.Label(r,new GUIContent(cell.it==1?cell.iid:cell.it==-1?"?":cell.it.ToString(),cell.iid),EditorStyles.miniLabel);
                }
            }
        }
    }
}
