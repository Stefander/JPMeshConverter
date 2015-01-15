using System;
using System.IO;
using System.Windows.Forms;

namespace JPMeshConverter {
    public partial class Form1 : Form {
        private ModelFileDialog fileDialog;

        public Form1() {
            InitializeComponent();
            fileDialog = new ModelFileDialog(System.AppDomain.CurrentDomain.BaseDirectory);
        }

        private void OnConvert(object sender, EventArgs e) {
            // Show the user a file dialog
            if (!fileDialog.Open()) {
                return;
            }

            // Parse the model data
            JurassicReader reader = new JurassicReader(fileDialog.FileName);

            // If the model couldn't be parsed correctly, error and return
            Mesh mesh = reader.MeshData;
            if (reader.MeshData == null) {
                MessageBox.Show("ERROR: Model could not be parsed.","Export failed!");
                return;
            }

            // Output the mesh data to a Wavefront OBJ
            String meshOutputName = fileDialog.FileName.Replace(".d3dmesh", ".obj");
            String mtlOutputName = meshOutputName.Replace(".obj",".mtl");
            String mtlName = mtlOutputName.Substring(mtlOutputName.LastIndexOf("\\")+1);
            File.WriteAllText(meshOutputName, mesh.GetObjData(mtlName));
            File.WriteAllText(mtlOutputName,mesh.GetMtlData());

            // Show the user it worked
            MessageBox.Show(meshOutputName+"\n\nProcessed "+mesh.Chunks.Count+" chunks.\n"+mesh.Vertices.Count + " vertices exported.", "Export successful!");
        }
    }
}
