using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;



namespace GameObjects.Animations
{

    public class TileAnimationGenerator
    {
        public Dictionary<int, TileAnimation> TileAnimations = new Dictionary<int, TileAnimation>();

        public TileAnimationGenerator()
        {
        }

        public TileAnimation AddTileAnimation(TileAnimationKind kind, Point position, bool looping)
        {
            TileAnimation animation = new TileAnimation();
            animation.Kind = kind;
            animation.Looping = looping;
            animation.Position = position;
            int hashCode = animation.GetHashCode();
            
            if (!this.TileAnimations.ContainsKey(hashCode))
            {
                // 🔥 Anti-Band-Aid：不添加空检查，让它崩溃
                // 日期：2026-03-09
                // 原因：如果 GetAnimation 返回 null 或 Texture 为 null，说明数据源错误
                // 解决：让它崩溃，追溯到 AllTileAnimations 加载逻辑或配置文件
                // 诊断：崩溃时检查 GameCommonData.AllTileAnimations 是否正确加载
                animation.LinkedAnimation = Session.Current.Scenario.GameCommonData.AllTileAnimations.GetAnimation((int) kind);
                
                // 🔥 诊断日志：记录动画添加（但不拦截错误）
                System.Diagnostics.Debug.WriteLine($"[TileAnimationGenerator] 动画添加 - Kind={kind}({(int)kind}), Position={position}, Looping={looping}, LinkedAnimation={(animation.LinkedAnimation != null ? "有效" : "NULL")}, Texture={(animation.LinkedAnimation?.Texture != null ? "有效" : "NULL")}");
                
                animation.Drawing = true;
                animation.currentFrameIndex = 0;
                animation.currentStayIndex = 0;
                this.TileAnimations.Add(hashCode, animation);
                
                System.Diagnostics.Debug.WriteLine($"[TileAnimationGenerator] ✅ 动画已添加到字典 - 当前动画总数={this.TileAnimations.Count}");
                
                return animation;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[TileAnimationGenerator] ⚠️ 动画已存在（哈希冲突） - Kind={kind}, Position={position}, HashCode={hashCode}");
            }
            
            return null;
        }

        public void Clear()
        {
            this.TileAnimations.Clear();
        }

        public void ClearFinishedAnimation()
        {
            List<TileAnimation> list = new List<TileAnimation>();
            foreach (TileAnimation animation in this.TileAnimations.Values)
            {
                if (!animation.Drawing)
                {
                    list.Add(animation);
                }
            }
            foreach (TileAnimation animation in list)
            {
                this.TileAnimations.Remove(animation.GetHashCode());
            }
        }

        public bool HasTileAnimation(TileAnimationKind kind, Point position, bool looping)
        {
            TileAnimation animation = new TileAnimation();
            animation.Kind = kind;
            animation.Looping = looping;
            animation.Position = position;
            return this.TileAnimations.ContainsKey(animation.GetHashCode());
        }

        public void RemoveTileAnimation(TileAnimationKind kind, Point position, bool looping)
        {
            TileAnimation animation = new TileAnimation();
            animation.Kind = kind;
            animation.Looping = looping;
            animation.Position = position;
            this.TileAnimations.Remove(animation.GetHashCode());
        }
    }
}

