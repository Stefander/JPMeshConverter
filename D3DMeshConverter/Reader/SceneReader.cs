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
            public List<SceneObject> children = new List<SceneObject>();
        }

        public string Name;
        public List<SceneObject> Objects;

        public override bool Read(uint subType, FileStream stream) {
            base.Read(subType, stream);

            if (subType == 0x6) {
                ReadChunk(0x49);
            } else if (subType == 0x9) {
                ReadChunk(0x6D);
            } else {
                MessageBox.Show("Subtype "+subType+" not supported!");
                return false;
            }
            
            // Read the scene name
            Name = ReadString();

            uint u1 = ReadUint32();

            if (subType == 0x6) {
                ReadChunk(0x4);
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
            } else if (subType == 0x9) {
                ReadFileNameBlock(0x0, false);
                ReadChunk(0x4C);
            }

            Objects = new List<SceneObject>();

            uint objectCount = ReadUint32();
            uint i = 0;
            Console.WriteLine("Object count: "+objectCount);

            List<SceneObject> objectList = new List<SceneObject>();
            for (i=0; i < objectCount; i++) {
                Console.WriteLine("CHUNK " + i);
                
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

            Console.WriteLine(objectName);

            byte[] unknownChunk = ReadChunk(0x10);
            uint u10 = ReadUint32(unknownChunk, 0x0);
            uint u11 = ReadUint32(unknownChunk, 0x4);
            uint u12 = ReadUint32(unknownChunk, 0xC);

            //Console.WriteLine("Dependencies:");
            obj.Dependencies = ReadFileNameBlock(0x0, false);

            uint dataSize = ReadUint32();
            
            CurrentPos();
            Console.WriteLine("Size: " + ToHex(dataSize));

            byte[] dataChunk = ReadChunk(dataSize - 0x4);
            Transform transform = new Transform();
            ObjectData objectData = new ObjectData(ReadChunk(dataSize-0x4), SubType);
            /*
            if (SubType == 0x9) {
                uint u18 = ReadUint32(dataChunk, 0x0);
                if (u18 != 0x0) {
                    uint u19 = ReadUint32(dataChunk, 0x10);
                    if (u18 == 0x8 && u19 == 0x1) {
                        uint u20 = ReadUint32(dataChunk, 0x1AD);
                        if (u20 == 0x3) {

                        }
                    }
                }
                
                Console.WriteLine("Ohi: "+obj.Name+": "+ToHex(dataSize)+" "+ToHex(u18));
                CurrentPos();
            } else if (SubType == 0x6) {
                uint u13 = ReadUint32(dataChunk, 0x0);

                
            }*/

            obj.Transform = transform;

            return obj;
        }

        private Mesh StripMesh(string path, string propName, string objectName) {
            Mesh mesh = null;
            JurassicReader r = new JurassicReader();
            if (r.Read(path + "\\" + propName)) {
                List<DependencyEntry> dependencies = (r.reader as PropReader).Dependencies;
                foreach (DependencyEntry dependency in dependencies) {
                    foreach (string entry in dependency.Objects) {
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
                
                if (propMesh != null) {
                    if (m == null) {
                        m = propMesh;
                    } else {
                        m.Combine(propMesh, obj.Name);
                    }
                }
            }

            return m;
        }
    }
}
