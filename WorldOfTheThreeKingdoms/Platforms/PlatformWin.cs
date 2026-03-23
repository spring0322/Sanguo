using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.IO.Compression;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Drawing.Drawing2D;
using System.Management;
using System.Net.NetworkInformation;
using SharpCompress.Common;
using Microsoft.Xna.Framework.Input;
using Tools;
using WorldOfTheThreeKingdoms;
using GameManager;
using WorldOfTheThreeKingdoms.GameScreens;

namespace Platforms
{
    /// <summary>
    /// 各平台不同的實現
    /// </summary>
    public class Platform : PlatformBase
    {
        public static new PlatFormType PlatFormType = PlatFormType.Win;

        public static new bool IsMobilePlatForm = false;

        public new string PreferFullMode = "Window";

        public static new string PreferResolution = "1368*768";

        public new bool DebugMode = true;
        public new bool ProcessGameData = true;

        public new bool AssetsPng = false;

        public new bool QuickTest = true;

        public new bool DisplayMetroStart = false;

        public static object wic; // WindowInputCapturer commented out

        // 🔥 性能优化：文件存在性缓存（避免重复 File.Exists() 调用）
        private static readonly Dictionary<string, bool> _fileExistsCache = [];
        private static readonly object _fileExistsCacheLock = new object();

        /// <summary>
        /// 內存使用占用
        /// </summary>
        public new string MemoryUsage
        {
            get
            {
                return (System.GC.GetTotalMemory(false) / 1024).ToString();
                //Android.Activity1.os.Debug.getNativeHeapAllocatedSize()
                //return "";
            }
        }

        //static bool IsTrialOrigin = true;
        //public static bool? isTrial;
        //public static bool IsTrial
        //{
        //    get
        //    {
        //        //Guide.SimulateTrialMode = true;  //Guide.IsTrialMode
        //        if (isTrial == null)
        //        {
        //            isTrial = IsTrialOrigin && !IsMemberUser;
        //        }
        //        return (bool)isTrial;
        //    }
        //    set
        //    {
        //        isTrial = value;
        //    }
        //}

        //public static bool IsMemberUser
        //{
        //    get
        //    {
        //        return Session.GameUser != null && !String.IsNullOrEmpty(Session.GameUser.UserRole) && Session.GameUser.UserRole.Contains("SanguoWind");
        //    }
        //}

        public new string Location
        {
            get
            {
                return AppContext.BaseDirectory;
            }
        }

        public static bool IsActive
        {
            get
            {
                return MainGame != null ? MainGame.IsActive : false;
            }
        }

        public new bool WindowInputCapturerEnable
        {
            get
            {
                return false; // WindowInputCapturer.Enable commented out
            }
            set
            {
                // WindowInputCapturer.Enable = value; commented out
            }
        }

        public new bool KeyBoardAvailable = true;

        // 🔥 性能优化：清理文件存在性缓存（场景切换时调用）
        public static void ClearFileExistsCache()
        {
            lock (_fileExistsCacheLock)
            {
                _fileExistsCache.Clear();
                System.Diagnostics.Debug.WriteLine("[Platform] 文件存在性缓存已清理");
            }
        }

        static GraphicsDeviceManager GraphicsDeviceManager = null;

        public static GraphicsDevice GraphicsDevice
        {
            get
            {
                return GraphicsDeviceManager != null ? GraphicsDeviceManager.GraphicsDevice : null;
            }
        }

