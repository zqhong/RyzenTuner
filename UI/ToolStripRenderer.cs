using System.Drawing;
using System.Windows.Forms;

namespace RyzenTuner.UI
{
    public class ToolStripRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item is ToolStripMenuItem { Checked: true } item)
            {
                e.TextFont = new Font(item.Font, FontStyle.Bold);
            }

            base.OnRenderItemText(e);
        }
    }
}