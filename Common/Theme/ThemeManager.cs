using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RyzenTuner.Common.Theme
{
    public enum ThemeMode
    {
        Light,
        Dark,
    }

    /// <summary>
    /// 主题管理器 — 定义浅色/深色双色板，递归应用到窗体的所有控件。
    ///
    /// 工作原理：
    /// 1. 首次切换到深色时，记录所有需变色控件的原始 ForeColor/BackColor。
    /// 2. 切换到浅色时从记录恢复，不依赖 Designer 中的具体颜色值来判断。
    /// </summary>
    public static class ThemeManager
    {
        public static ThemeMode CurrentMode { get; private set; } = ThemeMode.Light;

        /// <summary>
        /// 主题切换事件（供外部监听，如导航图标重绘）
        /// </summary>
        public static event Action<ThemeMode>? ThemeChanged;

        /// <summary>
        /// 存储已记录的控件原始颜色，用于从深色切回浅色时恢复。
        /// </summary>
        private static readonly Dictionary<Control, OriginalColors> _originals = new();

        private struct OriginalColors
        {
            public Color BackColor;
            public Color ForeColor;
            public FlatStyle FlatStyle;
            public bool UseVisualStyleBackColor;
            public int FlatBorderSize;
            public Color FlatBorderColor;
            public Color MouseOverBackColor;
            public bool EnableHeadersVisualStyles;
            public Color? ColumnHeaderBack;
            public Color? ColumnHeaderFore;
            public Color? RowBack;
            public Color? RowFore;
            public Color? AltRowBack;
            public Color? AltRowFore;
            public Color? GridColor;
            public Color? DataGridBg;
            public Color? SelectionBack;
            public Color? SelectionFore;
            public Color? ColumnHeaderSelectionBack;
            public Color? ColumnHeaderSelectionFore;
        }

        // ================================================================
        // 浅色色板（与 Designer 硬编码值一致的默认值）
        // ================================================================

        private static readonly Color L_SidebarBg = Color.FromArgb(234, 234, 234);
        private static readonly Color L_ContentBg = Color.FromArgb(243, 243, 243);
        private static readonly Color L_NavActiveBg = Color.White;
        private static readonly Color L_NavHover = Color.FromArgb(10, 0, 0, 0);
        private static readonly Color L_IconPrimary = Color.FromArgb(80, 80, 80);
        private static readonly Color L_IconSecondary = Color.FromArgb(215, 215, 215);

        // ================================================================
        // 深色色板
        // ================================================================

        private static readonly Color D_SidebarBg = Color.FromArgb(37, 37, 38);       // #252526
        private static readonly Color D_ContentBg = Color.FromArgb(45, 45, 48);       // #2D2D30
        private static readonly Color D_NavActiveBg = Color.FromArgb(62, 62, 66);    // #3E3E42
        private static readonly Color D_NavHover = Color.FromArgb(40, 40, 43);
        private static readonly Color D_IconPrimary = Color.FromArgb(200, 200, 200);
        private static readonly Color D_IconSecondary = Color.FromArgb(60, 60, 60);
        private static readonly Color D_ControlBg = Color.FromArgb(62, 62, 66);      // #3E3E42
        private static readonly Color D_ControlText = Color.FromArgb(224, 224, 224); // #E0E0E0
        private static readonly Color D_GrayText = Color.FromArgb(153, 153, 153);    // #999999
        private static readonly Color D_LinkBlue = Color.FromArgb(78, 170, 255);     // 浅蓝链接
        private static readonly Color D_DataGridHeaderBg = Color.FromArgb(55, 55, 58);
        private static readonly Color D_DataGridRowBg = Color.FromArgb(45, 45, 48);
        private static readonly Color D_DataGridAltRowBg = Color.FromArgb(50, 50, 55);
        private static readonly Color D_DataGridGridColor = Color.FromArgb(80, 80, 85);
        private static readonly Color D_DataGridBg = Color.FromArgb(30, 30, 30);
        private static readonly Color D_SelectionBg = Color.FromArgb(38, 79, 120);
        private static readonly Color D_SelectionFg = D_ControlText;

        // ================================================================
        // 公开属性
        // ================================================================

        public static Color SidebarBg => CurrentMode == ThemeMode.Dark ? D_SidebarBg : L_SidebarBg;
        public static Color ContentBg => CurrentMode == ThemeMode.Dark ? D_ContentBg : L_ContentBg;
        public static Color NavActiveBg => CurrentMode == ThemeMode.Dark ? D_NavActiveBg : L_NavActiveBg;
        public static Color NavHover => CurrentMode == ThemeMode.Dark ? D_NavHover : L_NavHover;
        public static Color IconPrimary => CurrentMode == ThemeMode.Dark ? D_IconPrimary : L_IconPrimary;
        public static Color IconSecondary => CurrentMode == ThemeMode.Dark ? D_IconSecondary : L_IconSecondary;

        // ================================================================
        // 公共方法
        // ================================================================

        /// <summary>
        /// 设置主题模式并立即应用到整个窗体。
        /// </summary>
        public static void SetTheme(ThemeMode mode, Form? form = null)
        {
            if (mode == CurrentMode)
                return;

            CurrentMode = mode;
            ThemeChanged?.Invoke(mode);

            if (form != null)
            {
                ApplyTheme(form);
            }
        }

        /// <summary>
        /// 将当前主题应用到整个窗体。
        /// </summary>
        public static void ApplyTheme(Form form)
        {
            if (form == null || form.IsDisposed)
                return;

            ApplyThemeToControls(form.Controls);
        }

        /// <summary>
        /// 清空已记录的原始颜色缓存（在窗体重建或重启时调用）
        /// </summary>
        public static void ClearOriginals()
        {
            _originals.Clear();
        }

        /// <summary>
        /// 强制重新记录原始颜色（在 ApplyTheme 之后调用，用于刷新缓存）
        /// </summary>
        public static void RebindOriginals(Form form)
        {
            _originals.Clear();
            RecordOriginalColors(form.Controls);
        }

        // ================================================================
        // 获取导航图标颜色的便捷方法
        // ================================================================

        public static (Color Primary, Color Secondary) GetNavIconColors()
        {
            return (IconPrimary, IconSecondary);
        }

        // ================================================================
        // 内部实现
        // ================================================================

        private static void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                RecordIfNeeded(ctrl);
                ApplyControl(ctrl);

                if (ctrl.HasChildren)
                {
                    ApplyThemeToControls(ctrl.Controls);
                }
            }
        }

        /// <summary>
        /// 首次遍历控件时记录原始颜色（用于后续从深色恢复浅色）
        /// </summary>
        private static void RecordIfNeeded(Control ctrl)
        {
            if (_originals.ContainsKey(ctrl))
                return;

            var orig = new OriginalColors
            {
                BackColor = ctrl.BackColor,
                ForeColor = ctrl.ForeColor,
            };

            if (ctrl is Button btn)
            {
                orig.FlatStyle = btn.FlatStyle;
                orig.UseVisualStyleBackColor = btn.UseVisualStyleBackColor;
                orig.FlatBorderSize = btn.FlatAppearance.BorderSize;
                orig.FlatBorderColor = btn.FlatAppearance.BorderColor;
                orig.MouseOverBackColor = btn.FlatAppearance.MouseOverBackColor;
            }

            if (ctrl is DataGridView dgv)
            {
                orig.EnableHeadersVisualStyles = dgv.EnableHeadersVisualStyles;
                orig.GridColor = dgv.GridColor;
                orig.DataGridBg = dgv.BackgroundColor;

                if (dgv.ColumnHeadersDefaultCellStyle != null)
                {
                    orig.ColumnHeaderBack = dgv.ColumnHeadersDefaultCellStyle.BackColor;
                    orig.ColumnHeaderFore = dgv.ColumnHeadersDefaultCellStyle.ForeColor;
                    orig.ColumnHeaderSelectionBack = dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor;
                    orig.ColumnHeaderSelectionFore = dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor;
                }

                if (dgv.DefaultCellStyle != null)
                {
                    orig.RowBack = dgv.DefaultCellStyle.BackColor;
                    orig.RowFore = dgv.DefaultCellStyle.ForeColor;
                    orig.SelectionBack = dgv.DefaultCellStyle.SelectionBackColor;
                    orig.SelectionFore = dgv.DefaultCellStyle.SelectionForeColor;
                }

                if (dgv.RowsDefaultCellStyle != null)
                {
                    orig.AltRowBack ??= dgv.RowsDefaultCellStyle.BackColor;
                    orig.AltRowFore ??= dgv.RowsDefaultCellStyle.ForeColor;
                }

                if (dgv.AlternatingRowsDefaultCellStyle != null)
                {
                    orig.AltRowBack = dgv.AlternatingRowsDefaultCellStyle.BackColor;
                    orig.AltRowFore = dgv.AlternatingRowsDefaultCellStyle.ForeColor;
                }
            }

            _originals[ctrl] = orig;
        }

        private static void ApplyControl(Control ctrl)
        {
            if (CurrentMode == ThemeMode.Light)
            {
                RestoreControl(ctrl);
            }
            else
            {
                DarkenControl(ctrl);
            }
        }

        /// <summary>
        /// 从深色恢复为浅色
        /// </summary>
        private static void RestoreControl(Control ctrl)
        {
            if (!_originals.TryGetValue(ctrl, out var orig))
                return;

            ctrl.BackColor = orig.BackColor;
            ctrl.ForeColor = orig.ForeColor;

            switch (ctrl)
            {
                case Button btn:
                    btn.FlatStyle = orig.FlatStyle;
                    btn.UseVisualStyleBackColor = orig.UseVisualStyleBackColor;
                    btn.FlatAppearance.BorderSize = orig.FlatBorderSize;
                    btn.FlatAppearance.BorderColor = orig.FlatBorderColor;
                    btn.FlatAppearance.MouseOverBackColor = orig.MouseOverBackColor;
                    break;

                case DataGridView dgv:
                    dgv.EnableHeadersVisualStyles = orig.EnableHeadersVisualStyles;
                    dgv.BackgroundColor = orig.DataGridBg ?? SystemColors.Control;
                    dgv.GridColor = orig.GridColor ?? SystemColors.ControlLight;

                    if (dgv.ColumnHeadersDefaultCellStyle != null)
                    {
                        dgv.ColumnHeadersDefaultCellStyle.BackColor = orig.ColumnHeaderBack ?? SystemColors.Control;
                        dgv.ColumnHeadersDefaultCellStyle.ForeColor = orig.ColumnHeaderFore ?? SystemColors.ControlText;
                        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = orig.ColumnHeaderSelectionBack ?? SystemColors.Highlight;
                        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = orig.ColumnHeaderSelectionFore ?? SystemColors.HighlightText;
                    }

                    if (dgv.DefaultCellStyle != null)
                    {
                        dgv.DefaultCellStyle.BackColor = orig.RowBack ?? SystemColors.Window;
                        dgv.DefaultCellStyle.ForeColor = orig.RowFore ?? SystemColors.WindowText;
                        dgv.DefaultCellStyle.SelectionBackColor = orig.SelectionBack ?? SystemColors.Highlight;
                        dgv.DefaultCellStyle.SelectionForeColor = orig.SelectionFore ?? SystemColors.HighlightText;
                    }

                    SetIfNotNull(dgv.RowsDefaultCellStyle, orig.AltRowBack, orig.AltRowFore);
                    SetIfNotNull(dgv.AlternatingRowsDefaultCellStyle, orig.AltRowBack, orig.AltRowFore);
                    break;
            }
        }

        /// <summary>
        /// 应用深色
        /// </summary>
        private static void DarkenControl(Control ctrl)
        {
            switch (ctrl)
            {
                case Panel:
                    // Panel 继承父级背景，不需要单独设置
                    break;

                case GroupBox gb:
                    gb.ForeColor = D_ControlText;
                    // GroupBox 继承 Background, 不需额外设置
                    break;

                case Label lbl:
                    DarkenLabel(lbl);
                    break;

                case CheckBox cb:
                    DarkenCheckBox(cb);
                    break;

                case RadioButton rb:
                    DarkenRadioButton(rb);
                    break;

                case Button btn:
                    DarkenButton(btn);
                    break;

                case ComboBox cmb:
                    cmb.BackColor = D_ControlBg;
                    cmb.ForeColor = D_ControlText;
                    break;

                case TextBox txt:
                    txt.BackColor = D_ControlBg;
                    txt.ForeColor = D_ControlText;
                    break;

                case NumericUpDown nud:
                    nud.BackColor = D_ControlBg;
                    nud.ForeColor = D_ControlText;
                    break;

                case DataGridView dgv:
                    DarkenDataGridView(dgv);
                    break;

                case ProgressBar pb:
                    pb.BackColor = D_ControlBg;
                    pb.ForeColor = Color.FromArgb(0, 122, 204);
                    break;
            }
        }

        private static void DarkenLabel(Label lbl)
        {
            var orig = _originals[lbl];

            // 判断原始 ForeColor 来确定该标签的角色
            var fg = orig.ForeColor;

            // LinkLabel 特殊处理
            if (lbl is LinkLabel ll)
            {
                ll.LinkColor = D_LinkBlue;
                ll.ActiveLinkColor = D_LinkBlue;
                ll.VisitedLinkColor = D_LinkBlue;
                return;
            }

            if (fg == Color.Gray || fg == SystemColors.GrayText)
            {
                // 灰色副标题标签（如 "CPU 频率"、"Version --" 等）
                lbl.ForeColor = D_GrayText;
            }
            else if (fg.ToArgb() == Color.FromArgb(0, 95, 184).ToArgb())
            {
                // 蓝色链接标签（关于页，labelAboutLink）
                lbl.ForeColor = D_LinkBlue;
            }
            else
            {
                // 其他普通标签
                lbl.ForeColor = D_ControlText;
            }
        }

        private static void DarkenCheckBox(CheckBox cb)
        {
            cb.ForeColor = D_ControlText;
        }

        private static void DarkenRadioButton(RadioButton rb)
        {
            rb.ForeColor = D_ControlText;
        }

        private static void DarkenButton(Button btn)
        {
            // 导航按钮 nav* 有特殊的 FlatStyle.Flat 和透明背景
            // 这里只处理非导航的常规按钮（保存、取消、开始等）
            var name = btn.Name;

            // 导航按钮 — 跳过，由 MainForm.SwitchPage() 管理
            if (name is "navHome" or "navSettings" or "navBenchmark" or "navAbout" or "navLogs")
                return;

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(102, 102, 102);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(54, 54, 58);
            btn.BackColor = D_ControlBg;
            btn.ForeColor = D_ControlText;
            btn.UseVisualStyleBackColor = false;
        }

        private static void DarkenDataGridView(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.BackgroundColor = D_DataGridBg;
            dgv.GridColor = D_DataGridGridColor;

            if (dgv.ColumnHeadersDefaultCellStyle != null)
            {
                dgv.ColumnHeadersDefaultCellStyle.BackColor = D_DataGridHeaderBg;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = D_ControlText;
                dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = D_DataGridHeaderBg;
                dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = D_ControlText;
            }

            if (dgv.DefaultCellStyle != null)
            {
                dgv.DefaultCellStyle.BackColor = D_DataGridRowBg;
                dgv.DefaultCellStyle.ForeColor = D_ControlText;
                dgv.DefaultCellStyle.SelectionBackColor = D_SelectionBg;
                dgv.DefaultCellStyle.SelectionForeColor = D_SelectionFg;
            }

            SetIfNotNull(dgv.RowsDefaultCellStyle, D_DataGridRowBg, D_ControlText);
            SetIfNotNull(dgv.AlternatingRowsDefaultCellStyle, D_DataGridAltRowBg, D_ControlText);

            // 设置选中行样式
            var rowStyle = dgv.RowsDefaultCellStyle;
            if (rowStyle != null)
            {
                rowStyle.SelectionBackColor = D_SelectionBg;
                rowStyle.SelectionForeColor = D_SelectionFg;
            }

            var altRowStyle = dgv.AlternatingRowsDefaultCellStyle;
            if (altRowStyle != null)
            {
                altRowStyle.SelectionBackColor = D_SelectionBg;
                altRowStyle.SelectionForeColor = D_SelectionFg;
            }
        }

        private static void SetIfNotNull(DataGridViewCellStyle? style, Color? backColor, Color? foreColor)
        {
            if (style == null) return;
            if (backColor.HasValue) style.BackColor = backColor.Value;
            if (foreColor.HasValue) style.ForeColor = foreColor.Value;
        }

        /// <summary>
        /// 记录所有已有控件的原始颜色（初始化时调用）
        /// </summary>
        private static void RecordOriginalColors(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                if (!_originals.ContainsKey(ctrl))
                {
                    _originals[ctrl] = new OriginalColors
                    {
                        BackColor = ctrl.BackColor,
                        ForeColor = ctrl.ForeColor,
                    };
                }

                if (ctrl.HasChildren)
                {
                    RecordOriginalColors(ctrl.Controls);
                }
            }
        }
    }
}
