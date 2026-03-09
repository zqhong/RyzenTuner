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
                using var boldFont = new Font(item.Font, FontStyle.Bold);
                e.TextFont = boldFont;
                base.OnRenderItemText(e);
                return;
            }

            base.OnRenderItemText(e);
        }
    }
}
