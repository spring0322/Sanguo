using GameObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GameObjects.PersonDetail
{
    [DataContract]
    public class BiographyTable
    {
        [DataMember]
        public Dictionary<int, Biography> Biographys = new Dictionary<int, Biography>();

        public bool AddBiography(Biography biography)
        {
            if (this.Biographys.ContainsKey(biography.ID))
            {
                this.Biographys.Remove(biography.ID);
            }
            this.Biographys.Add(biography.ID, biography);
            return true;
        }

        public void Clear()
        {
            this.Biographys.Clear();
        }

        public Biography GetBiography(int biographyID)
        {
            Biography biography = null;
            var exists = this.Biographys.TryGetValue(biographyID, out biography);
            
            // 🔥 调试：记录查找失败的情况
            if (!exists && biographyID < 5)
            {
                System.Diagnostics.Debug.WriteLine($"[GetBiography] ID={biographyID} 不存在于字典中，字典总数={this.Biographys.Count}");
            }
            
            return biography;
        }

        public GameObjectList GetBiographyList()
        {
            GameObjectList list = new GameObjectList();
            foreach (Biography biography in this.Biographys.Values)
            {
                list.Add(biography);
            }
            return list;
        }

        public int Count
        {
            get
            {
                return this.Biographys.Count;
            }
        }
    }
}

