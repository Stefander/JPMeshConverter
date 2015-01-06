/*
 * Copyright (c) 2015 Stefan Wijnker
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE.
*/

using System;

namespace JPMeshConverter {
    public class D3DReader : ByteReader {
        private enum ModelType {
            StaticMesh=0xD,
            SkeletalMesh=0xE
        }

        private ModelType modelType;
        public Mesh MeshData { get; private set; }

        /// <summary>
        /// Reads the mesh data from a file
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <returns>Mesh data</returns>
        public D3DReader(string fileName) {
            Open(fileName);

            if (!ReadHeader()) {
                MeshData = null;
            }

            MeshData = new Mesh();

            uint chunkCount = ReadUint32();
            for (uint i = 0; i < chunkCount; i++) {
                MeshChunk chunk = ReadMeshChunk(i);
                MeshData.Chunks.Add(chunk);
            }

            uint unknown = ReadUint32(); // Unknown
            uint unknown1 = ReadUint32(); // Unknown

            if (modelType == ModelType.SkeletalMesh) {
                uint chunkSize = ReadUint32(); // Size of chunk
                uint count2 = ReadUint32(); // Unknown
                uint count3 = ReadUint32(); // Unknown
                ReadChunk(chunkSize);
            } else {
                // Alternating 0x8 and 0x0
                ReadPadding(5);
            }

            ReadMaterial();
            ParseMeshData();

            // Close the stream
            Close();
        }

        /// <summary>
        /// Reads the model header
        /// </summary>
        /// <returns>True when successful, false when failed</returns>
        private bool ReadHeader() {
            // 0x0: Read the model identifier
            String identifier = ReadString(4);

            // Make sure it's a valid model file
            if (!identifier.Equals("ERTM")) {
                return false;
            }

            // 0x4: Model type
            modelType = (ModelType)ReadUint32();
            Console.WriteLine("Model type: " + modelType);

            // 0x8: Unknown, constant
            ReadChunk(0x60);

            // 0x68: Unknown, constant, but varies with type
            int unknownSize = modelType == ModelType.StaticMesh ? 0x3C : 0x48;
            ReadChunk((uint)unknownSize); // Unknown

            // 0xB8: Model name
            String meshName = ReadString();
            Console.WriteLine("Model: " + meshName);

            ReadChunk(0x6); // Unknown

            Vector3 pos1 = ReadVector3(); // Min AABB?
            Vector3 pos2 = ReadVector3(); // Max AABB?

            uint unknown = ReadUint32(); // 20?

            String preStr = ToHex(ReadChunk(0xC));
            String str = ToHex(ReadChunk(0x8));

            return true;
        }

        /// <summary>
        /// Parses mesh vertices and triangles
        /// </summary>
        private void ParseMeshData() {
            ReadChunk(0x2);
            ReadPadding(3);
            ReadUint32(); // Constant: 0x653030
            ReadChunk(0x2);

            uint u1 = ReadUint32(); // Unknown
            uint u2 = ReadUint32(); // Unknown
            uint u3 = ReadUint32(); // Unknown

            Console.WriteLine("Face data size: "+(u1/3)+" ("+u1+")");
            // Parse triangle data
            // Triangles are specified by 3 unsigned 16 bit integers
            for (int i = 0; i < u1 / 3; i++) {
                uint v1 = ReadUint16();
                uint v2 = ReadUint16();
                uint v3 = ReadUint16();
                Triangle f = new Triangle() { V1 = v1, V2 = v2, V3 = v3 };
                MeshData.Triangles.Add(f);
            }

            // Amount of vertices
            uint vertexCount = ReadUint32();

            // Size of every vertex declaration
            uint vertexDataSize = ReadUint32();

            ReadChunk(0xAC); // Unknown
            Console.WriteLine("Vertex count: "+vertexCount+" data size: "+(vertexCount*vertexDataSize));

            // Parse all vertices
            for (uint j = 0; j < vertexCount; j++) {
                Vector3 p = ReadVector3();

                // TODO: Figure out what these parameters are (UV, normal?)
                uint chunkSize = vertexDataSize - 0xC;
                byte[] chunkData = ReadChunk(chunkSize);

                Vector2 uv = Vector2.zero;
                Vector3 normal = Vector3.up;

                if (chunkSize >= 4) {
                    uv = new Vector2(ReadFloat16(chunkData, 0x0), 1-ReadFloat16(chunkData,0x2));
                }

                Vertex v = new Vertex() { Index = j, Position = p, UV = uv, Normal = normal };
                MeshData.Vertices.Add(v);
            }
        }

