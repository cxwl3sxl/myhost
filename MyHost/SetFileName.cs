using System;
using System.Windows.Forms;

namespace MyHost
{
    public partial class SetFileName : Form
    {
        public SetFileName()
        {
            InitializeComponent();
        }

        public static string ShowInput(string @default, Form patent)
        {
            var dialog = new SetFileName {textBox1 = {Text = @default}};
            dialog.ShowDialog(patent);
            return dialog.textBox1.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
