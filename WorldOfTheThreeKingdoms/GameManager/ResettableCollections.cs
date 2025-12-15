using System;
using System.Collections.Generic;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameManager
{
    /// <summary>
    /// 可重置的List包装器
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    public class ResettableList<T> : IResettable
    {
        private List<T> _list;

        public ResettableList()
        {
            _list = new List<T>();
        }

        public ResettableList(int capacity)
        {
            _list = new List<T>(capacity);
        }

        // 委托List的常用方法
        public int Count => _list.Count;
        public T this[int index] 
        { 
            get => _list[index]; 
            set => _list[index] = value; 
        }

        public void Add(T item) => _list.Add(item);
        public void AddRange(IEnumerable<T> items) => _list.AddRange(items);
        public bool Remove(T item) => _list.Remove(item);
        public void RemoveAt(int index) => _list.RemoveAt(index);
        public bool Contains(T item) => _list.Contains(item);
        public int IndexOf(T item) => _list.IndexOf(item);
        public void Insert(int index, T item) => _list.Insert(index, item);
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        /// <summary>
        /// 重置列表 - 清空但保留容量
        /// </summary>
        public void Reset()
        {
            _list.Clear();
        }

        /// <summary>
        /// 获取内部列表的只读访问
        /// </summary>
        public IReadOnlyList<T> AsReadOnly() => _list.AsReadOnly();

        /// <summary>
        /// 转换为普通List（谨慎使用）
        /// </summary>
        public List<T> ToList() => new List<T>(_list);
    }

    /// <summary>
    /// 可重置的Dictionary包装器
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public class ResettableDictionary<TKey, TValue> : IResettable
    {
        private Dictionary<TKey, TValue> _dict;

        public ResettableDictionary()
        {
            _dict = new Dictionary<TKey, TValue>();
        }

        public ResettableDictionary(int capacity)
        {
            _dict = new Dictionary<TKey, TValue>(capacity);
        }

        // 委托Dictionary的常用方法
        public int Count => _dict.Count;
        public TValue this[TKey key] 
        { 
            get => _dict[key]; 
            set => _dict[key] = value; 
        }

        public void Add(TKey key, TValue value) => _dict.Add(key, value);
        public bool Remove(TKey key) => _dict.Remove(key);
        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);
        public Dictionary<TKey, TValue>.KeyCollection Keys => _dict.Keys;
        public Dictionary<TKey, TValue>.ValueCollection Values => _dict.Values;

        /// <summary>
        /// 重置字典 - 清空但保留容量
        /// </summary>
        public void Reset()
        {
            _dict.Clear();
        }
    }

    /// <summary>
    /// 可重置的HashSet包装器
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public class ResettableHashSet<T> : IResettable
    {
        private HashSet<T> _set;

        public ResettableHashSet()
        {
            _set = new HashSet<T>();
        }

        public ResettableHashSet(int capacity)
        {
            _set = new HashSet<T>();
        }

        // 委托HashSet的常用方法
        public int Count => _set.Count;
        public bool Add(T item) => _set.Add(item);
        public bool Remove(T item) => _set.Remove(item);
        public bool Contains(T item) => _set.Contains(item);
        public void UnionWith(IEnumerable<T> other) => _set.UnionWith(other);
        public void IntersectWith(IEnumerable<T> other) => _set.IntersectWith(other);

        /// <summary>
        /// 重置集合 - 清空但保留容量
        /// </summary>
        public void Reset()
        {
            _set.Clear();
        }
    }

    /// <summary>
    /// 游戏专用的可重置集合 - 部队列表
    /// </summary>
    public class ResettableTroopList : IResettable
    {
        private TroopList _troopList;

        public ResettableTroopList()
        {
            _troopList = new TroopList();
        }

        // 委托TroopList的方法
        public int Count => _troopList.Count;
        public Troop this[int index] => _troopList[index] as Troop;

        public void Add(Troop troop) => _troopList.Add(troop);
        public void Remove(Troop troop) => _troopList.Remove(troop);
        public bool Contains(Troop troop) => _troopList.HasGameObject(troop);
        public void Clear() => _troopList.Clear();

        /// <summary>
        /// 重置部队列表
        /// </summary>
        public void Reset()
        {
            _troopList.Clear();
        }

        /// <summary>
        /// 获取内部TroopList
        /// </summary>
        public TroopList GetTroopList() => _troopList;
    }
}