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
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace JPMeshConverter {
    public class Mesh {
        public List<Vertex> Vertices;
        public List<Triangle> Triangles;
        public List<MeshChunk> Chunks;

        public Mesh() {
            Vertices = new List<Vertex>();
            Triangles = new List<Triangle>();
            Chunks = new List<MeshChunk>();
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

            foreach (MeshChunk chunk in Chunks) {
                objData.Append("g chunk"+chunk.Index+"\n");
                objData.Append("usemtl "+chunk.DiffuseTexture.Replace(".d3dtx","")+"\n");
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

        public string GetMtlData() {
            StringBuilder mtlData = new StringBuilder();
            List<string> textureList = new List<string>();
            foreach (MeshChunk chunk in Chunks) {
                string diffuseName = chunk.DiffuseTexture.Replace(".d3dtx","");
                if (textureList.IndexOf(diffuseName) == -1) {
                    textureList.Add(diffuseName);
                }
            }

            foreach (string texture in textureList) {
                mtlData.Append("newmtl "+texture+"\n");
                
                // If the color is in the filename, try to parse it
                string sPattern = "^color_[A-Fa-f0-9]{3,6}$";
                Color diffuseColor = Color.White;
                if (Regex.IsMatch(texture, sPattern)) {
                    ColorConverter converter = new ColorConverter();
                    diffuseColor = (Color)converter.ConvertFromString("#"+texture.Substring(texture.IndexOf("_") + 1));
                }

                mtlData.Append("Kd " + (diffuseColor.R / 255.0f) + " " + (diffuseColor.G / 255.0f) + " " + (diffuseColor.B / 255.0f) + "\n");
                mtlData.Append("Ka 1.000 1.000 1.000\nKs 0.000 0.000 0.000\nmap_d " + texture + "\n");
            }

            return mtlData.ToString();
        }
    }

    /// <summary>
    /// Mesh chunk class
    /// </summary>
    public class MeshChunk {
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
    /// Vector2 class
    /// </summary>
    public class Vector2 {
        public float X;
        public float Y;
        
        public Vector2(float x, float y) {
            X = x;
            Y = y;
        }

        public static Vector2 zero {
            get {
                return new Vector2(0,0);
            }
        }

        public override string ToString() {
            return "(" + X + "," + Y + ")";
        }
    }

    /// <summary>
    /// Vector3 class
    /// </summary>
    public class Vector3 {
        public float X = 0;
        public float Y = 0;
        public float Z = 0;

        public Vector3(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 up {
            get {
                return new Vector3(0,0.5f,0);
            }
        }

        public override string ToString() {
            return "(" + X + "," + Y + "," + Z + ")";
        }
    }
}
