using Microsoft.Xna.Framework.Content;
using Tools;
using WorldOfTheThreeKingdoms.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Platforms;
using WorldOfTheThreeKingdoms.GameGlobal;
using WorldOfTheThreeKingdoms.Serialization;

namespace GameManager
{
    [DataContract]
    public class Setting
    {
        [DataMember]
        public string UserGuid { get; set; }
        [DataMember]
        public string DeviceID { get; set; }
        [DataMember]
        public string Language { get; set; }
        [DataMember]
        public int? MusicVolume { get; set; }
        [DataMember]
        public int? SoundVolume { get; set; }
        [DataMember]
        public string DisplayMode { get; set; }
        [DataMember]
        public string Resolution { get; set; }
        [DataMember]
        public string GamerName { get; set; }
        //[DataMember]
        //public string Difficulty { get; set; }
        //[DataMember]
        //public string BattleSpeed { get; set; }
        [DataMember]
        public int? SpeedUp { get; set; }
        [DataMember]
        public bool Chuchangsuiji { get; set; }

        [DataMember]
        public string MOD { get; set; }

        /// <summary>
        /// 头像包
        /// </summary>
        [DataMember]
        public string PortraitPack { get; set; }

        public string MODRuntime
        {
            get
            {
                if (Session.Current == null || Session.Current.Scenario == null || Session.Current.Scenario.MOD == null)
                {
                    return Setting.Current.MOD;
                }
                else
                {
                    return Session.Current.Scenario.MOD;
                }
            }
        }

        [DataMember]
        public GlobalVariables GlobalVariables { get; set; }

        public static Setting Current = null;

        public Setting()
        {

        }

        public static void Init(bool prepare)
        {
            string file = "Settings.json";
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);

            // 🔥 技术性修复：确保Current对象始终被初始化，避免ArgumentNullException
            if (Current == null)
            {
                Current = new Setting();
            }

            if (System.IO.File.Exists(path))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(path);
                    var loadedSetting = SimpleSerializer.DeserializeJson<Setting>(json);
                    if (loadedSetting != null)
                    {
                        Current = loadedSetting;
                    }

                    if (prepare)
                    {
                        Prepare();
                    }

