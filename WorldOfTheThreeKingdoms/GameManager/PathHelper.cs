
using System;
using System.IO;
using Platforms;

namespace WorldOfTheThreeKingdoms.GameManager
{
    public static class PathHelper
    {
        public static string GetGameDataPath()
        {
            return Platform.Current.DirectoryName(Platform.Current.Location) + "/GameData";
        }
    }
}
