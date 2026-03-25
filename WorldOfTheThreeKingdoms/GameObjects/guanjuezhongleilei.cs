using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using WorldOfTheThreeKingdoms.GameGlobal;  // 🔥 2026-03-23 添加：支持 [GenerateUIAccessor] 特性



namespace GameObjects
{
    [DataContract]
    [GenerateUIAccessor]  // 🔥 2026-03-23 修复：添加源生成器特性，支持 UI 访问官爵属性
    public class guanjuezhongleilei : GameObject
	{
        [DataMember]
        public int shengwangshangxian
        {
            get;
            set;
        }
        [DataMember]
        public int xuyaogongxiandu
        {
            get;
            set;
        }
        [DataMember]
        public bool ShowDialog
        {
            get;
            set;
        }
        [DataMember]
        public int xuyaochengchi
        {
            get;
            set;
        }
        [DataMember]
        public int Loyalty
        {
            get;
            set;

        }
        
	}
}
