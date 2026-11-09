using System;
using System.Reflection;
using NUnit.Framework;
using QFramework;
using UnityEditor;
using UnityEngine;
namespace AllSorted.Tests
{
    public sealed class HomeNavigationTests : WalletTestBase
    {
        private static Type Type(string name) {var t=typeof(SortBoardModel).Assembly.GetType("AllSorted."+name);Assert.IsNotNull(t,"Missing Home navigation capability: "+name);return t;}
        private static object Model() {return typeof(IArchitecture).GetMethod("GetModel").MakeGenericMethod(Type("SortHomeNavigationModel")).Invoke(AllSortedApp.Interface,null);}
        private static object Read(object o,string n) {return o.GetType().GetProperty(n)!=null?o.GetType().GetProperty(n).GetValue(o,null):o.GetType().GetField(n).GetValue(o);}
        private static int Number(object o,string n) {return Convert.ToInt32(Read(o,n));}
        private static object Send(string name,params object[] args) {var command=(ICommand)Activator.CreateInstance(Type(name),args);AllSortedApp.Interface.SendCommand(command);return command;}
        private static object Tab(int i) {return Enum.ToObject(Type("SortHomeTab"),i);}
        [SetUp] public void Reset() {Model();Send("ClearSortHomeEntryCommand");Send("SelectSortHomeTabCommand",Tab(1));}
        [TearDown] public void Cleanup() {if(typeof(SortBoardModel).Assembly.GetType("AllSorted.ClearSortHomeEntryCommand")!=null) Send("ClearSortHomeEntryCommand");}

        [Test] public void FreshNavigationStartsHomeWithoutAnEntry()
        {
            var fresh=Activator.CreateInstance(Type("SortHomeNavigationModel"));
            Assert.AreEqual(1,Number(Read(fresh,"SelectedTab"),"Value"));Assert.AreEqual(0,Number(fresh,"PendingLevel"));
        }

        [Test] public void InvalidOrRepeatedSelectionsCannotPublishOrChangeGameplay()
        {
            var model=Model();var selected=Read(model,"SelectedTab");var board=AllSortedApp.Interface.GetModel<SortBoardModel>();
            var original=board.Board;int revision=board.BoardRevision.Value;int notices=0;
            // Model revision provides a non-generic observer for completed selection transactions.
            var changes=(BindableProperty<int>)Read(model,"Revision");var subscription=changes.Register(v=>notices++);
            try {
                Assert.IsTrue((bool)Read(Send("SelectSortHomeTabCommand",Tab(2)),"Accepted"));Assert.AreEqual(1,notices);
                Assert.IsFalse((bool)Read(Send("SelectSortHomeTabCommand",Tab(2)),"Accepted"));
                foreach(int invalid in new[]{-1,3,int.MaxValue}) Assert.IsFalse((bool)Read(Send("SelectSortHomeTabCommand",Tab(invalid)),"Accepted"));
                Assert.AreEqual(2,Number(selected,"Value"));Assert.AreEqual(1,notices);Assert.AreSame(original,board.Board);Assert.AreEqual(revision,board.BoardRevision.Value);
            } finally {subscription.UnRegister();}
        }

        [Test] public void PendingEntryIsExclusiveValidatedAndConsumedExactlyOnce()
        {
            var model=Model();
            foreach(int invalid in new[]{-1,0,4,int.MaxValue}) Assert.IsFalse((bool)Read(Send("RequestSortHomeEntryCommand",invalid),"Accepted"));
            Assert.AreEqual(0,Number(model,"PendingLevel"));Assert.IsTrue((bool)Read(Send("RequestSortHomeEntryCommand",3),"Accepted"));
            Assert.IsFalse((bool)Read(Send("RequestSortHomeEntryCommand",1),"Accepted"));Assert.AreEqual(3,Number(model,"PendingLevel"));
            Assert.AreEqual(3,Number(Send("ConsumeSortHomeEntryCommand"),"LevelNumber"));Assert.AreEqual(0,Number(Send("ConsumeSortHomeEntryCommand"),"LevelNumber"));
            Assert.IsTrue((bool)Read(Send("RequestSortHomeEntryCommand",2),"Accepted"));Send("ClearSortHomeEntryCommand");Assert.AreEqual(0,Number(model,"PendingLevel"));
        }

        [Test] public void NavigationDoesNotChangeSessionPreferencesOrAllowances()
        {
            var app=AllSortedApp.Interface;var settings=app.GetModel<SortSettingsModel>();var quota=app.GetModel<SortBoosterModel>();
            bool music=settings.GetEnabled(SortSetting.Music);int settingsRevision=settings.Revision.Value,quotaRevision=quota.Revision.Value;
            for(int i=0;i<3;i++) {Send("SelectSortHomeTabCommand",Tab(i));Send("RequestSortHomeEntryCommand",i+1);Send("ConsumeSortHomeEntryCommand");}
            Assert.AreEqual(music,settings.GetEnabled(SortSetting.Music));Assert.AreEqual(settingsRevision,settings.Revision.Value);Assert.AreEqual(quotaRevision,quota.Revision.Value);
        }
        [Test] public void MenuSpriteSlicesHaveUniqueStableNonzeroIdentifiers()
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Sprites/UI/Home"})) {
                string path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(!importer || importer.spriteImportMode!=SpriteImportMode.Multiple) continue;
                var ids=new System.Collections.Generic.HashSet<long>();
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if(asset is Sprite) {
                    string assetGuid;long id;Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out assetGuid,out id));
                    Assert.AreNotEqual(0,id,path+":"+asset.name+" must survive normal Play Mode serialization.");Assert.IsTrue(ids.Add(id),path+" has duplicate local IDs.");
                }
                var serialized=new SerializedObject(importer);var sprites=serialized.FindProperty("m_SpriteSheet.m_Sprites");var spriteIds=new System.Collections.Generic.HashSet<string>();
                for(int i=0;i<sprites.arraySize;i++) Assert.IsTrue(spriteIds.Add(sprites.GetArrayElementAtIndex(i).FindPropertyRelative("m_SpriteID").stringValue),path+" has duplicate sprite GUIDs.");
            }
        }
    }
}
