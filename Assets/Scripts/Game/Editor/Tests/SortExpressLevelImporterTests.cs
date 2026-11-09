using System;
using System.IO;
using System.Text;
using AllSorted.Editor;
using NUnit.Framework;

namespace AllSorted.Tests
{
    public class SortExpressLevelImporterTests
    {
        private string root;
        private string source;
        private string destination;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "SortExpressImportTests-" + Guid.NewGuid().ToString("N"));
            source = Path.Combine(root, "source");
            destination = Path.Combine(root, "archive");
            Directory.CreateDirectory(source);
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); }

        private void Level(int number, string json, string suffix = "a")
        {
            File.WriteAllText(Path.Combine(source, "Level" + number.ToString("D5") + "_" + suffix + ".json"), json, new UTF8Encoding(false));
        }

        [Test]
        public void Import_PreservesBytesAndIsDeterministic()
        {
            Level(1, " {\"tl\":300,\"s\":[{\"sicw\":3,\"si\":[{\"rd\":[{\"it\":1,\"iid\":\"apple\"},{\"it\":0},{\"it\":0}]}]}],\"ig\":[]}\n");
            var report = SortExpressLevelImporter.Import(source, destination, 1);
            var original = File.ReadAllBytes(Directory.GetFiles(source)[0]);
            CollectionAssert.AreEqual(original, File.ReadAllBytes(Path.Combine(destination, report.entries[0].sourceFileName)));
            Assert.AreEqual("fixed", report.entries[0].classification);
            Assert.AreEqual("unsupported-until-runtime-validation", report.entries[0].runtimeSupport);
            Assert.AreEqual(64, report.entries[0].sha256.Length);
            var manifest = File.ReadAllBytes(Path.Combine(destination, SortExpressLevelImporter.ManifestFileName));
            SortExpressLevelImporter.Import(source, destination, 1);
            CollectionAssert.AreEqual(manifest, File.ReadAllBytes(Path.Combine(destination, SortExpressLevelImporter.ManifestFileName)));
        }

        [Test]
        public void Import_ClassifiesMixedAndReportsNestedRawFeatures()
        {
            Level(1, "{\"tl\":5,\"s\":[{\"sicw\":4,\"im\":true,\"si\":[{\"rd\":[{\"it\":1,\"iid\":\"a\",\"pr\":{\"icl\":true}},{\"it\":-1}]}]}],\"ig\":[{\"isg\":[{\"icd\":[{\"id2\":{\"it\":1,\"pr\":{\"ib\":true}}}]}]}]}");
            var entry = SortExpressLevelImporter.Import(source, destination, 1).entries[0];
            Assert.AreEqual("mixed", entry.classification);
            CollectionAssert.Contains(entry.diagnostics, "width-not-three");
            CollectionAssert.Contains(entry.diagnostics, "im");
            CollectionAssert.Contains(entry.diagnostics, "icl");
            CollectionAssert.Contains(entry.diagnostics, "nested-pr");
            Assert.AreEqual(5, entry.rawTl);
        }

        [Test]
        public void Import_DoesNotCountGeneratorTemplatesAsFixedBoardItems()
        {
            Level(1, "{\"tl\":1,\"s\":[{\"sicw\":3,\"si\":[{\"rd\":[{\"it\":-1}]}]}],\"ig\":[{\"isg\":[{\"icd\":[{\"id2\":{\"it\":1}}]}]}]}");
            Assert.AreEqual("generated", SortExpressLevelImporter.Import(source, destination, 1).entries[0].classification);
        }

        [TestCase("{\"tl\":1,\"s\":[],\"ig\":[],}")]
        [TestCase("{\"tl\":1,\"tl\":2,\"s\":[],\"ig\":[]}")]
        [TestCase("{\"tl\":1,\"s\":[],\"ig\":[]}garbage")]
        [TestCase("{\"tl\":1,\"s\":[],\"ig\":[],\"x\":01}")]
        public void Import_RejectsMalformedJsonBeforeAnyWrite(string invalid)
        {
            Level(1, "{\"tl\":1,\"s\":[],\"ig\":[]}"); Level(2, invalid);
            Assert.Throws<InvalidDataException>(() => SortExpressLevelImporter.Import(source, destination, 2));
            Assert.IsFalse(Directory.Exists(destination));
        }

        [Test]
        public void Import_RejectsMissingAndDuplicateNumbersBeforeAnyWrite()
        {
            Level(1, "{\"tl\":1,\"s\":[],\"ig\":[]}"); Level(1, "{}", "duplicate");
            Assert.Throws<InvalidDataException>(() => SortExpressLevelImporter.Import(source, destination, 2));
            Assert.IsFalse(Directory.Exists(destination));
        }

        [Test]
        public void Import_RejectsIncompleteNumberRangeBeforeAnyWrite()
        {
            Level(1, "{\"tl\":1,\"s\":[],\"ig\":[]}");
            Assert.Throws<InvalidDataException>(() => SortExpressLevelImporter.Import(source, destination, 2));
            Assert.IsFalse(Directory.Exists(destination));
        }

        [Test]
        public void Import_RejectsInvalidUtf8BeforeAnyWrite()
        {
            File.WriteAllBytes(Path.Combine(source, "Level00001_a.json"), new byte[] { 0xc3, 0x28 });
            Assert.Throws<InvalidDataException>(() => SortExpressLevelImporter.Import(source, destination, 1));
            Assert.IsFalse(Directory.Exists(destination));
        }

        [Test]
        public void Import_RefusesToOverwriteExistingArchive()
        {
            Level(1, "{\"tl\":1,\"s\":[],\"ig\":[]}");
            SortExpressLevelImporter.Import(source, destination, 1);
            Level(1, "{\"tl\":2,\"s\":[],\"ig\":[]}");
            Assert.Throws<IOException>(() => SortExpressLevelImporter.Import(source, destination, 1));
            StringAssert.Contains("\"tl\":1", File.ReadAllText(Path.Combine(destination, "Level00001_a.json")));
        }
    }
}
