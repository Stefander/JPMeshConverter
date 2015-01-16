using System;
using System.Collections.Generic;
using System.IO;

namespace JPMeshConverter {
    public struct LanguageEntry {
        public string Name;
        public string Text;
    }
    public class LanguageReader : BaseReader {
        public List<LanguageEntry> entries;
        public override void Read(FileStream stream) {
            base.Read(stream);
            entries = new List<LanguageEntry>();
            byte[] unknown = ReadChunk(0x4C);
            
            uint entryCount = ReadUint32();
            for (int i = 0; i < entryCount; i++) {
                entries.Add(ReadLanguageString());
            }
        }

        private LanguageEntry ReadLanguageString() {
            string name = ReadString();
            string content = ReadString();
            uint u1 = ReadUint32();
            uint u2 = ReadUint32();
            return new LanguageEntry() { Name = name, Text = content };
        }
    }
}
