using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 火势蔓延管理器（基于风向风力系统）
/// 日期：2026-03-10
/// 
/// 🔥 核心功能：
/// 1. 每回合结算火势蔓延（基于风向风力）
/// 2. 自然熄灭判定（雨天直接扑灭）
/// 3. 对着火地块上的部队造成伤害
/// 
/// 🔥 性能优化：
/// - 使用预分配列表，避免 GC
/// - 逆序遍历，方便移除元素
/// - 无 LINQ，纯 for 循环
/// </summary>
public class FireSpreadManager
{
    // 🔥 预分配的列表，永不扩容，永不销毁，保证 AOT 下 0 GC
    private readonly List<Point> _newFiresQueue = new(1000);
    private readonly List<Point> _currentFiresCache = new(1000); // 🔥 新增：缓存当前火势列表
    private readonly Random _rng = new();
    
    // 🔥 配置参数（从 Session.Parameters 读取）
    private int FireStayProb => Session.Parameters.FireStayProb;
    private float FireSpreadProbMultiply => Session.Parameters.FireSpreadProbMultiply;
    private float FireDamageScale => Session.Parameters.FireDamageScale;

    /// <summary>
    /// 回合结束时触发，结算火势蔓延与自然熄灭
    /// 🧊 COLD PATH：每回合调用一次，允许使用可读代码
    /// </summary>
    public void ProcessFireSpread()
    {
        // 🔥 ANTI-BAND-AID：移除防御性空检查
        // 如果 Scenario 为 null，说明游戏状态异常，应该让异常抛出
        var scenario = Session.Current.Scenario;
        
        _newFiresQueue.Clear(); // 清空缓冲队列，不释放内存
        _currentFiresCache.Clear(); // 清空缓存列表
        
        // 🔥 获取当前所有着火的地块（从 FireTable）
        var firePositions = scenario.FireTable.Positions;
        if (firePositions.Count == 0) return;
        
        // 🔥 复制到缓存列表，避免在遍历时修改 FireTable
        foreach (var pos in firePositions)
        {
            _currentFiresCache.Add(pos);
        }
        
        // 逆序遍历，方便在自然熄灭时直接移除元素
        for (int i = _currentFiresCache.Count - 1; i >= 0; i--)
        {
            Point currentFire = _currentFiresCache[i];
            
            // 1. 判定自然熄灭
            if (ShouldExtinguish(currentFire))
            {
                scenario.ClearPositionFire(currentFire);
                System.Diagnostics.Debug.WriteLine($"[火势蔓延] {currentFire} 的火焰已熄灭");
                continue;
            }
            
            // 2. 获取当前风况
            var wind = scenario.WeatherManager.GetWindAt(currentFire);
            
            // 3. 只有大风(Strong)或狂风(Gale)才能引发火势蔓延
            if (wind.Force >= WindForce.Strong)
            {
                // 4. 寻找下风口坐标
                Point downwindTile = WindMath.GetDownwindTile(currentFire, wind.Direction);
                
                // 5. 判定下风口是否满足蔓延条件
                if (CanSpreadTo(downwindTile, wind.Force))
                {
                    // 放入缓冲队列，避免在当前循环中修改 FireTable
                    _newFiresQueue.Add(downwindTile);
                }
            }
        }
        
        // 6. 将新点燃的地块合并到 FireTable，并造成实际伤害
        for (int i = 0; i < _newFiresQueue.Count; i++)
        {
            Point newFire = _newFiresQueue[i];
            
            // 点燃地块
            scenario.SetPositionOnFire(newFire);
            
            // 🔥 核心数值联动：对新着火地块上的部队造成伤害
            var victimTroop = scenario.GetTroopByPositionNoCheck(newFire);
            if (victimTroop != null)
            {
                // 🔥 ANTI-BAND-AID：直接访问属性，让异常自然抛出
                var terrain = scenario.GetTerrainDetailByPositionNoCheck(newFire);
                float damageScale = FireDamageScale * terrain.FireDamageRate;
                
                // 对部队造成火焰伤害
                victimTroop.ReceiveFireDamage(damageScale);
                
                System.Diagnostics.Debug.WriteLine(
                    $"[火势蔓延] 狂风将大火吹到了 {newFire}，{victimTroop.DisplayName} 受到火焰伤害！");
            }
        }
    }

    /// <summary>
    /// 判定火焰是否应该熄灭
    /// </summary>
    private bool ShouldExtinguish(Point tile)
    {
        var scenario = Session.Current.Scenario;
        
        // 1. 雨天直接扑灭
        var weather = scenario.WeatherManager.GetWeatherAt(tile);
        if (weather == WeatherType.Rain)
        {
            return true;
        }
        
        // 2. 自然熄灭概率判定（使用配置参数）
        return _rng.Next(100) < (100 - FireStayProb);
    }

    /// <summary>
    /// 判定特定地块是否能被引燃（结合地形与风力）
    /// </summary>
    private bool CanSpreadTo(Point tile, WindForce windForce)
    {
        var scenario = Session.Current.Scenario;
        
        // 1. 防越界检查
        if (scenario.PositionOutOfRange(tile))
        {
            return false;
        }
        
        // 2. 防重复点燃检查
        if (scenario.FireTable.HasPosition(tile) || _newFiresQueue.Contains(tile))
        {
            return false;
        }
        
        // 3. 获取地形类型
        var terrainKind = scenario.GetTerrainKindByPositionNoCheck(tile);
        
        // 4. 🔥 C# 12 模式匹配获取地形基础易燃概率
        int baseIgniteChance = terrainKind switch
        {
            TerrainKind.森林 => 80,  // 森林极度易燃
            TerrainKind.草原 => 60,  // 草原易燃
            TerrainKind.平原 => 20,  // 平原杂草有一定概率
            TerrainKind.荒地 => 40,  // 荒地干燥易燃
            TerrainKind.沙漠 => 10,  // 沙漠植被少，难燃
            TerrainKind.水域 => 0,   // 水域绝对不可点燃
            TerrainKind.山地 => 10,  // 山地植被少
            TerrainKind.峻岭 => 5,   // 峻岭更难燃
            TerrainKind.湿地 => 5,   // 湿地潮湿难燃
            TerrainKind.栈道 => 30,  // 栈道木质结构易燃
            _ => 0
        };
        
        if (baseIgniteChance <= 0)
        {
            return false;
        }
        
        // 5. 狂风(Gale)额外增加蔓延概率（使用配置参数）
        float windModifier = windForce == WindForce.Gale ? 1.5f : 1.0f;
        int finalChance = (int)(baseIgniteChance * windModifier * FireSpreadProbMultiply);
        
        return _rng.Next(100) < finalChance;
    }
}