        private float ReadFloat16(byte[] data, uint offset) {
            return (ReadUint16(data, offset) / (float)UInt16.MaxValue)*2;
        }

        /// <summary>
        /// Reads the current mesh chunk
        /// </summary>
        /// <param name="index">Chunk index</param>
        /// <returns>Mesh chunk</returns>
        private MeshChunk ReadMeshChunk(uint index) {
            MeshChunk chunk = new MeshChunk();
            chunk.Index = index;

            ReadChunk(0x24);

            chunk.FirstVertex = ReadUint32();
            chunk.LastVertex = ReadUint32();
            chunk.FaceOffset = (ReadUint32()/3);
            chunk.FaceCount = ReadUint32();

            uint unknown1 = ReadUint32(); // Unknown
            uint unknown2 = ReadUint32(); // Unknown
            uint unknown3 = ReadUint32(); // Unknown

            Vector3 unknownpos1 = ReadVector3();
            Vector3 unknownpos2 = ReadVector3();

            uint const1 = ReadUint32(); // Constant: 0x14

            uint unknown4 = ReadUint32();
            uint unknown5 = ReadUint32();
            uint unknown6 = ReadUint32();
            uint unknown7 = ReadUint32();

            uint materialIndex = ReadUint32();

            uint unknown9 = ReadUint32();
            uint unknown10 = ReadUint32(); // MAX_VALUE
            uint unknown11 = ReadUint32(); // MAX_VALUE

            uint unknown12 = ReadUint32();

            uint unknown13 = ReadUint32(); // MAX_VALUE
            uint unknown14 = ReadUint32(); // MAX_VALUE
            uint unknown15 = ReadUint32(); // MAX_VALUE

            // Read texture names
            for (int j = 0; j < 9; j++) {
                string textureName = ReadString();
                if (j == 0) {
                    chunk.DiffuseTexture = textureName;
                }
            }

            ReadChunk(0x19); // Unknown
            ReadString();
            ReadChunk(0xD9); // Unknown

            return chunk;
        }

        /// <summary>
        /// Reads the material chunk
        /// </summary>
        private void ReadMaterial() {
            ReadUint32(); // Zero

            String materialType = ReadString(4); // Unknown
            
            ReadChunk(0x1);
            uint mat01 = ReadUint32(); // Unknown
            uint mat02 = ReadUint32(); // Unknown
            uint mat03 = ReadUint32(); // Unknown

            // Parse the 8 file name slots
            for (int i = 0; i < 8; i++) {
                uint unknown = ReadUint32();
                ReadFileNameBlock();
            }

            uint u1 = ReadUint32(); // Unknown
            uint u2 = ReadUint32(); // Unknown
            uint u3 = ReadUint32(); // Unknown
            uint u4 = ReadUint32(); // Unknown
            uint u5 = ReadUint32(); // Unknown

            ReadPadding(5); // Constant 0x3F80, end with 0x83F80
        }

        /// <summary>
        /// Reads file name blocks
        /// </summary>
        private void ReadFileNameBlock() {
            // Read the texture count
            uint textureCount = ReadUint32();

            // Read all texture entries
            for (int i = 0; i < textureCount; i++) {
                String textureName = ReadString();
                ReadChunk(0x32); // Unknown
            }
        }

        /// <summary>
        /// Shows the current stream position, in hexadecimal format
        /// </summary>
        private void CurrentPos() {
            string hexPosition = ToHex((uint)_stream.Position);
            Console.WriteLine("0x"+hexPosition);
        }

        /// <summary>
        /// Helper function for reading padding
        /// </summary>
        /// <param name="length">Padding length (in uint32)</param>
        private void ReadPadding(int length) {
            for (int i = 0; i < length; i++) {
                ReadUint32();
            }
        }

        /// <summary>
        /// Helper function for reading strings
        /// </summary>
        /// <returns>The string, null if invalid</returns>
        private String ReadString() {
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