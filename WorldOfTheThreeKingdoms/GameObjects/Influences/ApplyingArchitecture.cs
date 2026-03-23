

namespace GameObjects.Influences
{
    public class ApplyingArchitecture
    {
        public Architecture arch;
        public Applier applier;
        public int applierID;

        private ApplyingArchitecture() { }
        public ApplyingArchitecture(Architecture a, Applier p, int i)
        {
            this.arch = a;
            this.applier = p;
            this.applierID = i;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is not ApplyingArchitecture a) return false;
            
            // 🔥 V17 修复：使用 Architecture.ID 比较，而不是引用比较
            // 原因：读档后 Architecture 对象会重新创建，引用会变化
            // 但 ID 是持久化的，可以正确匹配
            return a.applierID == this.applierID && 
                   a.applier == this.applier && 
                   a.arch.ID == this.arch.ID;  // 关键修复！
        }

        public override int GetHashCode()
        {
            // 🔥 V17 修复：使用 Architecture.ID 计算哈希，保持一致性
            return 158 * this.arch.ID.GetHashCode() + 37 * this.applier.GetHashCode() + this.applierID;
        }
    }
}
