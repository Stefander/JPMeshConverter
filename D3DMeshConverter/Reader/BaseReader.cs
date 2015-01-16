using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPMeshConverter {
    public enum FileType {
        Unknown = 0x0,
        Prop = 0x3,
        Language = 0x4,
        Scene = 0x6,
        Skeleton = 0x7,
        StaticMesh = 0xD,
        SkeletalMesh = 0xE
    }

    public abstract class BaseReader : ByteReader {
        public FileType Type;

        public virtual void Read(FileStream stream) {
            _stream = stream;
        }

        /// <summary>
        /// Reads file name blocks
        /// </summary>
        protected void ReadFileNameBlock() {
            // Read the texture count
            uint textureCount = ReadUint32();
            // Read all texture entries
            for (int i = 0; i < textureCount; i++) {
                String textureName = ReadString();
                byte[] unknownBytes = ReadChunk(0x32); // Unknown
            }
        }

        /// <summary>
        /// Shows the current stream position, in hexadecimal format
        /// </summary>
        protected void CurrentPos() {
            string hexPosition = ToHex((uint)_stream.Position);
            Console.WriteLine("0x" + hexPosition);
        }

        /// <summary>
        /// Helper function for reading padding
        /// </summary>
        /// <param name="length">Padding length (in uint32)</param>
        protected void ReadPadding(int length) {
            for (int i = 0; i < length; i++) {
                ReadUint32();
            }
        }

        /// <summary>
        /// Helper function for reading strings
        /// </summary>
        /// <returns>The string, null if invalid</returns>
        protected String ReadString() {
            uint fileNameOffset = ReadUint32(); // Filename length+8
            uint fileNameLength = ReadUint32(); // Filename length

            // If this isn't an appropriate string header, return null
            if (fileNameOffset != fileNameLength + 8) {
                return null;
            }

            // Read the string and return it
            return ReadString(fileNameLength);
        }
    }
}
