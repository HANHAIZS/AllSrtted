using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AllSorted.Editor
{
    /// <summary>Archives original data; classifications never imply gameplay support.</summary>
    public static class SortExpressLevelImporter
    {
        public const int ExpectedLevelCount = 2200;
        public const string ArchiveAssetPath = "Assets/Art/DesignImports/SortExpress/Levels";
        public const string ManifestFileName = "SourceManifest.json";
        private static readonly Regex FilePattern = new Regex(@"^Level(\d{5})_.+\.json$", RegexOptions.CultureInvariant);

        [Serializable]
        public sealed class ImportReport
        {
            public int schemaVersion = 1;
            public string description = "Byte-exact Sort Express source archive. Classification describes raw data only. tl is preserved without assuming a time unit. Runtime support requires separate validation.";
            public int levelCount;
            public int fixedCount;
            public int mixedCount;
            public int generatedCount;
            public LevelEntry[] entries;
        }

        [Serializable]
        public sealed class LevelEntry
        {
            public int levelNumber;
            public string sourceFileName;
            public string sha256;
            public int byteLength;
            public string classification;
            public string runtimeSupport = "unsupported-until-runtime-validation";
            public double rawTl;
            public int shelfCount;
            public int fixedItemCount;
            public int placeholderCount;
            public bool hasGenerator;
            public string[] diagnostics;
        }

        private sealed class SourceFile
        {
            public LevelEntry entry;
            public byte[] bytes;
        }

        [MenuItem("All Sorted/Levels/Import Sort Express Raw Configurations...")]
        public static void ImportFromMenu()
        {
            var folder = EditorUtility.OpenFolderPanel("Select Sort Express configs/levels (2200 JSON files)", "", "");
            if (string.IsNullOrEmpty(folder)) return;
            try
            {
                var report = Import(folder);
                AssetDatabase.Refresh();
                Debug.Log("Archived " + report.levelCount + " source levels at " + ArchiveAssetPath + ". Raw classification does not imply runtime support.");
            }
            catch (Exception exception)
            {
                Debug.LogError("Sort Express import stopped: " + exception.Message);
            }
        }

        public static ImportReport Import(string sourceFolder)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Import(sourceFolder, Path.Combine(projectRoot, ArchiveAssetPath), ExpectedLevelCount);
        }

        /// <summary>Validates the complete source and existing archive before writing. Never deletes or overwrites conflicting data.</summary>
        public static ImportReport Import(string sourceFolder, string destinationFolder, int expectedCount)
        {
            if (expectedCount <= 0) throw new ArgumentOutOfRangeException(nameof(expectedCount));
            if (!Directory.Exists(sourceFolder)) throw new DirectoryNotFoundException(sourceFolder);
            var paths = Directory.GetFiles(sourceFolder, "*.json", SearchOption.TopDirectoryOnly);
            if (paths.Length != expectedCount)
                throw new InvalidDataException("Expected " + expectedCount + " JSON files; found " + paths.Length + ".");
            var files = new SortedDictionary<int, SourceFile>();
            foreach (var path in paths)
            {
                var name = Path.GetFileName(path);
                var match = FilePattern.Match(name);
                int number;
                if (!match.Success || !int.TryParse(match.Groups[1].Value, out number) || number < 1 || number > expectedCount)
                    throw new InvalidDataException("Invalid or out-of-range level filename: " + name);
                if (files.ContainsKey(number)) throw new InvalidDataException("Duplicate source level number: " + number);
                var bytes = File.ReadAllBytes(path);
                try
                {
                    var json = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
                    var data = new StrictJson(json).Read() as Dictionary<string, object>;
                    if (data == null) throw new InvalidDataException("Root must be an object.");
                    var entry = Inspect(data, number, name);
                    entry.byteLength = bytes.Length;
                    using (var algorithm = SHA256.Create())
                        entry.sha256 = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                    files.Add(number, new SourceFile { entry = entry, bytes = bytes });
                }
                catch (Exception exception) when (exception is InvalidDataException || exception is DecoderFallbackException)
                {
                    throw new InvalidDataException(name + ": " + exception.Message, exception);
                }
            }
            var report = new ImportReport { levelCount = expectedCount, entries = new LevelEntry[expectedCount] };
            for (var number = 1; number <= expectedCount; number++)
            {
                if (!files.ContainsKey(number)) throw new InvalidDataException("Missing source level number: " + number);
                var entry = files[number].entry;
                report.entries[number - 1] = entry;
                if (entry.classification == "fixed") report.fixedCount++;
                else if (entry.classification == "mixed") report.mixedCount++;
                else report.generatedCount++;
            }
            var manifestBytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(report, true) + "\n");
            // Preflight every destination, including the manifest, before the first filesystem mutation.
            if (Directory.Exists(destinationFolder))
            {
                var allowed = new HashSet<string>(StringComparer.Ordinal) { ManifestFileName };
                foreach (var file in files.Values) allowed.Add(file.entry.sourceFileName);
                foreach (var path in Directory.GetFiles(destinationFolder, "*.json"))
                    if (!allowed.Contains(Path.GetFileName(path)))
                        throw new IOException("Archive contains an unrecognized JSON file; preserve or migrate it explicitly: " + path);
            }
            foreach (var file in files.Values) CheckExisting(Path.Combine(destinationFolder, file.entry.sourceFileName), file.bytes);
            var manifestPath = Path.Combine(destinationFolder, ManifestFileName);
            CheckExisting(manifestPath, manifestBytes);
            Directory.CreateDirectory(destinationFolder);
            foreach (var file in files.Values) WriteNew(Path.Combine(destinationFolder, file.entry.sourceFileName), file.bytes);
            WriteNew(manifestPath, manifestBytes);
            return report;
        }

        private static void CheckExisting(string path, byte[] expected)
        {
            if (Directory.Exists(path)) throw new IOException("An archive file path is already a directory: " + path);
            if (!File.Exists(path)) return;
            var existing = File.ReadAllBytes(path);
            if (existing.Length == expected.Length)
            {
                var equal = true;
                for (var index = 0; index < existing.Length; index++)
                    if (existing[index] != expected[index]) { equal = false; break; }
                if (equal) return;
            }
            throw new IOException("Refusing to replace different archive data: " + path);
        }

        private static void WriteNew(string path, byte[] bytes)
        {
            if (File.Exists(path)) return;
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) stream.Write(bytes, 0, bytes.Length);
        }

        private static LevelEntry Inspect(Dictionary<string, object> root, int number, string name)
        {
            object value;
            if (!root.TryGetValue("tl", out value) || !(value is double)) throw new InvalidDataException("tl must be a number.");
            var entry = new LevelEntry { levelNumber = number, sourceFileName = name, rawTl = (double)value };
            var shelves = RequiredArray(root, "s");
            var generators = RequiredArray(root, "ig");
            entry.shelfCount = shelves.Count;
            foreach (var generator in generators)
            {
                var group = RequireObject(generator, "ig entry");
                if (RequiredArray(group, "isg").Count > 0) entry.hasGenerator = true;
            }
            var diagnostics = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var shelf in shelves)
            {
                var data = RequireObject(shelf, "s entry");
                if (!data.TryGetValue("sicw", out value) || !(value is double) || (double)value != Math.Truncate((double)value) || (double)value <= 0)
                    throw new InvalidDataException("Shelf sicw must be a positive integer.");
                if ((double)value != 3) diagnostics.Add("width-not-three");
                foreach (var row in RequiredArray(data, "si"))
                {
                    var cells = RequiredArray(RequireObject(row, "si entry"), "rd");
                    if (cells.Count != 3) diagnostics.Add("row-width-not-three");
                    foreach (var cell in cells)
                    {
                        var item = RequireObject(cell, "rd entry");
                        if (!item.TryGetValue("it", out value) || !(value is double) || (double)value != Math.Truncate((double)value))
                            throw new InvalidDataException("Cell it must be an integer.");
                        if ((double)value == -1) entry.placeholderCount++;
                        else if ((double)value == 1)
                        {
                            object id;
                            if (!item.TryGetValue("iid", out id) || !(id is string) || string.IsNullOrEmpty((string)id))
                                throw new InvalidDataException("Fixed cell must have a nonempty iid.");
                            entry.fixedItemCount++;
                        }
                        else if ((double)value != 0) diagnostics.Add("unknown-item-type");
                    }
                }
            }
            // Recursion also sees flags/properties embedded in generation templates, not just visible cells.
            InspectRawFeatures(root, diagnostics);
            var generated = entry.hasGenerator || entry.placeholderCount > 0;
            entry.classification = !generated ? "fixed" : entry.fixedItemCount > 0 ? "mixed" : "generated";
            if (entry.hasGenerator) diagnostics.Add("ig-isg");
            if (entry.placeholderCount > 0) diagnostics.Add("it-minus-one");
            entry.diagnostics = new string[diagnostics.Count];
            diagnostics.CopyTo(entry.diagnostics);
            return entry;
        }

        private static Dictionary<string, object> RequireObject(object value, string label)
        {
            var result = value as Dictionary<string, object>;
            if (result == null) throw new InvalidDataException(label + " must be an object.");
            return result;
        }

        private static List<object> RequiredArray(Dictionary<string, object> data, string key)
        {
            object value;
            if (!data.TryGetValue(key, out value) || !(value is List<object>)) throw new InvalidDataException(key + " must be an array.");
            return (List<object>)value;
        }

        private static void InspectRawFeatures(object node, SortedSet<string> diagnostics)
        {
            var data = node as Dictionary<string, object>;
            if (data != null)
            {
                foreach (var pair in data)
                {
                    if ((pair.Key == "im" || pair.Key == "icl" || pair.Key == "icn" || pair.Key == "isd") && pair.Value is bool && (bool)pair.Value)
                        diagnostics.Add(pair.Key);
                    if (pair.Key == "pr" && pair.Value != null) diagnostics.Add("nested-pr");
                    InspectRawFeatures(pair.Value, diagnostics);
                }
            }
            var list = node as List<object>;
            if (list != null) foreach (var child in list) InspectRawFeatures(child, diagnostics);
        }

        // JsonUtility cannot reject duplicate keys or validate unknown fields. This reader validates the
        // entire JSON document without adding a package dependency or interpreting unknown mechanics.
        private sealed class StrictJson
        {
            private readonly string text;
            private int offset;
            public StrictJson(string text) { this.text = text; }
            public object Read()
            {
                var result = Value(0); White();
                if (offset != text.Length) Fail("Trailing content");
                return result;
            }
            private void White() { while (offset < text.Length && " \t\r\n".IndexOf(text[offset]) >= 0) offset++; }
            private void Fail(string reason) { throw new InvalidDataException(reason + " at JSON offset " + offset + "."); }
            private bool Take(char c) { White(); if (offset < text.Length && text[offset] == c) { offset++; return true; } return false; }
            private void Need(char c) { if (!Take(c)) Fail("Expected '" + c + "'"); }
            private object Value(int depth)
            {
                if (depth > 128) Fail("JSON nesting limit exceeded");
                White(); if (offset >= text.Length) { Fail("Unexpected end"); return null; }
                var c = text[offset];
                if (c == '"') return String();
                if (c == '{')
                {
                    offset++; var result = new Dictionary<string, object>(StringComparer.Ordinal);
                    if (Take('}')) return result;
                    do
                    {
                        White(); if (offset >= text.Length || text[offset] != '"') Fail("Expected object key");
                        var key = String(); Need(':');
                        if (result.ContainsKey(key)) Fail("Duplicate object key: " + key);
                        result.Add(key, Value(depth + 1));
                        if (Take('}')) return result;
                        Need(',');
                    } while (true);
                }
                if (c == '[')
                {
                    offset++; var result = new List<object>();
                    if (Take(']')) return result;
                    do { result.Add(Value(depth + 1)); if (Take(']')) return result; Need(','); } while (true);
                }
                foreach (var literal in new[] { "true", "false", "null" })
                    if (offset + literal.Length <= text.Length && string.CompareOrdinal(text, offset, literal, 0, literal.Length) == 0)
                    { offset += literal.Length; if (literal == "null") return null; return literal == "true"; }
                return Number();
            }
            private string String()
            {
                Need('"'); var result = new StringBuilder();
                while (offset < text.Length)
                {
                    var c = text[offset++];
                    if (c == '"') return result.ToString();
                    if (c < 32) Fail("Unescaped control character");
                    if (c != '\\') { result.Append(c); continue; }
                    if (offset == text.Length) Fail("Unterminated escape");
                    c = text[offset++];
                    switch (c)
                    {
                        case '"': case '\\': case '/': result.Append(c); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u':
                            ushort code;
                            if (offset + 4 > text.Length || !ushort.TryParse(text.Substring(offset, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                            { Fail("Invalid unicode escape"); return null; }
                            result.Append((char)code); offset += 4; break;
                        default: Fail("Invalid escape"); break;
                    }
                }
                Fail("Unterminated string"); return null;
            }
            private bool Digit() { return offset < text.Length && text[offset] >= '0' && text[offset] <= '9'; }
            private object Number()
            {
                var start = offset;
                if (offset < text.Length && text[offset] == '-') offset++;
                if (!Digit()) Fail("Expected number");
                if (text[offset] == '0') offset++;
                else while (Digit()) offset++;
                if (offset < text.Length && text[offset] == '.')
                { offset++; if (!Digit()) Fail("Expected fraction"); while (Digit()) offset++; }
                if (offset < text.Length && (text[offset] == 'e' || text[offset] == 'E'))
                {
                    offset++;
                    if (offset < text.Length && (text[offset] == '+' || text[offset] == '-')) offset++;
                    if (!Digit()) Fail("Expected exponent"); while (Digit()) offset++;
                }
                double value;
                if (!double.TryParse(text.Substring(start, offset - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsInfinity(value) || double.IsNaN(value))
                    Fail("Invalid number");
                return value;
            }
        }
    }
}
