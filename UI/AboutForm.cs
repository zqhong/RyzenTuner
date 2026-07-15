using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace RyzenTuner.UI
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();

            richTextBox1.Text = richTextBox1.Text
                .Replace("{version}",
                    $"V{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}")
                .Replace("{copyright_year}", DateTime.Now.Year.ToString());

            // 获取 ryzenadj 版本和编译日期
            LoadRyzenAdjInfo();

            richTextBox1.ReadOnly = true;
            richTextBox1.DetectUrls = true;
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            Text = Properties.Strings.TextAbout;
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void LoadRyzenAdjInfo()
        {
            try
            {
                var ryzenadjPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libryzenadj.dll");

                if (!File.Exists(ryzenadjPath))
                {
                    SetRyzenAdjPlaceholders("N/A");
                    return;
                }

                // 从 PE 头读取链接时间戳作为编译日期
                var compileDate = GetPeLinkTimestamp(ryzenadjPath);
                var ryzenadjDate = compileDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";

                SetRyzenAdjPlaceholders(ryzenadjDate);
            }
            catch
            {
                SetRyzenAdjPlaceholders("N/A");
            }
        }

        private static DateTime? GetPeLinkTimestamp(string dllPath)
        {
            using var stream = File.OpenRead(dllPath);
            using var reader = new BinaryReader(stream);

            // PE 头偏移量位于 DOS 头 0x3C 处
            stream.Seek(0x3C, SeekOrigin.Begin);
            var peOffset = reader.ReadInt32();

            // PE 签名(4字节)后的第 4 个字节是时间戳
            stream.Seek(peOffset + 8, SeekOrigin.Begin);
            var timestamp = reader.ReadUInt32();

            if (timestamp == 0)
                return null;

            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(timestamp)
                .ToLocalTime();
        }

        private void SetRyzenAdjPlaceholders(string date)
        {
            richTextBox1.Text = richTextBox1.Text
                .Replace("{ryzenadj_date}", date);
        }
    }
}