using Microsoft.Xna.Framework;
using System;
using System.Runtime.Serialization;

namespace GameObjects
{
    [DataContract]
    public class NoFoodPosition
    {
        [DataMember]
        public int Days;

        [DataMember]
        public Point Position;

        private NoFoodPosition() { }
        public NoFoodPosition(Point position, int days)
        {
            this.Position = position;
            this.Days = days;
        }
    }
}

