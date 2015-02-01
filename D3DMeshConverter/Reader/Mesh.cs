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

using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace JPAssetReader {
    public class Mesh {
        public List<Vertex> Vertices;
        public List<Triangle> Triangles;
        public List<MeshChunk> Chunks;

        public Mesh() {
            Vertices = new List<Vertex>();
            Triangles = new List<Triangle>();
            Chunks = new List<MeshChunk>();
        }

        public void Transform(Matrix m) {
            foreach (Vertex v in Vertices) {
                v.Position = m * v.Position;
            }
        }

        public void Combine(Mesh other) {
            uint vertexOffset = (uint)Vertices.Count;
            uint triangleOffset = (uint)Triangles.Count;
            uint chunkOffset = (uint)Chunks.Count;

            foreach (Vertex v in other.Vertices) {
                Vertices.Add(v);
            }

            foreach (Triangle t in other.Triangles) {
                Triangle triangle = new Triangle() { V1 = t.V1 + vertexOffset, V2 = t.V2 + vertexOffset, V3 = t.V3 + vertexOffset };
                Triangles.Add(triangle);
            }

            foreach (MeshChunk c in other.Chunks) {
                MeshChunk chunk = new MeshChunk();
                chunk.Name = c.Name;
                chunk.DiffuseTexture = c.DiffuseTexture;
                chunk.FaceCount = c.FaceCount;
                chunk.FaceOffset = c.FaceOffset + triangleOffset;
                chunk.FirstVertex = c.FirstVertex + triangleOffset;
                chunk.LastVertex = c.LastVertex + triangleOffset;
                chunk.Index = c.Index + chunkOffset;
                Chunks.Add(chunk);
            }
        }

        /// <summary>
        /// This method will output a Wavefront OBJ file
        /// </summary>
        /// <returns>OBJ file data</returns>
        public string GetObjData(string mtlLibraryName) {
            StringBuilder objData = new StringBuilder();

            objData.Append("mtllib " + mtlLibraryName + "\n");

            foreach (Vertex v in Vertices) {
                objData.Append("v " + v.Position.X + " " + v.Position.Y + " " + v.Position.Z + "\n");
                objData.Append("vt " + v.UV.X + " " + v.UV.Y + "\n");
                //objData.Append("vn " + v.Normal.X + " " + v.Normal.Y + " " + v.Normal.Z + "\n");
            }

            string diffuseTexture = "";
            string lastChunk = "";
            int chunkIndex = 0;
            foreach (MeshChunk chunk in Chunks) {
                if (chunk.DiffuseTexture != diffuseTexture || lastChunk != chunk.Name) {
                    if (lastChunk != chunk.Name) {
                        chunkIndex = 0; }

                    objData.Append("g " + chunk.Index + "_" + chunk.Name + (chunkIndex > 0 ? chunkIndex.ToString() : "") + "\n");
                    objData.Append("usemtl m_" + Common.GetFileName(chunk.DiffuseTexture) + "\n");
                    diffuseTexture = chunk.DiffuseTexture;
                    lastChunk = chunk.Name;
                    chunkIndex++;
                }

                for (uint i = chunk.FaceOffset; i < chunk.FaceOffset + chunk.FaceCount; i++) {
                    Triangle t = Triangles[(int)i];
                    uint v1 = t.V1 + 1;
                    uint v2 = t.V2 + 1;
                    uint v3 = t.V3 + 1;
                    //objData.Append("f " + v1 + "/" + v1 + "/" + v1 + " " + v2 + "/" + v2 + "/" + v2 + " " + v3 + "/" + v3 + "/" + v3 + "\n");
                    objData.Append("f " + v1 + "/" + v1 + " " + v2 + "/" + v2 + " " + v3 + "/" + v3 + "\n");
                }
            }

            return objData.ToString();
        }

        public void SortChunks() {
            // Sort on diffuse texture
            Chunks.Sort(delegate(MeshChunk p1, MeshChunk p2) { return p1.DiffuseTexture.CompareTo(p2.DiffuseTexture); });
        }

        public List<string> GetTextures() {
            List<string> textureList = new List<string>();
            foreach (MeshChunk chunk in Chunks) {
                string diffuseName = Common.GetFileName(chunk.DiffuseTexture);
                if (textureList.IndexOf(diffuseName) == -1) {
                    textureList.Add(diffuseName); }
            }

            return textureList;
        }

        public string GetMtlData() {
            StringBuilder mtlData = new StringBuilder();
            List<string> textureList = GetTextures();

            foreach (string texture in textureList) {
                mtlData.Append("newmtl m_" + texture + "\n");

                // If the color is in the filename, try to parse it
                string sPattern = "^color_[A-Fa-f0-9]{3,6}$";
                Color diffuseColor = Color.White;
                if (Regex.IsMatch(texture, sPattern)) {
                    ColorConverter converter = new ColorConverter();
                    diffuseColor = (Color)converter.ConvertFromString("#" + texture.Substring(texture.IndexOf("_") + 1));
                }

                mtlData.Append("Kd " + (diffuseColor.R / 255.0f) + " " + (diffuseColor.G / 255.0f) + " " + (diffuseColor.B / 255.0f) + "\n");
                mtlData.Append("Ka 1.000 1.000 1.000\nKs 0.000 0.000 0.000\nmap_Kd " + texture + ".dds\n");
            }

            return mtlData.ToString();
        }
    }

    /// <summary>
    /// Mesh chunk class
    /// </summary>
    public class MeshChunk {
        public string Name;
        public uint Index;
        public uint FirstVertex;
        public uint LastVertex;
        public uint FaceOffset;
        public uint FaceCount;
        public string DiffuseTexture;
    }

    /// <summary>
    /// Vertex class
    /// </summary>
    public class Vertex {
        public uint Index;
        public Vector3 Position;
        public Vector2 UV;
        public Vector3 Normal;
    }

    /// <summary>
    /// Triangle class
    /// </summary>
    public class Triangle {
        public uint V1;
        public uint V2;
        public uint V3;
    }

    /// <summary>
    /// Transform class
    /// </summary>
    public class Transform {
        public Vector3 Position = Vector3.zero;
        public Quaternion Rotation = Quaternion.identity;
        public Vector3 Scale = Vector3.one;
    }
}
