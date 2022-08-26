using System;
using System.Windows.Forms;
using RyzenTuner.Common.Container;

namespace RyzenTuner.UI
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();

            richTextBox1.Text = richTextBox1.Text.Replace("{version}",
                $"V{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
            richTextBox1.Text = richTextBox1.Text.Replace("{copyright_year}", DateTime.Now.Year.ToString());

            richTextBox1.ReadOnly = true;
            richTextBox1.Enabled = false;
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            // Form 标题
            Text = Properties.Strings.TextAbout;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
        }
    }
}