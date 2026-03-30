using GameManager;
using GameObjects.ArchitectureDetail.EventEffect;
using GameObjects.Conditions;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using WorldOfTheThreeKingdoms.GameGlobal;  // 🔥 2026-03-05 添加：支持 [GenerateUIAccessor] 特性

namespace GameObjects
{
    [DataContract]
    public class PersonIdDialog
    {
        [DataMember]
        public int id;
        [DataMember]
        public string dialog;
        [DataMember]
        public string yesdialog;
        [DataMember]
        public string nodialog;
    }

    [DataContract]
    [GenerateUIAccessor]  // 🔥 2026-03-05 修复：添加源生成器特性，支持 UI 访问 ID 等属性
    public partial class Event : GameObject
    {
        [DataMember]
        public int AfterEventHappened = -1;
        public TroopEvent AfterHappenedEvent;
        [DataMember]
        public int happenChance;

        [DataMember]
        public bool happened;
        [DataMember]
        public bool repeatable;

        [DataMember]
        public String nextScenario;

        [DataMember]
        public string personString { get; set; }

        public Dictionary<int, List<Person>> person;

        [DataMember]
        public string PersonCondString { get; set; }

        public Dictionary<int, List<Condition>> personCond;

        [DataMember]
        public string architectureString { get; set; }
        public ArchitectureList architecture;

        [DataMember]
        public string architectureCondString { get; set; }

        public List<Condition> architectureCond;

        [DataMember]
        public string factionString { get; set; }

        public FactionList faction;

        [DataMember]
        public string factionCondString { get; set; }

        public List<Condition> factionCond;
        
        public List<PersonIdDialog> dialog;

        [DataMember]
        public string dialogString { get; set; }

