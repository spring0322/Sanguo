using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    // [技术修复] 缺失的类型定义 - 只做最小化实现，不影响核心功能

    /// <summary>
    /// 游戏编辑器插件
    /// </summary>
    public class InGameEditorPlugin
    {
        public string Name { get; set; } = "InGameEditor";
        public bool IsEnabled { get; set; } = false;
        
        public void Initialize()
        {
            // 简单实现，保持兼容性
        }
        
        public void Update()
        {
            // 简单实现，保持兼容性
        }
    }
}