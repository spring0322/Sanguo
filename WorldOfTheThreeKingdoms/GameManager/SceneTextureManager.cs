using System;
using System.Collections.Generic;
using System.Linq;
using Tools;

namespace GameManager
{
    /// <summary>
    /// 游戏场景类型
    /// </summary>
    public enum GameSceneType
    {
        MainMenu,
        GamePlay,
        PersonDetail,
        ArchitectureDetail,
        TroopDetail,
        Battle,
        Diplomacy,
        Technology,
        Event
    }

    /// <summary>
    /// 场景纹理管理器，根据游戏场景自动管理纹理加载和释放
    /// </summary>
    public static class SceneTextureManager
    {
        private static GameSceneType _currentScene = GameSceneType.MainMenu;
        private static readonly Dictionary<GameSceneType, SceneTextureConfig> _sceneConfigs = new Dictionary<GameSceneType, SceneTextureConfig>();
        private static readonly HashSet<string> _preloadedTextures = new HashSet<string>();

        static SceneTextureManager()
        {
            InitializeSceneConfigs();
        }

        /// <summary>
        /// 切换到新场景
        /// </summary>
        /// <param name="newScene">新场景类型</param>
        /// <param name="forceReload">是否强制重新加载</param>
        public static void SwitchToScene(GameSceneType newScene, bool forceReload = false)
        {
            if (_currentScene == newScene && !forceReload)
                return;

            var oldScene = _currentScene;
            _currentScene = newScene;

            try
            {
                // 释放旧场景的纹理
                ReleaseSceneTextures(oldScene);

                // 预加载新场景的纹理
                PreloadSceneTextures(newScene);

                WebTools.TakeWarnMsg($"场景切换: {oldScene} -> {newScene}", "SceneTextureManager.SwitchToScene", null);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg($"场景切换时发生错误: {oldScene} -> {newScene}", "SceneTextureManager.SwitchToScene", ex);
            }
        }

        /// <summary>
        /// 获取当前场景
        /// </summary>
        /// <returns></returns>
        public static GameSceneType GetCurrentScene()
        {
            return _currentScene;
        }

        /// <summary>
        /// 预加载场景纹理
        /// </summary>
        /// <param name="scene">场景类型</param>
        public static void PreloadSceneTextures(GameSceneType scene)
        {
            if (!_sceneConfigs.TryGetValue(scene, out var config))
                return;

            // 预加载必需的纹理
            if (config.RequiredTextures != null)
            {
                TextureManager.PreloadTextures(config.RequiredTextures, $"Scene_{scene}", true);
                foreach (var texture in config.RequiredTextures)
                {
                    _preloadedTextures.Add(texture);
                }
            }

            // 预加载可选的纹理（如果内存充足）
            if (config.OptionalTextures != null)
            {
                var memoryStats = TextureManager.GetMemoryStats();
                if (memoryStats.MemoryUsagePercentage < 70) // 内存使用率低于70%时才预加载可选纹理
                {
                    TextureManager.PreloadTextures(config.OptionalTextures, $"Scene_{scene}_Optional", false);
                }
            }
        }

        /// <summary>
        /// 释放场景纹理
        /// </summary>
        /// <param name="scene">场景类型</param>
        public static void ReleaseSceneTextures(GameSceneType scene)
        {
            // 释放场景相关的纹理类别
            TextureManager.ReleaseCategory($"Scene_{scene}");
            TextureManager.ReleaseCategory($"Scene_{scene}_Optional");

            // 如果不是人物相关场景，释放头像纹理
            if (scene != GameSceneType.PersonDetail && scene != GameSceneType.GamePlay)
            {
                TextureManager.ReleaseCategory("Portrait");
            }
        }

        /// <summary>
        /// 配置场景纹理需求
        /// </summary>
        /// <param name="scene">场景类型</param>
        /// <param name="requiredTextures">必需纹理列表</param>
        /// <param name="optionalTextures">可选纹理列表</param>
        public static void ConfigureScene(GameSceneType scene, IEnumerable<string> requiredTextures, IEnumerable<string> optionalTextures = null)
        {
            _sceneConfigs[scene] = new SceneTextureConfig
            {
                RequiredTextures = requiredTextures?.ToList(),
                OptionalTextures = optionalTextures?.ToList()
            };
        }

        /// <summary>
        /// 清理所有场景纹理
        /// </summary>
        public static void ClearAllSceneTextures()
        {
            foreach (var scene in Enum.GetValues<GameSceneType>())
            {
                ReleaseSceneTextures(scene);
            }
            _preloadedTextures.Clear();
        }

        #region 私有方法

        private static void InitializeSceneConfigs()
        {
            // 主菜单场景
            ConfigureScene(GameSceneType.MainMenu, new[]
            {
                @"Content\Textures\Resources\Start\Background.jpg",
                @"Content\Textures\GameComponents\GameFrame\Data\GameFrame.png"
            });

            // 游戏主界面场景
            ConfigureScene(GameSceneType.GamePlay, new[]
            {
                @"Content\Textures\GameComponents\GameFrame\Data\GameFrame.png",
                @"Content\Textures\GameComponents\ToolBar\Data\ToolBar.png"
            });

            // 人物详情场景
            ConfigureScene(GameSceneType.PersonDetail, new[]
            {
                @"Content\Textures\GameComponents\PersonDetail\Data\PersonDetail.png"
            });

            // 建筑详情场景
            ConfigureScene(GameSceneType.ArchitectureDetail, new[]
            {
                @"Content\Textures\GameComponents\ArchitectureDetail\Data\ArchitectureDetail.png"
            });

            // 部队详情场景
            ConfigureScene(GameSceneType.TroopDetail, new[]
            {
                @"Content\Textures\GameComponents\TroopDetail\Data\TroopDetail.png"
            });

            // 战斗场景
            ConfigureScene(GameSceneType.Battle, new[]
            {
                @"Content\Textures\Resources\Effects\Battle.png"
            });

            // 外交场景
            ConfigureScene(GameSceneType.Diplomacy, new[]
            {
                @"Content\Textures\GameComponents\GameFrame\Data\GameFrame.png"
            });

            // 技术场景
            ConfigureScene(GameSceneType.Technology, new[]
            {
                @"Content\Textures\GameComponents\FactionTechniques\Data\FactionTechniques.png"
            });

            // 事件场景
            ConfigureScene(GameSceneType.Event, new[]
            {
                @"Content\Textures\GameComponents\tupianwenzi\Data\tupianwenzi.png"
            });
        }

        #endregion
    }

    /// <summary>
    /// 场景纹理配置
    /// </summary>
    public class SceneTextureConfig
    {
        public List<string> RequiredTextures { get; set; }
        public List<string> OptionalTextures { get; set; }
    }

    /// <summary>
    /// 场景纹理管理扩展方法
    /// </summary>
    public static class SceneTextureExtensions
    {
        /// <summary>
        /// 为游戏对象添加场景感知的纹理获取方法
        /// </summary>
        /// <param name="texturePath">纹理路径</param>
        /// <param name="isPermanent">是否为永久纹理</param>
        /// <returns></returns>
        public static Microsoft.Xna.Framework.Graphics.Texture2D GetSceneTexture(string texturePath, bool isPermanent = false)
        {
            var currentScene = SceneTextureManager.GetCurrentScene();
            var category = isPermanent ? $"Scene_{currentScene}_Permanent" : $"Scene_{currentScene}";
            
            return TextureManager.GetTexture(texturePath, category, isPermanent);
        }
    }
}
