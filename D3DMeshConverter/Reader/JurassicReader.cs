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

namespace JPAssetReader {
    public class JurassicReader : BaseReader {
        public BaseReader reader { get; private set; }
        public Mesh mesh { get { if (reader is IMeshReader) { return (reader as IMeshReader).GetMesh(); } return null; } }

        /// <summary>
        /// Reads the mesh data from a file
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <returns>Mesh data</returns>
        public bool Read(string fileName) {
            // Open a new filestream
            Open(fileName);
            
            // Decide which reader to use, or show an error when failed
            bool success = ReadHeader();

            //Console.WriteLine(Common.GetFile(_stream.Name)+": "+ToHex(SubType));
            if (success && SetReader()) {
                success = reader.Read(SubType, _stream);
            }

            // Close the stream
            Close();

            return success;
        }
        
        /// <summary>
        /// This method decides which reader is going to be used
        /// </summary>
        /// <param name="type">File type</param>
        /// <returns>The appropriate reader</returns>
        private bool SetReader() {
            string extension = Common.GetExtension(_stream.Name);

            if (extension.Equals("d3dmesh")) {
                reader = new D3DReader();
            } else if (extension.Equals("lang")) {
                reader = new LanguageReader();
            } else if (extension.Equals("prop")) {
                reader = new PropReader();
            } else if (extension.Equals("scene")) {
                reader = new SceneReader();
            } else {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads the model header
        /// </summary>
        /// <returns>True when successful, false when failed</returns>
        private bool ReadHeader() {
            // 0x0: Read the model identifier
            String identifier = ReadString(4);

            // Make sure it's a valid Telltale file
            if (!identifier.Equals("ERTM")) {
                SubType = 0;
                return false;
            }

            // 0x4: Model type
            SubType = ReadUint32();
            return true;
        }
    }
}