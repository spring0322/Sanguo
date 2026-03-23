using System;
using System.Collections.Generic;
using System.Diagnostics;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;

namespace GameManager
{
    public enum DebugChannel
    {
        Strategic,   // 战略决策
        Tactical,    // 战术移动/战斗
        Diplomacy,   // 外交
        Personnel,   // 人事调动
        Legion,      // 军团管理
        Influence,   // 势图计算
        Critical     // 关键错误/异常
    }

    /// <summary>
    /// AI 决策调试器 - 提供结构化日志输出
    /// </summary>
    public static class AIDebugger
    {
        private static Dictionary<DebugChannel, bool> _channels = new Dictionary<DebugChannel, bool>
        {
            { DebugChannel.Strategic, true },
            { DebugChannel.Tactical, false },  // 默认关闭高频战术日志
            { DebugChannel.Diplomacy, true },
            { DebugChannel.Personnel, true },
            { DebugChannel.Legion, true },
            { DebugChannel.Influence, false }, // 默认关闭高频势图日志
            { DebugChannel.Critical, true }
        };

        public static bool EnableConsoleOutput = true;
        
        // 新增：内存日志存储
        private static readonly List<string> _memoryLogs = new List<string>();
        private const int MaxLogCount = 30;
        private static readonly object _logLock = new object();

        public static void Log(DebugChannel channel, string message, Faction faction = null, Person person = null)
        {
            if (!_channels.ContainsKey(channel) || !_channels[channel]) return;

            string timeStr = GetGameTimeStr();
            string context = BuildContextString(faction, person);
            string output = $"[{timeStr}] [{channel}] {context}{message}";

            if (EnableConsoleOutput)
            {
                Debug.WriteLine(output);
            }
            
            // 存入内存日志
            AddLog(output);
        }

        public static void LogDecision(DebugChannel channel, string actionName, string subject, Dictionary<string, float> scores, float finalScore, float threshold)
        {
            if (!_channels.ContainsKey(channel) || !_channels[channel]) return;

            string timeStr = GetGameTimeStr();
            string header = $"[{timeStr}] [{channel}] [{actionName}] {subject} 的决策评估:";
            
            Debug.WriteLine(header);
            AddLog(header);
            
            foreach (var kv in scores)
            {
                string scoreLine = $"  > {kv.Key}: {kv.Value:F2}";
                Debug.WriteLine(scoreLine);
                // 决策详情暂不全部存入屏幕日志，避免刷屏，只存总分
            }
            
            string resultLine = $"  最终总分: {finalScore:F2} / 阈值: {threshold:F2}";
            Debug.WriteLine(resultLine);
            AddLog(resultLine);
        }
        
        private static void AddLog(string message)
        {
            lock (_logLock)
            {
                _memoryLogs.Add(message);
                if (_memoryLogs.Count > MaxLogCount)
                {
                    _memoryLogs.RemoveAt(0);
                }
            }
        }
        
        public static List<string> GetLogs()
        {
            lock (_logLock)
            {
                return new List<string>(_memoryLogs);
            }
        }

        private static string GetGameTimeStr()
        {
            try
            {
                if (Session.Current?.Scenario?.Date != null)
                {
                    return Session.Current.Scenario.Date.ToDateString();
                }
            }
            catch { }
            return DateTime.Now.ToString("HH:mm:ss");
        }

        private static string BuildContextString(Faction f, Person p)
        {
            string res = "";
            if (f != null) res += $"[{f.Name}] ";
            if (p != null) res += $"({p.Name}) ";
            return res;
        }

        public static void SetChannel(DebugChannel channel, bool enabled)
        {
            _channels[channel] = enabled;
        }
    }
}