        [DataMember]
        public string effectString { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<int, List<EventEffect>> effect;
        [System.Text.Json.Serialization.JsonIgnore]
        public List<PersonDialog> matchedDialog;
        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<Person, List<EventEffect>> matchedEffect;

        public List<PersonDialog> matchedyesDialog = new List<PersonDialog>();
        public List<PersonDialog> matchednoDialog = new List<PersonDialog>();
        
        public List<PersonIdDialog> yesdialog = new List<PersonIdDialog>();
        public List<PersonIdDialog> nodialog = new List<PersonIdDialog>();

        [DataMember]
        public string yesdialogString { get; set; }
        [DataMember]
        public string nodialogString { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<Person, List<EventEffect>> matchedYesEffect;
        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<Person, List<EventEffect>> matchedNoEffect;

        [DataMember]
        public string yesEffectString { get; set; }

        [DataMember]
        public string noEffectString { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<int, List<EventEffect>> yesEffect = new Dictionary<int,List<EventEffect>>();
        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<int, List<EventEffect>> noEffect = new Dictionary<int,List<EventEffect>>();

        [DataMember]
        public string architectureEffectString { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<EventEffect> architectureEffect = new List<EventEffect>();

        [DataMember]
        public string factionEffectIDString { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<EventEffect> factionEffect = new List<EventEffect>();

        [DataMember]
        public string yesArchitectureEffectString { get; set; }

        [DataMember]
        public string noArchitectureEffectString { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<EventEffect> yesArchitectureEffect = new List<EventEffect>();
        [System.Text.Json.Serialization.JsonIgnore]
        public List<EventEffect> noArchitectureEffect = new List<EventEffect>();

        public List<PersonIdDialog> scenBiography = new List<PersonIdDialog>() ;

        [DataMember]
        public string scenBiographyString { get; set; }
        
        public List<PersonDialog> matchedScenBiography = new List<PersonDialog> () ;

        [DataMember]
        public String Image = "";
        [DataMember]
        public String Sound = "";
        [DataMember]
        public bool GloballyDisplayed = false;
        [DataMember]
        public int StartYear = 0;
        [DataMember]
        public int StartMonth = 1;
        [DataMember]
        public int EndYear = 99999;
        [DataMember]
        public int EndMonth = 12;

        [DataMember]
        public bool Minor = false;

        [DataMember]
        public string TryToShowString { get; set; }

        private bool involveLeader = false;
        public bool InvolveLeader
        {
            get
            {
                return involveLeader;
            }
        }

        public event ApplyEvent OnApplyEvent;

        private enum DialogContentKind
        {
            Dialog,
            YesDialog,
            NoDialog
        }

        private static readonly char[] StringSeparators = [' ', '\n', '\r', '\t'];
        private static readonly char[] DialogLineSeparators = ['\r', '\n'];

        public void Init()
        {
            yesEffect = new Dictionary<int, List<EventEffect>>();

            noEffect = new Dictionary<int, List<EventEffect>>();

            architectureEffect = new List<EventEffect>();

            factionEffect = new List<EventEffect>();

            yesArchitectureEffect = new List<EventEffect>();

            noArchitectureEffect = new List<EventEffect>();

            if (dialog == null)
            {
                dialog = new List<PersonIdDialog>();
            }
            if (yesdialog == null)
            {
                yesdialog = new List<PersonIdDialog>();
            }
            if (nodialog == null)
            {
                nodialog = new List<PersonIdDialog>();
            }
            if (scenBiography == null)
            {
                scenBiography = new List<PersonIdDialog>();
            }
        }

        private static string[] SplitTokens(string data)
        {
            return string.IsNullOrWhiteSpace(data) ? [] : data.Split(StringSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        private void ValidatePairTokenCount(string fieldName, string[] strArray)
        {
            if ((strArray.Length & 1) != 0)
            {
                throw new FormatException($"Event {this.ID} field {fieldName} contains {strArray.Length} tokens and cannot be parsed as key/value pairs.");
            }
        }

        private bool TryGetDialogSpeakerId(string token, out int speakerId)
        {
            if (!int.TryParse(token, out speakerId))
            {
                return false;
            }

            return this.person == null || this.person.Count == 0 || this.person.ContainsKey(speakerId);
        }

        private static void AddDialogEntry(List<PersonIdDialog> result, int speakerId, string text, DialogContentKind contentKind)
        {
            PersonIdDialog item = new PersonIdDialog();
            item.id = speakerId;
            switch (contentKind)
            {
                case DialogContentKind.YesDialog:
                    item.yesdialog = text;
                    break;
                case DialogContentKind.NoDialog:
                    item.nodialog = text;
                    break;
                default:
                    item.dialog = text;
                    break;
            }
            result.Add(item);
        }

        private bool TryParseStructuredDialogEntries(string data, string fieldName, DialogContentKind contentKind, List<PersonIdDialog> result)
        {
            if (data.IndexOf('\t') < 0)
            {
                return false;
            }

            string[] lines = data.Split(DialogLineSeparators, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int separatorIndex = line.IndexOf('\t');
                if (separatorIndex <= 0)
                {
                    throw new FormatException($"Event {this.ID} field {fieldName} contains a malformed dialog record on line {i + 1}.");
                }

                string speakerToken = line[..separatorIndex];
                if (!TryGetDialogSpeakerId(speakerToken, out int speakerId))
                {
                    throw new FormatException($"Event {this.ID} field {fieldName} contains invalid speaker token '{speakerToken}' on line {i + 1}.");
                }

                AddDialogEntry(result, speakerId, line[(separatorIndex + 1)..], contentKind);
            }
            return true;
        }

        private List<PersonIdDialog> ParseDialogEntries(string data, string fieldName, DialogContentKind contentKind)
        {
            List<PersonIdDialog> result = new List<PersonIdDialog>();
            if (string.IsNullOrWhiteSpace(data))
            {
                return result;
            }

            if (TryParseStructuredDialogEntries(data, fieldName, contentKind, result))
            {
                return result;
            }

            string[] strArray = SplitTokens(data);
            int index = 0;
            while (index < strArray.Length)
            {
                if (!TryGetDialogSpeakerId(strArray[index], out int speakerId))
                {
                    throw new FormatException($"Event {this.ID} field {fieldName} expected a speaker token at position {index}, but found '{strArray[index]}'.");
                }

                int textStart = index + 1;
                if (textStart >= strArray.Length)
                {
                    throw new FormatException($"Event {this.ID} field {fieldName} is missing dialog text after speaker {speakerId}.");
                }

                int nextSpeakerIndex = strArray.Length;
                for (int i = textStart + 1; i < strArray.Length; i++)
                {
                    if (TryGetDialogSpeakerId(strArray[i], out _))
                    {
                        nextSpeakerIndex = i;
                        break;
                    }
                }

                AddDialogEntry(result, speakerId, string.Join(" ", strArray, textStart, nextSpeakerIndex - textStart), contentKind);
                index = nextSpeakerIndex;
            }

            return result;
        }

        private static string GetDialogText(PersonIdDialog dialogEntry, DialogContentKind contentKind)
        {
            return contentKind switch
            {
                DialogContentKind.YesDialog => dialogEntry.yesdialog ?? string.Empty,
                DialogContentKind.NoDialog => dialogEntry.nodialog ?? string.Empty,
                _ => dialogEntry.dialog ?? string.Empty
            };
        }

        private static string NormalizeSerializedDialogText(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : text.Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string SerializeDialogEntries(List<PersonIdDialog> dialogEntries, DialogContentKind contentKind)
        {
            if (dialogEntries == null || dialogEntries.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < dialogEntries.Count; i++)
            {
                PersonIdDialog dialogEntry = dialogEntries[i];
                builder.Append(dialogEntry.id);
                builder.Append('\t');
                builder.Append(NormalizeSerializedDialogText(GetDialogText(dialogEntry, contentKind)));
                builder.Append('\n');
            }
            return builder.ToString();
        }

        public void ApplyEventDialogs(Architecture a, Screen screen)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Event.ApplyEventDialogs] Event ID={this.ID}, Name={this.Name}, Architecture={a.Name}, OnApplyEvent订阅数={(this.OnApplyEvent?.GetInvocationList().Length ?? 0)}");
            #endif
            
            Session.Current.Scenario = Session.Current.Scenario;
            if (this.OnApplyEvent != null)
            {
                this.OnApplyEvent(this, a, screen);
            }
            else
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[Event.ApplyEventDialogs] ⚠️ OnApplyEvent 为 null！事件未订阅！");
                #endif
            }
            
            foreach (PersonDialog i in matchedScenBiography) 
            {
                if (i.SpeakingPerson != null)
                {
                    Session.Current.Scenario.YearTable.addPersonInGameBiography(i.SpeakingPerson, Session.Current.Scenario.Date, i.Text);
                }
            }
            if (nextScenario != null && nextScenario.Length > 0)
            {
                // Session.Current.Scenario.EnableLoadAndSave = false;

                List<int> factionIds = new List<int>();
                foreach (Faction f in Session.Current.Scenario.PlayerFactions) 
                {
                    factionIds.Add(f.ID);
                }

                //暫時取消
                //OleDbConnectionStringBuilder builder = new OleDbConnectionStringBuilder
                //{
                //    DataSource = "GameData/Scenario/" + nextScenario,
                //    Provider = "Microsoft.Jet.OLEDB.4.0"
                //};
                //Session.Current.Scenario.LoadGameScenarioFromDatabase(builder.ConnectionString, factionIds);

                //Session.MainGame.mainGameScreen.ReloadScreenData();

                //Session.Current.Scenario.EnableLoadAndSave = true;
            }
        }

        public void DoYesApplyEvent(Architecture a)
        {
            if (this.yesEffect != null)
            {

                foreach (KeyValuePair<Person, List<EventEffect>> i in matchedYesEffect)
                {
                    foreach (EventEffect j in i.Value)
                    {
                        j.ApplyEffect(i.Key, this);
                    }
                }
                foreach (PersonDialog yesdialog in this.matchedyesDialog)
                {
                    if (yesdialog.SpeakingPerson != null)
                    {
                        Session.MainGame.mainGameScreen.xianshishijiantupian(yesdialog.SpeakingPerson, null, yesdialog.Text, true);
                    }
                    else
                    {
                        Session.MainGame.mainGameScreen.xianshishijiantupian(a.BelongedFaction.Leader, null, yesdialog.Text, true);
                    }
                }
            }
            if (this.yesArchitectureEffect != null)
            {
                foreach (EventEffect i in yesArchitectureEffect)
                {
                    i.ApplyEffect(a, this);
                }

            }
        }

        public void DoNoApplyEvent(Architecture a)
        {
            if (this.noEffect != null)
            {
                foreach (KeyValuePair<Person, List<EventEffect>> i in matchedNoEffect)
                {
                    foreach (EventEffect j in i.Value)
                    {
                        j.ApplyEffect(i.Key, this);
                    }
                }
                foreach (PersonDialog nodialog in this.matchednoDialog)
                {
                    if (nodialog.SpeakingPerson != null)
                    {
                        Session.MainGame.mainGameScreen.xianshishijiantupian(nodialog.SpeakingPerson, null, nodialog.Text, true);
                    }
                    else
                    {
                        Session.MainGame.mainGameScreen.xianshishijiantupian(a.BelongedFaction.Leader, null, nodialog.Text, true);
                    }
                }
            }
            if (this.noArchitectureEffect != null)
            {
                foreach (EventEffect j in noArchitectureEffect)
                {
                    j.ApplyEffect(a, this);
                }

            }
        }

        public void DoApplyEvent(Architecture a)
        {
            if (matchedEffect != null)
            {
                foreach (KeyValuePair<Person, List<EventEffect>> i in matchedEffect)
                {
                    foreach (EventEffect j in i.Value)
                    {
                        j.ApplyEffect(i.Key, this);
                    }
                }
            }
            if (architectureEffect != null)
            {
                foreach (EventEffect i in architectureEffect)
                {
                    i.ApplyEffect(a, this);
                }
            }
            if (factionEffect != null && a.BelongedFaction != null)
            {
                foreach (EventEffect i in factionEffect)
                {
                    i.ApplyEffect(a.BelongedFaction, this);
                }
            }
        }

        public bool matchEventPersons(Architecture a)
        {
            GameObjectList allPersons = a.AllPersonAndChildren.GetList();

            HashSet<int> haveCond = new HashSet<int>();
            foreach (KeyValuePair<int, List<Condition>> i in this.personCond)
            {
                haveCond.Add(i.Key);
            }

            HashSet<int> noCond = new HashSet<int>();
            foreach (KeyValuePair<int, List<Person>> i in this.person)
            {
                if (!haveCond.Contains(i.Key) && i.Value.Count == 0)
                {
                    noCond.Add(i.Key);
                }
            }

            Dictionary<int, List<Person>> candidates = new Dictionary<int, List<Person>>();
            foreach (int i in this.person.Keys)
            {
                candidates[i] = new List<Person>();
                if (noCond.Contains(i))
                {
                    foreach (Person p in allPersons.GetList())
                    {
                        candidates[i].Add(p);
                    }
                }
            }

            // check person in the architecture
            foreach (KeyValuePair<int, List<Condition>> i in this.personCond)
            {
                foreach (Person p in allPersons)
                {
                    bool ok = Condition.CheckConditionList(i.Value, p, this);
                    if (ok)
                    {
                        if (this.person[i.Key].Contains(null) || this.person[i.Key].Contains(p))
                        {
                            candidates[i.Key].Add(p);
                        }
                    }
                }
            }
            // check 7000 - 8000 persons which can be in anywhere
            foreach (KeyValuePair<int, List<Person>> i in this.person)
            {
                foreach (Person p in i.Value)
                {
                    if (p != null /*&& p.ID >= 7000 && p.ID < 8000*/)
                    {
                        bool ok;
                        if (this.personCond.ContainsKey(i.Key))
                        {
                            ok = Condition.CheckConditionList(this.personCond[i.Key], p, this);
                        }
                        else
                        {
                            ok = true;
                        }
                        if (ok)
                        {
                            if (this.person[i.Key].Contains(null) || this.person[i.Key].Contains(p))
                            {
                                candidates[i.Key].Add(p);
                            }
                        }
                    }
                }
            }

            foreach (List<Person> i in candidates.Values)
            {
                if (i.Count == 0) return false;
            }

            Dictionary<int, Person> matchedPersons = new Dictionary<int, Person>();
            foreach (KeyValuePair<int, List<Person>> i in candidates)
            {
                if (i.Value.Count <= 0) return false;
                Person selected = i.Value[GameObject.Random(i.Value.Count)];
                matchedPersons[i.Key] = selected;
                foreach (List<Person> j in candidates.Values)
                {
                    j.Remove(selected);
                }
            }

            matchedDialog = new List<PersonDialog>();
            foreach (PersonIdDialog i in this.dialog)
            {
                if (!matchedPersons.ContainsKey(i.id)) return false;

                PersonDialog pd = new PersonDialog();
                pd.SpeakingPerson = matchedPersons[i.id];
                pd.Text = i.dialog;
                for (int j = 0; j < matchedPersons.Count; ++j)
                {
                    pd.Text = pd.Text.Replace("%" + j, matchedPersons[j].Name);
                }
                matchedDialog.Add(pd);
            }

            matchedyesDialog = new List<PersonDialog>();
            foreach (PersonIdDialog i in this.yesdialog)
            {
                if (!matchedPersons.ContainsKey(i.id)) return false;

                PersonDialog pd = new PersonDialog();
                pd.SpeakingPerson = matchedPersons[i.id];
                pd.Text = i.yesdialog;
                for (int j = 0; j < matchedPersons.Count; ++j)
                {
                    pd.Text = pd.Text.Replace("%" + j, ' ' + matchedPersons[j].Name + ' ');
                }
                matchedyesDialog.Add(pd);
            }

            matchednoDialog = new List<PersonDialog>();
            foreach (PersonIdDialog i in this.nodialog)
            {
                if (!matchedPersons.ContainsKey(i.id)) return false;

                PersonDialog pd = new PersonDialog();
                pd.SpeakingPerson = matchedPersons[i.id];
                pd.Text = i.nodialog;
                for (int j = 0; j < matchedPersons.Count; ++j)
                {
                    pd.Text = pd.Text.Replace("%" + j, ' ' + matchedPersons[j].Name + ' ');
                }
                matchednoDialog.Add(pd);
            }

            matchedScenBiography = new List<PersonDialog>();
            foreach (PersonIdDialog i in this.scenBiography)
            {
                if (!matchedPersons.ContainsKey(i.id)) return false;

                PersonDialog pd = new PersonDialog();
                pd.SpeakingPerson = matchedPersons[i.id];
                pd.Text = i.dialog;
                for (int j = 0; j < matchedPersons.Count; ++j)
                {
                    pd.Text = pd.Text.Replace("%" + j, matchedPersons[j].Name);
                }
                matchedScenBiography.Add(pd);
            }

            matchedEffect = new Dictionary<Person, List<EventEffect>>();
            foreach (KeyValuePair<int, List<EventEffect>> i in this.effect)
            {
                matchedEffect.Add(matchedPersons[i.Key], i.Value);
            }
            matchedYesEffect = new Dictionary<Person, List<EventEffect>>();
            foreach (KeyValuePair<int, List<EventEffect>> i in this.yesEffect)
            {
                matchedYesEffect.Add(matchedPersons[i.Key], i.Value);
            }
            matchedNoEffect = new Dictionary<Person, List<EventEffect>>();
            foreach (KeyValuePair<int, List<EventEffect>> i in this.noEffect)
            {
                matchedNoEffect.Add(matchedPersons[i.Key], i.Value);
            }

            if (a.BelongedFaction != null)
            {
                foreach (Person p in matchedPersons.Values)
                {
                    if (p == a.BelongedFaction.Leader && Session.Current.Scenario.IsPlayer(a.BelongedFaction))
                    {
                        involveLeader = true;
                    }
                }
            }

            return true;
        }

        public bool checkConditions(Architecture a)
        {
            if (this.happened && !this.repeatable) return false;
            if (GameObject.Random(this.happenChance) != 0)
            {
                return false;
            }

            if (this.AfterEventHappened >= 0)
            {
                if (!((Session.Current.Scenario.AllEvents.GetGameObject(this.AfterEventHappened) is Event ? (Event)Session.Current.Scenario.AllEvents.GetGameObject(this.AfterEventHappened) : null)).happened)
                {
                    return false;
                }
            }

            if (Session.Current.Scenario.Date.Year < this.StartYear || Session.Current.Scenario.Date.Year > this.EndYear) return false;

            if (Session.Current.Scenario.Date.Year == this.StartYear)
            {
                if (Session.Current.Scenario.Date.Month < this.StartMonth) return false;
            }

            if (Session.Current.Scenario.Date.Year == this.EndYear)
            {
                if (Session.Current.Scenario.Date.Month > this.EndMonth) return false;
            }

            if (!Condition.CheckConditionList(this.architectureCond, a, this)) return false;
            if (!Condition.CheckConditionList(this.factionCond, a.BelongedFaction, this)) return false;

            if (architecture.Count > 0 || faction.Count > 0)
            {
                bool contains = false;
                if (architecture != null)
                {
                    foreach (Architecture archi in this.architecture)
                    {
                        if (archi.ID == a.ID)
                        {
                            contains = true;
                        }
                    }
                }

                if (faction != null)
                {
                    foreach (Faction f in faction)
                    {
                        if (a.BelongedFaction != null && f != null)
                        {
                            if (f.ID == a.BelongedFaction.ID)
                            {
                                contains = true;
                            }
                        }
                    }

                }
                if (contains)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            
           
            return this.matchEventPersons(a);
        }

        public bool IsStart()
        {
            Condition cstart = Session.Current.Scenario.GameCommonData.AllConditions.GetCondition(9998);
            if (cstart == null) return false;
            return this.architectureCond.Contains(cstart) || this.factionCond.Contains(cstart);
        }

        public bool IsEnd()
        {
            Condition cend = Session.Current.Scenario.GameCommonData.AllConditions.GetCondition(9999);
            if (cend == null) return false;
            return this.architectureCond.Contains(cend) || this.factionCond.Contains(cend);
        }

        public void LoadPersonIdFromString(PersonList persons, string data)
        {
            string[] strArray = SplitTokens(data);

            this.person = new Dictionary<int, List<Person>>();
            ValidatePairTokenCount(nameof(personString), strArray);
            for (int i = 0; i < strArray.Length; i += 2)
            {
                int n = int.Parse(strArray[i]);
                int pid = int.Parse(strArray[i + 1]);
                if (!this.person.ContainsKey(n))
                {
                    this.person[n] = new List<Person>();
                }
                if (pid != -1)
                {
                    this.person[n].Add((persons.GetGameObject(pid) is Person ? (Person)persons.GetGameObject(pid) : null));
                }
                else
                {
                    this.person[n].Add(null);
                }
            }
        }

        public string SavePersonIdToString()
        {
            string result = "";
            foreach (KeyValuePair<int, List<Person>> i in this.person)
            {
                foreach (Person j in i.Value)
                {
                    result += i.Key + " " + (j == null ? - 1 : j.ID) + " ";
                }
            }
            return result;
        }

        public void LoadPersonCondFromString(ConditionTable allConds, string data)
        {
            string[] strArray = SplitTokens(data);

            this.personCond = new Dictionary<int, List<Condition>>();
            ValidatePairTokenCount(nameof(PersonCondString), strArray);
            for (int i = 0; i < strArray.Length; i += 2)
            {
                int n = int.Parse(strArray[i]);
                int id = int.Parse(strArray[i + 1]);
                if (!allConds.Conditions.ContainsKey(id)) continue;
                if (!this.personCond.ContainsKey(n))
                {
                    this.personCond[n] = new List<Condition>();
                }
                this.personCond[n].Add(allConds.Conditions[id]);
            }
        }

        public string SavePersonCondToString()
        {
            string result = "";
            foreach (KeyValuePair<int, List<Condition>> i in this.personCond)
            {
                foreach (Condition j in i.Value)
                {
                    result += i.Key + " " + j.ID + " ";
                }
            }
            return result;
        }

        public void LoadArchitectureFromString(ArchitectureList archs, string data)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            this.architecture = new ArchitectureList();
            foreach (string i in strArray)
            {
                this.architecture.Add(archs.GetGameObject(int.Parse(i)) as Architecture);
            }
        }

        public void LoadArchitctureCondFromString(ConditionTable c, string data)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            this.architectureCond = new List<Condition>();
            foreach (string i in strArray)
            {
                if (!c.Conditions.ContainsKey(int.Parse(i))) continue;
                this.architectureCond.Add(c.Conditions[int.Parse(i)]);
            }
        }

        public string SaveArchitecureCondToString()
        {
            string result = "";
            foreach (Condition i in this.architectureCond)
            {
                result += i.ID + " ";
            }
            return result;
        }

        public void LoadFactionFromString(FactionList factions, string data)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            this.faction = new FactionList();
            foreach (string i in strArray)
            {
                this.faction.Add(factions.GetGameObject(int.Parse(i)) as Faction);
            }
        }

        public void LoadFactionCondFromString(ConditionTable c, string data)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            this.factionCond = new List<Condition>();
            foreach (string i in strArray)
            {
                if (!c.Conditions.ContainsKey(int.Parse(i))) continue;
                this.factionCond.Add(c.Conditions[int.Parse(i)]);
            }
        }

        public string SaveFactionCondToString()
        {
            string result = "";
            foreach (Condition i in this.factionCond)
            {
                result += i.ID + " ";
            }
            return result;
        }

        public void LoadDialogFromString(string data)
        {
            this.dialog = ParseDialogEntries(data, nameof(dialogString), DialogContentKind.Dialog);
        }

        public void LoadyesDialogFromString(string data)
        {
            this.yesdialog = ParseDialogEntries(data, nameof(yesdialogString), DialogContentKind.YesDialog);
        }

        public void LoadnoDialogFromString(string data)
        {
            this.nodialog = ParseDialogEntries(data, nameof(nodialogString), DialogContentKind.NoDialog);
        }
        
        public void LoadScenBiographyFromString(string data)
        {
            this.scenBiography = ParseDialogEntries(data, nameof(scenBiographyString), DialogContentKind.Dialog);
        }

        public string SaveDialogToString()
        {
            return SerializeDialogEntries(this.dialog, DialogContentKind.Dialog);
        }

        public string SaveyesDialogToString()
        {
            return SerializeDialogEntries(this.yesdialog, DialogContentKind.YesDialog);
        }

        public string SavenoDialogToString()
        {
            return SerializeDialogEntries(this.nodialog, DialogContentKind.NoDialog);
        }
        
        public string SaveScenBiographyToString()
        {
            return SerializeDialogEntries(this.scenBiography, DialogContentKind.Dialog);
        }

        public void LoadEffectFromString(EventEffectTable allEffect, string data)
        {
            string[] strArray = SplitTokens(data);

            this.effect = new Dictionary<int, List<EventEffect>>();
            ValidatePairTokenCount(nameof(effectString), strArray);
            for (int i = 0; i < strArray.Length; i += 2)
            {
                int n = int.Parse(strArray[i]);
                int id = int.Parse(strArray[i + 1]);
                if (!allEffect.EventEffects.ContainsKey(id)) continue;
                if (!this.effect.ContainsKey(n))
                {
                    this.effect[n] = new List<EventEffect>();
                }
                this.effect[n].Add(allEffect.EventEffects[id]);
            }
        }

        public string SaveEventEffectToString()
        {
            string result = "";
            foreach (KeyValuePair<int, List<EventEffect>> i in this.effect)
            {
                foreach (EventEffect j in i.Value)
                {
                    result += i.Key + " " + j.ID + " ";
                }
            }
            return result;
        }

        public void LoadYesEffectFromString(EventEffectTable allEffect, string data)
        {
            string[] strArray = SplitTokens(data);

            this.yesEffect = new Dictionary<int, List<EventEffect>>();
            ValidatePairTokenCount(nameof(yesEffectString), strArray);
            for (int i = 0; i < strArray.Length; i += 2)
            {
                int n = int.Parse(strArray[i]);
                int id = int.Parse(strArray[i + 1]);
                if (!allEffect.EventEffects.ContainsKey(id)) continue;
                if (!this.yesEffect.ContainsKey(n))
                {
                    this.yesEffect[n] = new List<EventEffect>();
                }
                this.yesEffect[n].Add(allEffect.EventEffects[id]);
            }
        }

        public string SaveYesEffectToString()
        {
            string result = "";
            foreach (KeyValuePair<int, List<EventEffect>> i in this.yesEffect)
            {
                foreach (EventEffect j in i.Value)
                {
                    result += i.Key + " " + j.ID + " ";
                }
            }
            return result;
        }

        public void LoadNoEffectFromString(EventEffectTable allEffect, string data)
        {
            string[] strArray = SplitTokens(data);

            this.noEffect = new Dictionary<int, List<EventEffect>>();
            ValidatePairTokenCount(nameof(noEffectString), strArray);
            for (int i = 0; i < strArray.Length; i += 2)
            {
                int n = int.Parse(strArray[i]);
                int id = int.Parse(strArray[i + 1]);
                if (!allEffect.EventEffects.ContainsKey(id)) continue;
                if (!this.noEffect.ContainsKey(n))
                {
                    this.noEffect[n] = new List<EventEffect>();
                }
                this.noEffect[n].Add(allEffect.EventEffects[id]);
            }
        }

        public string SaveNoEffectToString()
        {
            string result = "";
            foreach (KeyValuePair<int, List<EventEffect>> i in this.noEffect)
            {
                foreach (EventEffect j in i.Value)
                {
                    result += i.Key + " " + j.ID + " ";
                }
            }
            return result;
        }

        public void LoadArchitectureEffectFromString(EventEffectTable allEffect, string data)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            this.architectureEffect = new List<EventEffect>();
            foreach (string i in strArray)
            {
                if (!allEffect.EventEffects.ContainsKey(int.Parse(i))) continue;
                this.architectureEffect.Add(allEffect.EventEffects[int.Parse(i)]);
            }
        }

        public string SaveArchitectureEffectToString()
        {
            string result = "";
            foreach (EventEffect i in this.architectureEffect)
            {
                result += i.ID + " ";
            }
            return result;
        }

        public void LoadYesArchitectureEffectFromString(EventEffectTable allEffect, string data)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            this.yesArchitectureEffect = new List<EventEffect>();
            foreach (string i in strArray)
            {
                if (!allEffect.EventEffects.ContainsKey(int.Parse(i))) continue;
                this.yesArchitectureEffect.Add(allEffect.EventEffects[int.Parse(i)]);
            }
        }

        public string SaveYesArchitectureEffectToString()
        {
            string result = "";
            foreach (EventEffect i in this.yesArchitectureEffect)
            {
                result += i.ID + " ";
            }
            return result;
        }

        public void LoadNoArchitectureEffectFromString(EventEffectTable allEffect, string data)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            this.noArchitectureEffect = new List<EventEffect>();
            foreach (string i in strArray)
            {
                if (!allEffect.EventEffects.ContainsKey(int.Parse(i))) continue;
                this.noArchitectureEffect.Add(allEffect.EventEffects[int.Parse(i)]);
            }
        }

        public string SaveNoArchitectureEffectToString()
        {
            string result = "";
            foreach (EventEffect i in this.noArchitectureEffect)
            {
                result += i.ID + " ";
            }
            return result;
        }

        public void LoadFactionEffectFromString(EventEffectTable allEffect, string data)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            this.factionEffect = new List<EventEffect>();
            foreach (string i in strArray)
            {
                if (!allEffect.EventEffects.ContainsKey(int.Parse(i))) continue;
                this.factionEffect.Add(allEffect.EventEffects[int.Parse(i)]);
            }
        }

        public string SaveFactionEffectToString()
        {
            string result = "";
            foreach (EventEffect i in this.factionEffect)
            {
                result += i.ID + " ";
            }
            return result;
        }
        
       /*
        public bool CheckFactionEvent(Architecture a)
        {
           if (this.faction != null && this.faction.GameObjects.Contains(a.BelongedFaction) && checkConditions(a))
            {
                return true ;
            }
            return false ;
        }
        */
        public delegate void ApplyEvent(Event te, Architecture a, Screen screen);

    }
}

