using System;
using GameObjects;
using GameObjects.TroopDetail;
using GameManager;
using Microsoft.Xna.Framework;

namespace WorldOfTheThreeKingdoms.Tests.AsyncPathfinding
{
    /// <summary>
    /// 测试场景构建器
    /// 用于创建最小化的测试场景和部队
    /// </summary>
    public class TestScenarioBuilder : IDisposable
    {
        private GameScenario? _currentScenario;
        private Session? _currentSession;

        /// <summary>
        /// 创建测试 Session（包含场景）
        /// </summary>
        public Session CreateSession()
        {
            _currentSession = new Session();
            _currentScenario = CreateMinimalScenario();
            _currentSession.Scenario = _currentScenario;
            Session.Current = _currentSession;
            return _currentSession;
        }

        /// <summary>
        /// 创建最小化的测试场景
        /// </summary>
        public GameScenario CreateMinimalScenario()
        {
            _currentScenario = new GameScenario();
            
            // GameScenario 构造函数已经初始化了集合，无需重新赋值
            // 只需要初始化地图数据
            
            // 创建简单的地图（200x200 用于性能测试）
            _currentScenario.ScenarioMap.MapDimensions = new Point(200, 200);
            _currentScenario.ScenarioMap.MapData = new int[200, 200];
            
            // 初始化地形数据（全部设为平原）
            for (int x = 0; x < 200; x++)
            {
                for (int y = 0; y < 200; y++)
                {
                    _currentScenario.ScenarioMap.MapData[x, y] = 1; // 平原
                }
            }
            
            return _currentScenario;
        }

        /// <summary>
        /// 创建测试部队（简化接口）
        /// </summary>
        /// <param name="scenario">场景</param>
        /// <param name="position">位置</param>
        /// <param name="name">部队名称</param>
        public Troop CreateTroop(GameScenario scenario, Point position, string name = "测试部队")
        {
            return CreateTestTroop(scenario, position, name, true);
        }

        /// <summary>
        /// 创建测试部队
        /// </summary>
        /// <param name="scenario">场景</param>
        /// <param name="position">位置</param>
        /// <param name="name">部队名称</param>
        /// <param name="addToScenario">是否添加到场景（默认 true）</param>
        public Troop CreateTestTroop(GameScenario scenario, Point position, string name = "测试部队", bool addToScenario = true)
        {
            var troop = new Troop();
            troop.ID = scenario.Troops.GetFreeGameObjectID();
            troop.Position = position;
            troop.Init();
            
            // 创建最小化的军队
            var military = new Military
            {
                ID = scenario.Militaries.GetFreeGameObjectID(),
                Kind = CreateTestMilitaryKind()
            };
            scenario.Militaries.AddMilitary(military);
            troop.Army = military;
            
            // 创建最小化的势力
            if (scenario.Factions.Count == 0)
            {
                var faction = new Faction { ID = 1 };
                scenario.Factions.AddFactionWithEvent(faction);
            }
            troop.BelongedFaction = scenario.Factions[0] as Faction;
            
            if (addToScenario)
            {
                scenario.Troops.AddTroopWithEvent(troop);
            }
            
            return troop;
        }

        /// <summary>
        /// 创建测试兵种
        /// </summary>
        private MilitaryKind CreateTestMilitaryKind()
        {
            return new MilitaryKind
            {
                ID = 1,
                Name = "测试兵种",
                Type = MilitaryType.步兵
            };
        }

        public void Dispose()
        {
            // 清理测试场景
            if (_currentScenario != null)
            {
                _currentScenario = null;
            }
            
            if (_currentSession != null)
            {
                _currentSession = null;
            }
        }
    }
}
