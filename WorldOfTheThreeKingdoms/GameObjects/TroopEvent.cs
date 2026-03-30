using GameManager;
using GameObjects.Conditions;
using GameObjects.TroopDetail.EventEffect;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace GameObjects
{
    [DataContract]
    public partial class TroopEvent : GameObject
    {
        private static readonly char[] StringSeparators = [' ', '\n', '\r', '\t'];
        private static readonly char[] DialogLineSeparators = ['\r', '\n'];

        [DataMember]
        public int AfterEventHappened = -1;

        public TroopEvent AfterHappenedEvent;

        [DataMember]
        public EventCheckAreaKind CheckArea;

        [DataMember]
        public string ConditionsString { get; set; }

        public ConditionTable Conditions = new ConditionTable();

        //[DataMember]
        [System.Text.Json.Serialization.JsonIgnore]
        public List<PersonDialog> Dialogs = new List<PersonDialog>();

        [DataMember]
        public string dialogString { get; set; }

        [DataMember]
        public string EffectAreasString { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<TroopEffectArea> EffectAreas = new List<TroopEffectArea>();

        [DataMember]
        public string EffectPersonsString { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<TroopEffectPerson> EffectPersons = new List<TroopEffectPerson>();

        private int happenChance;
        private bool happened;

        [DataMember]
        public int LaunchPersonString { get; set; }
        public Person LaunchPerson;
        private bool repeatable;

        [DataMember]
        public string SelfEffectsString { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<GameObjects.TroopDetail.EventEffect.EventEffect> SelfEffects = new List<GameObjects.TroopDetail.EventEffect.EventEffect>();

        [DataMember]
        public string TargetPersonsString { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<PersonRelation> TargetPersons = new List<PersonRelation>();

        [DataMember]
        public String Image = "";

        [DataMember]
        public String Sound = "";

        [DataMember]
        public string TryToShowString { get; set; }

        public event ApplyTroopEvent OnApplyTroopEvent;

        public void Init()
        {
            Conditions = new ConditionTable();

            EffectAreas = new List<TroopEffectArea>();

            EffectPersons = new List<TroopEffectPerson>();

            SelfEffects = new List<GameObjects.TroopDetail.EventEffect.EventEffect>();

            TargetPersons = new List<PersonRelation>();

            if (Dialogs == null)
            {
                Dialogs = new List<PersonDialog>();
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
                throw new FormatException($"TroopEvent {this.ID} field {fieldName} contains {strArray.Length} tokens and cannot be parsed as key/value pairs.");
            }
        }

        private static bool TryGetDialogSpeakerId(Dictionary<int, Person> persons, string token, out int speakerId)
        {
            if (!int.TryParse(token, out speakerId))
            {
                return false;
            }

            return speakerId < 0 || persons == null || persons.ContainsKey(speakerId);
        }

        private List<PersonDialog> ParseDialogEntries(Dictionary<int, Person> persons, string data, string fieldName)
        {
            List<PersonDialog> result = new List<PersonDialog>();
            if (string.IsNullOrWhiteSpace(data))
            {
                return result;
            }

            if (data.IndexOf('\t') >= 0)
            {
                string[] lines = data.Split(DialogLineSeparators, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int separatorIndex = line.IndexOf('\t');
                    if (separatorIndex <= 0)
                    {
                        throw new FormatException($"TroopEvent {this.ID} field {fieldName} contains a malformed dialog record on line {i + 1}.");
                    }

                    string speakerToken = line[..separatorIndex];
                    if (!TryGetDialogSpeakerId(persons, speakerToken, out int speakerId))
                    {
                        throw new FormatException($"TroopEvent {this.ID} field {fieldName} contains invalid speaker token '{speakerToken}' on line {i + 1}.");
                    }

                    PersonDialog item = new PersonDialog();
                    item.SpeakingPersonID = speakerId;
                    item.SpeakingPerson = speakerId >= 0 && persons != null && persons.ContainsKey(speakerId) ? persons[speakerId] : null;
                    item.Text = line[(separatorIndex + 1)..];
                    result.Add(item);
                }

                return result;
            }

            string[] strArray = SplitTokens(data);
            int index = 0;
            while (index < strArray.Length)
            {
                if (!TryGetDialogSpeakerId(persons, strArray[index], out int speakerId))
                {
                    throw new FormatException($"TroopEvent {this.ID} field {fieldName} expected a speaker token at position {index}, but found '{strArray[index]}'.");
                }

                int textStart = index + 1;
                if (textStart >= strArray.Length)
                {
                    throw new FormatException($"TroopEvent {this.ID} field {fieldName} is missing dialog text after speaker {speakerId}.");
                }

                int nextSpeakerIndex = strArray.Length;
                for (int i = textStart + 1; i < strArray.Length; i++)
                {
                    if (TryGetDialogSpeakerId(persons, strArray[i], out _))
                    {
                        nextSpeakerIndex = i;
                        break;
                    }
                }

                PersonDialog item = new PersonDialog();
                item.SpeakingPersonID = speakerId;
                item.SpeakingPerson = speakerId >= 0 && persons != null && persons.ContainsKey(speakerId) ? persons[speakerId] : null;
                item.Text = string.Join(" ", strArray, textStart, nextSpeakerIndex - textStart);
                result.Add(item);
                index = nextSpeakerIndex;
            }

            return result;
        }

        private static string NormalizeSerializedDialogText(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : text.Replace('\r', ' ').Replace('\n', ' ');
        }

        public void ApplyEventDialogs(Troop troop)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[TroopEvent.ApplyEventDialogs] TroopEvent ID={this.ID}, Name={this.Name}, Troop={troop.DisplayName}, OnApplyTroopEvent订阅数={(this.OnApplyTroopEvent?.GetInvocationList().Length ?? 0)}");
            #endif
            
            if (this.OnApplyTroopEvent != null)
            {
                this.OnApplyTroopEvent(this, troop);
            }
            else
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[TroopEvent.ApplyEventDialogs] ⚠️ OnApplyTroopEvent 为 null！事件未订阅！");
                #endif
            }
        }

        public void ApplyEventEffects(Troop self)
        {
            if (((self != null) && !self.Destroyed) && (!this.Happened || this.Repeatable))
            {
                Troop troopByPositionNoCheck;
                this.Happened = true;
                TroopList list = new TroopList();
                if (this.SelfEffects.Count > 0)
                {
                    list.Add(self);
                    foreach (GameObjects.TroopDetail.EventEffect.EventEffect effect in this.SelfEffects)
                    {
                        effect.ApplyEffect(self.Leader);
                    }
                }
                foreach (TroopEffectPerson person in this.EffectPersons)
                {
                    person.Effect.ApplyEffect(person.EffectPerson);
                    if ((person.EffectPerson.LocationTroop != null) && (list.GetGameObject(person.EffectPerson.LocationTroop.ID) == null))
                    {
                        list.Add(person.EffectPerson.LocationTroop);
                    }
                }
                List<TroopEffectArea> list2 = new List<TroopEffectArea>();
                List<TroopEffectArea> list3 = new List<TroopEffectArea>();
                List<TroopEffectArea> list4 = new List<TroopEffectArea>();
                foreach (TroopEffectArea area in this.EffectAreas)
                {
                    switch (area.Kind)
                    {
                        case EffectAreaKind.视野敌军:
                            list2.Add(area);
                            break;

                        case EffectAreaKind.视野友军:
                            list2.Add(area);
                            break;

                        case EffectAreaKind.八格敌军:
                            list3.Add(area);
                            break;

                        case EffectAreaKind.八格友军:
                            list3.Add(area);
                            break;

                        case EffectAreaKind.攻击范围敌军:
                            list4.Add(area);
                            break;

                        case EffectAreaKind.攻击范围友军:
                            list4.Add(area);
                            break;
                    }
                }
                foreach (TroopEffectArea area in list2)
                {
                    foreach (Point point in self.BaseViewArea.Area)
                    {
                        if (self.BelongedFaction.IsPositionKnown(point))
                        {
                            troopByPositionNoCheck = Session.Current.Scenario.GetTroopByPositionNoCheck(point);
                            if (troopByPositionNoCheck != null)
                            {
                                switch (area.Kind)
                                {
                                    case EffectAreaKind.视野敌军:
                                        if (!self.IsFriendly(troopByPositionNoCheck.BelongedFaction))
                                        {
                                            area.Effect.ApplyEffect(troopByPositionNoCheck.Leader);
                                        }
                                        break;

                                    case EffectAreaKind.视野友军:
                                        if (self.IsFriendly(troopByPositionNoCheck.BelongedFaction))
                                        {
                                            area.Effect.ApplyEffect(troopByPositionNoCheck.Leader);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
                foreach (TroopEffectArea area in list3)
                {
                    foreach (Point point in GameArea.GetArea(self.Position, 1, true).Area)
                    {
                        if (self.BelongedFaction.IsPositionKnown(point))
                        {
                            troopByPositionNoCheck = Session.Current.Scenario.GetTroopByPositionNoCheck(point);
                            if (troopByPositionNoCheck != null)
                            {
                                switch (area.Kind)
                                {
                                    case EffectAreaKind.八格敌军:
                                        if (!self.IsFriendly(troopByPositionNoCheck.BelongedFaction))
                                        {
                                            area.Effect.ApplyEffect(troopByPositionNoCheck.Leader);
                                        }
                                        break;

                                    case EffectAreaKind.八格友军:
                                        if (self.IsFriendly(troopByPositionNoCheck.BelongedFaction))
                                        {
                                            area.Effect.ApplyEffect(troopByPositionNoCheck.Leader);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
                foreach (TroopEffectArea area in list4)
                {
                //Label_0539:
                    foreach (Point point in self.OffenceArea.Area)
                    {
                        if (!self.BelongedFaction.IsPositionKnown(point))
                        {
                            continue;
                        }
                        troopByPositionNoCheck = Session.Current.Scenario.GetTroopByPositionNoCheck(point);
                        if (troopByPositionNoCheck != null)
                        {
                            switch (area.Kind)
                            {
                                case EffectAreaKind.攻击范围敌军:
                                    if (!self.IsFriendly(troopByPositionNoCheck.BelongedFaction))
                                    {
                                        area.Effect.ApplyEffect(troopByPositionNoCheck.Leader);
                                    }
                                    break;

                                case EffectAreaKind.攻击范围友军:
                                    if (self.IsFriendly(troopByPositionNoCheck.BelongedFaction))
                                    {
                                        area.Effect.ApplyEffect(troopByPositionNoCheck.Leader);
                                    }
                                    break;
                            }
                        }
                    }
                }
                foreach (Troop troop in list)
                {
                    Troop.CheckTroopRout(troop);
                }
            }
        }

        public bool CheckCondition(Troop troop)
        {
            return Condition.CheckConditionList(this.Conditions.Conditions.Values, troop);
        }

        public bool CheckTroop(Troop troop)
        {
            if (!this.Happened || this.Repeatable)
            {
                if (!((this.AfterHappenedEvent == null) || this.AfterHappenedEvent.Happened))
                {
                    return false;
                }
                if (!GameObject.Chance(this.HappenChance))
                {
                    return false;
                }
                if ((this.LaunchPerson == null) || troop.Persons.HasGameObject(this.LaunchPerson))
                {
                    if (!this.CheckCondition(troop))
                    {
                        return false;
                    }
                    if (this.TargetPersons.Count <= 0)
                    {
                        return true;
                    }
                    GameArea baseViewArea = null;
                    switch (this.CheckArea)
                    {
                        case EventCheckAreaKind.视野:
                            baseViewArea = troop.BaseViewArea;
                            break;

                        case EventCheckAreaKind.八格:
                            baseViewArea = GameArea.GetArea(troop.Position, 1, true);
                            break;

                        case EventCheckAreaKind.攻击范围:
                            baseViewArea = troop.OffenceArea;
                            break;
                    }
                    if (baseViewArea != null)
                    {
                        int num = 0;
                        foreach (Point point in baseViewArea.Area)
                        {
                            if (troop.BelongedFaction != null)
                            {
                                if (troop.BelongedFaction.IsPositionKnown(point))
                                {
                                    Troop troopByPositionNoCheck = Session.Current.Scenario.GetTroopByPositionNoCheck(point);
                                    if (troopByPositionNoCheck != null)
                                    {
                                        foreach (PersonRelation relation in this.TargetPersons)
                                        {
                                            if (((relation.Relation == PersonRelationKind.友好) == troop.IsFriendly(troopByPositionNoCheck.BelongedFaction)) && troopByPositionNoCheck.Persons.HasGameObject(relation.SpeakingPerson))
                                            {
                                                num++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return (num == this.TargetPersons.Count);
                    }
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.ID;
        }

        public void LoadDialogFromString(Dictionary<int, Person> persons, string data)
        {
            if (data == null) return;
            this.Dialogs = ParseDialogEntries(persons, data, nameof(dialogString));
        }

        public void LoadEffectAreaFromString(EventEffectTable eventEffects, string data)
        {
            string[] strArray = SplitTokens(data);
            this.EffectAreas.Clear();
            ValidatePairTokenCount(nameof(EffectAreasString), strArray);
            for (int i = 0; i < strArray.Length; i += 2)
            {
                TroopEffectArea item = new TroopEffectArea();
                item.Kind = (EffectAreaKind) int.Parse(strArray[i]);
                item.Effect = eventEffects.GetEventEffect(int.Parse(strArray[i + 1]));
                this.EffectAreas.Add(item);
            }
        }

        public void LoadEffectPersonFromString(Dictionary<int, Person> persons, EventEffectTable eventEffects, string data)
        {
            string[] strArray = SplitTokens(data);
            this.EffectPersons.Clear();
            ValidatePairTokenCount(nameof(EffectPersonsString), strArray);
            for (int i = 0; i < strArray.Length; i += 2)
            {
                if (!persons.ContainsKey(int.Parse(strArray[i]))) continue;
                TroopEffectPerson item = new TroopEffectPerson();
                item.EffectPerson = persons[int.Parse(strArray[i])];
                item.Effect = eventEffects.GetEventEffect(int.Parse(strArray[i + 1]));
                this.EffectPersons.Add(item);
            }
        }

        public void LoadSelfEffectFromString(EventEffectTable eventEffects, string data)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.SelfEffects.Clear();
            for (int i = 0; i < strArray.Length; i++)
            {
                GameObjects.TroopDetail.EventEffect.EventEffect eventEffect = eventEffects.GetEventEffect(int.Parse(strArray[i]));
                if (eventEffect != null)
                {
                    this.SelfEffects.Add(eventEffect);
                }
            }
        }

        public void LoadTargetPersonFromString(Dictionary<int, Person> persons, string data)
        {
            string[] strArray = SplitTokens(data);
            this.TargetPersons.Clear();
            ValidatePairTokenCount(nameof(TargetPersonsString), strArray);
            for (int i = 0; i < strArray.Length; i += 2)
            {
                if (!persons.ContainsKey(int.Parse(strArray[i + 1]))) continue;
                PersonRelation item = new PersonRelation();
                item.Relation = (PersonRelationKind) int.Parse(strArray[i]);
                item.SpeakingPerson = persons[int.Parse(strArray[i + 1])];
                this.TargetPersons.Add(item);
            }
        }

        public string SaveDialogToString()//剧本的部队事件的对话武将默认全部变成了0，而且目前源码转换中，并没有这一块的安排
        {
            if (this.Dialogs == null || this.Dialogs.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (PersonDialog dialog in this.Dialogs)
            {
                builder.Append(dialog.SpeakingPerson != null ? dialog.SpeakingPerson.ID : -1);
                builder.Append('\t');
                builder.Append(NormalizeSerializedDialogText(dialog.Text));
                builder.Append('\n');
            }
            return builder.ToString();
        }

        public string SaveEffectAreaToString()
        {
            string str = "";
            foreach (TroopEffectArea area in this.EffectAreas)
            {
                object obj2 = str;
                str = string.Concat(new object[] { obj2, (int) area.Kind, " ", area.Effect.ID, " " });
            }
            return str;
        }

        public string SaveEffectPersonToString()
        {
            string str = "";
            foreach (TroopEffectPerson person in this.EffectPersons)
            {
                object obj2 = str;
                str = string.Concat(new object[] { obj2, person.EffectPerson.ID, " ", person.Effect.ID, " " });
            }
            return str;
        }

        public string SaveSelfEffectToString()
        {
            string str = "";
            foreach (GameObjects.TroopDetail.EventEffect.EventEffect effect in this.SelfEffects)
            {
                str = str + effect.ID + " ";
            }
            return str;
        }

        public string SaveTargetPersonToString()
        {
            string str = "";
            foreach (PersonRelation relation in this.TargetPersons)
            {
                object obj2 = str;
                str = string.Concat(new object[] { obj2, (int) relation.Relation, " ", relation.SpeakingPerson.ID, " " });
            }
            return str;
        }

        [DataMember]
        public int HappenChance
        {
            get
            {
                return this.happenChance;
            }
            set
            {
                this.happenChance = value;
            }
        }
        [DataMember]
        public bool Happened
        {
            get
            {
                return this.happened;
            }
            set
            {
                this.happened = value;
            }
        }
        [DataMember]
        public bool Repeatable
        {
            get
            {
                return this.repeatable;
            }
            set
            {
                this.repeatable = value;
            }
        }

        public delegate void ApplyTroopEvent(TroopEvent te, Troop troop);
    }
}

