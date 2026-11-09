using System;
using System.IO;
using System.Text;
using GameFoundation.Save;
using UnityEngine;
namespace AllSorted
{
    /// <summary>Retains Foundation atomic writes while distinguishing missing files from failed reads.</summary>
    public sealed class SortResourceSaveStore : ISaveStore
    {
        private readonly string directory;
        private readonly FileSaveStore files;
        public SortResourceSaveStore(string directory=null)
        {
            this.directory=string.IsNullOrWhiteSpace(directory)?Path.Combine(Application.persistentDataPath,"GameFoundation","Saves"):Path.GetFullPath(directory);
            files=new FileSaveStore(this.directory);
        }
        public bool TryRead(string slot,out string value){return Read(PathFor(slot),out value);}
        public bool TryReadBackup(string slot,out string value){return Read(PathFor(slot)+".backup",out value);}
        public void Write(string slot,string value){files.Write(slot,value);}
        public void Restore(string slot,string value){files.Restore(slot,value);}
        public void Delete(string slot){files.Delete(slot);}
        private string PathFor(string slot)
        {
            var name=(string.IsNullOrWhiteSpace(slot)?"default":slot.Trim()).ToCharArray();
            for(int i=0;i<name.Length;i++)if(!char.IsLetterOrDigit(name[i]) && name[i]!='-' && name[i]!='_' && name[i]!='.')name[i]='_';
            return Path.Combine(directory,new string(name)+".json");
        }
        private static bool Read(string path,out string value)
        {
            try {value=File.ReadAllText(path,Encoding.UTF8);return true;}
            catch(FileNotFoundException){value=null;return false;}
            catch(DirectoryNotFoundException){value=null;return false;}
        }
    }
}
