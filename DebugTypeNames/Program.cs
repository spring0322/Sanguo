// 调试类型名称
using System;
using GameObjects.TroopDetail.EventEffect;

namespace DebugTypeNames
{
    class Program
    {
        static void Main(string[] args)
        {
            var eventEffectTable = new EventEffectTable();
            Console.WriteLine($"EventEffectTable 完整类型名: {eventEffectTable.GetType().FullName}");
            Console.WriteLine($"EventEffectTable 类型: {eventEffectTable.GetType()}");
            Console.WriteLine($"typeof(EventEffectTable): {typeof(EventEffectTable)}");
            Console.WriteLine($"typeof(EventEffectTable).FullName: {typeof(EventEffectTable).FullName}");
        }
    }
}