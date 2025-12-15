using Microsoft.Xna.Framework.Graphics;

namespace WorldOfTheThreeKingdoms.Helpers
{
    /// <summary>
    /// 双向链表的节点
    /// </summary>
    public class LRUCacheNode
    {
        public string Key;              // 资源的路径 (用于从字典反向查找)
        public Texture2D Texture;       // 实际的纹理资源
        public long SizeInBytes;        // 占用显存大小
        public LRUCacheNode Previous;   // 前一个节点
        public LRUCacheNode Next;       // 后一个节点

        public LRUCacheNode(string key, Texture2D texture, long size)
        {
            Key = key;
            Texture = texture;
            SizeInBytes = size;
        }
    }
}