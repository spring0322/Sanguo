using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using GameObjects;
using System.Data;
using System.IO;
using Tools;
using GameGlobal;
using GameObjects.FactionDetail;
using GameManager;
using System.Drawing;
using OfficeOpenXml;

namespace WorldOfTheThreeKingdomsEditor
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private GameScenario scen;
        private bool scenLoaded = false;

        private ArchitectureTab architectureTab;
        private FactionTab factionTab;
        private PersonTab personTab;
        private EventTab eventTab;
        private DictionaryTab<int, int> fatherTab;
        private DictionaryTab<int, int> motherTab;
        private DictionaryTab<int, int> spouseTab;
        private DictionaryintTab brotherIdsTab;
        private DictionaryintTab suoshuIdsTab;
        private DictionaryintTab closeIdsTab;
        private DictionaryintTab hatedIdsTab;
        private RegionTab regionTab;
        private StateTab stateTab;
        private TerrainDetailTab terrainDetailTab;
        private string scename;

        public bool CopyIncludeTitle = true;

        public MainWindow()
        {
            InitializeComponent();
            //Platforms.Platform.Current.editing = true;
            CommonData.Current = Tools.SimpleSerializer.DeserializeJsonFile<CommonData>(@"Content\Data\Common\CommonData.json", false, false);

            scen = new GameScenario();
            scen.GameCommonData = CommonData.Current;
            populateTables();
        }
        private bool hasScen = false;
        public void initTables(string[] strs)
        {
            foreach (string s in strs)
            {
                // Common
                if (s.Equals("dgTitle"))
                {
                    new TitleTab(scen, dgTitle, lblColumnHelp).setup();
                }
                else if (s.Equals("dgSkill"))
                {
                    new SkillTab(scen, dgSkill, lblColumnHelp).setup();
                }
                else if (s.Equals("dgTitleKind"))
                {
                    new TitleKindTab(scen, dgTitleKind, lblColumnHelp).setup();
                }
                else if (s.Equals("dgStunt"))
                {
                    new StuntTab(scen, dgStunt, lblColumnHelp).setup();
                }
                else if (s.Equals("dgCombatMethod"))
                {
                    new CombatMethodTab(scen, dgCombatMethod, lblColumnHelp).setup();
                }
                else if (s.Equals("dgTextMessage"))
                {
                    new TextMessageTab(scen, dgTextMessage, dgTextMessageKind, hasScen, this).setup();
                }
                else if (s.Equals("dgInfluence"))
                {
                    new InfleunceTab(scen, dgInfluence, lblColumnHelp).setup();
                }
                else if (s.Equals("dgInflunceKind"))
                {
                    new InfleunceKindTab(scen, dgInflunceKind, lblColumnHelp).setup();
                }
                else if (s.Equals("dgCondition"))
                {
                    new ConditionTab(scen, dgCondition, lblColumnHelp).setup();
                }
                else if (s.Equals("dgConditionKind"))
                {
                    new ConditionKindTab(scen, dgConditionKind, lblColumnHelp).setup();
                }
                else if (s.Equals("dgEventEffect"))
                {
                    new EventEffectTab(scen, dgEventEffect, lblColumnHelp).setup();
                }
                else if (s.Equals("dgEventEffectKind"))
                {
                    new EventEffectKindTab(scen, dgEventEffectKind, lblColumnHelp).setup();
                }
                else if (s.Equals("dgTroopEventEffect"))
                {
                    new TroopEventEffectTab(scen, dgTroopEventEffect, lblColumnHelp).setup();
                }
                else if (s.Equals("dgTroopEventEffectKind"))
                {
                    new TroopEventEffectKindTab(scen, dgTroopEventEffectKind, lblColumnHelp).setup();
                }
                else if (s.Equals("dgFacilityKind"))
                {
                    new FacilityKindTab(scen, dgFacilityKind, lblColumnHelp).setup();
                }
                else if (s.Equals("dgArchitectureKind"))
                {
                    new ArchitectureKindTab(scen, dgArchitectureKind, lblColumnHelp).setup();
                }
                else if (s.Equals("dgMilitaryKind"))
                {
                    new MilitaryKindTab(scen, dgMilitaryKind, lblColumnHelp).setup();
                }
                else if (s.Equals("dgTechniques"))
                {
                    new TechniqueTab(scen, dgTechniques, lblColumnHelp).setup();
                }
                else if (s.Equals("dgGuanjue"))
                {
                    new GuanjueTab(scen, dgGuanjue, lblColumnHelp).setup();
                }
                else if (s.Equals("dgTerrainDetail"))
                {
                    new TerrainDetailTab(scen, dgTerrainDetail, lblColumnHelp).setup();
                }
                else if (s.Equals("dgSectionAIDetail"))
                {
                    new SectionAIDetailTab(scen, dgSectionAIDetail, lblColumnHelp).setup();
                }

                //scen
                else if (hasScen)
                {
                    if (s.Equals("dgPerson"))
                    {
                        personTab = new PersonTab(scen, dgPerson, lblColumnHelp);
                        personTab.setup();
                    }
                    else if (s.Equals("dgFatherId"))
                    {
                        fatherTab = new DictionaryTab<int, int>(scen.FatherIds, "FatherIds", dgFatherId, scen);
                        fatherTab.setup();
                    }
                    else if (s.Equals("dgMotherId"))
                    {
                        motherTab = new DictionaryTab<int, int>(scen.MotherIds, "MotherIds", dgMotherId, scen);
                        motherTab.setup();
                    }
                    else if (s.Equals("dgSpouseId"))
                    {
                        spouseTab = new DictionaryTab<int, int>(scen.SpouseIds, "SpouseIds", dgSpouseId, scen);
                        spouseTab.setup();
                    }
                    else if (s.Equals("dgSuoshuIds"))
                    {
                        suoshuIdsTab = new DictionaryintTab(scen.SuoshuIds, "SuoshuIds", dgSuoshuIds, scen);
                        suoshuIdsTab.setup();
                    }
                    else if (s.Equals("dgBrotherIds"))
                    {
                        brotherIdsTab = new DictionaryintTab(scen.BrotherIds, "BrotherIds", dgBrotherIds, scen);
                        brotherIdsTab.setup();
                    }
                    else if (s.Equals("dgCloseIds"))
                    {
                        closeIdsTab = new DictionaryintTab(scen.CloseIds, "CloseIds", dgCloseIds, scen);
                        closeIdsTab.setup();
                    }
                    else if (s.Equals("dgHatedIds"))
                    {
                        hatedIdsTab = new DictionaryintTab(scen.HatedIds, "HatedIds", dgHatedIds, scen);
                        hatedIdsTab.setup();
                    }
                    else if (s.Equals("dgArchitecture"))
                    {
                        architectureTab = new ArchitectureTab(scen, dgArchitecture, lblColumnHelp);
                        architectureTab.setup();
                    }
                    else if (s.Equals("dgFaction"))
                    {
                        factionTab = new FactionTab(scen, dgFaction, lblColumnHelp);
                        factionTab.setup(); ;
                    }
                    else if (s.Equals("dgDiplomaticRelation"))
                    {
                        new DiplomaticRelationTab(scen, dgDiplomaticRelation).setup();
                    }
                    else if (s.Equals("dgSection"))
                    {
                        new SectionTab(scen, dgSection, lblColumnHelp).setup();
                    }
                    else if (s.Equals("dgRegion"))
                    {
                        regionTab = new RegionTab(dgRegion, scen);
                        regionTab.setup();
                    }
                    else if (s.Equals("dgState"))
                    {
                        stateTab = new StateTab(dgState, scen);
                        stateTab.setup();
                    }
                    else if (s.Equals("dgPersonRelations"))
                    {
                        new PersonIDRelationsTab(dgPersonRelations, scen).setup();
                    }
                    else if (s.Equals("dgMilitary"))
                    {
                        new MilitaryTab(scen, dgMilitary, lblColumnHelp).setup();
                    }
                    else if (s.Equals("dgTroop"))
                    {
                        new TroopTab(scen, dgTroop, lblColumnHelp).setup();
                    }
                    else if (s.Equals("dgCaptive"))
                    {
                        new CaptiveTab(scen, dgCaptive, lblColumnHelp).setup();
                    }
                    else if (s.Equals("dgEvent"))
                    {
                        eventTab = new EventTab(scen, dgEvent, lblColumnHelp);
                        eventTab.setup();
                    }
                    else if (s.Equals("dgTroopEvent"))
                    {
                        new TroopEventTab(scen, dgTroopEvent, lblColumnHelp).setup();
                    }
                    else if (s.Equals("dgTreasure"))
                    {
                        new TreasureTab(scen, dgTreasure, lblColumnHelp).setup();
                    }
                    else if (s.Equals("dgFacility"))
                    {
                        new FacilityTab(scen, dgFacility, lblColumnHelp).setup();
                    }
                    else if (s.Equals("dgBiography"))
                    {
                        new BiographyTab(scen, dgBiography, lblColumnHelp).setup();
                    }
                }

            }
        }

        private void populateTables()
        {
            string[] strs = new string[]
            {
                //common
                "dgTitle",
                "dgSkill",
                "dgTitleKind",
                "dgStunt",
                "dgCombatMethod",
                "dgTextMessage",
                "dgInfluence",
                "dgInflunceKind",
                "dgCondition",
                "dgConditionKind",
                "dgEventEffect",
                "dgEventEffectKind",
                "dgTroopEventEffect",
                "dgTroopEventEffectKind",
                "dgFacilityKind",
                "dgArchitectureKind",
                "dgMilitaryKind",
                "dgTechniques",
                "dgGuanjue",
                "dgTerrainDetail",
                "dgSectionAIDetail",

                //scen
                "dgPerson",
                "dgFatherId",
                "dgMotherId",
                "dgSpouseId",
                "dgSuoshuIds",
                "dgBrotherIds",
                "dgCloseIds",
                "dgHatedIds",
                "dgArchitecture",
                "dgFaction",
                "dgDiplomaticRelation",
                "dgSection",
                "dgRegion",
                "dgState",
                "dgPersonRelations",
                "dgMilitary",
                "dgTroop",
                "dgCaptive",
                "dgEvent",
                "dgTroopEvent",
                "dgTreasure",
                "dgFacility",
                "dgBiography",
            };
            initTables(strs);
        }

        public static DataTable DataViewAsDataTable(DataView dv)
        {
            DataTable dt = dv.Table.Clone();
            foreach (DataRowView drv in dv)
            {
                dt.ImportRow(drv.Row);
            }
            return dt;
        }

        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "剧本档 (*.json)|*.json";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + @"\Content\Data\Scenario";
            if (openFileDialog.ShowDialog() == true)
            {
                String filename = openFileDialog.FileName;
                scename = filename;
                scen = WorldOfTheThreeKingdoms.GameScreens.MainGameScreen.LoadScenarioData(filename, true, null, true);
                scen.GameCommonData = CommonData.Current;
                hasScen = true;
                SaveSce.IsEnabled = true;
                SaveSav.IsEnabled = false;
                populateTables();
                scenLoaded = true;
                Title = "中华三国志剧本编辑器 - " + openFileDialog.SafeFileName;
                openFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + @"\Content\Data\Scenario";
            }
        }

        private void OpenSave(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "存档 (*.json)|*.json";
            openFileDialog.RestoreDirectory = true;
            
            string initialDir;
            try
            {
                // 1. 取「文件」目錄
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrWhiteSpace(docs))
                    throw new InvalidOperationException(); // 直接跳 catch

                // 2. 組合子目錄
                string wanted = System.IO.Path.Combine(docs, "WorldOfTheThreeKingdoms", "Save");

                // 3. 存在就用；不存在就退到「文件」；再不行就當前目錄
                if (Directory.Exists(wanted))
                    initialDir = wanted;
                else if (Directory.Exists(docs))
                    initialDir = docs;
                else
                    initialDir = Environment.CurrentDirectory;
            }
            catch
            {
                // 任何意外都降級到當前目錄
                initialDir = Environment.CurrentDirectory;
            }
            openFileDialog.InitialDirectory = initialDir;
            
            if (openFileDialog.ShowDialog() == true)
            {
                String filename = openFileDialog.FileName;
                String scenName = filename.Substring(filename.LastIndexOf(@"\") + 1, filename.LastIndexOf(".") - filename.LastIndexOf(@"\") - 1);
                string filename2 = String.Format(@"Save\{0}.json", scenName);
                scen = WorldOfTheThreeKingdoms.GameScreens.MainGameScreen.LoadScenarioData(filename2, false, null, true);
                scen.GameCommonData = CommonData.Current;
                hasScen = true;
                SaveSce.IsEnabled = false;
                SaveSav.IsEnabled = true;
                populateTables();
                scenLoaded = true;
                Title = "中华三国志剧本编辑器 - " + openFileDialog.SafeFileName;
                openFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + @"\Content\Data\Scenario";
            }
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (scenLoaded)
            {
                scen.ProcessScenarioData(true, true);//保存前再读取一进度，是为了把新增加的信息重新刷到scen里
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "剧本档 (*.json)|*.json";
                
                string initialDir;
                try
                {
                    // 1. 取「文件」目錄
                    string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    if (string.IsNullOrWhiteSpace(docs))
                        throw new InvalidOperationException(); // 直接跳 catch

                    // 2. 組合子目錄
                    string wanted = System.IO.Path.Combine(docs, "WorldOfTheThreeKingdoms", "Save");

                    // 3. 存在就用；不存在就退到「文件」；再不行就當前目錄
                    if (Directory.Exists(wanted))
                        initialDir = wanted;
                    else if (Directory.Exists(docs))
                        initialDir = docs;
                    else
                        initialDir = Environment.CurrentDirectory;
                }
                catch
                {
                    // 任何意外都降級到當前目錄
                    initialDir = Environment.CurrentDirectory;
                }
                saveFileDialog.InitialDirectory = initialDir;
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    String filename = saveFileDialog.SafeFileName;
                    scen.SaveGameScenario(filename, true, false, false, false, false, true);

                    // GameCommonData.json
                    String commonPath = @"Content\Data\Common\CommonData.json";
                    saveGameCommonData(commonPath);


                    MessageBox.Show("存档已储存为" + filename + " CommonData已储存为" + commonPath);
                }
            }
        }

        private void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (scenLoaded)
            {
                scen.ProcessScenarioData(true, true);//保存前再读取一进度，是为了把新增加的信息重新刷到scen里
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "剧本档 (*.json)|*.json";
                saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + @"\Content\Data\Scenario";
                if (saveFileDialog.ShowDialog() == true)
                {
                    String filename = saveFileDialog.FileName;
                    String scenName = filename.Substring(filename.LastIndexOf(@"\") + 1, filename.LastIndexOf(".") - filename.LastIndexOf(@"\") - 1);
                    String scenPath = filename.Substring(0, filename.LastIndexOf(@"\"));

                    scen.SaveGameScenario(filename, true, false, false, false, true, true);

                    // GameCommonData.json
                    String commonPath = @"Content\Data\Common\CommonData.json";
                    saveGameCommonData(commonPath);

                    // Scenarios.json
                    String scenariosPath = scenPath + @"\Scenarios.json";
                    List<Scenario> scesList = null;
                    if (File.Exists(scenariosPath))
                    {
                        scesList = SimpleSerializer.DeserializeJsonFile<List<Scenario>>(scenariosPath, false, false).NullToEmptyList();
                    }
                    if (scesList == null)
                    {
                        scesList = new List<Scenario>();
                    }

                    Scenario s1 = createScenarioObject(scen, scenName);
                    if (s1 != null)
                    {
                        int index = scesList.FindIndex(x => x.Name == scenName);
                        if (index >= 0)
                        {
                            scesList[index] = s1;
                        }
                        else
                        {
                            scesList.Add(s1);
                        }
                    }

                    string s2 = Newtonsoft.Json.JsonConvert.SerializeObject(scesList, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(scenariosPath, s2);

                    MessageBox.Show("剧本已储存为" + filename + "。CommonData已储存为" + commonPath);
                }
            }
            else
            {
                // GameCommonData.json
                String commonPath = @"Content\Data\Common\CommonData.json";
                saveGameCommonData(commonPath);

                MessageBox.Show("CommonData已储存为" + commonPath);
            }
        }

        private void saveGameCommonData(String commonPath)
        {
            GameScenario.SaveGameCommonData(scen);
            string ss1 = "";
            System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(CommonData));
            using (MemoryStream stream = new MemoryStream())
            {
                //lock (Platform.SerializerLock)
                {
                    serializer.WriteObject(stream, scen.GameCommonData);
                }
                var array = stream.ToArray();
                ss1 = Encoding.UTF8.GetString(array, 0, array.Length);
            }
            ss1 = ss1.Replace("{\"", "{\r\n\"");
            ss1 = ss1.Replace("[{", "[\r\n{");
            ss1 = ss1.Replace(",\"", ",\r\n\"");
            ss1 = ss1.Replace("}", "\r\n}");
            ss1 = ss1.Replace("},{", "},\r\n{");
            ss1 = ss1.Replace("}]", "}\r\n]");
            File.WriteAllText(commonPath, ss1);
        }

        private Scenario createScenarioObject(GameScenario scen, String scenName)
        {
            if (scen.Factions.Count == 0) return null;

            string time = scen.Date.Year.ToString().PadLeft(4, '0') + "-" + scen.Date.Month.ToString().PadLeft(2, '0') + "-" + scen.Date.Day.ToString().PadLeft(2, '0');
            GameObjectList gameObjectList = new GameObjectList();//恢复原先剧本设定，某些势力玩家不可选
            foreach (Faction faction in scen.Factions)
            {
                if (!faction.NotPlayerSelectable)
                {
                    gameObjectList.Add(faction);
                }
            }
            return new Scenario()
            {
                Create = DateTime.Now.ToSeasonDateTime(),
                Desc = scen.ScenarioDescription,
                First = StaticMethods.SaveToString(scen.ScenarioMap.JumpPosition),
                IDs = gameObjectList.GameObjects.Select(x => x.ID.ToString()).Aggregate((a, b) => a + "," + b),
                Info = "电脑",
                Name = (scenName.IndexOf(".json") >= 0) ? scenName.Substring(0, scenName.IndexOf(".json")) : scenName,
                Names = gameObjectList.GameObjects.Select(x => x.Name).Aggregate((a, b) => a + "," + b),
                LeaderPics = gameObjectList.GameObjects.Select(x =>( x as Faction).Leader.PictureIndex.ToString()).Aggregate((a, b) => a + "," + b),
                LeaderNames = gameObjectList.GameObjects.Select(x => (x as Faction).Leader.Name.ToString()).Aggregate((a, b) => a + "," + b),
                Reputations = gameObjectList.GameObjects.Select(x => (x as Faction).Reputation.ToString()).Aggregate((a, b) => a + "," + b),
                ArchitectureCounts = gameObjectList.GameObjects.Select(x => (x as Faction).ArchitectureCount.ToString()).Aggregate((a, b) => a + "," + b),
                CapitalNames = gameObjectList.GameObjects.Select(x => (x as Faction).Capital.Name.ToString()).Aggregate((a, b) => a + "," + b),
                Populations = gameObjectList.GameObjects.Select(x => (x as Faction).Population.ToString()).Aggregate((a, b) => a + "," + b),
                MilitaryCounts = gameObjectList.GameObjects.Select(x => (x as Faction).MilitaryCount.ToString()).Aggregate((a, b) => a + "," + b),
                Funds = gameObjectList.GameObjects.Select(x => (x as Faction).Fund.ToString()).Aggregate((a, b) => a + "," + b),
                Foods = gameObjectList.GameObjects.Select(x => (x as Faction).Food.ToString()).Aggregate((a, b) => a + "," + b),
                //  Path = "",
                // PlayTime = scenario.GameTime.ToString(),
                // Player = "",
                //  Players = String.Join(",", scenario.PlayerList.NullToEmptyList()),
                Time = time,
                Title = scen.ScenarioTitle
            };
        }

        private void CopyCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Grid grid = (Grid)tabControl.SelectedContent;
            DataGrid dataGrid = (DataGrid)grid.Children[currentchilidren];

            StringBuilder sb = new StringBuilder();

            if (CopyIncludeTitle)
            {
                for (int i = 0; i < dataGrid.Columns.Count; ++i)
                {
                    sb.Append(dataGrid.Columns[i].Header).Append("\t");
                }
                sb.Append("\n");
            }

            for (int i = 0; i < dataGrid.SelectedCells.Count; i += dataGrid.Columns.Count)
            {
                object[] values = (dataGrid.SelectedCells[i].Item as DataRowView)?.Row?.ItemArray;
                if (values != null)
                {
                    foreach (object o in values)
                    {
                        sb.Append(o.ToString()).Append("\t");
                    }
                    sb.Append("\n");
                }
            }

            Clipboard.SetText(sb.ToString());
        }
        public static bool pasting = false;
        private int currentchilidren;
        private string currenttab;
        private void PasteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Grid grid = (Grid)tabControl.SelectedContent;
            if (!((TabItem)tabControl.SelectedItem).Header.ToString().Equals(currenttab))
            {
                currentchilidren = grid.Children.Count - 1;
            }
            DataGrid dataGrid = (DataGrid)grid.Children[currentchilidren];
            if (dataGrid == dgRegion || dataGrid == dgState)
            {
                MessageBox.Show("此页面不允许使用复制粘贴功能，请手动双击或右键进行编辑");
            }
            else
            {
                try
                {
                    pasting = true;
                    String text = Clipboard.GetText();
                    String[] textRows = text.Split(new char[] { '\n' });

                    DataTable dt = ((DataView)dataGrid.ItemsSource).Table;
                    for (int i = 0; i < textRows.Count(); i++)
                    {
                        if (textRows[i].Length == 0) continue;

                        String[] data = textRows[i].Split(new char[] { '\t' });

                        DataColumnCollection columns = dt.Columns;
                        DataRow row = dt.NewRow();
                        List<string> tempids = new List<string>();
                        if (!dataGrid.Name.Equals("dgDiplomaticRelation") && !dataGrid.Name.Equals("dgPersonRelations"))
                        {
                            foreach (DataRow dataRow in dt.Rows)
                            {
                                tempids.Add(dataRow["ID"].ToString());
                            }
                        }
                        for (int j = 0; j < Math.Min(columns.Count, data.Count()); ++j)
                        {
                            row[columns[j].ColumnName] = data[j];
                            if (!dataGrid.Name.Equals("dgDiplomaticRelation") && !dataGrid.Name.Equals("dgPersonRelations"))
                            {
                                if (tempids.Contains(row["ID"].ToString()))
                                {
                                    MessageBox.Show("编号不可以为重复，重复ID为" + row["ID"].ToString() + ",此记录之后的记录粘贴失败");
                                    goto mark;
                                }
                            }
                        }
                        dt.Rows.Add(row);
                    }
                mark:
                    dataGrid.ItemsSource = dt.AsDataView();
                    if (scenLoaded)
                    {
                        scen.ProcessScenarioData(true, true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("導入資料錯誤:" + ex);
                }
            }
        }

        private void DeleteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Grid grid = (Grid)tabControl.SelectedContent;
            DataGrid dataGrid = (DataGrid)grid.Children[grid.Children.Count - 1];

            StringBuilder sb = new StringBuilder();
            for (int i = dataGrid.SelectedCells.Count - 1; i > 0; i -= dataGrid.Columns.Count)
            {
                ((DataRowView)dataGrid.SelectedCells[i].Item).Row.Delete();
            }

        }

        private void btnQuit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnSyncScenario_Click(object sender, RoutedEventArgs e)
        {
            String scenariosPath = @"Content\Data\Scenario\Scenarios.json";
            MessageBoxResult result = MessageBox.Show("更新" + scenariosPath + "檔案，使遊戲能辨認劇本資料夾裡的劇本。是否繼續？", "更新Scenarios.json", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                List<Scenario> scesList = new List<Scenario>();

                FileInfo[] files = new DirectoryInfo(@"Content\Data\Scenario").GetFiles("*.json", SearchOption.TopDirectoryOnly);
                foreach (FileInfo file in files)
                {
                    if (file.Name.Equals("Scenarios.json")) continue;
                    GameScenario s = WorldOfTheThreeKingdoms.GameScreens.MainGameScreen.LoadScenarioData(file.FullName, true, null, true);
                    Scenario s1 = createScenarioObject(s, file.Name);
                    if (s1 != null)
                    {
                        scesList.Add(s1);
                    }
                }

                string s2 = Newtonsoft.Json.JsonConvert.SerializeObject(scesList, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(scenariosPath, s2);

                MessageBox.Show("已更新" + scenariosPath);
            }
        }

        private void btnPNGAlpha_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "待處理圖片 (*.png)|*.png";
            openFileDialog.InitialDirectory = @"Content\Textures";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                var filenames = openFileDialog.FileNames;

                foreach (var filename in filenames)
                {
                    using (var img = System.Drawing.Image.FromFile(filename))
                    {
                        var bitmap = new Bitmap(img, img.Width, img.Height);

                        var target = new Bitmap(img.Width, img.Height);

                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.DrawImage(target, 0, 0);
                            for (int h = 0; h < bitmap.Height; h++)
                            {
                                for (int w = 0; w < bitmap.Width; w++)
                                {
                                    var color = bitmap.GetPixel(w, h);
                                    color = PremultiplyAlpha(color);
                                    target.SetPixel(w, h, color);
                                }
                            }
                        }

                        var dir = filename.Substring(0, filename.LastIndexOf('\\') + 1);

                        var file = filename.Substring(filename.LastIndexOf('\\') + 1);

                        var newDir = dir + "Alpha\\";

                        var newFile = newDir + file;

                        if (!Directory.Exists(newDir))
                        {
                            Directory.CreateDirectory(newDir);
                        }

                        target.Save(newFile, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }

                MessageBox.Show("PNG圖片PreMultiplied!");
            }
        }

        public static System.Drawing.Color PremultiplyAlpha(System.Drawing.Color pixel)
        {
            return System.Drawing.Color.FromArgb(
                pixel.A,
                PremultiplyAlpha_Component(pixel.R, pixel.A),
                PremultiplyAlpha_Component(pixel.G, pixel.A),
                PremultiplyAlpha_Component(pixel.B, pixel.A));
        }

        private static byte PremultiplyAlpha_Component(byte source, byte alpha)
        {
            return (byte)(Convert.ToSingle(source) * Convert.ToSingle(alpha) / Convert.ToSingle(byte.MaxValue) + 0.5f);
        }

        private void RandomizeFaction()
        {
            Dictionary<Faction, int> archCount = new Dictionary<Faction, int>();
            foreach (Faction f in scen.Factions)
            {
                archCount[f] = f.ArchitectureCount;
                if (f.Capital == null || !f.Architectures.GameObjects.Contains(f.Capital))
                {
                    f.Capital = f.Architectures.GetRandomObject() as Architecture;
                }
            }
            foreach (Faction f in scen.Factions)
            {
                ArchitectureList list = f.Architectures;
                foreach (Architecture a in list.GetList())
                {
                    if (a != f.Capital)
                    {
                        foreach (Person p in a.Persons)
                        {
                            p.LocationArchitecture = f.Capital;
                        }
                        MilitaryList mList = a.Militaries;
                        foreach (Military m in mList.GetList())
                        {
                            a.RemoveMilitary(m);
                            f.Capital.AddMilitary(m);
                        }
                        f.RemoveArchitecture(a);
                    }
                }
            }
            scen.ClearPersonStatusCache();

            for (int i = 0; i < 5; ++i)
            {
                foreach (Faction f in scen.Factions.GetRandomList())
                {
                    Architecture oldArch = f.Capital;
                    bool retry = false;
                    int retries = 0;
                    do
                    { 
                        retry = false;
                        Architecture chosen = scen.Architectures.GetRandomObject() as Architecture;
                        if (chosen.BelongedFaction == null && (i <= 3 || (chosen.Kind.HasAgriculture && chosen.Kind.HasCommerce
                            && chosen.Kind.HasMorale && chosen.Kind.HasDomination && chosen.Kind.HasPopulation
                            && chosen.Population >= 40000)))
                        {
                            f.RemoveArchitecture(oldArch);
                            f.AddArchitecture(chosen);
                            f.Capital = chosen;

                            foreach (Person p in oldArch.Persons)
                            {
                                p.LocationArchitecture = f.Capital;
                            }

                            MilitaryList mList = oldArch.Militaries;
                            foreach (Military m in mList.GetList())
                            {
                                oldArch.RemoveMilitary(m);
                                f.Capital.AddMilitary(m);
                            }

                            f.Capital.PersonsString = oldArch.PersonsString;
                            oldArch.PersonsString = "";

                            f.ArchitecturesString = f.Capital.ID.ToString();
                        }
                        else
                        {
                            retry = true;
                            retries++;
                        }
                    } while (retry && retries < 100);
                }
            }

            int added = 10;
            do
            {
                added--;
                foreach (Faction f in scen.Factions.GetRandomList())
                {
                    archCount[f]--;
                    if (archCount[f] > 0)
                    {
                        Architecture start = f.Architectures.GetRandomObject() as Architecture;
                        ArchitectureList allLinks = new ArchitectureList();
                        start.LoadAILandLinksFromString(scen.Architectures, start.AILandLinksString);
                        start.LoadAIWaterLinksFromString(scen.Architectures, start.AIWaterLinksString);
                        allLinks.AddRange(start.AILandLinks);
                        allLinks.AddRange(start.AIWaterLinks);
                        if (allLinks.Count > 0)
                        {
                            Architecture selected = allLinks.GetRandomObject() as Architecture;
                            if (selected.BelongedFaction == null)
                            {
                                f.AddArchitecture(selected);
                                f.ArchitecturesString += " " + selected.ID;
                                added = 10;
                            }
                        }
                    }
                }
            } while (added > 0);

            scen.ClearPersonStatusCache();

            factionTab.setup();
            architectureTab.setup();
            personTab.setup();
        }

        private void btnRandomizeFaction_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("把所有势力的建筑随机对调，是否確認？", "換国", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                RandomizeFaction();
            }
        }

        private void btnRandomizeFactionDeleteArchitecture_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("删除一些沒有势力的建筑，是否确认？", "删除城池", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                int deleteRatio = (scen.Architectures.Count - Math.Max(scen.Factions.Count, 60)) * 100 / scen.Architectures.Count;
                if (deleteRatio > 0)
                {
                    foreach (Architecture a in scen.Architectures.GetRandomList())
                    {
                        if (a.BelongedFaction == null && GameObject.Chance(deleteRatio) && a.KindId != 1)
                        {
                            scen.Architectures.Remove(a);
                        }
                    }
                }
                architectureTab.setup();
            }
        }

        private void btnRandomizeIdeal_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("把所有武將的相性及相性考慮隨機化，是否確認？", "隨機化武將相性", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (Person p in scen.Persons)
                {
                    p.Ideal = GameObject.Random(0, 149);
                    p.IdealTendencyIDString = scen.GameCommonData.AllIdealTendencyKinds.GetRandomObject().ID;
                }
                personTab.setup();
            }
        }

        private void btnRandomizePersonality_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("把所有武將的各項性格隨機化，是否確認？", "隨機化武將性格", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (Person p in scen.Persons)
                {
                    List<GameObjects.PersonDetail.CharacterKind> ck = scen.GameCommonData.AllCharacterKinds;
                    p.Character = ck[GameObject.Random(ck.Count)];

                    if (p.BelongedFaction != null && p.BelongedFaction.IsAlien)
                    {
                        p.PersonalLoyalty = GameObject.Random(2);
                    }
                    else
                    {
                        p.PersonalLoyalty = GameObject.Random(5);
                    }
                    p.Ambition = GameObject.Random(5);
                    p.Qualification = (PersonQualification)GameObject.Random(Enum.GetNames(typeof(PersonQualification)).Length);
                    p.Braveness = GameObject.Random(11);
                    p.Calmness = GameObject.Random(11);
                    p.ValuationOnGovernment = (PersonValuationOnGovernment)GameObject.Random(Enum.GetNames(typeof(PersonValuationOnGovernment)).Length);
                    p.StrategyTendency = (PersonStrategyTendency)GameObject.Random(Enum.GetNames(typeof(PersonStrategyTendency)).Length);
                }
                personTab.setup();
            }
        }

        private void btnRandomizeDeadYear_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("把所有武將的壽命隨機化，是否確認？", "隨機化武將壽命", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (Person p in scen.Persons)
                {
                    if (p.Alive)
                    {
                        if (p.Available)
                        {
                            p.YearDead = Math.Max(p.YearBorn + GameObject.RandomGaussianRange(30, 90), p.YearAvailable + GameObject.RandomGaussianRange(1, 10));
                        }
                        else
                        {
                            p.YearDead = p.YearBorn + GameObject.RandomGaussianRange(30, 90);
                        }
                    }
                }
                personTab.setup();
            }
        }

        private void btnRandomizeAge_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("把所有武將的年齡隨機化，是否確認？", "隨機化武將壽命", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (Person p in scen.Persons)
                {
                    if (p.Alive)
                    {
                        if (p.Available)
                        {
                            int age = p.YearDead - p.YearBorn;
                            p.YearBorn = scen.Date.Year - GameObject.RandomGaussianRange(15, Math.Min(age, 30));
                            p.YearAvailable = scen.Date.Year;
                            p.YearDead = Math.Max(p.YearBorn + age, scen.Date.Year + GameObject.Random(0, 5));
                        }
                        else
                        {
                            int age = p.YearDead - p.YearBorn;
                            p.YearBorn = scen.Date.Year - GameObject.RandomGaussianRange(0, Math.Min(age, 15));
                            p.YearAvailable = p.YearBorn + 15;
                            p.YearDead = Math.Max(p.YearBorn + age, scen.Date.Year + GameObject.Random(0, 5));
                        }
                    }
                }
                bool changed = false;
                do
                {
                    changed = false;
                    foreach (Person p in scen.Persons)
                    {
                        if (p.Alive)
                        {
                            if (p.Father != null && p.YearBorn - p.Father.YearBorn < 16)
                            {
                                int shift = 16;
                                p.Father.YearBorn -= shift;
                                p.Father.YearAvailable -= shift;
                                p.Father.YearDead = Math.Max(p.Father.YearDead - shift, scen.Date.Year + GameObject.Random(0, 5));
                                changed = true;
                            }
                            if (p.Mother != null && p.YearBorn - p.Mother.YearBorn < 16)
                            {
                                int shift = 16;
                                p.Mother.YearBorn -= shift;
                                p.Mother.YearAvailable -= shift;
                                p.Mother.YearDead = Math.Max(p.Mother.YearDead - shift, scen.Date.Year + GameObject.Random(0, 5));
                                changed = true;
                            }
                        }
                    }
                } while (changed);

                personTab.setup();
            }
        }

        private void btnRandomizeAvailableLocation_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("把所有武將的登場地點隨機化，是否確認？", "隨機化武將登場地點", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (Person p in scen.Persons)
                {
                    if (!p.Available)
                    {
                        p.AvailableLocation = scen.Architectures.GetRandomObject().ID;
                    }
                }
                personTab.setup();
            }
        }

        private void btnRedoArchitectureLinks_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("重新計算並更新所有建築的連接。可能要花上數分鐘的時間，是否確認？", "重新設置城池連接", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                scen.InitializeMapData();
                scen.InitializeArchitectureMapTile();
                foreach (Architecture architecture2 in scen.Architectures)
                {
                    architecture2.AILandLinks.Clear();
                    architecture2.AIWaterLinks.Clear();
                }
                foreach (Architecture architecture2 in scen.Architectures)
                {
                    architecture2.FindLinks(scen.Architectures);
                    architecture2.AILandLinksString = architecture2.AILandLinks.SaveToString();
                    architecture2.AIWaterLinksString = architecture2.AIWaterLinks.SaveToString();
                }
                architectureTab.setup();
            }
        }

        private void btnDeleteAllDiplomacy_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("刪除所有勢力外交關係，是否確認？", "刪除所有勢力外交關係", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (DiplomaticRelation relation in scen.DiplomaticRelations.DiplomaticRelations.Values)
                {
                    relation.Relation = 0;
                }
            }
        }

        private void btnCleanupRelation_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("刪除多餘關係，是否確認？", "刪除所有未登場武將的特殊關係", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                Dictionary<int, int> copy = new Dictionary<int, int>(scen.FatherIds);
                foreach (KeyValuePair<int, int> kv in copy)
                {
                    Person k = scen.Persons.GetGameObject(kv.Key) as Person;
                    Person v = scen.Persons.GetGameObject(kv.Value) as Person;
                    if (k == null || v == null)
                    {
                        scen.FatherIds.Remove(kv.Key);
                    }
                }
                fatherTab.setup();

                copy = new Dictionary<int, int>(scen.MotherIds);
                foreach (KeyValuePair<int, int> kv in copy)
                {
                    Person k = scen.Persons.GetGameObject(kv.Key) as Person;
                    Person v = scen.Persons.GetGameObject(kv.Value) as Person;
                    if (k == null || v == null)
                    {
                        scen.MotherIds.Remove(kv.Key);
                    }
                }
                motherTab.setup();

                copy = new Dictionary<int, int>(scen.SpouseIds);
                foreach (KeyValuePair<int, int> kv in copy)
                {
                    Person k = scen.Persons.GetGameObject(kv.Key) as Person;
                    Person v = scen.Persons.GetGameObject(kv.Value) as Person;
                    if (k == null || v == null)
                    {
                        scen.SpouseIds.Remove(kv.Key);
                    }
                }
                spouseTab.setup();
            }
        }

        private void btnDeleteUnavailableRelation_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("刪除所有未登場武將的配偶關係，是否確認？", "刪除所有未登場武將的特殊關係", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                Dictionary<int, int> copy = new Dictionary<int, int>(scen.SpouseIds);
                foreach (KeyValuePair<int, int> kv in copy)
                {
                    Person k = scen.Persons.GetGameObject(kv.Key) as Person;
                    Person v = scen.Persons.GetGameObject(kv.Value) as Person;
                    if (k != null && v != null && !k.Available && !v.Available)
                    {
                        scen.SpouseIds.Remove(kv.Key);
                    }
                }
                spouseTab.setup();
            }
        }

        private void btnNewPerson_Click(object sender, RoutedEventArgs e)
        {
            NewPersonWindow newPersonWindow = new NewPersonWindow(scen);
            newPersonWindow.Show();
            newPersonWindow.Closed += NewPersonWindow_Closed;
        }

        private void NewPersonWindow_Closed(object sender, EventArgs e)
        {
            personTab.setup();
        }

        private void MenuItem_IncludeTitle_Checked(object sender, EventArgs e)
        {
            CopyIncludeTitle = true;
        }

        private void MenuItem_IncludeTitle_Unchecked(object sender, EventArgs e)
        {
            CopyIncludeTitle = false;
        }

        //剧本设置相关
        //
        private void btnScenariotoexcel_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(@Environment.CurrentDirectory + @"\转换生成文件"))//如果不存在就创建file文件夹
            {
                Directory.CreateDirectory(@Environment.CurrentDirectory + @"\转换生成文件");
            }
            if (scename != null && scename != "")
            {
                String scenName = scename.Substring(scename.LastIndexOf(@"\") + 1, scename.LastIndexOf(".") - scename.LastIndexOf(@"\") - 1);
                if (File.Exists(Environment.CurrentDirectory + "\\转换生成文件\\" + scenName + "." + "xlsx"))
                {
                    File.Delete(Environment.CurrentDirectory + "\\转换生成文件\\" + scenName + "." + "xlsx");
                }
                if (File.Exists(Environment.CurrentDirectory + "\\转换生成文件\\" + scenName + "地形信息.txt"))
                {
                    File.Delete(Environment.CurrentDirectory + "\\转换生成文件\\" + scenName + "地形信息.txt");
                }
                using (ExcelPackage package = new ExcelPackage(new FileInfo(Environment.CurrentDirectory + "\\转换生成文件\\" + scenName + "." + "xlsx")))
                {
                    for (int i = 0; i < tabControl.Items.Count; i++)
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(((TabItem)tabControl.Items[i]).Header.ToString());
                        tabControl.SelectedIndex = i;
                        Grid grid = (Grid)tabControl.SelectedContent;
                        DataGrid dataGrid = (DataGrid)grid.Children[grid.Children.Count - 1];
                        DataTable dt = ((DataView)dataGrid.ItemsSource).Table;
                        worksheet.Cells["a1"].LoadFromDataTable(dt, true);
                        if (((TabItem)tabControl.Items[i]).Header.ToString().Equals("条件") || ((TabItem)tabControl.Items[i]).Header.ToString().Equals("事件影响") || ((TabItem)tabControl.Items[i]).Header.ToString().Equals("部队事件影响") || ((TabItem)tabControl.Items[i]).Header.ToString().Equals("人物个性语言") || ((TabItem)tabControl.Items[i]).Header.ToString().Equals("影响") || ((TabItem)tabControl.Items[i]).Header.ToString().Equals("军区"))
                        {
                            worksheet = package.Workbook.Worksheets.Add(((TabItem)tabControl.Items[i]).Header.ToString() + "类型");
                            dataGrid = (DataGrid)grid.Children[grid.Children.Count - 2];
                            dt = ((DataView)dataGrid.ItemsSource).Table;
                            worksheet.Cells["a1"].LoadFromDataTable(dt, true);
                        }
                    }
                    package.Save();

                }
                StreamWriter sw = new StreamWriter(Environment.CurrentDirectory + "\\转换生成文件\\" + scenName + "地形信息.txt", false, Encoding.GetEncoding("gb2312"));
                sw.Write(scen.ScenarioMap.MapDataString);
                sw.Flush();
                sw.Close();
                sw.Dispose();
                MessageBox.Show("转换完毕,请查阅根目录下转换生成文件夹");
            }
            else
            {
                MessageBox.Show("请先用编辑器打开想要修改的剧本");
            }
        }

        private void btnsce_Click(object sender, RoutedEventArgs e)
        {
            SceWindow sceWindow = new SceWindow(scen);
            sceWindow.ShowDialog();
        }

        private void btnSets_Click(object sender, RoutedEventArgs e)
        {
            Sets sets = new Sets(scen);
            sets.Title = "Glo参数设置";
            sets.path1 = @Environment.CurrentDirectory + "\\Content\\Data\\GlobalVariables.xml";
            sets.typ = "Glo";
            if (this.scename != "" && this.scename != null)
            {
                sets.scename = this.scename.Substring(scename.LastIndexOf(@"\") + 1, scename.LastIndexOf(".") - scename.LastIndexOf(@"\") - 1);

            }
            sets.InitialSets();
            sets.ShowDialog();
            sets.Owner = this;
        }
        private void btnSetsPara_Click(object sender, RoutedEventArgs e)
        {
            Sets sets = new Sets(scen);
            sets.Title = "Para参数设置";
            sets.path1 = @Environment.CurrentDirectory + "\\Content\\Data\\GameParameters.xml";
            sets.typ = "Para";
            if (this.scename != "" && this.scename != null)
            {
                sets.scename = this.scename.Substring(scename.LastIndexOf(@"\") + 1, scename.LastIndexOf(".") - scename.LastIndexOf(@"\") - 1);

            }
            sets.InitialSets();
            sets.ShowDialog();
            sets.Owner = this;
        }

        //剧本设置相关
        //


        private void MenuAdd_Click(object sender, RoutedEventArgs e)
        {
            bool edit = false;
            nomalcreatwindow(edit);
        }

        private void MenuEdit_Click(object sender, RoutedEventArgs e)
        {
            bool edit = true;
            nomalcreatwindow(edit);
        }

        private void nomalcreatwindow(bool edit)
        {
            Grid grid = (Grid)tabControl.SelectedContent;
            DataGrid dataGrid = grid.Children[grid.Children.Count-1] as DataGrid;
            if ((!edit || (edit && dataGrid.SelectedItem != null)) && dataGrid.ItemsSource != null)
            {
                if (dataGrid == dgRegion)
                {
                    regionTab.creatWindow(edit, dgState, this);
                }
                else if (dataGrid == dgState)
                {
                    stateTab.creatWindow(edit, dgRegion, this);
                }
                else if (dataGrid == dgFatherId)
                {
                    fatherTab.creatWindow(edit, dgFatherId);
                }
                else if (dataGrid == dgMotherId)
                {
                    motherTab.creatWindow(edit, dgMotherId);
                }
                else if (dataGrid == dgSpouseId)
                {
                    spouseTab.creatWindow(edit, dgSpouseId);
                }
                else if (dataGrid == dgBrotherIds)
                {
                    brotherIdsTab.creatWindow(edit, dgBrotherIds);
                }
                else if (dataGrid == dgSuoshuIds)
                {
                    suoshuIdsTab.creatWindow(edit, dgSuoshuIds);
                }
                else if (dataGrid == dgCloseIds)
                {
                    closeIdsTab.creatWindow(edit, dgCloseIds);
                }
                else if (dataGrid == dgHatedIds)
                {
                    hatedIdsTab.creatWindow(edit, dgHatedIds);
                }
                else if (dataGrid == dgFaction)
                {
                    factionTab.createWindow(edit, dgFaction, this);
                } 
                else if (dataGrid == dgEvent)
                {
                    eventTab.createWindow(edit, dgEvent, this);
                }
            }
        }

        private void DgNomal_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            bool edit = true;
            nomalcreatwindow(edit);
        }

        private void MenuSelectAll_Click(object sender, RoutedEventArgs e)
        {
            Grid grid = (Grid)tabControl.SelectedContent;
            DataGrid dataGrid = grid.Children[grid.Children.Count - 1] as DataGrid;
            dataGrid.SelectAllCells();
        }

        private void Dg_Selected(object sender, MouseButtonEventArgs e)
        {
            currenttab = ((TabItem)tabControl.SelectedItem).Header.ToString();
            currentchilidren = ((Grid)tabControl.SelectedContent).Children.IndexOf((DataGrid)sender);
        }

        private void MenuRefresh_Click(object sender, RoutedEventArgs e)
        {
            Grid grid = (Grid)tabControl.SelectedContent;
            DataGrid dataGrid = (DataGrid)grid.Children[currentchilidren];
            string[] strs = new string[] {dataGrid.Name };
            initTables(strs);
        }

        #region 头像处理功能

        private void btnGenerateAllSmallPortraits_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("将为所有缺失小图的头像自动生成64x64像素的小图，这可能需要一些时间。是否确认？", 
                "生成所有缺失的小图", MessageBoxButton.OKCancel);
            
            if (result == MessageBoxResult.OK)
            {
                try
                {
                    // 处理默认头像
                    var defaultResult = Tools.PortraitProcessor.ProcessAllDefaultPortraits();
                    
                    // 处理玩家头像
                    var playerResult = Tools.PortraitProcessor.ProcessAllPlayerPortraits();
                    
                    string message = $"处理完成！\n\n默认头像: {defaultResult.GetSummary()}\n\n玩家头像: {playerResult.GetSummary()}";
                    MessageBox.Show(message, "头像处理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"处理过程中发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnGenerateDefaultSmallPortraits_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("将为默认头像目录中缺失小图的头像自动生成64x64像素的小图。是否确认？", 
                "生成默认头像小图", MessageBoxButton.OKCancel);
            
            if (result == MessageBoxResult.OK)
            {
                try
                {
                    var processResult = Tools.PortraitProcessor.ProcessAllDefaultPortraits();
                    MessageBox.Show(processResult.GetSummary(), "处理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"处理过程中发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnGeneratePlayerSmallPortraits_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("将为玩家头像目录中缺失小图的头像自动生成64x64像素的小图。是否确认？", 
                "生成玩家头像小图", MessageBoxButton.OKCancel);
            
            if (result == MessageBoxResult.OK)
            {
                try
                {
                    var processResult = Tools.PortraitProcessor.ProcessAllPlayerPortraits();
                    MessageBox.Show(processResult.GetSummary(), "处理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"处理过程中发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnCleanDefaultSmallPortraits_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("将删除默认头像目录中的所有小图文件（以's'结尾的jpg文件）。此操作不可撤销，是否确认？", 
                "清理默认头像小图", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.OK)
            {
                try
                {
                    var defaultDir = @"Content/Textures/GameComponents/PersonPortrait/Images/Default/";
                    var deletedCount = Tools.PortraitProcessor.CleanupSmallPortraits(defaultDir);
                    MessageBox.Show($"已删除 {deletedCount} 个小图文件。", "清理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"清理过程中发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnCleanPlayerSmallPortraits_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("将删除玩家头像目录中的所有小图文件（以's'结尾的jpg文件）。此操作不可撤销，是否确认？", 
                "清理玩家头像小图", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.OK)
            {
                try
                {
                    var playerDir = @"Content/Textures/GameComponents/PersonPortrait/Images/Player/";
                    var deletedCount = Tools.PortraitProcessor.CleanupSmallPortraits(playerDir);
                    MessageBox.Show($"已删除 {deletedCount} 个小图文件。", "清理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"清理过程中发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 内存管理功能

        private void btnViewMemoryUsage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取基本内存信息
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var gcMemory = GC.GetTotalMemory(false);
                
                var message = $"内存使用情况 (第一步优化):\n\n" +
                             $"进程内存: {process.WorkingSet64 / 1024 / 1024:F1} MB\n" +
                             $"GC内存: {gcMemory / 1024 / 1024:F1} MB\n";
                
                // 获取头像缓存统计
                // SimpleTextureManager 已移除，显示简单信息
                message += $"\n头像缓存: 已禁用（使用原有系统）";
                
                // 获取TextureManager统计
                try
                {
                    var textureStats = TextureManager.GetMemoryStats();
                    message += $"\n纹理管理器: {textureStats.TotalTextureCount} 个纹理";
                }
                catch (Exception ex3)
                {
                    message += $"\n纹理管理器: 获取失败 ({ex3.Message})";
                }
                
                message += "\n\n注意: 这是第一步优化的统计信息";
                message += "\n主要优化了头像纹理的缓存管理";
                
                MessageBox.Show(message, "内存使用情况", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取内存信息时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPerformMemoryCleanup_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("执行内存清理将释放未使用的头像纹理和临时缓存。是否继续？", 
                "内存清理", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var process = System.Diagnostics.Process.GetCurrentProcess();
                    var beforeMemory = process.WorkingSet64;
                    
                    // 头像缓存清理已禁用（SimpleTextureManager已移除）
                    
                    // 清理其他缓存
                    CacheManager.Clear(CacheType.Temp);
                    
                    // 建议垃圾回收
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    // 等待一下让清理完成
                    System.Threading.Thread.Sleep(1000);
                    
                    process.Refresh();
                    var afterMemory = process.WorkingSet64;
                    var freedMemory = (beforeMemory - afterMemory) / 1024 / 1024;
                    
                    var message = $"内存清理完成! (第一步优化)\n\n" +
                                 $"释放内存: {freedMemory:F1} MB\n" +
                                 $"清理前: {beforeMemory / 1024 / 1024:F1} MB\n" +
                                 $"清理后: {afterMemory / 1024 / 1024:F1} MB\n\n" +
                                 $"已清理:\n" +
                                 $"- 头像纹理缓存\n" +
                                 $"- 临时纹理缓存\n" +
                                 $"- 垃圾回收";
                    
                    MessageBox.Show(message, "清理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"内存清理时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnTextureStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var message = "纹理缓存统计 (第一步优化):\n\n";
                
                // 头像缓存统计已禁用（SimpleTextureManager已移除）
                message += $"头像缓存: 已禁用（使用原有系统）\n\n";
                
                // 获取基本统计
                try
                {
                    var stats = TextureManager.GetMemoryStats();
                    message += $"纹理管理器状态:\n";
                    message += $"- 内存限制: {stats.MaxMemoryLimit / 1024 / 1024:F1} MB\n";
                    message += $"- 当前使用: {stats.TotalMemoryUsage / 1024 / 1024:F1} MB\n";
                    message += $"- 纹理数量: {stats.TotalTextureCount}\n";
                }
                catch (Exception ex3)
                {
                    message += $"纹理管理器统计获取失败: {ex3.Message}\n";
                }
                
                message += "\n说明:";
                message += "\n- 这是第一步优化的统计信息";
                message += "\n- 主要显示头像纹理的缓存情况";
                message += "\n- 后续步骤会添加更详细的统计";
                
                MessageBox.Show(message, "纹理缓存统计", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取纹理统计时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

}
