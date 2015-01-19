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
using System.IO;
using System.Windows.Forms;

namespace JPAssetReader {
    class SceneReader : BaseReader, IMeshReader {
        public class SceneObject {
            public string Name;
            public List<FileEntry> Dependencies;
            public Transform Transform;
            public string Group;
            public SceneObjectType Type;
            public List<SceneObject> children = new List<SceneObject>();
        }

        public enum SceneObjectType {
            Group = 0x1,
            Model = 0x2,
            Camera = 0x4
        }

        public string Name;
        public List<SceneObject> Objects;

        public override bool Read(uint subType, FileStream stream) {
            base.Read(subType, stream);

            if (subType != 0x6) {
                MessageBox.Show("Subtype "+ToHex(subType)+" not supported!");
                return false;
            }

            ReadChunk(0x49);

            // Read the scene name
            Name = ReadString();

            uint u1 = ReadUint32();
            ReadChunk(0x4); // Zero
            uint u2 = ReadUint32();

            ReadChunk(0x9);
            float u3 = ReadFloat();
            ReadByte();
            float u4 = ReadFloat();
            byte[] unknownChunk = ReadChunk(0x8); // Zero, padding?
            Vector2 u5 = ReadVector2();
            ReadByte();
            float u6 = ReadFloat();
            ReadChunk(0x8); // Zero, padding?
            Vector2 u7 = ReadVector2();
            ReadChunk(0x5);
            Vector2 u8 = ReadVector2();
            uint u9 = ReadUint32();

            Objects = new List<SceneObject>();

            uint objectCount = ReadUint32();
            uint i = 0;

            List<SceneObject> objectList = new List<SceneObject>();
            for (i=0; i < objectCount; i++) {
                SceneObject sceneObject = ReadObject();

                if (sceneObject.Group != null && sceneObject.Group.Length > 0) {
                    SceneObject parent = objectList.Find(obj => obj.Name.Equals(sceneObject.Group));

                    if (parent == null) {
                        Console.WriteLine("Couldn't find parent object '"+sceneObject.Group+"'!");
                        continue; }

                    parent.children.Add(sceneObject);
                } else {
                    Objects.Add(sceneObject);
                }

                objectList.Add(sceneObject);
            }

            return true;
        }

        private SceneObject ReadObject() {
            string objectName = ReadString();

            SceneObject obj = new SceneObject() { Name = objectName };

            byte[] unknownChunk = ReadChunk(0x10);
            uint u10 = ReadUint32(unknownChunk, 0x0);
            uint u11 = ReadUint32(unknownChunk, 0x4);
            uint u12 = ReadUint32(unknownChunk, 0xC);

            obj.Dependencies = ReadFileNameBlock(0x0, false);

            uint dataSize = ReadUint32();

            byte[] dataChunk = ReadChunk(dataSize - 0x4);
            uint u13 = ReadUint32(dataChunk, 0x0);

            uint u14 = ReadUint32(dataChunk, 0x20);
            uint u15 = ReadUint32(dataChunk, 0x24);

            uint u16 = ReadUint32(dataChunk, 0x2C);
            uint u17 = ReadUint32(dataChunk, 0x30);

            uint u18 = ReadUint32(dataChunk, 0x38);

            uint offset = u18 * 0xC;

            if (u18 != 0x3) {
                offset += (uint)(u18 == 0x5 ? 2 : 1); }
            
            obj.Type = (SceneObjectType)ReadUint32(dataChunk, 0x4B + offset);

            // Read transform data
            Transform transform = new Transform();
            if (obj.Type == SceneObjectType.Group) {
                Vector2 u19 = ReadVector2(dataChunk, 0x4F + offset); // Unknown
                Vector3 u20 = ReadVector3(dataChunk, 0x5B + offset); // Unknown (valid v3)
                Vector3 u21 = ReadVector3(dataChunk, 0x68 + offset); // Unknown
                uint u22 = ReadUint32(dataChunk, 0x73 + offset);
                uint cOffset = 0x77;
                uint currentOffset = (uint)(_stream.Position-dataChunk.Length)+(offset+cOffset);
                uint currentLength = (uint)(dataChunk.Length - (offset + cOffset));
                if (u22 == 0) {
                    Vector3 u23 = ReadVector3(dataChunk,0x87+offset); // Unknown
                    transform.Position = ReadVector3(dataChunk, 0xB7 + offset); // Position?
                    obj.Group = ReadString(dataChunk,0x10B+offset,false);
                } else {
                    obj.Group = ReadString(dataChunk, 0xAF + offset, false);
                    //Console.WriteLine(obj.Name + " " + ToHex(currentOffset) + " " + ToHex(currentLength));
                    //Console.WriteLine(u22 + " " + ToHex(dataChunk, offset + 0x77));
                }
            } else if (obj.Type == SceneObjectType.Model) {
                uint u20 = ReadUint32(dataChunk, 0x4F + offset); // Unknown
                uint u21 = ReadUint32(dataChunk, 0x53 + offset); // Unknown

                transform.Scale = ReadVector3(dataChunk, 0x5B + offset); // Scale ?
                transform.Position = ReadVector3(dataChunk, 0x73 + offset); // Position ?
                transform.Rotation = ReadQuaternion(dataChunk, 0x9B + offset); // Quaternion ?
                obj.Group = ReadString(dataChunk, 0xC7 + offset, false);
            }

            obj.Transform = transform;

            return obj;
        }

