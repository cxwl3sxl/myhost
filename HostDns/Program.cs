using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace HostDns
{
    internal class Program
    {
        private static string _hostFile;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (!CheckPrincipal()) return;
            if (!CheckArgument(args)) return;
            if (!FindHostFile()) return;

            if (args.Contains("-list"))
            {
                List();
            }
            else if (args.Contains("-add"))
            {
                Add(args);
            }
            else if (args.Contains("-remove"))
            {
                Remove(args);
            }
        }

        static bool FindHostFile()
        {
            _hostFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                @"System32\drivers\etc\hosts");
            if (File.Exists(_hostFile)) return true;
            Console.Error.WriteLine($"未能找到Host文件({_hostFile})");
            Environment.ExitCode = 1;
            return false;
        }

        static bool CheckPrincipal()
        {
            var id = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(id);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator)) return true;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("运行程序需要管理员权限，否则某些功能无法正常运行！请按任意键退出后重启本程序！");
            Console.ReadKey();
            Environment.ExitCode = 1;
            return false;
        }

        static bool CheckArgument(string[] args)
        {
            if (args.Length != 0) return true;
            Console.WriteLine("使用方法：HostDns.exe [命令] [参数]");
            Console.WriteLine(" -list                列出所有Host文件中配置的地址");
            Console.WriteLine(" -add [ip] [domain]   向Host文件中添加域名映射");
            Console.WriteLine(" -remove [domain]     移除指定域名的配置项");
            Environment.ExitCode = 1;
            return false;
        }

        static void List()
        {
            var host = BuildHostFile();
            foreach (var item in host)
            {
                if (item.IsComment) continue;
                Console.WriteLine(item);
            }
        }

        static List<HostItem> BuildHostFile()
        {
            var list = new List<HostItem>();
            var lines = File.ReadAllLines(_hostFile);
            foreach (var line in lines)
            {
                list.Add(new HostItem(line));
            }

            return list;
        }

        static void Remove(string[] args)
        {
            var index = Array.IndexOf(args, "-remove");
            var domainIndex = index + 1;
            if (domainIndex >= args.Length)
            {
                Console.Error.WriteLine("参数错误，使用方式：-remove [domain]");
                return;
            }

            var domain = args[domainIndex];

            var host = BuildHostFile();
            var target = host.FirstOrDefault(a => a.Domain == domain);
            if (target == null)
            {
                Console.Error.WriteLine($"指定的域名不存在 {domain}");
                return;
            }

            host.Remove(target);
            if (SaveHost(host, out var msg))
            {
                Console.Write("域名删除成功");
                return;
            }

            Environment.ExitCode = 1;
            Console.Error.WriteLine($"删除域名失败:{msg}");
        }

        static void Add(string[] args)
        {
            var index = Array.IndexOf(args, "-add");
            var ipIndex = index + 1;
            var domainIndex = ipIndex + 1;
            if (domainIndex >= args.Length)
            {
                Console.Error.WriteLine("参数错误，使用方式：-add [ip] [domain]");
                return;
            }

            var ip = args[ipIndex];
            var domain = args[domainIndex];

            var host = BuildHostFile();
            var target = host.FirstOrDefault(a => a.Domain == domain);
            if (target != null)
            {
                Console.Error.WriteLine($"该域名已存在 {domain}");
                return;
            }

            host.Add(new HostItem($"{ip} {domain}"));
            if (SaveHost(host, out var msg))
            {
                Console.Write("域名添加成功");
                return;
            }

            Environment.ExitCode = 1;
            Console.Error.WriteLine($"添加域名失败:{msg}");
        }

        static bool SaveHost(List<HostItem> host, out string message)
        {
            message = null;
            try
            {
                var sb = new StringBuilder();
                foreach (var item in host)
                {
                    sb.AppendLine(item.ToString());
                }

                File.WriteAllText(_hostFile, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
    }
}