        public static void InitGraphicsDeviceManager()
        {
            GraphicsDeviceManager = new GraphicsDeviceManager(MainGame);

            GraphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
            
            // 🔥 修复：在创建时立即设置正确的初始窗口尺寸
            // 不设置会导致 MonoGame 使用默认值 800x480，让 LoadContent 读到错误的 Viewport
            string resolution = Setting.Current?.Resolution;
            
            // 首次启动或配置为空：根据屏幕分辨率自动选择合适的窗口大小
            if (String.IsNullOrEmpty(resolution) || !resolution.Contains('*'))
            {
                try
                {
                    var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                    int screenW = displayMode.Width;
                    int screenH = displayMode.Height;
                    
                    // 预设的 16:9 分辨率列表（从大到小）
                    string[] candidates = { "2560*1440", "1920*1080", "1600*900", "1368*768", "1280*720" };
                    
                    // 选择不超过屏幕 85% 的最大分辨率
                    int maxW = (int)(screenW * 0.85);
                    int maxH = (int)(screenH * 0.85);
                    
                    resolution = "1280*720"; // 保底
                    foreach (string candidate in candidates)
                    {
                        string[] p = candidate.Split('*');
                        int cw = int.Parse(p[0]);
                        int ch = int.Parse(p[1]);
                        if (cw <= maxW && ch <= maxH)
                        {
                            resolution = candidate;
                            break;
                        }
                    }
                    
                    // 保存到配置，下次启动就不需要再检测了
                    if (Setting.Current != null)
                    {
                        Setting.Current.Resolution = resolution;
                    }
                    System.Diagnostics.Debug.WriteLine($"[InitGraphicsDeviceManager] 首次启动自适应: 屏幕{screenW}x{screenH}, 选择窗口{resolution}");
                }
                catch
                {
                    resolution = PreferResolution;  // 异常时用默认 "1368*768"
                }
            }
            
            if (!String.IsNullOrEmpty(resolution) && resolution.Contains('*'))
            {
                string[] parts = resolution.Split('*');
                if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h) && w > 0 && h > 0)
                {
                    GraphicsDeviceManager.PreferredBackBufferWidth = w;
                    GraphicsDeviceManager.PreferredBackBufferHeight = h;
                    System.Diagnostics.Debug.WriteLine($"[InitGraphicsDeviceManager] 初始窗口尺寸: {w}x{h}");
                }
            }
        }

        public static void SetGraphicsWidthHeight(int width, int height)
        {
            System.Diagnostics.Debug.WriteLine($"[SetGraphicsWidthHeight] 请求设置窗口尺寸: {width}x{height}");
            
            GraphicsDeviceManager.PreferredBackBufferWidth = width;
            GraphicsDeviceManager.PreferredBackBufferHeight = height;
            
            // 🔥 关键修复：立即应用更改，确保窗口尺寸生效
            // 之前缺少这行，导致窗口尺寸一直是默认的 800x480
            GraphicsDeviceManager.ApplyChanges();
            
            System.Diagnostics.Debug.WriteLine($"[SetGraphicsWidthHeight] ApplyChanges() 完成");
        }

        public static void GraphicsApplyChanges()
        {
            GraphicsDeviceManager.ApplyChanges();
        }

        public override void SetMouseVisible(bool visible)
        {
            MainGame.IsMouseVisible = visible;
        }

        public override void SetWindowAllowUserResizing(bool allow)
        {
            MainGame.Window.AllowUserResizing = true;

            // Use MonoGame's built-in mouse wheel handling instead of WinForms
            // Mouse wheel input is handled through InputManager in MonoGame
            
            // Note: Window maximization and mouse wheel events are handled differently in MonoGame
            // The game window behavior is controlled through GraphicsDeviceManager
        }

        //public override void SetWindowBorder(bool visible)
        //{
        //    Form xnaWindow = (Form)Control.FromHandle((MainGame.Window.Handle));
        //    xnaWindow.FormBorderStyle = visible ? FormBorderStyle.Fixed3D : FormBorderStyle.None;
        //}

        public override Vector2 GetWorkingArea()
        {
            // Use MonoGame's GraphicsAdapter to get screen dimensions
            var adapter = GraphicsAdapter.DefaultAdapter;
            var displayMode = adapter.CurrentDisplayMode;
            return new Vector2(displayMode.Width, displayMode.Height);
        }

        public override void SetFullScreen(bool full)
        {
            GraphicsDeviceManager.IsFullScreen = full;
        }

        public override void SetFullScreen2(bool full)
        {
            // Use MonoGame's built-in fullscreen handling instead of WinForms
            // This method is called for custom fullscreen behavior
            
            // Note: MonoGame handles fullscreen through GraphicsDeviceManager.IsFullScreen
            // Custom window manipulation should use MonoGame APIs when possible
        }

        /// <summary>
        /// 解決方案文件夾
        /// </summary>
        public new string SolutionDir
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory.Split(new string[] { "WorldOfTheThreeKingdoms" }, StringSplitOptions.None)[0];  //.Replace(@"WorldOfTheThreeKingdoms\bin\Win\", "");                
            }
        }

        public override string GetDeviceID()
        {
            try
            {
                string addr = "";
                var sts = GetMacByNetworkInterface();
                if (sts != null && sts.Count > 0)
                {
                    addr = sts[0];
                }
                return addr;
            }
            catch
            {
                return "";
            }
        }

        //返回描述本地计算机上的网络接口的对象(网络接口也称为网络适配器)。
        public static NetworkInterface[] NetCardInfo()
        {
            return NetworkInterface.GetAllNetworkInterfaces();
        }

        ///<summary>
        /// 通过NetworkInterface读取网卡Mac
        ///</summary>
        ///<returns></returns>
        public static List<string> GetMacByNetworkInterface()
        {
            List<string> macs = new List<string>();
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                macs.Add(ni.GetPhysicalAddress().ToString());
            }
            return macs;
        }

        public override string GetDeviceInfo()
        {
            string hostName = "";
            try
            {
                hostName = GraphicsDevice.Adapter.Description.ToString() + " " + System.Net.Dns.GetHostName();
            }
            catch
            {

            }

            return hostName;  // + " " + Session.Resolution;
        }

        public override string GetSystemInfo()
        {
            return System.Environment.OSVersion.Platform + " " + System.Environment.OSVersion.VersionString;
        }

        /// <summary>
        /// 應用程序目錄
        /// </summary>
        public new string ApplicationUrl
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory + "\\";
            }
        }
        /// <summary>
        /// 遊戲完整路徑
        /// </summary>
        public new string GameApplicationUrl
        {
            get
            {
                return ApplicationUrl + "WorldOfTheThreeKingdoms.exe";
            }
        }

        #region 加載資源文件

        /// <summary>
        /// 加載資源文本
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public string LoadText(string res)
        {
            res = res.Replace("\\", "/");

            res = GetMODFile(res);

            lock (Platform.IoLock)
            {
                // 🔥 修复：检查文件是否存在
                if (!File.Exists(res))
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadText] ❌ 文件不存在: {res}");
                    return null;
                }
                
                string content = File.ReadAllText(res);
                
                // 🔥 修复：检查读取的内容
                if (string.IsNullOrEmpty(content))
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadText] ⚠️ 文件内容为空: {res}");
                }
                
                return content;
            }
        }

        /// <summary>
        /// 加載資源文本
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public string[] LoadTexts(string res)
        {
            res = res.Replace("\\", "/");

            res = base.GetMODFile(res);

            lock (Platform.IoLock)
            {
                return File.ReadAllLines(res);
            }
        }
        /// <summary>
        /// 加載資源文件
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public byte[] LoadFile(string res)
        {
            res = res.Replace("\\", "/");

            res = base.GetMODFile(res);

            using (var dest = new MemoryStream())
            {
                lock (Platform.IoLock)
                {
                    using (Stream stream = TitleContainer.OpenStream(res))
                    {
                        stream.CopyTo(dest);
                        return dest.ToArray();
                    }
                }
            }
        }

        #region 處理文件夾事宜


        public override string[] GetDirectories(string dir, bool all, bool full)
        {
            if (Directory.Exists(dir))
            {
                return Directory.GetDirectories(dir, "*.*", all ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获取路径下的文件夹名称列表
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<string> GetDirectoryNames(string path)
        {
            if (Directory.Exists(path))
            {
                string[] dirs = Directory.GetDirectories(path);

                return dirs.Select(x => Path.GetFileName(x)).ToList();
            }

            return new List<string>();
        }

        public override string[] GetDirectoriesBasic(string dir, bool all, bool full)
        {
            return GetDirectories(dir, all, true);
        }

        public override string[] GetFiles(string dir, bool all = false)
        {
            if (DirectoryExists(dir))
            {
                return Directory.GetFiles(dir, "*.*", all ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            else
            {
                return null;
            }
        }

        public override string[] GetFilesBasic(string dir, bool all = false)
        {
            return GetFiles(dir, false);
        }

        public override string ReadAllText(string file)
        {
            return File.ReadAllText(file);
        }

        public override string[] ReadAllLines(string file)
        {
            return File.ReadAllLines(file);
        }

        public override byte[] ReadAllBytes(string file)
        {
            return File.ReadAllBytes(file);
        }

        public override void WriteAllText(string file, string xml1)
        {
            File.WriteAllText(file, xml1, Encoding.UTF8);
        }

        public override void WriteAllBytes(string file, byte[] bytes1)
        {
            File.WriteAllBytes(file, bytes1);
        }

        public override Stream FileOpenWrite(string file, bool write)  //FileStream
        {
            if (write)
            {
                return File.OpenWrite(file) as Stream;
            }
            else
            {
                return File.OpenRead(file) as Stream;
            }
        }

        public override bool FileExists(string file)
        {
            return File.Exists(file);
        }

        public override void FileDelete(string file)
        {
            if (FileExists(file))
            {
                File.Delete(file);
            }
        }

        public override bool DirectoryExists(string dir)
        {
            return Directory.Exists(dir);
        }

        public override void DirectoryCreateDirectory(string dir)
        {
            Directory.CreateDirectory(dir);
        }

        public override string DirectoryName(string dir)
        {
            if (String.IsNullOrEmpty(dir))
            {
                return "";
            }
            else
            {
                return Path.GetDirectoryName(dir);
            }
        }

        public override string GetFileNameFromPath(string file)
        {
            return Path.GetFileName(file);
        }

        #endregion

        public Texture2D LoadTextureFromStream(Stream stream)
        {
            return Texture2D.FromStream(GraphicsDevice, stream);
        }

        public Texture2D LoadTexture(string res)
        {
            return LoadTexture(res, false);
        }

        /// <summary>
        /// 加載資源材質
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public Texture2D LoadTexture(string res, bool isUser)
        {
            try
            {
                if (isUser)
                {
                    if (!UserFileExist(res))
                    {
                        //暫時沒有文件
                        return null;
                    }
                }
                else
                {
                    res = base.GetMODFile(res);
                }

                return LoadTextureWithFormatPriority(res, isUser);
            }
            catch (Exception ex)
            {
                if (res.Contains("yueluo_1.0"))
                {

                }
                else
                {
                    WebTools.TakeWarnMsg("加载游戏材质失败:" + res, "LoadTexture:" + UserApplicationDataPath + res, ex);
                }
                return null;
            }
        }

        /// <summary>
        /// 按格式优先级加载纹理：DDS > PNG > JPG
        /// </summary>
        private Texture2D LoadTextureWithFormatPriority(string path, bool isUser)
        {
            Texture2D texture = null;
            
            // 获取不带扩展名的基础路径
            string basePath = Path.HasExtension(path) ? Path.ChangeExtension(path, null) : path;
            
            // 按优先级尝试不同格式：DDS > PNG > JPG
            string[] extensions = [".dds", ".png", ".jpg"];
            
            foreach (string ext in extensions)
            {
                string fullPath = basePath + ext;
                
                // 🔥 性能优化：使用缓存检查文件是否存在（避免重复 I/O）
                bool fileExists;
                lock (_fileExistsCacheLock)
                {
                    if (!_fileExistsCache.TryGetValue(fullPath, out fileExists))
                    {
                        // 首次检查，执行实际 I/O 并缓存结果
                        fileExists = isUser ? UserFileExist(fullPath) : File.Exists(fullPath);
                        _fileExistsCache[fullPath] = fileExists;
                    }
                }
                
                if (!fileExists) continue;
                
                try
                {
                    lock (Platform.IoLock)
                    {
                        if (ext == ".dds")
                        {
                            // 使用DDS加载器
                            texture = WorldOfTheThreeKingdoms.Helpers.DDSLoader.Load(Platform.GraphicsDevice, fullPath);
                        }
                        else
                        {
                            // 使用标准方法加载PNG、JPG
                            using (var stream = isUser ? LoadUserFileStream(fullPath) : TitleContainer.OpenStream(fullPath))
                            {
                                texture = Texture2D.FromStream(Platform.GraphicsDevice, stream);
                            }
                        }
                        
                        // 如果加载成功，直接返回
                        if (texture != null)
                        {
                            return texture;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 当前格式加载失败，继续尝试下一个格式
                    WebTools.TakeWarnMsg($"加载{ext}格式失败，尝试下一个格式: {fullPath}", "LoadTextureWithFormatPriority", ex);
                    continue;
                }
            }
            
            // 所有格式都失败了
            return null;
        }
        #endregion

        #region 處理用戶文件

        private new string UserApplicationDataPath
        {
            get
            {
                string path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\WorldOfTheThreeKingdoms\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        public bool UserDirectoryExist(string path)
        {
            try
            {
                return DirectoryExists(UserApplicationDataPath + path);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("判斷用戶文件夾失敗:" + path, "UserDirectoryExist:" + UserApplicationDataPath + path, ex);
                return false;
            }
        }

        public void UserDirectoryCreate(string path)
        {
            try
            {
                DirectoryCreateDirectory(UserApplicationDataPath + path);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("創建用戶文件夾失敗:" + path, "UserCreateDirectory:" + UserApplicationDataPath + path, ex);
            }
        }

        /// <summary>
        /// 獲得用戶文件
        /// </summary>
        /// <param name="searchPatterns"></param>
        /// <returns></returns>
        public string[] GetUserFileNames(string dir, string searchPattern)
        {
            try
            {
                lock (Platform.IoLock)
                {
                    var files = Directory.GetFiles(UserApplicationDataPath + dir, searchPattern);
                    if (files != null)
                    {
                        files = files.Select(fi => Path.GetFileName(fi)).ToArray();
                    }
                    return files;
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("获取文件列表失败:" + searchPattern, "GetUserFileNames:" + UserApplicationDataPath, ex);
                return null;
            }
            //var storage = GetIsolatedStorageFile();
            //return storage.GetFileNames(searchPattern);
        }
        /// <summary>
        /// 加載用戶文本
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public string GetUserText(string res)
        {
            try
            {
                lock (Platform.IoLock)
                {
                    if (File.Exists(UserApplicationDataPath + res))
                    {
                        using (var dest = new MemoryStream())
                        {
                            using (Stream stream = File.Open(UserApplicationDataPath + res, FileMode.Open))
                            {
                                using (var streamReader = new StreamReader(stream))
                                {
                                    return streamReader.ReadToEnd();
                                }
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("加载用户文本失败:" + res, "GetUserText:" + UserApplicationDataPath + res, ex);
                return null;
            }
        }
        /// <summary>
        /// 加載用戶文本段
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public string[] GetUserFileString(string res)
        {
            try
            {
                lock (Platform.IoLock)
                {
                    if (File.Exists(UserApplicationDataPath + res))
                    {
                        using (var dest = new MemoryStream())
                        {
                            using (Stream stream = File.Open(UserApplicationDataPath + res, FileMode.Open))
                            {
                                using (var streamReader = new StreamReader(stream))
                                {
                                    List<string> list = new List<string>();
                                    while (!streamReader.EndOfStream)
                                    {
                                        list.Add(streamReader.ReadLine());
                                    }
                                    return list.ToArray();
                                }
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("加载用户文本失败:" + res, "GetUserFileString:" + UserApplicationDataPath + res, ex);
                return null;
            }
        }
        /// <summary>
        /// 加載用戶文件
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public byte[] GetUserFile(string res)
        {
            try
            {
                lock (Platform.IoLock)
                {
                    if (File.Exists(UserApplicationDataPath + res))
                    {
                        using (var dest = new MemoryStream())
                        {
                            using (Stream stream = File.Open(UserApplicationDataPath + res, FileMode.Open))
                            {
                                stream.CopyTo(dest);
                                return dest.ToArray();
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("加载用户文件失败:" + res, "GetUserFile:" + UserApplicationDataPath + res, ex);
                return null;
            }
        }
        ///// <summary>
        ///// 獲取用戶鍵值
        ///// </summary>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //public object GetUserValueByKey(string key)
        //{
        //    try
        //    {
        //        string[] strings = GetUserFileString("settings.config");
        //        if (strings != null)
        //        {
        //            string value = strings.FirstOrDefault(st => st.Contains("=") && st.Split('=')[0] == key);
        //            if (!String.IsNullOrEmpty(value))
        //            {
        //                return value.Split('=')[1];
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        WebTools.TakeWarnMsg("获取用户键值失败:" + key, "GetUserValueByKey:", ex);
        //    }
        //    return null;
        //}
        public Stream LoadUserFileStream(string res)
        {
            return LoadUserFileStream(res, false);
        }
        /// <summary>
        /// 加載用戶文件流
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public Stream LoadUserFileStream(string res, bool write)
        {
            lock (Platform.IoLock)
            {
                if (write || File.Exists(UserApplicationDataPath + res))
                {
                    return File.Open(UserApplicationDataPath + res, write ? FileMode.OpenOrCreate : FileMode.Open);
                }
                else
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// 加載用戶材質
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public Texture2D LoadUserTexture(string res)
        {
            try
            {
                using (var stream = Current.LoadUserFileStream(res))
                {
                    if (stream == null)
                    {
                        return null;
                    }
                    Texture2D tex = Texture2D.FromStream(Platform.GraphicsDevice, stream);
                    return tex;
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("加载用户材质失败:" + res, "LoadUserTexture:" + UserApplicationDataPath + res, ex);
                return null;
            }
        }

        /// <summary>
        /// 判断用户文件是否存在
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public List<bool> UserFileExist(List<string> files)
        {
            if (files.Count == 0) return new List<bool> { false };

            var result = new List<bool>();
            foreach (var file in files)
            {
                var filePath = file.Trim();
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    var isExist = File.Exists(UserApplicationDataPath + filePath);

                    result.Add(isExist);
                }
                else
                {
                    result.Add(false);
                }
            }

            return result;
        }

        /// <summary>
        /// 判断用户文件是否存在
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool UserFileExist(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) return false;

            var filePath = file.Trim();

            var isExist = File.Exists(UserApplicationDataPath + filePath);

            return isExist;
        }

        /// <summary>
        /// 判断用户文件是否存在 (string[] 版本，兼容其他平台)
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public bool[] UserFileExist(string[] res)
        {
            if (res == null || res.Length == 0)
            {
                return new bool[] { false };
            }

            var result = new bool[res.Length];
            for (int i = 0; i < res.Length; i++)
            {
                result[i] = UserFileExist(res[i]);
            }
            return result;
        }

        /// <summary>
        /// 保存用戶文本
        /// </summary>
        /// <param name="res"></param>
        /// <param name="content"></param>
        public void SaveUserFile(string res, string content, bool fullPathProvided = false)
        {
            try
            {
                DelUserFiles(new string[] { res }, null);
                lock (Platform.IoLock)
                {
                    String path = res;
                    if (!fullPathProvided)
                    {
                        path = UserApplicationDataPath + res;
                    }
                    File.WriteAllText(path, content);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                throw ex;
#else
                WebTools.TakeWarnMsg("保存用户文本失败:" + res, "SaveUserFile:" + UserApplicationDataPath + res, ex);
#endif
            }
        }
        ///// <summary>
        ///// 保存用戶鍵值
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="o"></param>
        //public void SaveUserValueByKey(string key, object o)
        //{
        //    string[] strings = GetUserFileString("settings.config");
        //    if (strings == null)
        //    {
        //        strings = new string[] { key + "=" + o.ToString() };
        //    }
        //    else
        //    {
        //        bool exist = false;
        //        for (int i = 0; i < strings.Length; i++)
        //        {
        //            var str = strings[i];
        //            if (str.Contains("="))
        //            {
        //                string[] data = str.Split('=');
        //                if (data[0] == key)
        //                {
        //                    exist = true;
        //                    data[1] = o.ToString();
        //                    strings[i] = key + "=" + o.ToString();
        //                }
        //            }
        //        }
        //        if (!exist)
        //        {
        //            strings = strings.Union(new string[] { key + "=" + o.ToString() }).ToArray();
        //        }
        //    }
        //    SaveUserFile("settings.config", String.Join("\r\n", strings));
        //}
        /// <summary>
        /// 保存用戶文件
        /// </summary>
        /// <param name="res"></param>
        /// <param name="bytes"></param>
        public void SaveUserFile(string res, byte[] bytes, PlatformTask action)
        {
            try
            {
                DelUserFiles(new string[] { res }, null);
                //File.WriteAllBytes(UserApplicationDataPath + res, bytes);
                lock (Platform.IoLock)
                {
                    using (var isolatedFileStream = File.OpenWrite(UserApplicationDataPath + res))
                    {
                        using (var fileWriter = new BinaryWriter(isolatedFileStream))
                        {
                            fileWriter.Write(bytes);
                        }
                    }
                }
                if (action != null)
                {
                    action.Start();
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("保存用户文件失败:" + res, "SaveUserFile:bytes" + bytes.Length + UserApplicationDataPath + res, ex);
            }
        }
        
        /// <summary>
        /// 刪除用戶文件
        /// </summary>
        /// <param name="files"></param>
        public void DelUserFiles(string[] files, PlatformTask action)
        {
            foreach (string file in files)
            {
                if (UserFileExist(file))
                {
                    try
                    {
                        lock (Platform.IoLock)
                        {
                            File.Delete(UserApplicationDataPath + file.Trim());
                        }
                    }
                    catch (Exception ex)
                    {
                        WebTools.TakeWarnMsg("删除用户文件失败:" + file, "File.Delete:" + UserApplicationDataPath + file, ex);
                    }
                }
                if (action != null)
                {
                    action.Start();
                }
            }
        }
        /// <summary>
        /// 獲取獨立存取文件
        /// </summary>
        /// <returns></returns>
        /*
        private IsolatedStorageFile GetIsolatedStorageFile()
        {
            return IsolatedStorageFile.GetUserStoreForApplication();
        }
        */
#endregion

        public static void Sleep(int time)
        {
            Thread.Sleep(time);
        }

        public override void OpenMarket(string key)
        {
            
        }

        public override void OpenReview(string key)
        {
            
        }

        public override byte[] ScreenShot(GraphicsDevice graphicsDevice, RenderTarget2D screenshot)
        {
            graphicsDevice.SetRenderTarget(null);
            byte[] shot = null;
            using (MemoryStream ms = new MemoryStream())
            {
                screenshot.SaveAsJpeg(ms, screenshot.Width, screenshot.Height);
                screenshot.Dispose();
                shot = ms.ToArray(); //.GetBuffer();                
            }
            //screenshot = new RenderTarget2D(graphicsDevice, 800, 480, false, SurfaceFormat.Color, DepthFormat.None);
            //screenshot = new RenderTarget2D(Season.GraphicsDevice, Season.GraphicsDevice.Viewport.Width, Season.GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.None);
            return shot;
        }
        /// <summary>
        /// 調用WebService (post方式)
        /// </summary>
        /// <param name="strURL"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string GetWebServicePost(string strURL, string data, PlatformTask onUpload)
        {
            var client = new WebClient();
            string result = null;
            byte[] sendData = Encoding.GetEncoding("UTF-8").GetBytes(data);

            //byte[] sendData = Season.Current.ReadAllBytes(@"C:\Projects\a.mp4");

            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            client.Headers.Add("ContentLength", sendData.Length.ToString());

            if (onUpload != null)
            {
                client.UploadProgressChanged += (sender, e) =>
                {
                    //UploadProgressChangedEventArgs
                    onUpload.ParamArray = new string[] { e.ProgressPercentage.ToString() };
                };
            }

            byte[] recData = client.UploadData(strURL, "POST", sendData);

            MemoryStream stream = new MemoryStream(recData);
            XmlTextReader reader = new XmlTextReader(stream);
            reader.MoveToContent();
            result = reader.ReadInnerXml();
            reader.Close();
            stream.Close();
            //string result = Encoding.ASCII.GetString(response);
            //return result;
            result = result.Replace("&lt;", "<").Replace("&gt;", ">");
            return result;
        }
        
        /// <summary>
        /// 調用WebService
        /// </summary>
        /// <param name="strURL"></param>
        /// <returns></returns>
        public string GetWebService(string strURL)
        {
            if (!OpenWeb)
            {
                return "";
            }
            else
            {
                var client = new WebClient();
                string result = null;
                byte[] response = client.DownloadData(new Uri(strURL));
                MemoryStream stream = new MemoryStream(response);
                XmlTextReader reader = new XmlTextReader(stream);
                reader.MoveToContent();
                result = reader.ReadInnerXml();
                reader.Close();
                stream.Close();
                //string result = Encoding.ASCII.GetString(response);
                //return result;
                result = result.Replace("&lt;", "<").Replace("&gt;", ">");
                return result;
            }
            //创建一个HTTP请求
            //var client = new WebClient();
            //client.DownloadStringCompleted += (s, ev) => 
            //{ 
            //responseTextBlock.Text = ev.Result; 
            //};
            //client.DownloadStringAsync(new Uri(strURL));

            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strURL);
            ////request.Method="get";
            //HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            //Stream s = response.GetResponseStream();
            ////转化为XML，自己进行处理
            //XmlTextReader Reader = new XmlTextReader(s);
            //Reader.MoveToContent();
            //string strValue = Reader.ReadInnerXml();
            //strValue = strValue.Replace("&lt;", "<");
            //strValue = strValue.Replace("&gt;", ">");
            ////MessageBox.Show(strValue);
            //Reader.Close();
            //request.Abort();
            //response.Close();
            //return "";
        }

        public void DownloadWebData(string file, PlatformTask action)
        {
            var client = new WebClient();
            byte[] result = client.DownloadData(new Uri(file));
            if (action != null)
            {
                action.ParamArrayResultBytes = result;
                action.Start();
            }
        }

        public void OpenLink(string link)
        {
            try
            {
                //"IExplore.exe " + 
                ProcessStartInfo startInfo = new ProcessStartInfo(link);
                //startInfo.WindowStyle = ProcessWindowStyle.Minimized;
                startInfo.UseShellExecute = true;
                Process.Start(startInfo);
            }
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
            catch (Exception ex)
#pragma warning restore CS0168 // The variable 'ex' is declared but never used
            {
                //SeasonTools.SendErrMsg("ProcessStartInfo打開Web出錯：", ex);
                //try
                //{
                //    Type tIE; object oIE;
                //    object[] oParameter = new object[1];
                //    tIE = Type.GetTypeFromProgID("InternetExplorer.Application");
                //    oIE = Activator.CreateInstance(tIE);
                //    oParameter[0] = (bool)true;
                //    tIE.InvokeMember("Visible", BindingFlags.SetProperty, null, oIE, oParameter);
                //    oParameter[0] = link;
                //    tIE.InvokeMember("Navigate2", BindingFlags.InvokeMethod, null, oIE, oParameter);
                //}
                //catch (Exception ex2)
                //{
                //    SeasonTools.SendErrMsg("InvokeMember打開Web出錯：", ex2);
                //}
            }
        }

        public override void InitInputCapturer()
        {
            // wic = new WindowInputCapturer(MainGame.Window.Handle); commented out
        }

        public override List<Character> GetChars()
        {
            return new List<Character>(); // WindowInputCapturer.myCharacters commented out
        }

        public override void ClearChars()
        {
            // WindowInputCapturer.myCharacters.Clear(); commented out
        }

        public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            //Session.Current.err = e.ToString();
            //if (Season.PlatForm == PlatForm.Win)
            //{
            //    Season.Current.OpenLink(WebTools.WebSite2 + "/service.aspx?mes=" + e.ToString() + "&platform=" + Season.PlatForm.ToString());
            //}
            //SeasonTools.SendErrMsg("RuntimeTerminating: " + args.IsTerminating, e);
        }

        public override void Exit()
        {
            try
            {
                MainGame.Exit();
            }
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
            catch (Exception ex)
#pragma warning restore CS0168 // The variable 'ex' is declared but never used
            {
                //退出失敗，當不要緊
            }
        }

        public static string picStatus = "";

        static float[] rotates = new float[] { 0f, (float)Math.PI / 2.0f, (float)Math.PI, (float)Math.PI * 3.0f / 2.0f };

        public override void ChoosePicture(PlatformTask action)
        {
            picStatus = "ChoosePicture";

            // Note: OpenFileDialog requires System.Windows.Forms which is not available in .NET 8
            // This functionality would need to be replaced with a cross-platform file picker
            // For now, we'll provide a placeholder implementation
            
            // TODO: Implement cross-platform file picker using:
            // - MonoGame.Framework.DesktopGL native dialogs
            // - Or a third-party cross-platform file dialog library
            
            if (action != null)
            {
                // Return empty result for now
                action.ParamArrayResult = new string[] { "" };
                action.ParamArrayResultBytes = new byte[0];
                action.Start();
            }
        }



        static byte[] TextureToPngBytes(Texture2D tex)
        {
            using (var ms = new MemoryStream())
            {
                tex.SaveAsPng(ms, tex.Width, tex.Height);
                return ms.ToArray();
            }
        }

        public override void MirrorPicture(byte[] image, PlatformTask action)
        {
            try
            {
                using (var ms = new MemoryStream(image))
                {
                    using (var tex = Texture2D.FromStream(Platform.GraphicsDevice, ms))
                    {
                        Color[] data = new Color[tex.Width * tex.Height];
                        tex.GetData(data);
                        Color[] newData = new Color[tex.Width * tex.Height];

                        // Flip X: Reverse pixels in each row
                        for (int y = 0; y < tex.Height; y++)
                        {
                            for (int x = 0; x < tex.Width; x++)
                            {
                                newData[y * tex.Width + (tex.Width - 1 - x)] = data[y * tex.Width + x];
                            }
                        }

                        using (var resultTex = new Texture2D(Platform.GraphicsDevice, tex.Width, tex.Height))
                        {
                            resultTex.SetData(newData);
                            var bytes = TextureToPngBytes(resultTex);

                            if (action != null)
                            {
                                action.ParamArrayResultBytes = bytes;
                                action.Start();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                 WebTools.TakeWarnMsg("MirrorPicture failed: " + ex.Message, "MirrorPicture", ex);
            }
        }

        public override void RotatePicture(byte[] image, int rotate, PlatformTask action)
        {
             try
            {
                using (var ms = new MemoryStream(image))
                {
                    using (var tex = Texture2D.FromStream(Platform.GraphicsDevice, ms))
                    {
                        Color[] data = new Color[tex.Width * tex.Height];
                        tex.GetData(data);
                        
                        int newW = tex.Width;
                        int newH = tex.Height;
                        
                        // 1 = 90 deg, 2 = 180 deg, 3 = 270 deg (assuming clockwise to match typical GDI+ behavior if strictly mapped)
                        // Note: GDI+'s RotateFlipType.Rotate90FlipNone is 90 clockwise.
                        if (rotate == 1 || rotate == 3)
                        {
                            newW = tex.Height;
                            newH = tex.Width;
                        }
                        
                        Color[] newData = new Color[newW * newH];

                        for (int y = 0; y < tex.Height; y++)
                        {
                            for (int x = 0; x < tex.Width; x++)
                            {
                                int newX = x;
                                int newY = y;
                                
                                if (rotate == 1) // 90 deg CW
                                {
                                    newX = tex.Height - 1 - y;
                                    newY = x;
                                }
                                else if (rotate == 2) // 180 deg
                                {
                                    newX = tex.Width - 1 - x;
                                    newY = tex.Height - 1 - y;
                                }
                                else if (rotate == 3) // 270 deg CW
                                {
                                    newX = y;
                                    newY = tex.Width - 1 - x;
                                }
                                
                                newData[newY * newW + newX] = data[y * tex.Width + x];
                            }
                        }

                        using (var resultTex = new Texture2D(Platform.GraphicsDevice, newW, newH))
                        {
                            resultTex.SetData(newData);
                            var bytes = TextureToPngBytes(resultTex);

                            if (action != null)
                            {
                                action.ParamArrayResultBytes = bytes;
                                action.Start();
                            }
                        }
                    }
                }
            }
             catch (Exception ex)
            {
                 WebTools.TakeWarnMsg("RotatePicture failed: " + ex.Message, "RotatePicture", ex);
            }
        }

        public override void CropPicture(byte[] image, int x, int y, int width, int height, PlatformTask action)
        {
            // Get your texture
            //Texture2D texture = Content.Load<Texture2D>("myTexture");

            // Calculate the cropped boundary
            Microsoft.Xna.Framework.Rectangle newBounds = new Microsoft.Xna.Framework.Rectangle(x, y, width, height); // avatar.Bounds;
            //newBounds.X -= Convert.ToInt32(posExts.X);
            //newBounds.Y -= Convert.ToInt32(posExts.Y);
            //const int resizeBy = 20;
            //newBounds.X += resizeBy;
            //newBounds.Y += resizeBy;
            //newBounds.Width -= resizeBy * 2;
            //newBounds.Height -= resizeBy * 2;

            // Create a new texture of the desired size
            var croppedPicture = new Texture2D(Platform.GraphicsDevice, newBounds.Width, newBounds.Height);

            // Copy the data from the cropped region into a buffer, then into the new texture
            var data = new Microsoft.Xna.Framework.Color[newBounds.Width * newBounds.Height];

            using (var memo = new MemoryStream(image))
            {
                var tex = Texture2D.FromStream(GraphicsDevice, memo);
                tex.GetData(0, newBounds, data, 0, newBounds.Width * newBounds.Height);
                croppedPicture.SetData(data);
                var memo2 = new MemoryStream();
                croppedPicture.SaveAsPng(memo2, width, height);
                var bytes = memo2.ToArray();
                if (action != null)
                {
                    action.ParamArrayResultBytes = bytes;
                    action.Start();
                }
                //Color[] colors = new Color[] { Color.White };
                //texture = new Texture2D(GraphicsDevice, 1, 1);
                //texture.SetData<Color>(colors);
            }

        }

        /// <summary>
        /// 壓縮圖片
        /// </summary>
        /// <param name="imageFile"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public override void ResizeImageFile(byte[] imageFile, int targetSizeWidth, int targetSizeHeight, bool sameRatio, PlatformTask action)
        {
            byte[] pic = null;

            try
            {
                using (var ms = new MemoryStream(imageFile))
                {
                    using (var tex = Texture2D.FromStream(Platform.GraphicsDevice, ms))
                    {
                        int newWidth = targetSizeWidth;
                        int newHeight = targetSizeHeight;

                        if (sameRatio)
                        {
                            float scale = GameTools.AutoSetScale(tex.Width, tex.Height, targetSizeWidth, targetSizeHeight);
                            newWidth = Convert.ToInt32(tex.Width * scale);
                            newHeight = Convert.ToInt32(tex.Height * scale);
                        }
                        
                        if (newWidth <= 0) newWidth = 1;
                        if (newHeight <= 0) newHeight = 1;

                        Color[] data = new Color[tex.Width * tex.Height];
                        tex.GetData(data);
                        
                        Color[] newData = new Color[newWidth * newHeight];
                        
                        // Bilinear interpolation
                        float xRatio = ((float)(tex.Width - 1)) / newWidth;
                        float yRatio = ((float)(tex.Height - 1)) / newHeight;
                        
                        for (int i = 0; i < newHeight; i++)
                        {
                            for (int j = 0; j < newWidth; j++)
                            {
                                int x = (int)(xRatio * j);
                                int y = (int)(yRatio * i);
                                float xDiff = (xRatio * j) - x;
                                float yDiff = (yRatio * i) - y;
                                
                                int index = y * tex.Width + x;
                                
                                // Safe bounds check
                                int indexA = index;
                                int indexB = (x + 1 < tex.Width) ? index + 1 : index;
                                int indexC = (y + 1 < tex.Height) ? index + tex.Width : index;
                                int indexD = (y + 1 < tex.Height && x + 1 < tex.Width) ? index + tex.Width + 1 : indexC;

                                Color a = data[indexA];
                                Color b = data[indexB];
                                Color c = data[indexC];
                                Color d = data[indexD];
                                
                                byte R = (byte)((a.R * (1 - xDiff) * (1 - yDiff) + b.R * xDiff * (1 - yDiff) + c.R * (1 - xDiff) * yDiff + d.R * xDiff * yDiff));
                                byte G = (byte)((a.G * (1 - xDiff) * (1 - yDiff) + b.G * xDiff * (1 - yDiff) + c.G * (1 - xDiff) * yDiff + d.G * xDiff * yDiff));
                                byte B = (byte)((a.B * (1 - xDiff) * (1 - yDiff) + b.B * xDiff * (1 - yDiff) + c.B * (1 - xDiff) * yDiff + d.B * xDiff * yDiff));
                                byte A = (byte)((a.A * (1 - xDiff) * (1 - yDiff) + b.A * xDiff * (1 - yDiff) + c.A * (1 - xDiff) * yDiff + d.A * xDiff * yDiff));
                                
                                newData[i * newWidth + j] = new Color(R, G, B, A);
                            }
                        }

                        using (var resultTex = new Texture2D(Platform.GraphicsDevice, newWidth, newHeight))
                        {
                            resultTex.SetData(newData);
                            pic = TextureToPngBytes(resultTex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("缩放图像失败:" + ex.Message.NullToString(), "ResizeImageFile", ex);
            }

            if (action != null)
            {
                action.ParamArrayResultBytes = pic;
                action.Start();
            }
        }

        public override void CropResizePicture(byte[] image, int x, int y, int width, int height, int targetSizeWidth, int targetSizeHeight, bool sameRatio, PlatformTask action)
        {
            byte[] pic = null;
            try
            {
                Microsoft.Xna.Framework.Rectangle newBounds = new Microsoft.Xna.Framework.Rectangle(x, y, width, height);

                var croppedPicture = new Texture2D(Platform.GraphicsDevice, newBounds.Width, newBounds.Height);

                var data = new Microsoft.Xna.Framework.Color[newBounds.Width * newBounds.Height];

                using (var memo = new MemoryStream(image))
                {
                    var tex = Texture2D.FromStream(GraphicsDevice, memo);
                    tex.GetData(0, newBounds, data, 0, newBounds.Width * newBounds.Height);
                    croppedPicture.SetData(data);
                    var memo2 = new MemoryStream();
                    croppedPicture.SaveAsPng(memo2, width, height);
                    pic = memo2.ToArray();
                }

                ResizeImageFile(pic, targetSizeWidth, targetSizeHeight, sameRatio, action);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("裁切缩放失败:" + ex.Message.NullToString(), "ResizeImageFile", ex);
            }
        }

    }

    public class PlatformTask
    {
        Action act;
        public bool IsStop = false;
        public string[] ParamArray = null;
        public string[] ParamArrayResult = null;
        public byte[] ParamArrayResultBytes = null;
        public AsyncCallback OnStartFinish = null;
        public PlatformTask(Action action)
        {
            act = action;
        }
        public bool IsAlive
        {
            get
            {
                return act != null;
            }
        }

        public void Abort()
        {
            IsStop = true;
        }
        public void Start()
        {
            Task.Run(() => 
            {
                act();
            }).ContinueWith(t => 
            {
                if (OnStartFinish != null)
                {
                    OnStartFinish(t);
                }
            });
        }
    }

    public class PlatformTask2
    {
        Thread thread;
        public PlatformTask2(Action action)
        {
            thread = new Thread(() => { action.Invoke(); });
        }
        public void Start()
        {
            thread.Start();
        }
    }

    public sealed class IMM
    {
        [DllImport("imm32.dll", CharSet = CharSet.Auto)]
        public extern static IntPtr ImmGetContext(IntPtr hWnd);
        [DllImport("imm32.dll", CharSet = CharSet.Auto)]
        public extern static IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);

    }

    public static class WindowMessage
    {
        public const int ImeSetContext = 0x0281;
        public const int InputLanguageChange = 0x0051;
    }

    /*
    // WindowInputCapturer class commented out - requires System.Windows.Forms
    // This needs to be replaced with MonoGame input handling
    public sealed class WindowInputCapturer : NativeWindow, IDisposable
    {
        //自定义字符串输出类
        //由于大部分输入法都支持输出词组，
        //所以这里使用List装载，不然每次只能获取一个字符
        //--by fhmsha
        public static List<Character> myCharacters = new List<Character>();

        public static bool Enable = false;

        private const int DLGC_WANTCHARS = 0x0080;

        private const int DLGC_WANTALLKEYS = 0x0004;

        private enum WindowMessages : int
        {
            WM_GETDLGCODE = 0x0087,
            WM_CHAR = 0x0102,
        }

        public WindowInputCapturer(IntPtr windowHandle)
        {
            AssignHandle(windowHandle);
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                ReleaseHandle();
                this.disposed = true;
            }
        }
        IntPtr context = IntPtr.Zero;

        protected override void WndProc(ref Message message)
        {
            //clayman
            if (message.Msg == WindowMessage.InputLanguageChange)
            {
                return; //Don't pass this message to base class!!!!
            }
            if (message.Msg == WindowMessage.ImeSetContext)
            {
                if (message.WParam.ToInt32() == 1)
                {
                    IntPtr imeContext = IMM.ImmGetContext(this.Handle);
                    if (context == IntPtr.Zero)
                        context = imeContext;
                    IMM.ImmAssociateContext(this.Handle, context);
                }
            }
            base.WndProc(ref message);

            if (Enable)
            {
                switch (message.Msg)
                {
                    case (int)WindowMessages.WM_GETDLGCODE:
                        {
                            if (Is32Bit)
                            {
                                int returnCode = message.Result.ToInt32();
                                returnCode |= (DLGC_WANTALLKEYS | DLGC_WANTCHARS);
                                message.Result = new IntPtr(returnCode);
                            }
                            else
                            {
                                long returnCode = message.Result.ToInt64();
                                returnCode |= (DLGC_WANTALLKEYS | DLGC_WANTCHARS);
                                message.Result = new IntPtr(returnCode);
                            }

                            break;
                        }
                    case (int)WindowMessages.WM_CHAR:
                        {
                            int charInt = message.WParam.ToInt32();
                            Character myCharacter = new Character();
                            myCharacter.IsUsed = false;
                            myCharacter.Chars = (char)charInt;
                            //汉字的unicode编码范围是4e00-9fa5（19968至40869）
                            //全/半角标点可以查看charInt输出
                            switch (charInt)
                            {
                                case 8:
                                    myCharacter.CharaterType = characterType.BackSpace;
                                    break;
                                case 9:
                                    myCharacter.CharaterType = characterType.Tab;
                                    break;
                                case 13:
                                    myCharacter.CharaterType = characterType.Enter;
                                    break;
                                case 27:
                                    myCharacter.CharaterType = characterType.Esc;
                                    break;
                                default:
                                    myCharacter.CharaterType = characterType.Char;
                                    break;
                            }
                            myCharacters.Add(myCharacter);
                            break;
                        }
                }
            }
        }

        private static bool Is32Bit
        {
            get { return (IntPtr.Size == 4); }
        }
        private bool disposed;

    }
    */
}
