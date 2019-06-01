using Harmony12;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace GuiWorldNpc
{
    public class Settings : UnityModManager.ModSettings
    {
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save<Settings>(this, modEntry);
        }
        public float scrollSpeed = 15;//�����ٶ�
        public bool open = true; //ʹ�ù������NPC
        public int numberOfColumns = 1;//һ����ʾ����


    }
    public static class Main
    {
        public static bool OnChangeList;
        public static bool showNpcInfo;

        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            #region ��������
            settings = Settings.Load<Settings>(modEntry);
            Logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            HarmonyInstance harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            #endregion

            Main.settings.open = true;
            return true;
        }

        static string title = "�������NPC ���԰�";
        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label(title, GUILayout.Width(300));
            // Main.settings.open = GUILayout.Toggle(Main.settings.open, "�������NPC");
            // Main.settings.open = !GUILayout.Toggle(!Main.settings.open, "ԭ������NPC");
            GUILayout.Label("�����ٶȡ���������");
            int speed;
            if(int.TryParse(GUILayout.TextField(settings.scrollSpeed.ToString()), out speed))
            {
                settings.scrollSpeed = speed;
            }
            GUILayout.Label("<color=#F63333>�������BUGӰ������ʹ��������ϷĿ¼Mods�ļ�����ɾ��GuiWorldNpcȻ��ȴ�����</color>");
        }


        [HarmonyPatch(typeof(SetPlaceActor), "ShowEventMassage")]
        public static class SetPlaceActor_ShowEventMassage_Patch
        {
            public static bool Prefix()
            {
                if (!Main.enabled)
                    return true;
                //Main.Logger.Log("�����NPC");
                //var t = WorldMapSystem_UpdatePlaceActor_Patch.actorHolder.transform;
                //GuiBaseUI.Main.LogAllChild(t, true);
                return true;
            }
        }

        [HarmonyPatch(typeof(WorldMapSystem), "RemoveActor")]
        public static class WorldMapSystem_RemoveActor_Patch1
        {
            public static bool Prefix()
            {
                if (!Main.enabled)
                    return true;
                //Main.Logger.Log("�Ƴ�NPC");
                WorldMapSystem_UpdatePlaceActor_Patch.actorHolder.data = new int[0];
                return false;
            }
        }

        [HarmonyPatch(typeof(WorldMapSystem), "UpdatePlaceActor", new Type[] { typeof(int)})]
        public static class WorldMapSystem_UpdatePlaceActor_Patch1
        {
            public static bool Prefix(int key)
            {
                if (!Main.enabled)
                    return true;
                //Main.Logger.Log("����NPC");
                bool show = DateFile.instance.mianPlaceId == WorldMapSystem.instance.choosePlaceId || WorldMapSystem.instance.playerNeighbor.Contains(WorldMapSystem.instance.choosePlaceId);
                if (UIMove.instance.movePlaceActorIn)
                {
                    for (int i = 1; i < WorldMapSystem.instance.actorHolder.childCount-1; i++)
                    {
                        GameObject gameObject = WorldMapSystem.instance.actorHolder.GetChild(i).gameObject;
                        var array = gameObject.name.Split(new char[] { ',' });
                        if (array.Length > 1)
                        {
                            if (int.Parse(array[1]) == key)
                            {
                                gameObject.GetComponentInChildren<SetPlaceActor>().SetActor(key, show);
                                break;
                            }
                        }
                    }
                }
                return false;
            }
        }


            [HarmonyPatch(typeof(WorldMapSystem), "UpdatePlaceActor", new Type[] { typeof(int), typeof(int) })]
        public static class WorldMapSystem_UpdatePlaceActor_Patch
        {
            public static NewWorldNpc actorHolder;

            static FieldInfo m_showPlaceActorTyp;
            // ����˽���ֶ�
            public static int showPlaceActorTyp
            {
                get {
                    if(m_showPlaceActorTyp == null)
                    {
                        m_showPlaceActorTyp = typeof(WorldMapSystem).GetField("showPlaceActorTyp", BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                    int value = 1;
                    try
                    {
                        value = (int)m_showPlaceActorTyp.GetValue(WorldMapSystem.instance);
                    }
                    catch
                    {
                        Main.Logger.Log("�������");
                    }
                    
                    return value;
                }
            }

            private static void UpdatePlaceActor(int partId, int placeId)
            {
                showNpcInfo  = DateFile.instance.mianPlaceId == WorldMapSystem.instance.choosePlaceId || WorldMapSystem.instance.playerNeighbor.Contains(WorldMapSystem.instance.choosePlaceId);
                List<int> list;
                int num = showPlaceActorTyp;
                switch (num)
                {
                    case 3:
                        list = new List<int>(DateFile.instance.HaveActor(partId, placeId, false, false, true, true));
                        break;
                    case 2:
                        list = new List<int>(DateFile.instance.HaveActor(partId, placeId, false, true, false, true));
                        break;
                    case 1:
                    default:
                        list = new List<int>(DateFile.instance.HaveActor(partId, placeId, true, false, false, true));
                        break;
                }


                List<int> list2 = new List<int>();
                if (list.Count > 0)
                {
                    Dictionary<int, int> dictionary = new Dictionary<int, int>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        int id = list[i];
                        int num2 = int.Parse(DateFile.instance.GetActorDate(id, 20, false));
                        dictionary.Add(list[i], Mathf.Abs(num2) * ((num2 <= 0) ? 15000 : 10000) - int.Parse(DateFile.instance.GetActorDate(id, 11, false)));
                    }
                    List<KeyValuePair<int, int>> list3 = new List<KeyValuePair<int, int>>(dictionary);
                    list3.Sort((KeyValuePair<int, int> s1, KeyValuePair<int, int> s2) => s1.Value.CompareTo(s2.Value));
                    foreach (KeyValuePair<int, int> keyValuePair in list3)
                    {
                        list2.Add(keyValuePair.Key);
                    }
                    // list2������Ҫ��ʾ�������б�
                    UIMove.instance.PlaceActorUimove(true);
                    if (partId == DateFile.instance.mianPartId)
                    {
                        WorldMapSystem.instance.worldMapPlaces[placeId].UpdatePlaceActors();
                    }
                }
                else
                {
                    for (int l = 0; l < WorldMapSystem.instance.actorHolder.childCount; l++)
                    {
                        WorldMapSystem.instance.actorHolder.GetChild(l).gameObject.SetActive(false);
                    }
                    UIMove.instance.PlaceActorUimove(false);
                }
                actorHolder.data = list2.ToArray();
            }


            private static void InitUI(int partId, int placeId)
            {
                actorHolder = WorldMapSystem.instance.actorHolder.parent.parent.gameObject.AddComponent<NewWorldNpc>();
                actorHolder.Init(partId, placeId);
            }

            public static bool Prefix(int partId, int placeId)
            {
                if (!Main.enabled)
                    return true;
                //Main.Logger.Log("ˢ��npc�ж���");
                //if (!Main.settings.open)
                //{
                //    return true;
                //}
                if (actorHolder == null)
                {
                    InitUI(partId, placeId);
                }
                UpdatePlaceActor(partId, placeId);

                return false;
            }
        }
    }
}