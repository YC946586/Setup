using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Newtonsoft.Json;
using Orange.Setup.Model;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Orange.Setup.ViewModel
{
    public class SetupViewModel : ViewModelBase
    {

        #region 属性

        /// <summary>
        /// 是否安装成功
        /// </summary>
        private string _notsucces = "成功";

        private string Msg = "";

        /// <summary>
        /// 应用程序名称
        /// </summary>
        private string appName = "Cnct.CUP.UpdateClient.exe";

        /// <summary>
        /// 卸载程序名称
        /// </summary>
        private string uninstallName = "Uninstall.exe";


        private SetupModel _pageCollection = new SetupModel();

        /// <summary>
        /// 界面数据
        /// </summary>
        public SetupModel PageCollection
        {
            get { return _pageCollection; }
            set
            {
                _pageCollection = value;
                RaisePropertyChanged("PageCollection");
            }
        }

        #endregion

        #region  命令

        private RelayCommand _setupCommand;

        /// <summary>
        ///  安装按钮
        /// </summary>
        public RelayCommand SetupCommand
        {
            get
            {
                if (_setupCommand == null)
                {
                    _setupCommand = new RelayCommand(Setup);
                }
                return _setupCommand;
            }
        }

        private RelayCommand _exitCommand;

        /// <summary>
        /// 关闭页
        /// </summary>
        public RelayCommand ExitCommand
        {
            get
            {
                if (_exitCommand == null)
                {
                    _exitCommand = new RelayCommand(Close);
                }
                return _exitCommand;
            }
            set { _exitCommand = value; }
        }

        private RelayCommand _customCommand;

        /// <summary>
        /// 自定义安装
        /// </summary>
        public RelayCommand CustomCommand
        {
            get
            {
                if (_customCommand == null)
                {
                    _customCommand = new RelayCommand(Custom);
                }
                return _customCommand;
            }
            set { _customCommand = value; }
        }

        private RelayCommand _browseCommand;

        /// <summary>
        /// 选择安装目录
        /// </summary>
        public RelayCommand BrowseCommand
        {
            get
            {
                if (_browseCommand == null)
                {
                    _browseCommand = new RelayCommand(Browse);
                }
                return _browseCommand;
            }
            set { _browseCommand = value; }
        }

        private RelayCommand _sigeCommand;

        /// <summary>
        /// 立即体验
        /// </summary>
        public RelayCommand SigeCommand
        {
            get
            {
                if (_sigeCommand == null)
                {
                    _sigeCommand = new RelayCommand(Sige);
                }
                return _sigeCommand;
            }
            set { _sigeCommand = value; }
        }


        #endregion

         

        public SetupViewModel()
        {
            ////检测.net版本 如果没有安装 就去下载
            //if (!Tool.CheckNetLanguage())
            //{
            //    Process.Start(@"https://dotnet.microsoft.com/download/thank-you/net452?survey=false");
            //    MessageBox.Show("检测到您暂未安装.NET4.5,请先安装.NET4.5", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            //    return;
            //}

            //检测VC++2013
            //if (!Tool.CheckVc2013())
            //{
            //    Process.Start(@"https://www.microsoft.com/en-us/download/confirmation.aspx?id=40784");

            //    MessageBox.Show("检测到您暂未安装VC++2013,请先安装VC++2013", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            //    return;
            //}
            //获取电脑盘符
            var drive = DriveInfo.GetDrives();
            if (drive.Length != 0)
            {
                var driveDate = drive.Where(s => s.IsReady).ToList();
                if (driveDate.Any())
                {
                    if (driveDate.Count > 1)
                    {
                        PageCollection.StrupPath = driveDate[1].Name + "Orange3.0";
                    }
                    else
                    {
                        PageCollection.StrupPath = driveDate[0].Name + "Orange3.0";
                    }
                }
            }
            //操作系统
            PageCollection.Winver = Tool.GetWindows();
        }

        /// <summary>
        /// 选择安装目录
        /// </summary>
        private void Browse()
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.Description = "请选择安装路径";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    PageCollection.StrupPath = fbd.SelectedPath + "\\Orange3.0";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 立即安装 GGOGOGOGO
        /// </summary>
        private void Setup()
        {
            try
            {
                PageCollection.GridHide = 2;
                //创建用户指定的安装目录文件夹
                if (!Directory.Exists(PageCollection.StrupPath))
                {
                    Directory.CreateDirectory(PageCollection.StrupPath);
                    DirectoryInfo dir = new DirectoryInfo(PageCollection.StrupPath);
                    dir.Attributes = FileAttributes.Normal & FileAttributes.Directory;

                }
                else
                {
                    FileInfo fi = new FileInfo(PageCollection.StrupPath);
                    //判断文件属性是否只读?是则修改为一般属性再删除
                    fi.Attributes = FileAttributes.Normal & FileAttributes.Directory;
                }
                //解压文件
                Thread thread = new Thread(GetAllDirFiles);
                thread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 开始解压 实现安装
        /// </summary>
        private void GetAllDirFiles()
        {
            try
            {
                //将软件解压到用户指定目录
                var filesPath = SetupFile.Orange3_0;
                Extract(filesPath);
                Adddesktop();
            }
            catch (Exception ex)
            {
                _notsucces = "失败";
                Msg = ex.Message;
                MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Task.Factory.StartNew(() =>
                {
                    us_gnsyjlEntity gnglEntity = new us_gnsyjlEntity()
                    {
                        ZJ = Guid.NewGuid().ToString("N"),
                        YHZJ = GetCpuSerialNumber(),
                        GNZJ = "Setup3.0",
                        GXSJ = DateTime.Now,
                        BZ = "系统安装",
                        TJMC = "Setup3.0",
                        JG = _notsucces,
                        CWYY = Msg,

                    };
                    gnglEntity.GXR = gnglEntity.YHZJ;
                    OrangeApiPost(gnglEntity);
                });
             
            }
        }


        /// <summary>
        /// 解压缩zip文件
        /// </summary>
        /// <param name="zipFile">解压的zip文件流</param>
        private bool Extract(byte[] zipFile)
        {
            try
            {
                PageCollection.Schedule = 0;
                PageCollection.StrupPath = PageCollection.StrupPath.TrimEnd('/') + "//";
                byte[] data = new byte[1024*1204];
                int size; //缓冲区的大小（字节）
                double fileCount = 0; //带待压文件的大小（字节）
                double osize = 0; //每次解压读取数据的大小（字节）
                using (ZipInputStream s = new ZipInputStream(new System.IO.MemoryStream(zipFile)))
                {
                    ZipEntry entry;
                    while ((entry = s.GetNextEntry()) != null)
                    {
                        fileCount += entry.Size; //获得待解压文件的大小
                    }
                }
                PageCollection.Maximum = fileCount;
                using (var s = new ZipInputStream(new System.IO.MemoryStream(zipFile)))
                {
                    ZipEntry theEntry;
                    while ((theEntry = s.GetNextEntry()) != null)
                    {
                        if (theEntry.IsDirectory)
                        {
                            continue;
                        }
                        string directorName = Path.Combine(PageCollection.StrupPath,
                            Path.GetDirectoryName(theEntry.Name));
                        string fileName = Path.Combine(directorName, Path.GetFileName(theEntry.Name));
                        if (!Directory.Exists(directorName))
                        {
                            Directory.CreateDirectory(directorName);
                        }
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            //设置文件为可读
                            if (File.Exists(fileName))
                            {
                                File.SetAttributes(fileName, FileAttributes.Normal);
                            }

                            using (FileStream streamWriter = System.IO.File.Create(fileName))
                            {
                                while (true)
                                {
                                    size = s.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        osize += size;
                                        streamWriter.Write(data, 0, size);

                                        PageCollection.Plah =
                                            Math.Round((osize/fileCount*100), 0).ToString(CultureInfo.InvariantCulture);
                                        PageCollection.Schedule = osize;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                //if (theEntry.Name.Contains("dll"))
                                //{
                                //    PageCollection.Message = theEntry.Name.Substring(0, theEntry.Name.Length - 3);
                                //}
                                //else
                                //{
                                    PageCollection.Message = theEntry.Name;
                                //}
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 增加桌面图标
        /// </summary>
        private void Adddesktop()
        {
            try
            {

                //删除注册表数据
                Tool.DeleteStartMenuShortcuts(ProcessBase.Orange3,PageCollection.StrupPath);

                //添加开始菜单快捷方式
                RegistryKey hkeyCurrentUser =
                    Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders");


                if (hkeyCurrentUser != null)
                {
           
                    string programsPath = hkeyCurrentUser.GetValue("Programs").ToString(); //获取开始菜单程序文件夹路径
                    Directory.CreateDirectory(programsPath + @"\四川橘子智税科技有限公司"); //在程序文件夹中创建快捷方式的文件夹

                    PageCollection.Schedule = PageCollection.Schedule + 1;
                    PageCollection.Plah = PageCollection.Schedule.ToString();
                    PageCollection.Message = "添加开始菜单快捷方式";

                    Tool.CreateShortcut(programsPath + @"\四川橘子智税科技有限公司\橘子财税服务平台.lnk", PageCollection.StrupPath, appName);

                    PageCollection.Schedule = PageCollection.Schedule + 1;
                    PageCollection.Plah = PageCollection.Schedule.ToString();
                    PageCollection.Message = "添加卸载目录";
                    Tool.CreateShortcut(programsPath + @"\四川橘子智税科技有限公司\卸载橘子财税服务平台.lnk", PageCollection.StrupPath,
                        uninstallName); //创建卸载快捷方式

                    //添加桌面快捷方式
                    string desktopPath = hkeyCurrentUser.GetValue("Desktop").ToString(); //获取桌面文件夹路径
                    PageCollection.Schedule = PageCollection.Schedule + 1;
                    PageCollection.Plah = PageCollection.Schedule.ToString();
                    PageCollection.Message = "添加桌面图标";
                    Tool.CreateShortcut(desktopPath + @"\橘子财税服务平台.lnk", PageCollection.StrupPath, appName); //创建快捷方式

                    PageCollection.Schedule = 100;
                    PageCollection.Plah = PageCollection.Schedule.ToString();
                    PageCollection.GridHide = 3;
                    PageCollection.Message = "橘子财税服务平台安装完成";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 自定义安装
        /// </summary>
        private void Custom()
        {
            try
            {
                PageCollection.GridHide = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭当前窗体
        /// </summary>
        /// <param name="win"></param>
        private void Close()
        {
            try
            {
                if (MessageBox.Show("你确定退出安装程序吗？", "温馨提示", MessageBoxButton.YesNo, MessageBoxImage.Information) ==
                    MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 立即体验
        /// </summary>
        private void Sige()
        {
            Process.Start(PageCollection.StrupPath + @"\" + appName);
            Application.Current.Shutdown(0);
        }

        /// <summary>
        /// 获取CPU的ID
        /// </summary>
        /// <returns></returns>
        private string GetCpuSerialNumber()
        {
            try
            {
                string cpuSerialNumber = string.Empty;
                ManagementClass mc = new ManagementClass("Win32_Processor");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    cpuSerialNumber = mo["ProcessorId"].ToString();
                    break;
                }
                mc.Dispose();
                moc.Dispose();
                return cpuSerialNumber;
            }
            catch (Exception ex)
            {
                return Guid.NewGuid().ToString("N");
            }    
         
        }

        /// <summary>
        /// Http POST 请求(参数 JSON) 有返回值的
        /// </summary>
        /// <param name="controller">控制器</param>
        /// <param name="method">方法</param>
        /// <param name="parameter">泛型 可以直接传实体</param>
        /// <returns></returns>
        private string OrangeApiPost<T>(T parameter)
        {
            try
            {
                string result = ""; //返回结果
                Encoding encoding = Encoding.UTF8;
                string url = "http://125.71.216.71:11422/" + "/com.cnct.orange.api." + "System" + "." + "Insertus_gnsyjl";
                string date = JsonConvert.SerializeObject(parameter);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Accept = "text/html, application/xhtml+xml, */*";
                request.ContentType = "application/json";

                byte[] buffer = encoding.GetBytes(date);
                request.ContentLength = buffer.Length;
                request.GetRequestStream().Write(buffer, 0, buffer.Length);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
                return result;
            }
            catch (Exception ex)
            {
                return "";
            }

        }
    }
}
