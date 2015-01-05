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
using System.Text;

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
        public override string ToString() {
            StringBuilder objData = new StringBuilder();
            
            foreach (Vertex v in Vertices) {
                objData.Append("v " + v.Position.X + " " + v.Position.Y + " " + v.Position.Z + "\n");
            }

            foreach (Triangle f in Triangles) {
                objData.Append("f " + (f.V1 + 1) + " " + (f.V2 + 1) + " " + (f.V3 + 1) + "\n");
            }

            return objData.ToString();
        }
    }

    /// <summary>
    /// Mesh chunk class
    /// </summary>
    public class MeshChunk {
        public uint Index;
    }

    /// <summary>
    /// Vertex class
    /// </summary>
    public class Vertex {
        public uint Index;
        public Vector3 Position;
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

        public override string ToString() {
            return "(" + X + "," + Y + "," + Z + ")";
        }
    }
}
