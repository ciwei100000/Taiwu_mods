using Harmony12;
using System;
using System.Collections.Generic;
using GameData;

namespace TaiwuEditor
{
    internal static class Patch
    {
        /// <summary>
        /// 最大好感和最大印象
        /// </summary>
        [HarmonyPatch(typeof(ui_MessageWindow), "SetMassageWindow")]
        private static class ui_MessageWindow_SetMassageWindow_Hook
        {
            private static bool Prefix(int[] baseEventDate)
            {
                // 最大好感
                if (Main.enabled && Main.settings.basicUISettings[5])
                {
                    MessageEventManager.Instance.MainEventData = (int[])baseEventDate.Clone();
                    // 主事件ID
                    int mainEventId = baseEventDate[2];
                    // 事件类型
                    int eventType = int.Parse(DateFile.instance.eventDate[mainEventId][2]);
                    //int num2 = DateFile.instance.MianActorID();
                    //int num3 = (num != 0) ? ((num != -1) ? num : num2) : baseEventDate[1];
                    if (eventType == 0) // 事件类型是与NPC互动
                    {
                        // baseEventDate[1]是互动的NPC的ID
                        int npcId = baseEventDate[1];
                        if (Characters.HasChar(npcId))
                        {
                            //try catch 目前用于跳过个别时候载入游戏时触发过月后npc互动会报错的问题
                            try
                            {
                                // 改变好感
                                DateFile.instance.ChangeFavor(npcId, 60000, true, false);
                            }
                            catch (Exception e)
                            {
                                Main.logger.Log("[TaiwuEditor]");
                                Main.logger.Log("好感修改失败");
                                Main.logger.Log(e.Message);
                                Main.logger.Log(e.StackTrace);
                            }
                        }
                    }
                }
                /// <summary>最大印象</summary>
                /// <see cref="DateFile.ChangeActorLifeFace"/>
                if (Main.enabled && Main.settings.basicUISettings[6])
                {
                    int mainEventId = baseEventDate[2];
                    int eventType = int.Parse(DateFile.instance.eventDate[mainEventId][2]);
                    int mainActorId = DateFile.instance.MianActorID();
                    //int num6 = (eventType != 0) ? ((eventType != -1) ? eventType : num5) : baseEventDate[1];
                    if (eventType == 0)
                    {
                        int npcId = baseEventDate[1];
                        if (Characters.HasChar(npcId))
                        {
                            //try catch 目前用于跳过个别特殊npc本身无印象数据会报错的问题
                            try
                            {
                                // 改变印象
                                // 时装ID
                                int fashionDress = int.Parse(DateFile.instance.GetActorDate(mainActorId, 305));
                                if (fashionDress > 0)
                                {
                                    // 时装身份ID
                                    int faceId = int.Parse(DateFile.instance.GetItemDate(fashionDress, 15));
                                    /// <seealso cref="DateFile.ResetActorLifeFace"/>
                                    if (faceId != 0)
                                    {
                                        DateFile.instance.actorLife[npcId].Remove(1001);
                                        // 添加新印象,100%
                                        DateFile.instance.actorLife[npcId].Add(1001, new List<int>
                                        {
                                            faceId,
                                            100
                                        });
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Main.logger.Log("[TaiwuEditor]");
                                Main.logger.Log("印象修改失败");
                                Main.logger.Log(e.Message);
                                Main.logger.Log(e.StackTrace);
                            }
                        }
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// 锁定戒心为0
        /// </summary>
        [HarmonyPatch(typeof(DateFile), "GetActorWariness")]
        private static class DateFile_GetActorWariness_Hook
        {
            private static void Postfix(ref int __result)
            {
                if (Main.enabled && Main.settings.basicUISettings[7])
                {
                    __result = 0;
                }
            }
        }

        /// <summary>
        /// 快速修习
        /// </summary>
        [HarmonyPatch(typeof(BuildingWindow), "StudySkillUp")]
        private static class HomeSystem_StudySkillUp_Hook
        {
            private static bool Prefix(int ___studySkillId, int ___studySkillTyp, ref BuildingWindow __instance)
            {
                if (!Main.enabled || !Main.settings.basicUISettings[2] || ___studySkillId <= 0 || ___studySkillTyp <= 0 || ___studySkillTyp > 17)
                {
                    return true;
                }
                else
                {
                    int mainActorId = DateFile.instance.MianActorID();
                    if (___studySkillTyp == 17)
                    {
                        if (DateFile.instance.GetGongFaLevel(mainActorId, ___studySkillId, 0) >= 100)
                        {
                            return false;
                        }
                        // 基础遗惠
                        int scoreGain = int.Parse(DateFile.instance.gongFaDate[___studySkillId][2]);
                        // 清零因为实战而获得的突破成功率加成
                        DateFile.instance.addGongFaStudyValue = 0;
                        DateFile.instance.ChangeActorGongFa(mainActorId, ___studySkillId, 100, 0, 0, false);
                        // 突破成功一次增加的遗惠
                        DateFile.instance.AddActorScore(302, scoreGain * 100);
                        if (DateFile.instance.GetGongFaLevel(mainActorId, ___studySkillId, 0) >= 100 && DateFile.instance.GetGongFaFLevel(mainActorId, ___studySkillId) >= 10)
                        {
                            // 修习到100%时增加的遗惠
                            DateFile.instance.AddActorScore(304, scoreGain * 100);
                        }
                    }
                    else
                    {
                        if (DateFile.instance.GetSkillLevel(___studySkillId) >= 100)
                        {
                            return false;
                        }
                        int scoreGain = int.Parse(DateFile.instance.skillDate[___studySkillId][2]);
                        // 清零因为较艺而获得的突破成功率加成
                        DateFile.instance.addSkillStudyValue = 0;
                        DateFile.instance.ChangeMianSkill(___studySkillId, 100, 0, false);
                        //DateFile.instance.actorSkills[___studySkillId][0] = 100;
                        // 突破成功一次增加的遗惠
                        DateFile.instance.AddActorScore(202, scoreGain * 100);
                        if (DateFile.instance.GetSkillLevel(___studySkillId) >= 100 && DateFile.instance.GetSkillFLevel(___studySkillId) >= 10)
                        {
                            // 修习到100%时增加的遗惠
                            DateFile.instance.AddActorScore(204, scoreGain * 100);
                        }
                    }
                    __instance.UpdateStudySkillWindow();
                    __instance.UpdateLevelUPSkillWindow();
                    __instance.UpdateReadBookWindow();
                    return false;
                }
            }
        }

        /// <summary>
        /// 奇遇直接到终点
        /// </summary>
        [HarmonyPatch(typeof(StorySystem), "OpenStory")]
        private static class StorySystem_OpenStory_Hook
        {
            private static bool Prefix(ref StorySystem __instance, ref bool ___keepHiding)
            {
                if (!Main.enabled || !Main.settings.basicUISettings[3])
                {
                    return true;
                }

                /// 借鉴<see cref="StorySystem.StorySystemOpend"/>。<see cref="StorySystem.StoryToBattle"/>会将此值设为true
                /// 用这个值判断是否本次调用<see cref="StorySystem.OpenStory"/>是否由<see cref="StorySystem.StoryToBattle"/>
                /// 返回的，防止奇遇终点是战斗时，无限进入战斗。
                if (___keepHiding)
                {
                    return true;
                }

                int storyId = __instance.storySystemStoryId;
#if DEBUG
                Main.logger.Log($"[TaiwuEditor]OpenStory: StoryId: {storyId}");
#endif
                if (storyId > 0)
                {
                    bool storyIdExist = false;
                    for (int i = 0; i < Main.settings.includedStoryTyps.Length; i++)
                    {
                        if (Main.settings.includedStoryTyps[i])
                        {
                            if (Main.settings.GetStoryTyp(i).IsContainStoryId(storyId))
                            {
                                storyIdExist = true;
                                break;
                            }
                        }
                    }
                    if (!storyIdExist)
                    {
                        return true;
                    }
                    // 关闭奇遇窗口
                    ToStoryMenu.Instance.CloseToStoryMenu();
                    // 终点的事件ID
                    int storyEndEventId = int.Parse(DateFile.instance.baseStoryDate[storyId][302]);
#if DEBUG
                    Main.logger.Log($"[TaiwuEditor]OpenStory: storyEndEventId: {storyEndEventId}");
                    Main.logger.Log($"[TaiwuEditor]OpenStory: StoryPlayer: {StorySystem.instance.storyPlayer.name}");
                    Main.logger.Log($"[TaiwuEditor]OpenStory: StoryPlayer_placeid: {StorySystem.instance.storyPlayer.parent.parent.parent.name}");
#endif
                    if (HelperBase.EventSetup(StorySystem.StoryBaseManager, storyEndEventId,
                                              __instance.storySystemPartId, __instance.storySystemPlaceId,
                                              __instance.storySystemStoryId))
                    {
#if DEBUG
                        Main.logger.Log("EventSetup successful");
#endif
                    }
                    else
                    {
                        Main.logger.Log($"[TaiwuEditor]OpenStory has been removed due to storyEndEventId is 0");
                        __instance.StoryEnd();
                    }
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 背包最大载重
        /// </summary>
        [HarmonyPatch(typeof(DateFile), "GetMaxItemSize")]
        private static class DateFile_GetMaxItemSize_Hook
        {
            private static void Postfix(ref int key, ref int __result)
            {
                if (Main.enabled && Main.settings.basicUISettings[4] && DateFile.instance.mianActorId == key)
                {
                    __result = 999999999;
                }
            }
        }

        /// <summary>
        /// 快速读书
        /// </summary>
        [HarmonyPatch(typeof(BuildingWindow), "StartReadBook")]
        private static class BuildingWindow_StartReadBook_Hook
        {
            private static bool Prefix(int ___readBookId, int ___studySkillTyp, BuildingWindow __instance)
            {
#if DEBUG
                Main.logger.Log($"[TaiwuEditor]快速读书: id: {___readBookId}，SkillTyp: {___studySkillTyp}");
#endif
                if (!Main.enabled || !Main.settings.basicUISettings[1] || ___studySkillTyp < 1 || ___studySkillTyp > 17 || ___readBookId < 1)
                {
                    return true;
                }
                else
                {
                    HelperBase.EasyReadV2(___readBookId, ___studySkillTyp, Main.settings.pagesPerFastRead);
                    __instance.UpdateReadBookWindow();
                    return false;
                }
            }
        }

        /// <summary>
        /// 锁定门派支持度
        /// </summary>
        [HarmonyPatch(typeof(DateFile), "GetGangPartValue")]
        private static class DateFile_GangPartValue_Hook
        {
            private static bool Prefix(int gangId, ref int __result)
            {
                if (!Main.enabled || !Main.settings.basicUISettings[8])
                {
                    return true;
                }
                // 太吾村没有支持度
                __result = (gangId == 16) ? 0 : (Main.settings.customLockValue[0] == 0) ? DateFile.instance.GetMaxWorldValue() : Main.settings.customLockValue[0] * 10;
                return false;
            }
        }

        /// <summary>
        /// 锁定地区恩义
        /// </summary>
        [HarmonyPatch(typeof(DateFile), "GetBasePartValue")]
        private static class DateFile_GetBasePartValue_Hook
        {
            // 返回锁定的值
            private static bool Prefix(ref int __result)
            {
                if (!Main.enabled || !Main.settings.basicUISettings[9])
                {
                    return true;
                }
                __result = (Main.settings.customLockValue[1] == 0) ? DateFile.instance.GetMaxWorldValue() : Main.settings.customLockValue[1] * 10;
                return false;
            }
        }

        /// <summary>
        /// 锁定地区恩义
        /// </summary>
        [HarmonyPatch(typeof(DateFile), "SetGangValue")]
        private static class DateFile_SetGangValue_Hook
        {
            // 阻止地区恩义减少
            private static bool Prefix(ref int value)
            {
                if (Main.enabled && Main.settings.basicUISettings[9] && value < 0)
                {
                    value = 0;
                }
                return true;
            }
        }

        /// <summary>
        /// 防止首次调用<see cref="ActorMenu.instance"/>时人物菜单自动激活造成错误
        /// </summary>
        /// <remarks>用<see cref="HarmonyInstance.Patch"/>加载，只在其他mod没有patch
        /// 这个方法时使用避免重复加载
        /// </remarks>
        internal static class ActorMenu_Awake_Hook
        {
            private static void Postfix(ActorMenu __instance)
            {
#if DEBUG
                Main.logger.Log("TaiwuEditor.ActorMenu_Awake_Patch patched");
#endif
                __instance.actorMenu.SetActive(false);

            }
        }
    }
}
