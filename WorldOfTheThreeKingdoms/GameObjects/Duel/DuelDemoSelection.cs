using System.Collections.Generic;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public static class DuelDemoSelection
{
    private static readonly List<Person> SelectedPersons = [];

    public static int Count => SelectedPersons.Count;

    public static void Clear()
    {
        SelectedPersons.Clear();
    }

    public static void Add(Person person)
    {
        if (person == null)
        {
            return;
        }

        SelectedPersons.Add(person);
    }

    public static List<int> GetSelectedFactionIds()
    {
        List<int> ids = [];
        for (int i = 0; i < SelectedPersons.Count; i++)
        {
            Person person = SelectedPersons[i];
            if (person?.BelongedFaction == null)
            {
                continue;
            }

            int id = person.BelongedFaction.ID;
            if (!ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    public static List<int> GetLastTwoPersonIds()
    {
        List<int> ids = [];
        int count = SelectedPersons.Count;
        if (count == 0)
        {
            return ids;
        }

        int start = count > 1 ? count - 2 : count - 1;
        for (int i = start; i < count; i++)
        {
            Person person = SelectedPersons[i];
            if (person == null)
            {
                continue;
            }

            ids.Add(person.ID);
        }

        return ids;
    }

    public static List<Person> GetLastTwoPersons()
    {
        List<Person> persons = [];
        int count = SelectedPersons.Count;
        if (count == 0)
        {
            return persons;
        }

        int start = count > 1 ? count - 2 : count - 1;
        for (int i = start; i < count; i++)
        {
            Person person = SelectedPersons[i];
            if (person != null)
            {
                persons.Add(person);
            }
        }

        return persons;
    }
}
