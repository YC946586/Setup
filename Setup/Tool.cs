using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using File = System.IO.File;

namespace Orange.Setup
{
    public class Tool
    {
        /// <summary>
        /// 获取用户当前操作系统
        /// </summary>
        /// <returns></returns>
        public static string GetWindows()
        {
            try
            {
                Version ver = Environment.OSVersion.Version;
                string strClient = "";
                if (ver.Major == 5 && ver.Minor == 1)
                {
                    strClient = "Windows XP";
                }
                else if (ver.Major == 6 && ver.Minor == 1)
                {
                    strClient = "Windows 7";
                }
                else if (ver.Major == 5 && ver.Minor == 0)
                {
                    strClient = "Windows 2000";
                }
                else if (ver.Major == 6 && ver.Minor == 2)
                {
                    strClient = "Windows 8 ";
                }
                else if (ver.Major == 6 && ver.Minor == 3)
                {
                    strClient = "Windows 8.1";
                }
                else if (ver.Major == 10 && ver.Minor == 0)
                {
                    strClient = "Windows 10";
                }
                else
                {
                    strClient = "其他操作系统";
                }
                return strClient;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        /// <summary>
        /// 检查是否已经安装了NET中文语言包
        /// </summary>
        /// <returns>返回检查结果</returns>
        public static bool CheckNetLanguage()
        {
            try
            {
                ////获取注册表的HKEY_LOCAL_MACHINE节点
                //RegistryKey hkeyLocalMachine = Registry.LocalMachine;
                ////获取注册表HKEY_LOCAL_MACHINE节点下面的Language节点
                //RegistryKey netFrameworkLanguage =
                //    hkeyLocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\");
                //if (netFrameworkLanguage != null)
                //{
                //    int releaseKey = Convert.ToInt32(netFrameworkLanguage.GetValue("Release"));
                //    if (releaseKey >= 393295)
                //    {
                //        return true; //"4.6 or later";
                //    }
                //    if ((releaseKey >= 379893))
                //    {
                //        return true; //"4.5.2 or later";
                //    }
                //    if ((releaseKey >= 378675))
                //    {
                //        return true; //"4.5.1 or later";
                //    }
                //    if ((releaseKey >= 378389))
                //    {
                //        return true; //"4.5 or later";
                //    }
                //    hkeyLocalMachine.Close();
                //    netFrameworkLanguage.Close();
                //}
                //return false;

                string oldname = "0";
                using (RegistryKey ndpKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
                {
                    foreach (string versionKeyName in ndpKey.GetSubKeyNames())
                    {
                        if (versionKeyName.StartsWith("v"))
                        {
                            RegistryKey versionKey = ndpKey.OpenSubKey(versionKeyName);
                            string newname = (string)versionKey.GetValue("Version", "");
                            if (string.Compare(newname, oldname) >= 0)
                            {
                                oldname = newname;
                            }
                            if (newname != "")
                            {
                                continue;
                            }
                            foreach (string subKeyName in versionKey.GetSubKeyNames())
                            {
                                RegistryKey subKey = versionKey.OpenSubKey(subKeyName);
                                newname = (string)subKey.GetValue("Version", "");
                                if (string.Compare(newname, oldname) >= 0)
                                {
                                    oldname = newname;
                                }
                            }
                        }
                    }
                }
                return string.Compare(oldname, "4.5") >= 0 ? true : false;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// 检测VC+=2013是否安装
        /// </summary>
        /// <returns></returns>
        public static bool CheckVc2013()
        {
            try
            {
                //*2013(X64) { 929FBD26 - 9020 - 399B - 9A7A - 751D61F0B942}
                //*2013(X86) { 13A4EE12 - 23EA - 3371 - 91EE - EFB36DDFFF3E}
                RegistryKey cuKey = Registry.LocalMachine;
                RegistryKey cnctkjptKey = cuKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\", true);
                if (cnctkjptKey != null)
                {
                    var check =
                        cnctkjptKey.GetSubKeyNames()
                            .Where(
                                s =>
                                    s.Contains("FF66E9F6-83E7-3A3E-AF14-8DE9A809A6A4") ||
                                    s.Contains("5e4b593b-ca3c-429c-bc49-b51cbf46e72a") ||
                                    s.Contains("13A4EE12-23EA-3371-91EE-EFB36DDFFF3E") ||
                                    s.Contains("929FBD26-9020-399B-9A7A-751D61F0B942"));
                    if (check.Any())
                    {
                        return true;
                    }
                    cuKey.Close();
                    cnctkjptKey.Close();
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 打开VC++2013安装
        /// </summary>
        public static void OpenVcredist()
        {
            var curPath = AppDomain.CurrentDomain.BaseDirectory + "\\EnvLibrary\\";
            Process.Start(curPath + "vcredist2013_x86.exe");
        }



        /// <summary>
        /// 删除开始栏快捷方式
        /// </summary>
        /// <param name="bBase"></param>
        /// <returns></returns>
        public static void DeleteStartMenuShortcuts(ProcessBase bBase,string stpPath)
        {
            try
            {
                RegistryKey cuKey = Registry.LocalMachine;
                RegistryKey cnctkjptKey = cuKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true);

                //获取AutoCAD软件快捷方式在桌面上的路径
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string pathCom = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                //返回桌面上*.lnk文件的集合
                string[] items = Directory.GetFiles(path, "*.lnk");
                string[] itemsCom = Directory.GetFiles(pathCom, "*.lnk");
                switch (bBase)
                {
                    case ProcessBase.Orange3:
                        {
                            #region 删除开始栏快捷方式
                            string strJkname = "四川世纪中税企业咨询管理有限公司";
                            string strFold = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\" +
                                             "Programs\\" + strJkname;
                            //判断文件夹是否存在，存在就删除
                            if (Directory.Exists(strFold))
                            {
                                Directory.Delete(strFold, true);
                            }
                            string strFold2 = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\" +
                                              "Programs\\" + "四川橘子智税科技有限公司";
                            //判断文件夹是否存在，存在就删除
                            if (Directory.Exists(strFold2))
                            {
                                Directory.Delete(strFold2, true);
                            }

                            #endregion

                            #region 删除Uninstall注册表

                            if (cnctkjptKey != null)
                            {
                                foreach (string aimKey in cnctkjptKey.GetSubKeyNames())
                                {
                                    //if (aimKey == "Orange2")
                                    //    CNCTKJPTKey.DeleteSubKeyTree("Orange2");
                                    if (aimKey == "Orange3")
                                        cnctkjptKey.DeleteSubKeyTree("Orange3");
                                }
                                cnctkjptKey.Close();
                            }
                          
                            cuKey.Close();


                            #endregion

                            #region 删除桌面快捷方式

                            foreach (string item in items)
                            {
                                Console.WriteLine(item);
                                if (item.Contains("橘子财税服务平台") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }
                                else if (item.Contains("橘子财税") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }
                                else if (item.Contains("卸载橘子财税服务平台") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }
                            }
                            foreach (string item in itemsCom)
                            {
                                Console.WriteLine(item);
                                if (item.Contains("橘子财税服务平台") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }
                                else if (item.Contains("橘子财税") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }
                                else if (item.Contains("卸载橘子财税服务平台") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }

                            }

                            #endregion

                            #region 新增控制面板卸载

                            RegistryKey cuKeyUni = Registry.LocalMachine;
                            RegistryKey cnctkjptUnin = cuKeyUni.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Orange3");
                            if (cnctkjptUnin != null)
                            {
                                cnctkjptUnin.SetValue("DisplayName", "橘子财税服务平台");
                                cnctkjptUnin.SetValue("Publisher", "四川橘子智税科技有限公司");
                                cnctkjptUnin.SetValue("UninstallString", stpPath + @"\Uninstall.exe");
                                cnctkjptUnin.SetValue("DisplayIcon", stpPath + @"\Cnct.CUP.UpdateClient.exe");
                                cnctkjptUnin.SetValue("DisplayVersion", "V3.0");
                                cnctkjptUnin.SetValue("InstallDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                            }

                            #endregion
                        }
                        break;
                    case ProcessBase.Orange2:
                        break;

                    case ProcessBase.Companies:
                        {
                            #region 删除开始栏快捷方式
                            //string strJkname = "四川世纪中税企业咨询管理有限公司";
                            //string strFold = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\" +
                            //                 "Programs\\" + strJkname;
                            ////判断文件夹是否存在，存在就删除
                            //if (Directory.Exists(strFold))
                            //{
                            //    Directory.Delete(strFold, true);
                            //}
                            //string strFold2 = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\" +
                            //                  "Programs\\" + "四川橘子智税科技有限公司";
                            ////判断文件夹是否存在，存在就删除
                            //if (Directory.Exists(strFold2))
                            //{
                            //    Directory.Delete(strFold2, true);
                            //}

                            #endregion

                            #region 删除Uninstall注册表

                            if (cnctkjptKey != null)
                            {
                                foreach (string aimKey in cnctkjptKey.GetSubKeyNames())
                                {
                                    //if (aimKey == "Orange2")
                                    //    CNCTKJPTKey.DeleteSubKeyTree("Orange2");
                                    if (aimKey == "CompaniesAid")
                                        cnctkjptKey.DeleteSubKeyTree("CompaniesAid");
                                }
                                cnctkjptKey.Close();
                            }

                            cuKey.Close();


                            #endregion

                            #region 删除桌面快捷方式

                            foreach (string item in items)
                            {
                                Console.WriteLine(item);
                                if (item.Contains("企助") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }
                                else if (item.Contains("企助") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }
                                else if (item.Contains("卸载企助") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }
                            }
                            foreach (string item in itemsCom)
                            {
                                Console.WriteLine(item);
                                if (item.Contains("企助") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }
                                else if (item.Contains("企助") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }
                                else if (item.Contains("卸载企助") && item.Contains(".lnk"))
                                {
                                    File.Delete(item);
                                }

                            }

                            #endregion

                            #region 新增控制面板卸载

                            RegistryKey cuKeyUni = Registry.LocalMachine;
                            RegistryKey cnctkjptUnin = cuKeyUni.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Orange3");
                            if (cnctkjptUnin != null)
                            {
                                cnctkjptUnin.SetValue("DisplayName", "企助");
                                cnctkjptUnin.SetValue("Publisher", "四川橘子智税科技有限公司");
                                cnctkjptUnin.SetValue("UninstallString", stpPath + @"\Uninstall.exe");
                                cnctkjptUnin.SetValue("DisplayIcon", stpPath + @"\Cnct.CUP.UpdateClient.exe");
                                cnctkjptUnin.SetValue("DisplayVersion", "V1.0");
                                cnctkjptUnin.SetValue("InstallDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                            }

                            #endregion
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(bBase), bBase, null);
                }



            }
            catch (Exception ex)
            {

            }
        }

       
        /// <summary>
        /// 创建快捷方式
        /// </summary>
        /// <param name="shortcutPath"></param>
        /// <param name="path">快捷方式的保存路径</param>
        public static void CreateShortcut(string shortcutPath, string path, string fileName)
        {
            try
            {
                //实例化WshShell对象 
                WshShell shell = new WshShell();

                //通过该对象的 CreateShortcut 方法来创建 IWshShortcut 接口的实例对象 
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                //设置快捷方式的目标所在的位置(源程序完整路径) 
                shortcut.TargetPath = path + @"\" + fileName;

                //快捷方式的描述 
                shortcut.Description = "四川橘子智税软件系统有限公司";

                //保存快捷方式 
                shortcut.Save();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public enum ProcessBase
    {
        Orange3,
        Orange2,
        Companies,
    }
}
