using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Content;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameManager
{
    /// <summary>
    /// 统一音频管理器 (Singleton模式)
    /// 支持场景化音乐播放：春夏秋冬四季、进攻防守战斗、游戏开始等
    /// </summary>
    public class AudioManager
    {
        // 单例模式：保证全局只有一个音频管理器
        private static AudioManager _instance;
        public static AudioManager Instance => _instance ??= new AudioManager();

        private ContentManager _content;
        
        // 缓存音效，防止重复加载
        private Dictionary<string, SoundEffect> _soundEffects = new Dictionary<string, SoundEffect>();
        
        // 缓存音乐
        private Dictionary<string, Song> _songs = new Dictionary<string, Song>();

        // 全局音量设置 (0.0f - 1.0f)
        public float MusicVolume { get; private set; } = 0.5f;
        public float SoundVolume { get; private set; } = 0.8f;

        // 当前播放的场景/歌曲名
        private string _currentMusicContext;
        
        // 🔥 新增：当前正在播放的具体歌曲路径
        private string _currentSongPath;

        // 音乐场景枚举
        public enum MusicScene
        {
            Start,      // 游戏开始
            Spring,     // 春季
            Summer,     // 夏季
            Autumn,     // 秋季
            Winter,     // 冬季
            Attack,     // 进攻
            Defend,     // 防守
            Battle      // 战斗
        }

        // 场景到文件夹的映射
        private readonly Dictionary<MusicScene, string> _sceneFolders = new Dictionary<MusicScene, string>
        {
            { MusicScene.Start, "Start" },
            { MusicScene.Spring, "Spring" },
            { MusicScene.Summer, "Summer" },
            { MusicScene.Autumn, "Autumn" },
            { MusicScene.Winter, "Winter" },
            { MusicScene.Attack, "Attack" },
            { MusicScene.Defend, "Defend" },
            { MusicScene.Battle, "Battle" }
        };

        // 每个场景当前播放的音乐索引（用于顺序播放）
        private readonly Dictionary<string, int> _scenePlayIndex = new Dictionary<string, int>();

        // 🔥 FIX: 添加初始化方法
        public void Initialize(ContentManager content)
        {
            _content = content;
            try
            {
                MediaPlayer.Volume = MusicVolume;
                MediaPlayer.IsRepeating = true; // 背景音乐循环播放
                System.Diagnostics.Debug.WriteLine("[AudioManager] 音频系统已初始化");
            }
            catch (ObjectDisposedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioManager] 初始化时 MediaPlayer 已被释放: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioManager] 初始化时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查 ContentManager 是否有效且未被释放
        /// </summary>
        private bool IsContentManagerValid()
        {
            if (_content == null) return false;
            
            try
            {
                // 尝试访问 ContentManager 的属性来检测是否已被释放
                var serviceProvider = _content.ServiceProvider;
                return serviceProvider != null;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 检查 Song 对象是否有效且未被释放
        /// </summary>
        private bool IsSongValid(Song song)
        {
            if (song == null) return false;
            
            try
            {
                // 尝试访问 Song 的属性来检测是否已被释放
                var duration = song.Duration;
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ==========================================
        // 🎵 BGM (背景音乐) 智能控制部分
        // ==========================================

        /// <summary>
        /// 播放指定场景的音乐
        /// </summary>
        /// <param name="scene">音乐场景</param>
        public void PlaySceneMusic(MusicScene scene)
        {
            if (_sceneFolders.TryGetValue(scene, out string folderName))
            {
                PlayMusic(folderName);
            }
        }

        /// <summary>
        /// 播放指定场景的音乐 (支持文件夹扫描和顺序播放)
        /// </summary>
        /// <param name="name">可以是具体的歌曲名，也可以是文件夹名(场景名)</param>
        public void PlayMusic(string name)
        {
            // 1. 检查基础条件
            if (!IsContentManagerValid()) 
            {
                System.Diagnostics.Debug.WriteLine("[AudioManager] ContentManager 无效，无法播放音乐");
                return;
            }

            // 🔥 修复：改进重复播放检查
            // 如果请求的场景已经在播放，且当前歌曲还在播放中，则跳过
            try
            {
                if (_currentMusicContext == name && 
                    MediaPlayer.State == MediaState.Playing &&
                    !string.IsNullOrEmpty(_currentSongPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioManager] 场景 {name} 已在播放中，跳过切换");
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                // MediaPlayer 已被释放，重置状态并继续
                _currentMusicContext = null;
                _currentSongPath = null;
                System.Diagnostics.Debug.WriteLine("[AudioManager] MediaPlayer 已被释放，重置播放状态");
            }

            try
            {
                Song songToPlay = null;
                string songAssetPath = "";

                // 1. 尝试判定 'name' 是否为文件夹 (例如 "Spring", "Start")
                // 构建绝对路径用于检测文件夹是否存在
                string baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Music", name);
                
                if (Directory.Exists(baseFolder))
                {
                    // === 这是一个场景文件夹 ===
                    System.Diagnostics.Debug.WriteLine($"[AudioManager] 检测到场景文件夹: {name}");
                    
                    // 🔧 修复：只加载已编译的 .xnb 文件
                    // MonoGame ContentManager 只能加载通过 Content Pipeline 编译的资源
                    // 原始音频文件 (.mp3/.wma) 必须先在 Content.mgcb 中添加并编译
                    string[] xnbFiles = Directory.GetFiles(baseFolder, "*.xnb");
                    
                    System.Diagnostics.Debug.WriteLine($"[AudioManager] 文件夹 {name} 包含 {xnbFiles.Length} 个已编译的音乐文件");
                    
                    if (xnbFiles.Length > 0)
                    {
                        // 顺序播放逻辑：根据场景记录当前播放索引
                        if (!_scenePlayIndex.ContainsKey(name))
                        {
                            _scenePlayIndex[name] = 0;
                        }

                        // 获取当前应该播放的文件
                        int currentIndex = _scenePlayIndex[name];
                        string selectedFile = xnbFiles[currentIndex % xnbFiles.Length];
                        
                        // 更新索引，下次播放下一首
                        _scenePlayIndex[name] = (currentIndex + 1) % xnbFiles.Length;
                        
                        // 获取文件名 (不含扩展名)
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(selectedFile);
                        
                        // 组合成 ContentManager 能识别的路径: "Music/Start/bgm01"
                        songAssetPath = $"Music/{name}/{fileNameNoExt}";
                        
                        System.Diagnostics.Debug.WriteLine($"[AudioManager] 场景 {name} 选择音乐: {fileNameNoExt} (索引: {currentIndex}, 文件: {Path.GetFileName(selectedFile)})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioManager] 警告: 文件夹 {name} 没有找到已编译的音乐文件 (.xnb)！");
                        System.Diagnostics.Debug.WriteLine($"[AudioManager] 提示: 请在 Content.mgcb 中添加音频文件并重新编译项目");
                        return;
                    }
                }
                else
                {
                    // === 这是一个具体的文件名 ===
                    // 假设直接传入了 "Music/Spring/Theme01" 或者是旧逻辑的直接文件名
                    songAssetPath = name.Contains("/") ? name : $"Music/{name}";
                }

                // 2. 安全的缓存检查和加载
                if (_songs.TryGetValue(songAssetPath, out songToPlay))
                {
                    if (!IsSongValid(songToPlay))
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioManager] 缓存的 Song 已失效，移除并重新加载: {songAssetPath}");
                        _songs.Remove(songAssetPath);
                        songToPlay = null;
                    }
                }

                // 3. 重新加载（如果需要）
                if (songToPlay == null)
                {
                    if (!IsContentManagerValid())
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioManager] ContentManager 在加载过程中变为无效");
                        return;
                    }

                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioManager] 尝试加载音乐: {songAssetPath}");
                        songToPlay = _content.Load<Song>(songAssetPath);
                        
                        if (!IsSongValid(songToPlay))
                        {
                            System.Diagnostics.Debug.WriteLine($"[AudioManager] 新加载的 Song 无效: {songAssetPath}");
                            return;
                        }

                        _songs[songAssetPath] = songToPlay;
                        System.Diagnostics.Debug.WriteLine($"[AudioManager] 成功加载音乐: {songAssetPath}, 时长: {songToPlay.Duration}");
                    }
                    catch (ObjectDisposedException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioManager] 加载时 ContentManager 被释放: {songAssetPath}, 错误: {ex.Message}");
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioManager] 音乐加载失败: {songAssetPath}, 错误: {ex.Message}");
                        return;
                    }
                }

                // 4. 安全播放
                if (!IsSongValid(songToPlay))
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioManager] 播放前检查失败，Song 无效: {songAssetPath}");
                    return;
                }

                try
                {
                    MediaPlayer.Stop(); // 先停止当前播放
                    MediaPlayer.Play(songToPlay);
                    _currentMusicContext = name;
                    _currentSongPath = songAssetPath;  // 🔥 记录当前播放的歌曲路径
                    
                    System.Diagnostics.Debug.WriteLine($"[AudioManager] 成功播放: {songAssetPath}");
                }
                catch (ObjectDisposedException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioManager] 播放时 MediaPlayer 或 Song 被释放: {songAssetPath}, 错误: {ex.Message}");
                    _songs.Remove(songAssetPath);
                    _currentMusicContext = null;
                    _currentSongPath = null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioManager] 播放失败: {songAssetPath}, 错误: {ex.Message}");
                    _currentMusicContext = null;
                    _currentSongPath = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioManager] 播放音乐时发生未预期错误: {name}, 错误: {ex.Message}");
                _currentMusicContext = null;
                _currentSongPath = null;
            }
        }

        /// <summary>
        /// 根据季节播放对应音乐
        /// </summary>
        /// <param name="season">游戏季节</param>
        public void PlaySeasonMusic(GameSeason season)
        {
            switch (season)
            {
                case GameSeason.春:
                    PlaySceneMusic(MusicScene.Spring);
                    break;
                case GameSeason.夏:
                    PlaySceneMusic(MusicScene.Summer);
                    break;
                case GameSeason.秋:
                    PlaySceneMusic(MusicScene.Autumn);
                    break;
                case GameSeason.冬:
                    PlaySceneMusic(MusicScene.Winter);
                    break;
            }
        }

        /// <summary>
        /// 根据战斗状态播放对应音乐
        /// </summary>
        /// <param name="battleState">战斗状态</param>
        public void PlayBattleMusic(ZhandouZhuangtai battleState)
        {
            switch (battleState)
            {
                case ZhandouZhuangtai.进攻:
                    PlaySceneMusic(MusicScene.Attack);
                    break;
                case ZhandouZhuangtai.防守:
                    PlaySceneMusic(MusicScene.Defend);
                    break;
                case ZhandouZhuangtai.和平:
                    // 和平状态下播放季节音乐
                    if (Session.Current?.Scenario?.Date?.Season != null)
                    {
                        PlaySeasonMusic(Session.Current.Scenario.Date.Season);
                    }
                    break;
            }
        }

        /// <summary>
        /// 播放游戏开始音乐
        /// </summary>
        public void PlayStartMusic()
        {
            PlaySceneMusic(MusicScene.Start);
        }

        /// <summary>
        /// 播放战斗音乐
        /// </summary>
        public void PlayCombatMusic()
        {
            PlaySceneMusic(MusicScene.Battle);
        }

        public void StopMusic()
        {
            try
            {
                MediaPlayer.Stop();
                _currentMusicContext = null;
                _currentSongPath = null;
                System.Diagnostics.Debug.WriteLine("[AudioManager] 音乐已停止");
            }
            catch (ObjectDisposedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioManager] 停止音乐时 MediaPlayer 已被释放: {ex.Message}");
                _currentMusicContext = null;
                _currentSongPath = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioManager] 停止音乐时出错: {ex.Message}");
                _currentMusicContext = null;
                _currentSongPath = null;
            }
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Math.Clamp(volume, 0f, 1f);
            try
            {
                MediaPlayer.Volume = MusicVolume;
                System.Diagnostics.Debug.WriteLine($"[AudioManager] 音乐音量设置为: {MusicVolume:P0}");
            }
            catch (ObjectDisposedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioManager] 设置音量时 MediaPlayer 已被释放: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioManager] 设置音量时出错: {ex.Message}");
            }
        }

        // ==========================================
        // 🔊 SE (音效) 控制部分
        // ==========================================
        public void PlaySound(string soundName)
        {
            if (_content == null || string.IsNullOrEmpty(soundName)) return;

            try
            {
                // 简单的防重复加载缓存
                if (!_soundEffects.TryGetValue(soundName, out var sfx))
                {
                    // 兼容带路径和不带路径的写法
                    string path = soundName.Contains("/") ? soundName : $"Sound/{soundName}";
                    sfx = _content.Load<SoundEffect>(path);
                    _soundEffects[soundName] = sfx;
                }

                sfx.Play(SoundVolume, 0f, 0f);
            }
            catch (Exception ex)
            {
                // 音效加载失败通常不报错，避免刷屏
                System.Diagnostics.Debug.WriteLine($"[AudioManager] SE加载失败: {soundName}, {ex.Message}");
            }
        }

        public void SetSoundVolume(float volume)
        {
            SoundVolume = Math.Clamp(volume, 0f, 1f);
        }

        /// <summary>
        /// 清理已释放的音频资源
        /// </summary>
        public void CleanupDisposedResources()
        {
            var disposedSongs = new List<string>();
            
            foreach (var kvp in _songs.ToList()) // 使用 ToList() 避免修改集合时的异常
            {
                if (!IsSongValid(kvp.Value))
                {
                    disposedSongs.Add(kvp.Key);
                }
            }
            
            // 移除已释放的 Song
            foreach (var key in disposedSongs)
            {
                _songs.Remove(key);
                System.Diagnostics.Debug.WriteLine($"[AudioManager] 清理已释放的音乐资源: {key}");
            }
            
            // 如果当前播放的音乐也被释放了，停止播放
            if (disposedSongs.Count > 0)
            {
                try
                {
                    var currentState = MediaPlayer.State;
                    if (currentState == MediaState.Playing)
                    {
                        // MediaPlayer 状态正常，但播放的 Song 可能已被释放
                        System.Diagnostics.Debug.WriteLine("[AudioManager] 检测到播放中的音乐资源被释放，但 MediaPlayer 状态正常");
                    }
                }
                catch (ObjectDisposedException)
                {
                    _currentMusicContext = null;
                    _currentSongPath = null;
                    System.Diagnostics.Debug.WriteLine("[AudioManager] MediaPlayer 也被释放，重置播放状态");
                }
            }
        }

        /// <summary>
        /// 释放所有音频资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 安全停止 MediaPlayer
                try
                {
                    MediaPlayer.Stop();
                    System.Diagnostics.Debug.WriteLine("[AudioManager] MediaPlayer 已停止");
                }
                catch (ObjectDisposedException)
                {
                    System.Diagnostics.Debug.WriteLine("[AudioManager] MediaPlayer 已被释放，跳过停止操作");
                }
                
                // 清理音效缓存
                foreach (var sfx in _soundEffects.Values)
                {
                    try
                    {
                        sfx?.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // 音效已被释放，忽略
                    }
                }
                _soundEffects.Clear();
                
                // 清理音乐缓存 - Song 对象通常由 ContentManager 管理，不需要手动释放
                _songs.Clear();
                
                _currentMusicContext = null;
                _currentSongPath = null;
                
                System.Diagnostics.Debug.WriteLine("[AudioManager] 音频资源已释放");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioManager] 释放资源时出错: {ex.Message}");
            }
        }

        // ==========================================
        // 🔄 更新循环 (淡入淡出等逻辑)
        // ==========================================
        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            try
            {
                // 定期清理无效资源
                if (gameTime.TotalGameTime.TotalSeconds % 30 < 0.1) // 每30秒清理一次
                {
                    CleanupDisposedResources();
                }

                // 检查音乐播放状态
                try
                {
                    if (MediaPlayer.State == MediaState.Stopped && !string.IsNullOrEmpty(_currentMusicContext))
                    {
                        // 音乐播放完毕，播放同场景的下一首
                        string currentScene = _currentMusicContext;
                        _currentMusicContext = null;
                        _currentSongPath = null;
                        
                        // 延迟一帧再播放，避免资源竞争
                        System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => 
                        {
                            if (IsContentManagerValid())
                            {
                                PlayMusic(currentScene);
                            }
                        });
                    }
                }
                catch (ObjectDisposedException)
                {
                    // MediaPlayer 已被释放
                    _currentMusicContext = null;
                    _currentSongPath = null;
                    System.Diagnostics.Debug.WriteLine("[AudioManager] Update 中检测到 MediaPlayer 被释放");
                }
            }
            catch (ObjectDisposedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioManager] Update 中检测到资源释放: {ex.Message}");
                _currentMusicContext = null;
                _currentSongPath = null;
                CleanupDisposedResources();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioManager] Update 过程中出错: {ex.Message}");
                _currentMusicContext = null;
                _currentSongPath = null;
            }
        }
    }
}