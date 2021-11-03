using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyHost
{
    public partial class Form1 : Form
    {
        private readonly BindingList<HostFileInfo> _host = new BindingList<HostFileInfo>();
        private readonly string _hostDir;
        private readonly Color _defaultTextColor;

        public Form1()
        {
            InitializeComponent();
            _defaultTextColor = rbContent.SelectionColor;
            _hostDir = Path.Combine(Directory.GetCurrentDirectory(), "hosts");
            if (!Directory.Exists(_hostDir))
            {
                Directory.CreateDirectory(_hostDir);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.MultiSelect = false;
            LoadCurrentHost();
            LoadPrivateHost();
            dataGridView1.DataSource = _host;
            SyncMenu();
        }

        void LoadCurrentHost()
        {
            var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                @"System32\drivers\etc\hosts");
            if (!File.Exists(file))
            {
                tsFileAddress.Text = "不存在??";
                return;
            }

            tsFileAddress.Text = file;
            rbContent.Text = File.ReadAllText(file);
        }

        void LoadPrivateHost()
        {
            _host.Clear();

            var txt = Directory.GetFiles(_hostDir, "*.txt");
            foreach (var s in txt)
            {
                var content = File.ReadAllText(s);
                _host.Add(new HostFileInfo()
                {
                    File = s,
                    Name = Path.GetFileNameWithoutExtension(s),
                    IsCurrent = content == rbContent.Text
                });
            }

            var current = _host.FirstOrDefault(a => a.IsCurrent);
            if (current != null)
            {
                SetEditFile(current.File);
                return;
            }

            if (string.IsNullOrWhiteSpace(rbContent.Text)) return;
            var defaultHost = Path.Combine(_hostDir, "default.txt");
            File.WriteAllText(defaultHost, rbContent.Text);
            _host.Add(new HostFileInfo()
            {
                File = defaultHost,
                Name = "default",
                IsCurrent = true
            });
            SetEditFile(defaultHost);
        }

        private void 复制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = GetCurrent();
            if (item == null)
            {
                SetStatus("当前没有选中项");
                return;
            }

            var fileName = SetFileName.ShowInput(null, this);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                SetStatus("请输入新的文件名称");
                return;
            }

            var target = Path.Combine(_hostDir, $"{fileName}.txt");
            if (File.Exists(target))
            {
                SetStatus("该文件已经存在");
                return;
            }

            File.Copy(item.File, target);

            _host.Add(new HostFileInfo()
            {
                File = target,
                IsCurrent = false,
                Name = fileName
            });

            SetStatus("复制成功");
        }

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = GetCurrent();
            if (item == null) return;
            if (MessageBox.Show($"确定要删除{item.Name}么?", "温馨提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.No) return;
            _host.Remove(item);
            File.Delete(item.File);
        }

        private void 应用ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = GetCurrent();
            if (item == null) return;

            UseHost(item);
        }

        void UseHost(HostFileInfo fileInfo)
        {
            try
            {
                var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    @"System32\drivers\etc\hosts");
                File.WriteAllText(file, File.ReadAllText(fileInfo.File));

                foreach (var hostFileInfo in _host)
                {
                    hostFileInfo.IsCurrent = false;
                }

                fileInfo.IsCurrent = true;
                dataGridView1.Refresh();
                SyncMenu();
                SetStatus("应用成功");
                MessageBox.Show($"{fileInfo.Name}已经成功启用", "温馨提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用失败，可能需要管理员权限\n{ex.Message}");
            }
        }

        private void 编辑ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = GetCurrent();
            if (item == null) return;
            SetEditFile(item.File);
        }

        HostFileInfo GetCurrent()
        {
            if (dataGridView1.SelectedRows.Count == 0) return null;
            return dataGridView1.SelectedRows[0].DataBoundItem as HostFileInfo;
        }

        void SetStatus(string txt)
        {
            tsStatus.Text = txt;
        }

        void SetEditFile(string file)
        {
            rbContent.Clear();
            rbContent.Select(0, 0);
            rbContent.SelectionColor = _defaultTextColor;
            rbContent.Text = File.ReadAllText(file);
            rbContent.Tag = file;
            SetStatus($"正在编辑：{Path.GetFileNameWithoutExtension(file)}");
            UpdateColor();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!(rbContent.Tag is string file)) return;
            if (!File.Exists(file)) return;
            File.WriteAllText(file, rbContent.Text);
            rbContent.Tag = null;
            SetStatus("保存成功");
        }

        private void 新建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fileName = SetFileName.ShowInput(null, this);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                SetStatus("请输入新的文件名称");
                return;
            }

            var target = Path.Combine(_hostDir, $"{fileName}.txt");
            if (File.Exists(target))
            {
                SetStatus("该文件已经存在");
                return;
            }

            File.WriteAllText(target, $"#This is a host file create at {DateTime.Now}");

            _host.Add(new HostFileInfo()
            {
                File = target,
                IsCurrent = false,
                Name = fileName
            });

            SetEditFile(target);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Visible = false;
            e.Cancel = true;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
            BringToFront();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
            Environment.Exit(0);
        }

        private void 退出ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
            Environment.Exit(0);
        }

        void SyncMenu()
        {
            host文件ToolStripMenuItem.DropDownItems.Clear();
            foreach (var info in _host)
            {
                host文件ToolStripMenuItem.DropDownItems.Add(
                    new ToolStripMenuItem(info.Name, info.Image, HostFileMenu_Click) {Tag = info});
            }
        }

        private void HostFileMenu_Click(object sender, EventArgs e)
        {
            if (!(sender is ToolStripMenuItem menuItem)) return;
            if (!(menuItem.Tag is HostFileInfo hostFile)) return;
            UseHost(hostFile);
        }

        void UpdateColor()
        {
            var txt = rbContent.Text;
            var length = 0;
            var oldIndex = rbContent.SelectionStart;
            var oldLength = rbContent.SelectionLength;
            for (var i = txt.Length - 1; i >= 0; i--)
            {
                if (txt[i] == '\n')
                {
                    length = 1;
                    continue;
                }

                if (txt[i] == '#')
                {
                    rbContent.Select(i, length);
                    rbContent.SelectionColor = Color.Green;
                    rbContent.Select(0, 0);
                    rbContent.SelectionColor = _defaultTextColor;
                    continue;
                }

                length++;
            }

            rbContent.Select(oldIndex, oldLength);
            rbContent.SelectionColor = _defaultTextColor;
        }

        private void rbContent_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.S && e.Control)
            {
                DelayInvoke(1000, () => btnSave_Click(null, null));
            }

            if (e.KeyCode == Keys.D3 && e.Shift)
            {
                rbContent.SelectionColor = Color.Green;
            }

            if (e.KeyCode == Keys.Enter)
            {
                rbContent.SelectionColor = _defaultTextColor;
            }
        }

        void DelayInvoke(int time, Action action)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(time);
                try
                {
                    Invoke(action);
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {

                }
            });
        }

        private void gitHubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/cxwl3sxl/myhost");
        }

        private void giteeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://gitee.com/horntec_admin/my-host");
        }

        private void 关于AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"一个开源的Windows Host文件管理器！{Environment.NewLine} v1.0", "关于", MessageBoxButtons.OK);
        }
    }

    public class HostFileInfo
    {
        public string Name { get; set; }
        public string File { get; set; }
        public bool IsCurrent { get; set; }

        public Image Image => IsCurrent ? Properties.Resources._checked : Properties.Resources.unChecked;
    }
}
