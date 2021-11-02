using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MyHost
{
    public partial class Form1 : Form
    {
        private readonly BindingList<HostFileInfo> _host = new BindingList<HostFileInfo>();
        private readonly string _hostDir;

        public Form1()
        {
            InitializeComponent();
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

            try
            {
                var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    @"System32\drivers\etc\hosts");
                File.WriteAllText(file, File.ReadAllText(item.File));

                foreach (var hostFileInfo in _host)
                {
                    hostFileInfo.IsCurrent = false;
                }

                item.IsCurrent = true;
                dataGridView1.Refresh();
                SetStatus("应用成功");
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
            rbContent.Text = File.ReadAllText(file);
            rbContent.Tag = file;
            SetStatus($"正在编辑：{Path.GetFileNameWithoutExtension(file)}");
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!(rbContent.Tag is string file)) return;
            if (!File.Exists(file)) return;
            File.WriteAllText(file, rbContent.Text);
            rbContent.Tag = null;
            SetStatus("保存成功");
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
