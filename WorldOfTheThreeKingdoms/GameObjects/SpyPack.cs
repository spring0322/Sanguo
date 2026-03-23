using System;


namespace GameObjects
{

    public class SpyPack
    {
        public int Days;
        public Person SpyPerson;

        private SpyPack() { }
        public SpyPack(Person person, int days)
        {
            this.SpyPerson = person;
            this.Days = days;
        }
    }
}