                    // Save(); // Auto-save on load might not be desired, but keeping original logic structure if needed. Removed to avoid redundant write.
                }
                catch (Exception ex)
                {
                    // 🔥 技术性修复：异常时确保有默认设置，避免后续空引用
                    System.Diagnostics.Debug.WriteLine($"[Setting] 初始用户设置失败: {ex.Message}");
                    WebTools.TakeWarnMsg("Initial settings failed:Settings.json", "Init:", ex);
                    
                    // 确保有默认设置
                    if (Current == null)
                    {
                        Current = new Setting();
                    }
                    
                    if (prepare)
                    {
                        try
                        {
                            Prepare();
                        }
                        catch (Exception prepareEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Setting] Prepare失败: {prepareEx.Message}");
                        }
                    }
                }
            }
            else
            {
               // Fallback to check legacy Setting.config if Settings.json doesn't exist
               string legacyFile = "Setting.config";
               
               // 🔥 技术性修复：安全检查文件存在性，避免ArgumentOutOfRangeException
               bool legacyFileExists = false;
               try
               {
                   var existResults = Platform.Current.UserFileExist(new string[] { legacyFile });
                   legacyFileExists = existResults != null && existResults.Length > 0 && existResults[0];
               }
               catch (Exception ex)
               {
                   System.Diagnostics.Debug.WriteLine($"[Setting] 检查遗留文件存在性失败: {ex.Message}");
                   legacyFileExists = false;
               }
               
               if (legacyFileExists)
               {
                    try
                    {
                        var loadedSetting = SimpleSerializer.DeserializeJsonFile<Setting>(legacyFile, true, false);
                        if (loadedSetting != null)
                        {
                            Current = loadedSetting;
                        }
                        if (prepare) Prepare();
                        Save(); // Save to new format
                    }
                     catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Setting] 初始遗留用户设置失败: {ex.Message}");
                        WebTools.TakeWarnMsg("Initial legacy settings failed:Setting.config", "Init:", ex);
                        
                        // 确保有默认设置
                        if (Current == null)
                        {
                            Current = new Setting();
                        }
                        
                        if (prepare)
                        {
                            try
                            {
                                Prepare();
                            }
                            catch (Exception prepareEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Setting] Prepare失败: {prepareEx.Message}");
                            }
                        }
                    }
               }
               else
               {
                   // 🔥 技术性修复：配置文件不存在时，确保有默认设置
                   if (prepare)
                   {
                       try
                       {
                           Prepare();
                       }
                       catch (Exception prepareEx)
                       {
                           System.Diagnostics.Debug.WriteLine($"[Setting] Prepare失败: {prepareEx.Message}");
                       }
                   }
               }
            }
        
            if (Current == null)
            {
                Current = new Setting();
                if (prepare) 
                {
                    try
                    {
                        Prepare();
                    }
                    catch (Exception prepareEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Setting] 最终Prepare失败: {prepareEx.Message}");
                    }
                }
                
                try
                {
                    Save();
                }
                catch (Exception saveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Setting] Save失败: {saveEx.Message}");
                }
            }
        }

        /// <summary>
        /// 异步初始化设置
        /// </summary>
        public static async Task InitAsync(bool prepare, CancellationToken cancellationToken = default)
        {
            string file = "Settings.json";
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);

            // 🔥 技术性修复：确保Current对象始终被初始化，避免ArgumentNullException
            if (Current == null)
            {
                Current = new Setting();
            }

            if (System.IO.File.Exists(path))
            {
                try
                {
                    string json = await System.IO.File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                    var loadedSetting = await SimpleSerializer.DeserializeJsonAsync<Setting>(json).ConfigureAwait(false);
                    if (loadedSetting != null)
                    {
                        Current = loadedSetting;
                    }

                    if (prepare)
                    {
                        Prepare();
                    }
                }
                catch (Exception ex)
                {
                    // 🔥 技术性修复：异常时确保有默认设置，避免后续空引用
                    System.Diagnostics.Debug.WriteLine($"[Setting] 异步初始用户设置失败: {ex.Message}");
                    WebTools.TakeWarnMsg("Initial settings failed:Settings.json", "InitAsync:", ex);
                    
                    // 确保有默认设置
                    if (Current == null)
                    {
                        Current = new Setting();
                    }
                    
                    if (prepare)
                    {
                        try
                        {
                            Prepare();
                        }
                        catch (Exception prepareEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Setting] Prepare失败: {prepareEx.Message}");
                        }
                    }
                }
            }
            else
            {
                if (prepare)
                {
                    try
                    {
                        Prepare();
                    }
                    catch (Exception prepareEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Setting] Prepare失败: {prepareEx.Message}");
                    }
                }
            }
            
            if (Current == null)
            {
                Current = new Setting();
                if (prepare)
                {
                    try
                    {
                        Prepare();
                    }
                    catch (Exception prepareEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Setting] 最终Prepare失败: {prepareEx.Message}");
                    }
                }
                
                try
                {
                    await SaveAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception saveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Setting] SaveAsync失败: {saveEx.Message}");
                }
            }
        }

        public static void Save()
        {
            try 
            {
                string file = "Settings.json";
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                
                string json = SimpleSerializer.SerializeJson(Setting.Current, false, true);
                
                System.IO.File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("Save settings failed", "Save", ex);
            }
        }

        /// <summary>
        /// 异步保存设置到文件
        /// </summary>
        public static async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try 
            {
                string file = "Settings.json";
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                
                string json = await SimpleSerializer.SerializeJsonAsync(Setting.Current, false, true).ConfigureAwait(false);
                
                await System.IO.File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("Save settings failed", "SaveAsync", ex);
            }
        }

        static void Prepare()
        {
            // 🔥 技术性修复：确保Current对象存在，避免ArgumentNullException
            if (Current == null)
            {
                Current = new Setting();
            }
            
            try
            {
                if (String.IsNullOrEmpty(Current.UserGuid))
                {
                    Current.UserGuid = Guid.NewGuid().ToString();
                }

                if (String.IsNullOrEmpty(Current.DeviceID))
                {
                    try
                    {
                        Current.DeviceID = Platform.Current.GetDeviceID();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Setting] 获取设备ID失败: {ex.Message}");
                        Current.DeviceID = "Unknown";
                    }
                }

                if (String.IsNullOrEmpty(Current.DisplayMode))
                {
                    try
                    {
                        Current.DisplayMode = Platform.Current.PreferFullMode;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Setting] 获取显示模式失败: {ex.Message}");
                        Current.DisplayMode = "Window";
                    }
                }
                
                if (Current.MusicVolume == null)
                {
                    Current.MusicVolume = 70;
                }
                if (Current.SoundVolume == null)
                {
                    Current.SoundVolume = 50;
                }
                if (Current.SpeedUp == null)
                {
                    Current.SpeedUp = 1;
                }
                // Chuchangsuiji is already a bool, no need to check if it's null or empty
                // Just ensure it has a default value if needed
                // Current.Chuchangsuiji = false; // This line is redundant since bool has default value false
    
                //if (Current.NewsBoard == null)
                //{
                //    Current.NewsBoard = new NewsBoard() { Detail = "游戏公告加载中，请稍候……" };
                //}

                if (String.IsNullOrEmpty(Current.Language))
                {
                    string name = "";
                    try
                    {
                        name = Platform.Current.CurrentLanguage; // System.Globalization.CultureInfo.InstalledUICulture.Name;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Setting] 获取系统语言失败: {ex.Message}");
                        //獲取系統語言失敗
                    }
                    if (name.ToLower().Contains("cn"))  // == "zh-cn")
                    {
                        Current.Language = "cn";
                    }
                    else
                    {
                        Current.Language = "tw";
                    }
                }

                if (Current.GlobalVariables == null)
                {
                    try
                    {
                        Current.GlobalVariables = Session.globalVariablesBasic.Clone();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Setting] 克隆GlobalVariables失败: {ex.Message}");
                        Current.GlobalVariables = new GlobalVariables();
                    }
                }
                
                // FIX: Force cheat mode to be disabled by default, ignoring saved state
                if (Current.GlobalVariables != null)
                {
                    Current.GlobalVariables.EnableCheat = false;
                }

                // 🔥 技术性修复：安全处理Resolution设置，避免ArgumentOutOfRangeException
                try
                {
                    if (String.IsNullOrEmpty(Session.Resolution))  // Season.PlatForm == PlatForm.iOS || Season.PlatForm == PlatForm.WinRT || Season.PlatForm == PlatForm.WP)
                    {
                        Session.Resolution = Platform.PreferResolution;
                    }
                    
                    // 验证Resolution格式
                    if (!String.IsNullOrEmpty(Current.Resolution) && Current.Resolution.Contains("*"))
                    {
                        var parts = Current.Resolution.Split('*');
                        if (parts.Length >= 2)
                        {
                            if (int.TryParse(parts[0].Trim(), out int width) && int.TryParse(parts[1].Trim(), out int height))
                            {
                                Session.RealResolution = Session.Resolution = Current.Resolution;
                            }
                            else
                            {
                                // 格式无效，使用默认分辨率
                                Session.RealResolution = Session.Resolution = Platform.PreferResolution;
                                Current.Resolution = Platform.PreferResolution;
                            }
                        }
                        else
                        {
                            // 格式无效，使用默认分辨率
                            Session.RealResolution = Session.Resolution = Platform.PreferResolution;
                            Current.Resolution = Platform.PreferResolution;
                        }
                    }
                    else
                    {
                        // Resolution为空或格式错误，使用默认分辨率
                        Session.RealResolution = Session.Resolution = Platform.PreferResolution;
                        Current.Resolution = Platform.PreferResolution;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Setting] 设置分辨率失败: {ex.Message}");
                    // 使用安全的默认分辨率
                    try
                    {
                        Session.RealResolution = Session.Resolution = Platform.PreferResolution;
                        Current.Resolution = Platform.PreferResolution;
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Setting] 设置默认分辨率也失败: {ex2.Message}");
                        Session.RealResolution = Session.Resolution = "1280*720";
                        Current.Resolution = "1280*720";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Setting] Prepare方法执行失败: {ex.Message}");
                // 确保基本设置存在
                if (Current.GlobalVariables == null)
                {
                    Current.GlobalVariables = new GlobalVariables();
                }
                if (String.IsNullOrEmpty(Current.Language))
                {
                    Current.Language = "tw";
                }
                if (String.IsNullOrEmpty(Current.Resolution))
                {
                    Current.Resolution = "1280*720";
                }
            }
        }

    }
}
