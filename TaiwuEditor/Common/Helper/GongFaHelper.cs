#define DEBUG
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TaiwuEditor
{
    internal class GongFaHelper : HelperBase
    {
        private const string errorString = "无功法数据";
        public GongFaDisplayer ActorDisp { get; private set; }
        public GongFaDisplayer OtherDisp { get; private set; }
        public GongFaEditor Editor { get; private set; }
        private readonly GongFaFilter filter;
        /// <summary>当前角色功法缓存</summary>
        private readonly CurrentActorGongFa currentActorGongFa = new CurrentActorGongFa();

        private static GongFaHelper instance;
        public static GongFaHelper Instance
        {
            get
            {
                if (instance == null)
                    instance = new GongFaHelper();
                return instance;
            }
        }

        protected GongFaHelper()
        {
            filter = new GongFaFilter(this);
            ActorDisp = new GongFaDisplayer("ActorGongFaDisPlayer");
            OtherDisp = new GongFaDisplayer("OtherGongFaDisplayer");
            Editor = new GongFaEditor(this);
        }

        public override void Update(int actorId)
        {
            var dateFile = DateFile.instance;
            if (currentActorId != actorId && currentActorGongFa.Lock())
            {
                currentActorId = actorId;
                currentActorGongFa.gongFas = dateFile?.actorGongFas == null
                                     && dateFile.actorGongFas.TryGetValue(actorId, out SortedDictionary<int, int[]> actorGongFa)
                                     ? actorGongFa : null;
                currentActorGongFa.Unlock();
            }
        }

        public class GongFaFilter
        {
            private readonly Dictionary<string, int> gongFaTypes = new Dictionary<string, int>(17);
            private readonly Dictionary<string, int> gongFaGangs = new Dictionary<string, int>();
            private readonly Dictionary<string, int> gongFaPrestiges = new Dictionary<string, int>
            {
                { "全部", 0 },
                { $"{textColors[20010]}一品{textColors[-1]}", 9 },
                { $"{textColors[20009]}二品{textColors[-1]}", 8 },
                { $"{textColors[20008]}三品{textColors[-1]}", 7 },
                { $"{textColors[20007]}四品{textColors[-1]}", 6 },
                { $"{textColors[20006]}五品{textColors[-1]}", 5 },
                { $"{textColors[20005]}六品{textColors[-1]}", 4 },
                { $"{textColors[20004]}七品{textColors[-1]}", 3 },
                { $"{textColors[20003]}八品{textColors[-1]}", 2 },
                { $"{textColors[20002]}九品{textColors[-1]}", 1 }
            };

            private readonly GongFaHelper parent;
            /// <summary>功法类型，0为全部类型</summary>
            private int type;
            /// <summary>功法门派，0为全部门派</summary>
            private int gang;
            /// <summary>功法品阶，0为全部品阶</summary>
            private int prestige;


            public GongFaFilter(GongFaHelper instance)
            {
                parent = instance;

                foreach (var data in DateFile.instance.baseSkillDate)
                {
                    if (data.Key > 100 && data.Value.TryGetValue(0, out var type))
                        gongFaTypes[type] = data.Key;
                }
                gongFaTypes["全部"] = 0;

                foreach (var data in DateFile.instance.presetGangDate)
                {
                    if (data.Value[2] == "1")
                        gongFaGangs[data.Value[0]] = data.Key;
                }
                gongFaGangs["全部"] = 0;

                DropDownMenu.CreateMenuInstance("gongFaType", new List<string>(gongFaTypes.Keys), (string name) =>
                {
                    if (!type.Equals(gongFaTypes[name]))
                    {
                        type = gongFaTypes[name];
                        GetCurrentActorGongFa();
                        GetCurrentOtherGongFa();
                    }
                });
                DropDownMenu.CreateMenuInstance("gongFaGang", new List<string>(gongFaGangs.Keys), (string name) =>
                {
                    if (gang != gongFaGangs[name])
                    {
                        gang = gongFaGangs[name];
                        GetCurrentActorGongFa();
                        GetCurrentOtherGongFa();
                    }
                });
                DropDownMenu.CreateMenuInstance("gongFaPrestige", new List<string>(gongFaPrestiges.Keys), (string name) =>
                {
                    if (prestige != gongFaPrestiges[name])
                    {
                        prestige = gongFaPrestiges[name];
                        GetCurrentActorGongFa();
                        GetCurrentOtherGongFa();
                    }
                });
            }

            public void FilterBar()
            {
                GUILayout.BeginHorizontal("box");

                GUILayout.EndHorizontal();
            }

            /// <summary>
            /// 返回满足筛选条件的当前角色的功法
            /// </summary>
            public void GetCurrentActorGongFa()
            {
                parent.ActorDisp.displayedGongFas.Clear();
                int locker = 0;
                Parallel.ForEach(parent.currentActorGongFa.gongFas.Keys, (key) =>
                {
                    try
                    {
                        // 游戏中不存在功法ID会直接返回，以便在部分分自定义功法MOD失效后玩家可以删除造成错误的功法
                        bool flag = !DateFile.instance.gongFaDate.TryGetValue(key, out var gongFa) ||
                                (type == 0) || gongFa.TryGetValue(61, out int gongFaType) && gongFaType == type &&
                                (gang == 0) || gongFa.TryGetValue(3, out int gongFaGang) && gongFaGang == gang &&
                                (prestige == 0) || gongFa.TryGetValue(2, out int gongFaPrestige) && gongFaPrestige == prestige;
                        if (flag)
                        {
                            while (Interlocked.CompareExchange(ref locker, 1, 0) == 1) ;
                            try
                            {
                                parent.ActorDisp.displayedGongFas.Add(key);
                            }
                            catch (Exception ex)
                            {
                                Main.logger.Log(ex.ToString());
                            }
                            Interlocked.Exchange(ref locker, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Main.logger.Log(ex.ToString());
                    }
                });
            }

            /// <summary>
            /// 返回满足筛选条件的当前角色的功法
            /// </summary>
            public void GetCurrentOtherGongFa()
            {
                parent.OtherDisp.displayedGongFas.Clear();
                int locker = 0;
                Parallel.ForEach(DateFile.instance.gongFaDate, (gongFa) => 
                {
                    try
                    {
                        bool flag = !parent.currentActorGongFa.gongFas.ContainsKey(gongFa.Key) &&
                        (type == 0) || gongFa.Value.TryGetValue(61, out int gongFaType) && gongFaType == type &&
                        (gang == 0) || gongFa.Value.TryGetValue(3, out int gongFaGang) && gongFaGang == gang &&
                        (prestige == 0) || gongFa.Value.TryGetValue(2, out int gongFaPrestige) && gongFaPrestige == prestige;
                        if (flag)
                        {
                            try
                            {
                                while (Interlocked.CompareExchange(ref locker, 1, 0) == 1) ;
                                parent.OtherDisp.displayedGongFas.Add(gongFa.Key);
                            }
                            catch (Exception ex)
                            {
                                Main.logger.Log(ex.ToString());
                            }
                            Interlocked.Exchange(ref locker, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Main.logger.Log(ex.ToString());
                    }
                });
                parent.OtherDisp.displayedGongFas.Sort();
            }
        }

        /// <summary>
        /// 当前角色功法类
        /// </summary>
        private class CurrentActorGongFa
        {
            /// <summary>多线程锁</summary>
            private int locked;
            public SortedDictionary<int, int[]> gongFas;
            /// <summary>
            /// Non-Blocking Lock
            /// </summary>
            /// <returns>是否加锁成功</returns>
            public bool Lock()
            {
#if DEBUG
                Main.logger.Log("[CurrentActorGongFa] Lock");
#endif
                return (Interlocked.CompareExchange(ref locked, 1, 0) == 0) ? true : false;
            }

            /// <summary>
            /// Non-Blocking UnLock
            /// </summary>
            public void Unlock()
            {
#if DEBUG
                Main.logger.Log("[CurrentActorGongFa] UnLock");
#endif
                Interlocked.Exchange(ref locked, 0);
            }
        }

        public class GongFaDisplayer
        {
            private Vector2 scrollBarPosition = new Vector2();
            public string Name { get; private set; }
            public HashSet<int> SelectedGongFa { get; } = new HashSet<int>();
            public float iconWidth = 120f;
            public readonly List<int> displayedGongFas = new List<int>();
            private int gongFaListIsLocked;

            public GongFaDisplayer(string name)
            {
                Name = name;
            }

            public bool Lock()
            {
#if DEBUG
                Main.logger.Log("[GongFaDisplayer] Lock");
#endif
                return (Interlocked.CompareExchange(ref gongFaListIsLocked, 1, 0) == 0) ? true : false;
            }

            public void Unlock()
            {
#if DEBUG
                Main.logger.Log("[GongFaDisplayer] UnLock");
#endif
                Interlocked.Exchange(ref gongFaListIsLocked, 0);
            }

            public void DisplayGongFas(int countPerRow, float scrollBarWidth)
            {
                scrollBarPosition = GUILayout.BeginScrollView(scrollBarPosition, GUILayout.Width(scrollBarWidth));
                {
                    if (gongFaListIsLocked == 1)
                    {
                        GUILayout.Label("正在载入......", LabelStyle);
                    }
                    else
                    {
                        if (countPerRow > 1)
                        {
                            int counter = 0;
                            foreach (var gongFaId in displayedGongFas)
                            {
                                counter++;
                                if (counter == 1)
                                    GUILayout.BeginHorizontal();

                                GongFaButton(gongFaId);
                                if (counter == countPerRow)
                                {
                                    counter = 0;
                                    GUILayout.EndHorizontal();
                                }
                            }
                            if (counter > 0)
                                GUILayout.EndHorizontal();
                        }
                        else
                            foreach (var gongFaId in displayedGongFas)
                                GongFaButton(gongFaId);
                    }
                }
                GUILayout.EndScrollView();
            }

            public void GongFaButton(int gongFaId)
            {
                var dateFile = DateFile.instance;
                if (dateFile?.gongFaDate != null
                    && dateFile.gongFaDate.TryGetValue(gongFaId, out Dictionary<int, string> gongFaInfo))
                {
                    gongFaInfo.TryGetValue(2, out int gongFaPrestige);
                    gongFaPrestige = Mathf.Clamp(gongFaPrestige, 0, 9);
                    string gongFaName = !gongFaInfo.TryGetValue(0, out string text) ? "" : (gongFaPrestige > 0) ? $"{textColors[gongFaPrestige + 20001]}{text}{textColors[-1]}" : text;
                    string gongFaGangName = gongFaInfo.TryGetValue(3, out int GangId)
                                            && dateFile?.presetGangDate != null
                                            && dateFile.presetGangDate.TryGetValue(GangId, 0, out text, gongFaInfo)
                                            ? text : "";
                    if (LabelStyle != null)
                    {
                        SelectionDisplayStyle.BeginStyle(LabelStyle, SelectedGongFa.Contains(gongFaId));
                        if (GUILayout.Button($"{gongFaName}\nLv{gongFaPrestige}\t{gongFaGangName}", LabelStyle, GUILayout.Width(iconWidth)))
                        {
                            OnSelect(gongFaId, Event.current.control);
                        }
                        SelectionDisplayStyle.EndStyle();
                    }
                    else
                    {
                        if (GUILayout.Button($"{gongFaName}\nLv{gongFaPrestige}\t{gongFaGangName}", GUILayout.Width(iconWidth)))
                            OnSelect(gongFaId, Event.current.control);
                    }
                }
                else
                    GUILayout.Button($"{errorString}\n", ButtonStyle ?? GUI.skin.button, GUILayout.Width(iconWidth));
            }

            private void OnSelect(int gongFaId, bool isMulti = false)
            {
                if (isMulti)
                {
                    SelectedGongFa.Add(gongFaId);
                }
                else
                {
                    SelectedGongFa.Clear();
                    SelectedGongFa.Add(gongFaId);
                }
            }
        }

        public class GongFaEditor
        {
            private readonly GongFaHelper parent;
            private readonly string[] gongFaStatus = new string[3];
            private bool isMaxLevel;

            public float labelWidth = 150f;
            public float FieldWidth = 40f;

            public GongFaEditor(GongFaHelper instance)
            {
                parent = instance;
            }

            public void GongFaEditBar()
            {
                GUILayout.BeginHorizontal();
                var labelStyle = LabelStyle ?? GUI.skin.label;
                var textFieldStyle = TextFieldStyle ?? GUI.skin.textField;
                var buttonStyle = ButtonStyle ?? GUI.skin.button;
                GUILayout.Label($"已选择{parent.ActorDisp.SelectedGongFa.Count}个功法", labelStyle, GUILayout.Width(labelWidth));
                GUILayout.Label("修习程度", labelStyle, GUILayout.ExpandWidth(false));
                gongFaStatus[0] = GUILayout.TextField(gongFaStatus[0], textFieldStyle, GUILayout.Width(FieldWidth));
                GUILayout.Label("心法", labelStyle, GUILayout.ExpandWidth(false));
                gongFaStatus[1] = GUILayout.TextField(gongFaStatus[1], textFieldStyle, GUILayout.Width(FieldWidth));
                GUILayout.Label("逆练", labelStyle, GUILayout.ExpandWidth(false));
                gongFaStatus[2] = GUILayout.TextField(gongFaStatus[2], textFieldStyle, GUILayout.Width(FieldWidth));
                if (GUILayout.Button("修改", buttonStyle, GUILayout.ExpandWidth(false)))
                {
                    OnChange();
                }
                if (GUILayout.Button("遗忘", buttonStyle, GUILayout.ExpandWidth(false)))
                {
                    OnForget();
                }
                GUILayout.EndHorizontal();
            }

            public void GongFaAddBar()
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"已选择{parent.OtherDisp.SelectedGongFa.Count}个功法", LabelStyle ?? GUI.skin.label, GUILayout.Width(labelWidth));
                isMaxLevel = GUILayout.Toggle(isMaxLevel, "大成", GUILayout.ExpandWidth(false));
                if (GUILayout.Button("学习", ButtonStyle ?? GUI.skin.button, GUILayout.ExpandWidth(false)))
                {
                    OnAdd();
                }
                GUILayout.EndHorizontal();
            }

            private void OnAdd()
            {

            }

            private void OnChange()
            {

            }

            private void OnForget()
            {

            }
        }

        private static class SelectionDisplayStyle
        {
            private static Texture2D originBackground;
            private static GUIStyle currentGUIStyle;
            private static bool selected = false;
            private static int callerCount = 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void BeginStyle(GUIStyle gUIStyle, bool isSelected = false)
            {
                if (callerCount != 0)
                {
                    throw new InvalidOperationException("No EndStyle() or Called More Than Once Before EndStyle()");
                }
                callerCount++;
                currentGUIStyle = gUIStyle;
                selected = isSelected;
                if (selected)
                {
                    originBackground = currentGUIStyle.normal.background;
                    currentGUIStyle.normal.background = GUI.skin.button.onActive.background;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EndStyle()
            {
                if (callerCount != 1)
                {
                    throw new InvalidOperationException("No BeginStyle() or Called More Than Once Before BeginStyle()");
                }

                callerCount--;
                if (selected)
                {
                    currentGUIStyle.normal.background = originBackground;
                }
            }
        }
    }
}
