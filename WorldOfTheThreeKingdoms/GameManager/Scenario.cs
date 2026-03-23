using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace GameManager
{
    [DataContract]
    public class Scenario
    {
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Path { get; set; }
        [DataMember]
        public string Time { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Create { get; set; }
        [DataMember]
        public string Info { get; set; }
        [DataMember]
        public string First { get; set; }
        [DataMember]
        public string Desc { get; set; }
        [DataMember]
        public string IDs { get; set; }
        [DataMember]
        public string Names { get; set; }
        [DataMember]
        public string Players { get; set; }
        [DataMember]
        public string Player { get; set; }
        [DataMember]
        public string PlayTime { get; set; }
        [DataMember]
        public string LeaderPics { get; set; }
        [DataMember]
        public string LeaderNames { get; set; }
        [DataMember]
        public string Reputations { get; set; }
        [DataMember]
        public string ArchitectureCounts { get; set; }
        [DataMember]
        public string CapitalNames { get; set; }
        [DataMember]
        public string Populations { get; set; }
        [DataMember]
        public string MilitaryCounts { get; set; }
        [DataMember]
        public string Funds { get; set; }
        [DataMember]
        public string Foods { get; set; }

        public string GameTime
        {
            get
            {
                if (string.IsNullOrEmpty(PlayTime)) return "";
                int playTime;
                if (int.TryParse(PlayTime, out playTime))
                {
                    return (playTime / 60 / 60) + ":" + (playTime / 60 % 60);
                }
                // If it's already a TimeSpan string, like "00:05:30", just use it
                return PlayTime.Trim();
            }
        }

        private string _summary;
        public string Summary
        {
            get
            {
                // 🔥 修复：严格按照文档格式实现存档显示，并添加文本截断防止超出UI
                // 格式："存档" + ID + ":    " + Info + "   |   " + Title + "   |   " + Time + "   |   " + Create + "   |   (" + GameTime + ")"
                // 日期：2026-02-25
                
                if (string.IsNullOrEmpty(Title))
                {
                    return $"存档{ID}: 空白存档";
                }

                // Info: 玩家势力名称，如果为空则显示"电脑"（限制长度）
                string infoDisplay = string.IsNullOrEmpty(Info) ? "电脑" : Info.Trim();
                
                // 🔥 向后兼容老存档：如果由于旧Bug存入了"电脑"，但实际包含玩家ID，则显示"玩家"
                if (infoDisplay == "电脑" && !string.IsNullOrWhiteSpace(Players))
                {
                    infoDisplay = "玩家";
                }

                if (infoDisplay.Length > 8) infoDisplay = infoDisplay.Substring(0, 7) + "…";
                
                // Title: 剧本标题（限制长度）
                string titleDisplay = Title.Trim();
                if (titleDisplay.Length > 10) titleDisplay = titleDisplay.Substring(0, 9) + "…";
                
                // Time: 游戏内日期（限制长度）
                string timeDisplay = string.IsNullOrEmpty(Time) ? "----" : Time.Trim();
                if (timeDisplay.Length > 12) timeDisplay = timeDisplay.Substring(0, 11) + "…";
                
                // Create: 存档创建时间（简化格式）
                string createDisplay = "";
                if (!string.IsNullOrEmpty(Create))
                {
                    if (DateTime.TryParse(Create, out DateTime dt))
                    {
                        createDisplay = dt.ToString("MM-dd HH:mm");  // 简化为月-日 时:分
                    }
                    else
                    {
                        createDisplay = Create.Trim();
                        if (createDisplay.Length > 11) createDisplay = createDisplay.Substring(0, 10) + "…";
                    }
                }
                
                // GameTime: 累计游戏时间 (HH:MM)
                string gameTimeDisplay = string.IsNullOrWhiteSpace(GameTime) ? "00:00" : GameTime.Trim();
                
                // 按照文档格式拼接（使用紧凑分隔符去空格）
                if (string.IsNullOrEmpty(createDisplay))
                {
                    return $"存档{ID}:{infoDisplay}|{titleDisplay}|{timeDisplay}|({gameTimeDisplay})";
                }
                else
                {
                    return $"存档{ID}:{infoDisplay}|{titleDisplay}|{timeDisplay}|{createDisplay}|({gameTimeDisplay})";
                }
            }
            set { _summary = value; }
        }
    }
}
