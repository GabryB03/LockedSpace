using MetroSuite;
using System.Windows.Forms;

public partial class MainForm : MetroForm
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void guna2Button1_Click(object sender, System.EventArgs e)
    {
        LockedSpaceManager.CreateLockedSpace(guna2TextBox1.Text, guna2TextBox2.Text);
    }

    private void guna2Button2_Click(object sender, System.EventArgs e)
    {
        openFileDialog1.FileName = "";

        if (openFileDialog1.ShowDialog().Equals(DialogResult.OK))
        {
            LockedSpaceManager.DecryptLockedSpace(openFileDialog1.FileName, guna2TextBox2.Text);
        }
    }
}