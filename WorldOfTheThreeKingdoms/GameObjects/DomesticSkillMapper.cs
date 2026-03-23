/// <summary>
/// 内政技能映射器 V1.0
/// 根据武将的技能、称号、影响等特性，智能匹配最适合的内政工作
/// 作者：AI系统整合团队
/// 日期：2026-01-14
/// </summary>

using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.PersonDetail;
using GameObjects.ArchitectureDetail;
using GameObjects.Influences;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects
{
    public static class DomesticSkillMapper
    {
        // 缓存：工作类型 -> 相关技能ID列表
        private static Dictionary<ArchitectureWorkKind, HashSet<int>> _workSkillMap;
        
        // 缓存：工作类型 -> 相关影响Kind ID列表
        private static Dictionary<ArchitectureWorkKind, HashSet<int>> _workInfluenceMap;
        
        // 是否已初始化
        private static bool _initialized = false;
        
        /// <summary>
        /// 初始化映射 (建议在游戏启动或加载 CommonData 后调用)
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            _workSkillMap = new Dictionary<ArchitectureWorkKind, HashSet<int>>();
            _workInfluenceMap = new Dictionary<ArchitectureWorkKind, HashSet<int>>();
            
            // =========================================================
            // 1. 硬编码配置 (作为默认值)
            
            _initialized = true;
        }
    }
}
