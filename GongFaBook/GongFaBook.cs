// GongFaBook.Main
using Harmony12;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;

namespace GongFaBook
{
    public static class Main
    {
        public class Settings : UnityModManager.ModSettings
        {
            public bool showAll = true;

            public override void Save(UnityModManager.ModEntry modEntry)
            {
                Save(this, modEntry);
            }
        }

        public static bool enabled;
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static Settings settings;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            /// 修复调用<see cref="ActorMenu.instance"/>时，人物窗口自动打开造成错误
            /// 只在其他mod没有修补这个bug时加载补丁，防止重复加载。
            var actorMenuAwake = typeof(ActorMenu).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            var postFixes = harmony.GetPatchInfo(actorMenuAwake)?.Postfixes;
            if (postFixes == null || postFixes.Count == 0)
            {
                var patchedPostfix = new HarmonyMethod(typeof(ActorMenu_Awake_Patch), "Postfix", new[] { typeof(ActorMenu) });
                harmony.Patch(actorMenuAwake, null, patchedPostfix);
            }
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginVertical("Box");
            settings.showAll = GUILayout.Toggle(settings.showAll, "是否显示所有不传之秘");
            GUILayout.EndVertical();
        }

        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (!value)
            {
                return false;
            }
            enabled = value;
            return true;
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }

    /// <summary>
    /// 显示不传之秘
    /// </summary>
    [HarmonyPatch(typeof(SetGongFaTree), "SetGongFaIcon")]
    public static class SetGongFaTree_SetGongFaIcon_Patch
    {
        private static bool Prefix(SetGongFaTree __instance, int typ, int gangId, int gangValue)
        {
            if (!Main.enabled)
            {
                return true;
            }
            int num = DateFile.instance.MianActorID();
            SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(__instance.gongFaImage, "gongFaImage", typ);
            __instance.gongFaNameText.text = DateFile.instance.baseSkillDate[101 + typ][0];
            int num2 = (gangValue >= 0) ? Mathf.Max(gangValue / 100 - 2, 0) : (-1);
            List<int> list = new List<int>();
            for (int i = 0; i < __instance.gongFaLevelText.Length; i++)
            {
                __instance.gongFaLevelText[i].text = DateFile.instance.massageDate[7003][3].Split('|')[i];
            }
            for (int j = 0; j < DateFile.instance.allGongFaKey.Count; j++)
            {
                int num3 = DateFile.instance.allGongFaKey[j];
                if (int.Parse(DateFile.instance.gongFaDate[num3][1]) == typ && int.Parse(DateFile.instance.gongFaDate[num3][3]) == gangId)
                {
                    list.Add(num3);
                }
            }
            for (int k = 0; k < __instance.gongFaIcons.Length; k++)
            {
                if (k < list.Count)
                {
                    int num4 = list[k];
                    __instance.gongFaImages[k].SetActive(value: true);
                    __instance.gongFaImages[k].name = "GongFaImage," + num4;
                    if (DateFile.instance.actorGongFas.ContainsKey(num) && DateFile.instance.actorGongFas[num].ContainsKey(num4))
                    {
                        SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(__instance.gongFaIcons[k], "gongFaSprites", int.Parse(DateFile.instance.gongFaDate[num4][98]));
                        __instance.gongFaNames[k].text = DateFile.instance.SetColoer((num2 < k) ? 20002 : 10003, DateFile.instance.gongFaDate[num4][0]);
                        if (DateFile.instance.GetGongFaLevel(num, num4) >= 100 && DateFile.instance.GetGongFaFLevel(num, num4) >= 10)
                        {
                            __instance.gongFaStudyMassageText[k].text = DateFile.instance.SetColoer(20009, DateFile.instance.massageDate[7007][5].Split('|')[3]);
                        }
                        else
                        {
                            __instance.gongFaStudyMassageText[k].text = DateFile.instance.SetColoer(20008, DateFile.instance.massageDate[7007][5].Split('|')[2]);
                        }
                        continue;
                    }
                    if (num2 < k && int.Parse(DateFile.instance.gongFaDate[num4][16]) == 1 && !Main.settings.showAll)
                    {
                        __instance.gongFaIcons[k].GetComponent<PointerEnter>().enabled = false;
                        SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(__instance.gongFaIcons[k], "gongFaSprites", int.Parse(DateFile.instance.gongFaDate[num4][98]));
                        __instance.gongFaNames[k].text = DateFile.instance.SetColoer(10004, DateFile.instance.massageDate[7007][5].Split('|')[0]);
                        __instance.gongFaStudyMassageText[k].text = DateFile.instance.SetColoer(10004, DateFile.instance.massageDate[7007][5].Split('|')[1]);
                    }
                    else
                    {
                        SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(__instance.gongFaIcons[k], "gongFaSprites", int.Parse(DateFile.instance.gongFaDate[num4][98]));
                        __instance.gongFaNames[k].text = DateFile.instance.SetColoer((num2 < k) ? 20002 : 10003, DateFile.instance.gongFaDate[num4][0]);
                        __instance.gongFaStudyMassageText[k].text = "";
                    }
                    __instance.gongFaIcons[k].color = ((num2 < k) ? new Color(0f, 0f, 0f) : new Color(1f, 1f, 1f));
                }
                else
                {
                    __instance.gongFaImages[k].SetActive(value: false);
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 功法书本显示手抄及正逆练效果
    /// </summary>
    [HarmonyPatch(typeof(WindowManage), "ShowItemMassage")]
    public static class WindowManage_ShowBookMassage_Patch
    {
        private static readonly StringBuilder str = new StringBuilder();
        private static void Postfix(int itemId, ref string ___baseWeaponMassage, ref Text ___informationMassage, ref Text ___informationName)
        {
            if (!Main.enabled)
            {
                return;
            }
            if (int.Parse(DateFile.instance.GetItemDate(itemId, 31)).Equals(17))
            {
                str.Clear();
                int clipIndex = ___baseWeaponMassage.IndexOf("所载心法");
                if (clipIndex > -1)
                    str.Append(___baseWeaponMassage.Substring(0, clipIndex - 18));
                else
                    str.Append(___baseWeaponMassage);
                string itemDescrip = DateFile.instance.GetItemDate(itemId, 99);
                if (int.Parse(DateFile.instance.GetItemDate(itemId, 35)).Equals(1))
                {
                    ___informationName.text = ___informationName.text.Insert(___informationName.text.IndexOf("》"), "·手抄");
                    str.Replace(itemDescrip, DateFile.instance.SetColoer(20010, itemDescrip));
                }
                else
                {
                    str.Replace(itemDescrip, DateFile.instance.SetColoer(20004, itemDescrip));
                }
                int gongFaKey = int.Parse(DateFile.instance.GetItemDate(itemId, 32, otherMassage: false));
                if (DateFile.instance.gongFaDate.TryGetValue(gongFaKey, out var gongFaInfo))
                {
                    int gongFaFEffect = int.Parse(gongFaInfo[103]);
                    if (DateFile.instance.gongFaFPowerDate.TryGetValue(gongFaFEffect, out var gongFaFPower))
                    {
                        str.Append(DateFile.instance.SetColoer(10002, "【所载心法】\n"));
                        str.Append(DateFile.instance.SetColoer(20004, $"·正练:{gongFaFPower[99]}\n"));
                    }
                    gongFaFEffect = int.Parse(DateFile.instance.gongFaDate[gongFaKey][104]);
                    if (DateFile.instance.gongFaFPowerDate.TryGetValue(gongFaFEffect, out gongFaFPower))
                    {
                        str.Append(DateFile.instance.SetColoer(20010, $"·逆练:{gongFaFPower[99]}\n"));
                        string debuff = gongFaFPower[98];
                        if (debuff.Length > 0)
                        {
                            str.Append(DateFile.instance.SetColoer(20010, $"·逆练Debuff: {debuff}\n"));
                        }
                    }
                }
                ___baseWeaponMassage = str.ToString();
                ___informationMassage.text = str.ToString();
            }
        }
    }

    /// <summary>
    /// 功法显示正逆练
    /// </summary>
    [HarmonyPatch(typeof(WindowManage), "ShowGongFaMassage")]
    public static class WindowManage_ShowGongFaMassage_Patch
    {
        /*private const int CORRECTED_VALUE = 100;
        private const int GONGFATYPE_PRACTICETYPE_CHECKNUM = 5000;
        private const int NORMAL_TYPE_ID = 103;
        private const int UNNORMAL_TYPE_ID = 104;*/

        private static readonly StringBuilder str = new StringBuilder();

        // 未练出心法效果或心法冲解则两种心法效果都显示
        private static void Postfix(WindowManage __instance, int skillId, int skillTyp, int levelTyp, int actorId, Toggle toggle, ref Text ___informationMassage, ref string ___baseGongFaMassage)
        {
            if (Main.enabled && skillTyp != 0 && skillTyp == 1)
            {
                actorId = (actorId != -1) ? actorId : ((!ActorMenu.instance.actorMenu.activeInHierarchy) ? DateFile.instance.MianActorID() : ActorMenu.instance.actorId);
                int gongFaFLevel = (levelTyp != -1 && levelTyp != 0) ? 10 : ((skillId != 0) ? DateFile.instance.GetGongFaFLevel(actorId, skillId) : 0);
                // 心法正/逆类型
                int gongFaFTyp = DateFile.instance.GetGongFaFTyp(actorId, skillId);
                // 心法正练效果
                int gongFaFEffect = int.Parse(DateFile.instance.gongFaDate[skillId][103]);
                if (gongFaFEffect > 0)
                {
                    // 心法逆练效果
                    int gongFaBadFEffect = int.Parse(DateFile.instance.gongFaDate[skillId][104]);
                    str.Clear();
                    str.Append(___baseGongFaMassage);
                    // 未练出心法效果或心法冲解则两种心法效果都显示
                    if (gongFaFLevel < 5 || gongFaFTyp == 2)
                    {
                        str.Append(DateFile.instance.SetColoer(20004, $"  如果正练\n"))
                            .Append(__instance.SetMassageTitle(8007, 3, 11, 20010))
                            .Append(__instance.Dit())
                            .Append(DateFile.instance.SetColoer(20002, $"{DateFile.instance.gongFaFPowerDate[gongFaFEffect][99]}{((DateFile.instance.gongFaFPowerDate[gongFaFEffect][98] == "") ? "" : DateFile.instance.massageDate[5001][4])}{DateFile.instance.gongFaFPowerDate[gongFaFEffect][98]}{DateFile.instance.massageDate[5001][5]}"))
                            .Append("\n\n");
                        str.Append(DateFile.instance.SetColoer(20004, $"  如果逆练\n"))
                            .Append(__instance.SetMassageTitle(8007, 3, 12, 20005))
                            .Append(__instance.Dit())
                            .Append(DateFile.instance.SetColoer(20002, $"{DateFile.instance.gongFaFPowerDate[gongFaBadFEffect][99]}{((DateFile.instance.gongFaFPowerDate[gongFaBadFEffect][98] == "") ? "" : DateFile.instance.massageDate[5001][4])}{DateFile.instance.gongFaFPowerDate[gongFaBadFEffect][98]}{DateFile.instance.massageDate[5001][5]}"))
                            .Append("\n\n");
                    }
                    else
                    {
                        // 显示出已练成心法的另外一种心法效果
                        bool flag = gongFaFTyp == 0;
                        int key = flag ? gongFaBadFEffect : gongFaFEffect;
                        str.Append(DateFile.instance.SetColoer(20004, $"  如果{(flag ? "逆" : "正")}练\n"))
                            .Append(__instance.SetMassageTitle(8007, 3, flag ? 12 : 11, flag ? 20010 : 20005))
                            .Append(__instance.Dit())
                            .Append(DateFile.instance.SetColoer(20002, $"{DateFile.instance.gongFaFPowerDate[key][99]}{((DateFile.instance.gongFaFPowerDate[key][98] == "") ? "" : DateFile.instance.massageDate[5001][4])}{DateFile.instance.gongFaFPowerDate[key][98]}{DateFile.instance.massageDate[5001][5]}"))
                            .Append("\n\n");
                    }
                    ___informationMassage.text = ___baseGongFaMassage = str.ToString();
                }
            }
        }
    }

    /// <summary>
    /// 防止首次调用ActorMenu.instance时自动打开角色菜单造成错误
    /// </summary>
    /// /// <remarks>用<see cref="HarmonyInstance.Patch"/>加载，只在其他mod没有patch这个方法
    /// 时使用，避免重复加载
    /// </remarks>
    internal static class ActorMenu_Awake_Patch
    {
        public static void Postfix(ActorMenu __instance)
        {
#if DEBUG
            Main.Logger.Log("GongFaBook.ActorMenu_Awake_Patch patched");
#endif
            __instance.actorMenu.SetActive(false);
        }
    }
}

