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
using System.IO;

namespace JPAssetReader {
    class D3DReader : BaseReader, IMeshReader {
        private Mesh mesh;
        private Vector2 UVScale;
        private string Name;

        public override bool Read(FileStream stream) {
            base.Read(stream);

            Name = ReadString();

            ReadChunk(0x6); // Unknown

            Vector3 pos1 = ReadVector3(); // Min AABB?
            Vector3 pos2 = ReadVector3(); // Max AABB?

            ReadChunk(0x18);

            mesh = new Mesh();

            uint chunkCount = ReadUint32();
            for (uint i = 0; i < chunkCount; i++) {
                MeshChunk chunk = ReadMeshChunk(i);
                mesh.Chunks.Add(chunk);
            }

            ReadChunk(0x8);
            if (GetFileType() == 0xE) {
                uint unknownSize = ReadUint32(); // Size of chunk
                ReadChunk(0x8);
                ReadChunk(unknownSize);
            } else {
                // Alternating 0x8 and 0x0
                ReadPadding(5);
            }

            ReadMeta();
            ParseMeshData();

            return true;
        }

        public Mesh GetMesh() {
            return mesh;
        }

        /// <summary>
        /// Parses mesh vertices and triangles
        /// </summary>
        private void ParseMeshData() {
            ReadChunk(0x14);

            uint indicesCount = ReadUint32();

            ReadChunk(0x8);

            // Parse triangle data
            // Triangles are specified by 3 vertex indices
            for (int i = 0; i < indicesCount / 3; i++) {
                uint vi1 = ReadUint16();
                uint vi2 = ReadUint16();
                uint vi3 = ReadUint16();
                Triangle f = new Triangle() { V1 = vi1, V2 = vi2, V3 = vi3 };
                mesh.Triangles.Add(f);
            }

            // Amount of vertices
            uint vertexCount = ReadUint32();

            // Size of every vertex declaration
            uint vertexDataSize = ReadUint32();

            ReadChunk(0xAC);

            // Parse all vertices
            for (uint j = 0; j < vertexCount; j++) {
                Vector3 p = ReadVector3();

                // TODO: Figure out what these parameters are (UV, normal?)
                uint chunkSize = vertexDataSize - 0xC;
                byte[] chunkData = ReadChunk(chunkSize);

                Vector2 uv = Vector2.zero;
                Vector3 normal = Vector3.up;
                Vector3 tangent = Vector3.up;

                if (chunkSize >= 4) {
                    float x = ReadFloat16(chunkData, 0x0) * UVScale.X;
                    float y = -ReadFloat16(chunkData, 0x2) * UVScale.Y;
                    uv = new Vector2(x, y);

                    // Not sure about the normal/tangent format, disabled for now
                    /*if (chunkSize >= 0x14) {
                        uint offset = chunkSize-0x10;
                        float nx = ReadFloat16(chunkData,offset);
                        float ny = ReadFloat16(chunkData,offset+0x2);
                        float nz = ReadFloat16(chunkData,offset+0x4);
                        normal = new Vector3(nx,ny,nz);

                        float tx = ReadFloat16(chunkData, offset+0x6);
                        float ty = ReadFloat16(chunkData, offset+0x8);
                        float tz = ReadFloat16(chunkData, offset+0xA);
                        tangent = new Vector3(tx,-ty,tz);
                    }*/
                }

                Vertex v = new Vertex() { Index = j, Position = p, UV = uv, Normal = normal };
                mesh.Vertices.Add(v);

                // Sort all the textures according to texture
                mesh.SortChunks();
            }
        }

        /// <summary>
        /// Reads the current mesh chunk
        /// </summary>
        /// <param name="index">Chunk index</param>
        /// <returns>Mesh chunk</returns>
        private MeshChunk ReadMeshChunk(uint index) {
            MeshChunk chunk = new MeshChunk();
            chunk.Name = Common.GetFileName(Name);
            chunk.Index = index;

            ReadChunk(0x24);

            chunk.FirstVertex = ReadUint32();
            chunk.LastVertex = ReadUint32();
            chunk.FaceOffset = (ReadUint32() / 3);
            chunk.FaceCount = ReadUint32();

            ReadChunk(0xC);

            Vector3 u1 = ReadVector3();
            Vector3 u2 = ReadVector3();

            ReadChunk(0x34);

            // Read texture names
            for (int j = 0; j < 9; j++) {
                string textureName = ReadString();
                if (j == 0) {
                    chunk.DiffuseTexture = textureName;
                }
            }

            ReadChunk(0x19);
            string reflMap = ReadString();
            ReadChunk(0x85);
            string unknownTexture = ReadString();
            ReadChunk(0x2B);
            string ssTexture = ReadString();
            ReadChunk(0x19);

            return chunk;
        }

        /// <summary>
        /// Reads the mesh meta
        /// </summary>
        private void ReadMeta() {
            ReadChunk(0x4);

            String materialType = ReadString(4); // Unknown

            ReadChunk(0xD);
            
            // Parse the 8 file name slots (0: Diffuse, 4: Normal)
            for (int i = 0; i < 8; i++) {
                byte[] unknown = ReadChunk(4);
                ReadDependencyBlock(0x32);
            }

            byte[] footer = ReadChunk(0x28);
            UVScale = new Vector2(ReadFloat(footer, 0xE), ReadFloat(footer, 0x12));
        }
    }
}
