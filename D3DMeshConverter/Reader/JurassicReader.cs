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
using System.Windows.Forms;

namespace JPMeshConverter {
    public class JurassicReader : BaseReader {
        private BaseReader reader = null;
        public Mesh mesh { get { if (Type == FileType.SkeletalMesh || Type == FileType.StaticMesh) { return (reader as D3DReader).MeshData; } return null; } }

        /// <summary>
        /// Reads the mesh data from a file
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <returns>Mesh data</returns>
        public JurassicReader(string fileName) {
            // Open a new filestream
            Open(fileName);

            // Decide which reader to use, or show an error when failed
            bool success = ReadHeader(out Type);
            
            if (success && (reader = PickReader(Type)) != null) {
                reader.Read(_stream);
            }

            // Close the stream
            Close();
        }

        /// <summary>
        /// This method decides which reader is going to be used
        /// </summary>
        /// <param name="type">File type</param>
        /// <returns>The appropriate reader</returns>
        private BaseReader PickReader(FileType type) {
            BaseReader outReader = null;

            if (type == FileType.StaticMesh || type == FileType.SkeletalMesh) {
                outReader = new D3DReader(type);
            } else if (type == FileType.Language) {
                outReader = new LanguageReader();
            }

            return outReader;
        }

        /// <summary>
        /// Reads the model header
        /// </summary>
        /// <returns>True when successful, false when failed</returns>
        private bool ReadHeader(out FileType type) {
            // 0x0: Read the model identifier
            String identifier = ReadString(4);

            // Make sure it's a valid Telltale file
            if (!identifier.Equals("ERTM")) {
                type = FileType.Unknown;
                return false;
            }

            // 0x4: Model type
            type = (FileType)ReadUint32();
            return true;
        }
    }
}