        private Mesh StripMesh(string path, string propName, string objectName) {
            Mesh mesh = null;
            JurassicReader r = new JurassicReader();
            if (r.Read(path + "\\" + propName)) {
                List<PropReader.DependencyEntry> dependencies = (r.reader as PropReader).Dependencies;
                foreach (PropReader.DependencyEntry dependency in dependencies) {
                    foreach (string entry in dependency.Files) {
                        // Only load d3dmeshes, for now :)
                        if (Common.GetExtension(entry).Equals("d3dmesh")) {
                            string meshPath = path + "\\" + entry;
                            if (!File.Exists(meshPath)) {
                                Console.WriteLine(entry + " does not exist - skipping");
                                continue;
                            }

                            JurassicReader modelReader = new JurassicReader();
                            if (modelReader.Read(meshPath)) {
                                Mesh propMesh = modelReader.mesh;
                                if (mesh == null) {
                                    mesh = propMesh;
                                }
                                else {
                                    mesh.Combine(propMesh,objectName);
                                }
                            }
                        }
                    }
                }
            }

            return mesh;
        }

        private Mesh ConstructMesh(SceneObject obj, Matrix matrix) {
            Mesh mesh = null;
            matrix.Translate(obj.Transform.Position);
            matrix.Scale(obj.Transform.Scale);
            matrix.Rotate(obj.Transform.Rotation);

            string path = Common.GetPath(_stream.Name);

            for (int i = 0; i < obj.Dependencies.Count; i++) {
                FileEntry file = obj.Dependencies[i];
                string propPath = path + "\\" + file.Name;
                if (Common.GetExtension(file.Name).Equals("prop")) {
                    if (File.Exists(propPath)) {
                        mesh = StripMesh(path, file.Name, obj.Name);
                    } else {
                        Console.WriteLine(file.Name + " does not exist - skipping");
                    }
                }
            }

            // Transform the mesh before adding the children
            if (mesh != null) {
                mesh.Transform(matrix);
            }

            foreach (SceneObject sceneObject in obj.children) {
                Mesh childMesh = ConstructMesh(sceneObject, matrix);

                if (childMesh != null) {
                    if (mesh == null) {
                        mesh = childMesh;
                    }
                    else {
                        mesh.Combine(childMesh,sceneObject.Name);
                    }
                }
            }

            return mesh;
        }

        public Mesh GetMesh() {
            Mesh m = null;
            foreach (SceneObject obj in Objects) {
                Mesh propMesh = ConstructMesh(obj, Matrix.identity);

                if (m == null) {
                    m = propMesh;
                } else {
                    m.Combine(propMesh, obj.Name);
                }
            }

            return m;
        }
    }
}
