using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// PersonList扩展方法
    /// </summary>
    public static class PersonListExtensions
    {
        /// <summary>
        /// Where扩展方法
        /// </summary>
        public static List<Person> Where(this PersonList personList, Func<Person, bool> predicate)
        {
            var result = new List<Person>();
            foreach (Person person in personList.GetList())
            {
                if (person != null && predicate(person))
                {
                    result.Add(person);
                }
            }
            return result;
        }

        /// <summary>
        /// OrderByDescending扩展方法
        /// </summary>
        public static List<Person> OrderByDescending<TKey>(this PersonList personList, Func<Person, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var list = new List<Person>();
            foreach (var obj in personList.GetList())
            {
                if (obj is Person p) list.Add(p);
            }
            list.Sort((x, y) => keySelector(y).CompareTo(keySelector(x)));
            return list;
        }

        /// <summary>
        /// OrderBy扩展方法
        /// </summary>
        public static List<Person> OrderBy<TKey>(this PersonList personList, Func<Person, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var list = new List<Person>();
            foreach (var obj in personList.GetList())
            {
                if (obj is Person p) list.Add(p);
            }
            list.Sort((x, y) => keySelector(x).CompareTo(keySelector(y)));
            return list;
        }

        /// <summary>
        /// OrderByDescending扩展方法 - 用于List<Person>
        /// </summary>
        public static List<Person> OrderByDescending<TKey>(this List<Person> list, Func<Person, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var result = new List<Person>(list);
            result.Sort((x, y) => keySelector(y).CompareTo(keySelector(x)));
            return result;
        }

        /// <summary>
        /// Select扩展方法
        /// </summary>
        public static List<TResult> Select<TResult>(this PersonList personList, Func<Person, TResult> selector)
        {
            var result = new List<TResult>();
            foreach (Person person in personList.GetList())
            {
                if (person != null)
                {
                    result.Add(selector(person));
                }
            }
            return result;
        }

        /// <summary>
        /// FirstOrDefault扩展方法
        /// </summary>
        public static Person FirstOrDefault(this List<Person> list)
        {
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// FirstOrDefault扩展方法 - 用于PersonList
        /// </summary>
        public static Person FirstOrDefault(this PersonList personList, Func<Person, bool> predicate = null)
        {
            foreach (Person person in personList.GetList())
            {
                if (person != null && (predicate == null || predicate(person)))
                {
                    return person;
                }
            }
            return null;
        }

        /// <summary>
        /// Take扩展方法
        /// </summary>
        public static List<Person> Take(this PersonList personList, int count)
        {
            var result = new List<Person>();
            var list = personList.GetList();
            for (int i = 0; i < Math.Min(count, list.Count); i++)
            {
                if (list[i] is Person p)
                {
                    result.Add(p);
                }
            }
            return result;
        }

        /// <summary>
        /// Contains扩展方法
        /// </summary>
        public static bool Contains(this PersonList personList, Person person)
        {
            foreach (var obj in personList.GetList())
            {
                if (obj == person) return true;
            }
            return false;
        }
    }
}