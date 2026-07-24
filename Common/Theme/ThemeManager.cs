using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
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
        private static volatile ThemeMode _currentMode = ThemeMode.Light;
        public static ThemeMode CurrentMode
        {
            get => _currentMode;
            private set => _currentMode = value;
        }

        /// <summary>
        /// 主题切换事件（供外部监听，如导航图标重绘）
        /// </summary>
        public static event Action<ThemeMode>? ThemeChanged;

        /// <summary>
        /// 存储已记录的控件原始颜色，用于从深色切回浅色时恢复。
        /// 使用 ConditionalWeakTable 避免强引用导致动态创建的控件无法被 GC 回收。
        /// </summary>
        private static ConditionalWeakTable<Control, OriginalColors> _originals = new();

        /// <summary>
        /// 线程同步锁（用于保护 _originals 和 CurrentMode）
        /// </summary>
        private static readonly object _lock = new();

        private sealed class OriginalColors
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
            // RowsDefaultCellStyle（与 AltRowBack/AltRowFore 分离存储）
            public Color? RowDefaultBack;
            public Color? RowDefaultFore;
            public Color? RowDefaultSelectionBack;
            public Color? RowDefaultSelectionFore;
            public Color? AltRowSelectionBack;
            public Color? AltRowSelectionFore;
            // LinkLabel
            public Color LinkColor;
            public Color ActiveLinkColor;
            public Color VisitedLinkColor;
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
        private static readonly Color D_HighlightRowBg = Color.FromArgb(30, 90, 40);

        // 链接标签浅色色板（用于 DarkenLabel 中的原始颜色判断）
        private static readonly Color L_LinkBlue = Color.FromArgb(0, 95, 184);

        /// <summary>
        /// 导航按钮的 Name 集合。
        /// 这些按钮由 MainForm.SwitchPage() 和 RefreshThemeColors() 管理深色样式，
        /// DarkenButton 应跳过它们。与此列表不同步的按钮会回退到默认 Button 深色样式。
        /// 修改 MainForm.Designer.cs 中的 nav* 按钮时需同步更新此集合。
        /// </summary>
        private static readonly HashSet<string> _navButtonNames = new(StringComparer.Ordinal)
        {
            "navHome",
            "navSettings",
            "navBenchmark",
            "navLogs",
            "navAbout",
        };

        // ================================================================
        // 公开属性
        // ================================================================

        public static Color SidebarBg { get { var m = CurrentMode; return m == ThemeMode.Dark ? D_SidebarBg : L_SidebarBg; } }
        public static Color ContentBg { get { var m = CurrentMode; return m == ThemeMode.Dark ? D_ContentBg : L_ContentBg; } }
        public static Color NavActiveBg { get { var m = CurrentMode; return m == ThemeMode.Dark ? D_NavActiveBg : L_NavActiveBg; } }
        public static Color NavHover { get { var m = CurrentMode; return m == ThemeMode.Dark ? D_NavHover : L_NavHover; } }
        public static Color IconPrimary { get { var m = CurrentMode; return m == ThemeMode.Dark ? D_IconPrimary : L_IconPrimary; } }
        public static Color IconSecondary { get { var m = CurrentMode; return m == ThemeMode.Dark ? D_IconSecondary : L_IconSecondary; } }
        public static Color ControlText { get { var m = CurrentMode; return m == ThemeMode.Dark ? D_ControlText : Color.Black; } }
        public static Color HighlightRowBg { get { var m = CurrentMode; return m == ThemeMode.Dark ? D_HighlightRowBg : Color.LightGreen; } }

        // ================================================================
        // 公共方法
        // ================================================================

        /// <summary>
        /// 设置主题模式并立即应用到整个窗体。
        /// </summary>
        public static void SetTheme(ThemeMode mode, Form? form = null)
        {
            if (!Application.MessageLoop)
                throw new InvalidOperationException(
                    "ThemeManager.SetTheme 必须在 UI 线程上调用");

            if (mode == CurrentMode)
                return;

            lock (_lock)
            {
                if (mode == CurrentMode)
                    return;

                // 记录原始颜色必须在更改 CurrentMode 之前进行。
                // 此时控件仍处于切换前的主题状态（浅色/设计器颜色），
                // 确保记录的 "originals" 始终是真正的原始颜色，
                // 而非前一次深色主题应用后的颜色。
                if (form != null && mode == ThemeMode.Dark)
                {
                    try
                    {
                        RecordOriginalColors(form.Controls);
                    }
                    catch (ObjectDisposedException)
                    {
                        // 窗体在记录原始颜色过程中被释放，静默忽略
                    }
                    catch (InvalidOperationException)
                    {
                        // 控件集合在迭代过程中被修改（如窗体关闭时），静默忽略
                    }
                }

                CurrentMode = mode;

                if (form != null)
                {
                    ApplyThemeCore(form);
                }
            }

            ThemeChanged?.Invoke(mode);
        }

        /// <summary>
        /// 将当前主题应用到整个窗体（外部调用时获取锁）。
        /// </summary>
        public static void ApplyTheme(Form form)
        {
            if (form == null || form.IsDisposed)
                return;

            if (!Application.MessageLoop)
                throw new InvalidOperationException(
                    "ThemeManager.ApplyTheme 必须在 UI 线程上调用");

            lock (_lock)
            {
                ApplyThemeCore(form);
            }
        }

        /// <summary>
        /// 应用主题的核心实现（调用方必须已持有 _lock）。
        /// </summary>
        private static void ApplyThemeCore(Form form)
        {
            if (form == null || form.IsDisposed)
                return;

            try
            {
                form.BackColor = CurrentMode == ThemeMode.Dark ? D_ContentBg : L_ContentBg;

                ApplyThemeToControls(form.Controls);
            }
            catch (ObjectDisposedException)
            {
                // 窗体在主题应用过程中被释放，静默忽略
            }
            catch (InvalidOperationException)
            {
                // 跨线程访问时忽略（控件已在其他线程上处理/重建）
            }
        }

        /// <summary>
        /// 清空已记录的原始颜色缓存。
        /// <para>警告：仅在窗体处于浅色模式（Light）或窗体重建/重启时调用。</para>
        /// <para>如果在深色模式下调用，后续切换回浅色模式无法恢复原始颜色。</para>
        /// </summary>
        public static void ClearOriginals()
        {
            lock (_lock)
            {
                if (CurrentMode != ThemeMode.Light)
                    throw new InvalidOperationException(
                        "ClearOriginals 必须在浅色模式下调用，否则后续切换回浅色模式无法正确恢复颜色");

                _originals = new ConditionalWeakTable<Control, OriginalColors>();
            }
        }

        /// <summary>
        /// 强制重新记录原始颜色（在 ApplyTheme 之后调用，用于刷新缓存）
        /// </summary>
        public static void RebindOriginals(Form form)
        {
            if (form == null || form.IsDisposed)
                return;

            if (!Application.MessageLoop)
                throw new InvalidOperationException(
                    "ThemeManager.RebindOriginals 必须在 UI 线程上调用");

            lock (_lock)
            {
                if (CurrentMode != ThemeMode.Light)
                    throw new InvalidOperationException("RebindOriginals 必须在浅色模式下调用");

                _originals = new ConditionalWeakTable<Control, OriginalColors>();
                try
                {
                    RecordOriginalColors(form.Controls);
                }
                catch (ObjectDisposedException)
                {
                    // 窗体已释放，静默忽略
                }
                catch (InvalidOperationException)
                {
                    // 控件集合已被修改（如窗体关闭时），静默忽略
                }
            }
        }

        // ================================================================
        // 获取导航图标颜色的便捷方法
        // ================================================================

        public static (Color Primary, Color Secondary) GetNavIconColors()
        {
            var mode = CurrentMode;
            return (
                mode == ThemeMode.Dark ? D_IconPrimary : L_IconPrimary,
                mode == ThemeMode.Dark ? D_IconSecondary : L_IconSecondary
            );
        }

        // ================================================================
        // 内部实现
        // ================================================================

        private static void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                // 注意：此处不调用 RecordIfNeeded() — 原始颜色仅在 SetTheme()
                // 切换深色模式 之前 通过 RecordOriginalColors() 记录一次。
                // 在已处于深色模式时调用 RecordIfNeeded() 会捕获深色值作为"原始"色，
                // 导致后续恢复浅色时控件保留深色外观。
                // 没有原始记录的控件在 RestoreControl() 中会通过亮度回退到 SystemColors。

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
            if (_originals.TryGetValue(ctrl, out _))
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

            if (ctrl is LinkLabel ll)
            {
                orig.LinkColor = ll.LinkColor;
                orig.ActiveLinkColor = ll.ActiveLinkColor;
                orig.VisitedLinkColor = ll.VisitedLinkColor;
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

                // 先保存 RowsDefaultCellStyle（正常的行样式）
                if (dgv.RowsDefaultCellStyle != null)
                {
                    orig.RowDefaultBack = dgv.RowsDefaultCellStyle.BackColor;
                    orig.RowDefaultFore = dgv.RowsDefaultCellStyle.ForeColor;
                }

                // 再保存 AlternatingRowsDefaultCellStyle（交替行样式），
                // 使用独立的 AltRowBack/AltRowFore，不再覆盖 RowDefaultBack/RowDefaultFore
                if (dgv.AlternatingRowsDefaultCellStyle != null)
                {
                    orig.AltRowBack = dgv.AlternatingRowsDefaultCellStyle.BackColor;
                    orig.AltRowFore = dgv.AlternatingRowsDefaultCellStyle.ForeColor;
                }

                // 保存行级选中颜色（与 DefaultCellStyle 分离存储）
                if (dgv.RowsDefaultCellStyle != null)
                {
                    orig.RowDefaultSelectionBack = dgv.RowsDefaultCellStyle.SelectionBackColor;
                    orig.RowDefaultSelectionFore = dgv.RowsDefaultCellStyle.SelectionForeColor;
                }

                if (dgv.AlternatingRowsDefaultCellStyle != null)
                {
                    orig.AltRowSelectionBack = dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor;
                    orig.AltRowSelectionFore = dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor;
                }
            }

            _originals.Add(ctrl, orig);
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
            {
                // 没有原始颜色记录（如 ClearOriginals 后调用 SetTheme(Light)）。
                // 若控件背景偏暗（深色模式特征），尝试恢复为默认浅色。
                // 排除 Color.Transparent (A=0)：透明背景控件的 GetBrightness() 返回 0，
                // 若不加区分会错误地将它们改为 SystemColors.Control 不透明背景。
                if (ctrl.BackColor.A > 0 && ctrl.BackColor.GetBrightness() < 0.4f)
                {
                    ctrl.BackColor = SystemColors.Control;
                    ctrl.ForeColor = SystemColors.ControlText;
                }

                return;
            }

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

                case LinkLabel ll:
                    ll.LinkColor = orig.LinkColor;
                    ll.ActiveLinkColor = orig.ActiveLinkColor;
                    ll.VisitedLinkColor = orig.VisitedLinkColor;
                    break;

                case DataGridView dgv:
                    dgv.EnableHeadersVisualStyles = orig.EnableHeadersVisualStyles;
                    dgv.BackgroundColor = orig.DataGridBg ?? SystemColors.Control;
                    dgv.GridColor = orig.GridColor ?? SystemColors.ControlLight;

                    // 如果 ColumnHeadersDefaultCellStyle 原始为 null，将暗色模式下新建的实例还原为 null
                    if (orig.ColumnHeaderBack == null &&
                        orig.ColumnHeaderFore == null &&
                        orig.ColumnHeaderSelectionBack == null &&
                        orig.ColumnHeaderSelectionFore == null)
                    {
                        dgv.ColumnHeadersDefaultCellStyle = null;
                    }
                    else if (dgv.ColumnHeadersDefaultCellStyle != null)
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

                    // 分别还原 RowsDefaultCellStyle 和 AlternatingRowsDefaultCellStyle
                    SetIfNotNull(dgv.RowsDefaultCellStyle, orig.RowDefaultBack ?? orig.RowBack, orig.RowDefaultFore ?? orig.RowFore);
                    SetIfNotNull(dgv.AlternatingRowsDefaultCellStyle, orig.AltRowBack ?? orig.RowBack, orig.AltRowFore ?? orig.RowFore);

                    // 还原行级选中颜色
                    SetSelectionIfNotNull(dgv.RowsDefaultCellStyle, orig.RowDefaultSelectionBack, orig.RowDefaultSelectionFore);
                    SetSelectionIfNotNull(dgv.AlternatingRowsDefaultCellStyle, orig.AltRowSelectionBack, orig.AltRowSelectionFore);
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
                    // 注意：启用视觉样式时，ProgressBar.ForeColor 可能无效。
                    // 如果需要实际改变条块颜色，需通过 P/Invoke 发送 PBM_SETBARCOLOR 消息。
                    pb.ForeColor = Color.FromArgb(0, 122, 204);
                    break;
            }
        }

        private static void DarkenLabel(Label lbl)
        {
            // LinkLabel 的类型检测不依赖原始颜色记录，提前处理
            if (lbl is LinkLabel ll)
            {
                ll.LinkColor = D_LinkBlue;
                ll.ActiveLinkColor = D_LinkBlue;
                ll.VisitedLinkColor = D_LinkBlue;
                lbl.ForeColor = D_ControlText;
                return;
            }

            if (!_originals.TryGetValue(lbl, out var orig))
            {
                // 没有原始颜色记录（如 ClearOriginals 后首次恢复），按普通标签处理
                lbl.ForeColor = D_ControlText;
                return;
            }

            // 判断原始 ForeColor 来确定该标签的角色
            var fg = orig.ForeColor;

            if (IsGrayTone(fg))
            {
                // 灰色副标题标签（如 "CPU 频率"、"Version --" 等）
                lbl.ForeColor = D_GrayText;
            }
            else if (fg.ToArgb() == L_LinkBlue.ToArgb())
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
            cb.BackColor = Color.Transparent;
            cb.ForeColor = D_ControlText;
        }

        private static void DarkenRadioButton(RadioButton rb)
        {
            rb.BackColor = Color.Transparent;
            rb.ForeColor = D_ControlText;
        }

        private static void DarkenButton(Button btn)
        {
            // 导航按钮有特殊的 FlatStyle.Flat 和透明背景，
            // 由 MainForm.SwitchPage() 和 RefreshThemeColors() 管理深色样式。
            // 这里只处理非导航的常规按钮（保存、取消、开始等）。
            var name = btn.Name;

            // 通过显式名称集合判断导航按钮，避免字符串前缀匹配带来的误判。
            if (!string.IsNullOrEmpty(name) && _navButtonNames.Contains(name))
                return;

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(102, 102, 102);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(54, 54, 58);
            btn.BackColor = D_ControlBg;
            btn.ForeColor = D_ControlText;
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
            else
            {
                // 列头样式为 null 时，自定义绘制以确保暗色
                dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = D_DataGridHeaderBg,
                    ForeColor = D_ControlText,
                    SelectionBackColor = D_DataGridHeaderBg,
                    SelectionForeColor = D_ControlText,
                };
            }

            if (dgv.DefaultCellStyle != null)
            {
                dgv.DefaultCellStyle.BackColor = D_DataGridRowBg;
                dgv.DefaultCellStyle.ForeColor = D_ControlText;
                dgv.DefaultCellStyle.SelectionBackColor = D_SelectionBg;
                dgv.DefaultCellStyle.SelectionForeColor = D_SelectionFg;
            }

            // 读取一次，避免两次读取属性可能返回不同实例（罕见但安全）
            var rowStyle = dgv.RowsDefaultCellStyle;
            var altRowStyle = dgv.AlternatingRowsDefaultCellStyle;

            SetIfNotNull(rowStyle, D_DataGridRowBg, D_ControlText);
            SetIfNotNull(altRowStyle, D_DataGridAltRowBg, D_ControlText);

            // 设置选中行样式
            if (rowStyle != null)
            {
                rowStyle.SelectionBackColor = D_SelectionBg;
                rowStyle.SelectionForeColor = D_SelectionFg;
            }

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

        private static void SetSelectionIfNotNull(DataGridViewCellStyle? style, Color? selectionBackColor, Color? selectionForeColor)
        {
            if (style == null) return;
            if (selectionBackColor.HasValue) style.SelectionBackColor = selectionBackColor.Value;
            if (selectionForeColor.HasValue) style.SelectionForeColor = selectionForeColor.Value;
        }

        /// <summary>
        /// 判断颜色是否为灰色调（R ≈ G ≈ B），用于 DarkenLabel 中识别副标题标签。
        /// 使用容差代替精确 ARGB 匹配，避免因设计器使用不同灰色值导致识别失败。
        /// </summary>
        private static bool IsGrayTone(Color color)
        {
            const int tolerance = 20;
            int maxDiff = Math.Max(
                Math.Max(Math.Abs(color.R - color.G), Math.Abs(color.G - color.B)),
                Math.Abs(color.R - color.B));
            return maxDiff <= tolerance &&
                   color.A > 200 &&
                   (color.R > 30 || color.G > 30 || color.B > 30) &&
                   (color.R < 225 || color.G < 225 || color.B < 225);
        }

        /// <summary>
        /// 记录所有已有控件的原始颜色（初始化时调用）
        /// </summary>
        private static void RecordOriginalColors(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                RecordIfNeeded(ctrl);

                if (ctrl.HasChildren)
                {
                    RecordOriginalColors(ctrl.Controls);
                }
            }
        }
    }
}
