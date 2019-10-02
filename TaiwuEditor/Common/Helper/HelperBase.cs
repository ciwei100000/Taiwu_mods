using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using GameData;

namespace TaiwuEditor
{
    internal abstract class HelperBase
    {
        protected static readonly Dictionary<int, string> textColors = new Dictionary<int, string>
        {
            { 10000, "<color=#323232FF>" },
            { 10001, "<color=#4B4B4BFF>" },
            { 10002, "<color=#B97D4BFF>" },
            { 10003, "<color=#9B8773FF>" },
            { 10004, "<color=#AF3737FF>" },
            { 10005, "<color=#FFE100FF>" },
            { 10006, "<color=#FF44A7FF>" },
            { 20001, "<color=#E1CDAAFF>" },
            { 20002, "<color=#8E8E8EFF>" }, // 九品灰
            { 20003, "<color=#FBFBFBFF>" }, // 八品白
            { 20004, "<color=#6DB75FFF>" }, // 七品绿
            { 20005, "<color=#8FBAE7FF>" }, // 六品蓝
            { 20006, "<color=#63CED0FF>" }, // 五品青
            { 20007, "<color=#AE5AC8FF>" }, // 四品紫
            { 20008, "<color=#E3C66DFF>" }, // 三品黄
            { 20009, "<color=#F28234FF>" }, // 二品橙
            { 20010, "<color=#E4504DFF>" }, // 一品红
            { 20011, "<color=#EDA723FF>" },
            { -1, "</color>" }
        };
        /// <summary>标签的样式</summary>
        public static GUIStyle LabelStyle { get; set; }
        /// <summary>按钮的样式</summary>
        public static GUIStyle ButtonStyle { get; set; }
        /// <summary>输入框的样式</summary>
        public static GUIStyle TextFieldStyle { get; set; }
        /// <summary>选项的样式</summary>
        public static GUIStyle ToggleStyle { get; set; }
        /// <summary>当前编辑的角色ID</summary>
        protected int currentActorId = -1;

        protected HelperBase() { }

        public abstract void Update(int actorId);

        /// <summary>
        /// 治疗解毒理气
        /// </summary>
        /// <param name="instance">DateFile实例</param>
        /// <param name="actorId">要处理的角色ID</param>
        /// <param name="func">功能选择，0疗伤，1祛毒，2调理内息</param>
        /// <param name="battle">是否处理战斗中的伤口、中毒和内息紊乱</param>
        public static void CureHelper(DateFile instance, int actorId, int func, bool battle = true)
        {
            if (instance == null)
            {
                return;
            }

            switch (func)
            {
                case 0:
                    if (instance.actorInjuryDate != null && instance.actorInjuryDate.TryGetValue(actorId, out Dictionary<int, int> injuries))
                    {
                        var injuryIds = new List<int>(injuries.Keys);
                        for (int i = 0; i < injuryIds.Count; i++)
                        {
                            injuries.Remove(injuryIds[i]);
                        }
                    }
                    if (battle && instance.ActorIsInBattle(actorId) != 0 && instance.battleActorsInjurys != null
                        && instance.battleActorsInjurys.TryGetValue(actorId, out Dictionary<int, int[]> battleInjuries))
                    {
                        var battleInjuriesIds = new List<int>(battleInjuries.Keys);
                        for (int i = 0; i < battleInjuriesIds.Count; i++)
                        {
                            battleInjuries.Remove(battleInjuriesIds[i]);
                        }
                    }
                    break;
                case 1:
                    if (Characters.HasChar(actorId))
                        for (int i = 0; i < 6; i++)
                        {
                            Characters.SetCharProperty(actorId, 51 + i, "0");
                        }

                    if (battle && instance.ActorIsInBattle(actorId) != 0 && instance.battleActorsPoisons != null
                        && instance.battleActorsPoisons.TryGetValue(actorId, out int[] poisons))
                    {
                        for (int i = 0; i < poisons.Length; i++)
                        {
                            poisons[i] = 0;
                        }
                    }
                    break;
                case 2:
                    if (Characters.HasChar(actorId))
                    {
                        Characters.SetCharProperty(actorId, 39, "0");
                    }
                    if (battle && instance.ActorIsInBattle(actorId) != 0 && instance.battleActorsMianQi != null)
                    {
                        instance.battleActorsMianQi[actorId] = 0;
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取角色生活信息
        /// </summary>
        /// <param name="instance">DateFile实例</param>
        /// <param name="resid">生活信息Id</param>
        /// <param name="actorid">角色ID</param>
        /// <returns></returns>
        public static int LifeDateHelper(DateFile instance, int actorid, int resid)
        {
            if (instance == null || instance.actorLife == null || !instance.actorLife.ContainsKey(actorid))
            {
                return -1;
            }
            actorid = (actorid == 0) ? instance.mianActorId : actorid;
            int result = instance.GetLifeDate(actorid, resid, 0);
            return (result == -1) ? 0 : result;
        }

        /// <summary>
        /// 设置人物相枢入邪值
        /// </summary>
        /// <param name="instance">DateFile实例</param>
        /// <param name="actorId">角色ID</param>
        /// <param name="value">目标入邪值</param>
        // 根据DateFile.SetActorXXChange重写
        public static void SetActorXXValue(DateFile instance, int actorId, int value)
        {
            if (instance == null || instance.actorLife == null || !instance.actorLife.ContainsKey(actorId))
            {
                return;
            }
            value = Mathf.Max(value, 0);
            // 清空角色相支持度缓存
            instance.AICache_ActorPartValue.Remove(actorId);
            instance.AICache_PartValue.Remove(int.Parse(instance.GetActorDate(actorId, 19, false)));
            int originalXXValue = DateFile.instance.GetLifeDate(actorId, 501, 0);
            // 设置入邪值
            if (instance.HaveLifeDate(actorId, 501))
            {
                instance.actorLife[actorId][501][0] = Mathf.Max(value, 0);
            }
            else
            {
                instance.actorLife[actorId].Add(501, new List<int>
                {
                    Mathf.Max(value, 0)
                });
            }
            // 入魔
            if (value >= 200)
            {
                // 是否是敌方阵营（如相枢）
                if (int.Parse(instance.GetActorDate(actorId, 6, false)) == 0)
                {
                    // 化魔之后加入敌方
                    Characters.SetCharProperty(actorId, 6, "1");
                    // 获取该角色的坐标
                    List<int> actorAtPlace = instance.GetActorAtPlace(actorId);
                    if (actorAtPlace != null)
                    {
                        // 添加角色经历
                        PeopleLifeAI.instance.AISetMassage(84, actorId, actorAtPlace[0], actorAtPlace[1], null, -1, true);
                    }
                }
                // 相枢化魔特性
                instance.ChangeActorFeature(actorId, 9997, 9999);
                instance.ChangeActorFeature(actorId, 9998, 9999);
                if (originalXXValue < 200)
                {
                    if (actorId == DateFile.instance.mianActorId)
                    {
                        GEvent.OnEvent(eEvents.MainActorXXConvertFully);
                        TipsWindow.instance.ShowNotifyEvent(eNotifyEvents.FallPossessed);
                    }
                    else if (DateFile.instance.IsMainActorMate(actorId))
                    {
                        TipsWindow.instance.ShowNotifyEvent(eNotifyEvents.MateFallPossessed, actorId);
                    }
                    else if (DateFile.instance.IsTaiWuVillager(actorId))
                    {
                        TipsWindow.instance.ShowNotifyEvent(eNotifyEvents.VillagerFallPossessed, actorId);
                    }
                }
            }
            else
            {
                // 转换为非敌方阵营
                Characters.RemoveCharProperty(actorId, 6);
                // 入邪
                if (value >= 100)
                {
                    // 相枢入邪特性
                    instance.ChangeActorFeature(actorId, 9997, 9998);
                    instance.ChangeActorFeature(actorId, 9999, 9998);
                    if (actorId == DateFile.instance.mianActorId)
                    {
                        GEvent.OnEvent(eEvents.MainActorXXConvertPartial);
                    }
                    if (originalXXValue < 100)
                    {
                        if (actorId == DateFile.instance.mianActorId)
                        {
                            TipsWindow.instance.ShowNotifyEvent(eNotifyEvents.FallEvil);
                        }
                        else if (DateFile.instance.IsMainActorMate(actorId))
                        {
                            TipsWindow.instance.ShowNotifyEvent(eNotifyEvents.MateFallEvil, actorId);
                        }
                        else if (DateFile.instance.IsTaiWuVillager(actorId))
                        {
                            TipsWindow.instance.ShowNotifyEvent(eNotifyEvents.VillagerFallEvil, actorId);
                        }
                    }
                }
                else
                {
                    // 正常
                    instance.ChangeActorFeature(actorId, 9998, 9997);
                    instance.ChangeActorFeature(actorId, 9999, 9997);
                    if (actorId == DateFile.instance.mianActorId)
                    {
                        GEvent.OnEvent(eEvents.MianActorXXConvertEnd);
                    }
                }
            }
        }

        /// <summary>
        /// 快速读书
        /// </summary>
        /// <param name="readbookid">书籍ID</param>
        /// <param name="studyskilltyp">技能类型</param>
        /// <param name="counter">每次快速读书篇数（只针对需要平衡正逆练的功法书，技艺书还是一次全读完）</param>
        // 快速读书 Highly Inspired by ReadBook.GetReadBooty()
        public static void EasyReadV2(int readbookid, int studyskilltyp, int counter)
        {
            int mainActorId = DateFile.instance.MianActorID();
            // 功法id
            int gongFaId = int.Parse(DateFile.instance.GetItemDate(readbookid, 32, true));
            // 可能代表“每页是否是残卷”
            int[] bookPage = DateFile.instance.GetBookPage(readbookid);
            for (int j = 0; j < 10; j++) // 每书10篇
            {
                //int experienceGainRatio = 100; // 读书获得历练加成比例
                // 获得的历练
                // int experienceGainPerPage = int.Parse(DateFile.instance.GetItemDate(HomeSystem.instance.readBookId, 34, true)) * experienceGainRatio / 100; 
                int experienceGainPerPage = int.Parse(DateFile.instance.GetItemDate(readbookid, 34, true)) * 100 / 100;
                // 读的是功法
                if (studyskilltyp == 17)
                {
                    // 如果是从来没读过的功法
                    if (!DateFile.instance.gongFaBookPages.ContainsKey(gongFaId))
                    {
                        DateFile.instance.gongFaBookPages.Add(gongFaId, new int[10]);
                    }
                    int studyDegree = DateFile.instance.gongFaBookPages[gongFaId][j];
                    // 若该篇章未曾读过
                    if (studyDegree != 1 && studyDegree > -100)
                    {
                        // 每篇读完应获得的遗惠
                        int scoreGain = int.Parse(DateFile.instance.gongFaDate[gongFaId][2]);
                        // 是否为手抄
                        int isShouChao = int.Parse(DateFile.instance.GetItemDate(readbookid, 35, true));
                        DateFile.instance.ChangeActorGongFa(mainActorId, gongFaId, 0, 0, isShouChao, true);
                        if (isShouChao != 0)
                        {
                            //增加内息紊乱
                            DateFile.instance.ChangeMianQi(mainActorId, 50 * scoreGain, 5);
                        }
                        // 该篇已经阅读完毕
                        DateFile.instance.gongFaBookPages[gongFaId][j] = 1;
                        counter--;
                        // 增加遗惠
                        DateFile.instance.AddActorScore(303, scoreGain * 100);
                        if (DateFile.instance.GetGongFaLevel(mainActorId, gongFaId, 0) >= 100 && DateFile.instance.GetGongFaFLevel(mainActorId, gongFaId) >= 10)
                        {
                            // 增加遗惠
                            DateFile.instance.AddActorScore(304, scoreGain * 100);
                        }
                        if (bookPage[j] == 0)
                        {
                            // 增加遗惠
                            DateFile.instance.AddActorScore(305, scoreGain * 100);
                        }
                    }
                    else
                    {
                        // 已经读过的篇章加1/10基础历练
                        experienceGainPerPage = experienceGainPerPage * 10 / 100;
                    }
                    DateFile.instance.gongFaExperienceP += experienceGainPerPage;
                    //达到快速读书单次上限，停止读书                    
                    if (counter < 1)
                    {
                        break;
                    }
                }
                else   //读的是技艺书
                {
                    // 如果是从来没读过的技艺
                    if (!DateFile.instance.skillBookPages.ContainsKey(gongFaId))
                    {
                        DateFile.instance.skillBookPages.Add(gongFaId, new int[10]);
                    }
                    int studyDegree = DateFile.instance.skillBookPages[gongFaId][j];
                    // 若该篇章未曾读过
                    if (studyDegree != 1 && studyDegree > -100)
                    {
                        // 每篇读完应获得的遗惠
                        int scoreGain = int.Parse(DateFile.instance.skillDate[gongFaId][2]);
                        // 若还未习得该项技艺
                        if (!DateFile.instance.actorSkills.ContainsKey(gongFaId))
                        {
                            // 将该技艺添加到太吾身上
                            DateFile.instance.ChangeMianSkill(gongFaId, 0, 0, true);
                        }
                        DateFile.instance.skillBookPages[gongFaId][j] = 1;
                        // 增加遗惠
                        DateFile.instance.AddActorScore(203, scoreGain * 100);
                        if (DateFile.instance.GetSkillLevel(gongFaId) >= 100 && DateFile.instance.GetSkillFLevel(gongFaId) >= 10)
                        {
                            // 增加遗惠
                            DateFile.instance.AddActorScore(204, scoreGain * 100);
                        }
                        if (bookPage[j] == 0)
                        {
                            // 增加遗惠
                            DateFile.instance.AddActorScore(205, scoreGain * 100);
                        }
                    }
                    else
                    {
                        // 已经读过的篇章加1/10基础历练
                        experienceGainPerPage = experienceGainPerPage * 10 / 100;
                    }
                    // 增加历练
                    DateFile.instance.gongFaExperienceP += experienceGainPerPage;
                }
            }
        }

        /// <summary>
        /// 终点事件设置
        /// </summary>
        /// <param name="endeventid">奇遇终点事件ID</param>
        /// <param name="storysystempartid">当前奇遇发生的地点区域ID</param>
        /// <param name="storysystemplaceid">当前奇遇发生的地点位置ID</param>
        /// <param name="storysystemstoryId">当前奇遇ID</param>
        /// <returns>是否成功</returns>
        /// Inspired by <see cref="StorySystem.EventSetup"/>
        public static bool EventSetup(StoryBaseManager storyBaseManager, int endeventid, int storysystempartid,
                                      int storysystemplaceid, int storysystemstoryId)
        {
            if (endeventid != 0)
            {
                int _storyValue = DateFile.instance.worldMapState[storysystempartid][storysystemplaceid][2];
                switch (endeventid)
                {
                    // 十四奇书奇遇终点
                    case 2307:
                        int _getBookActorId = 0;
                        int _getBookActorScore = 0;
                        // 获取同道(包含太吾)
                        List<int> _actorFamily = DateFile.instance.GetFamily(true);
                        // 奇遇地块上的人物ID
                        List<int> _placeActors = DateFile.instance.HaveActor(storysystempartid, storysystemplaceid, true, false, false, false);
                        for (int i = 0; i < _placeActors.Count; i++)
                        {
                            int _actorId = _placeActors[i];
                            if (!_actorFamily.Contains(_actorId) && DateFile.instance.HaveLifeDate(_actorId, 710))
                            {
                                // 选出战力最强的拿到书与主角在终点决战
                                int _score = int.Parse(DateFile.instance.GetActorDate(_actorId, 993, false));
                                if (_score > _getBookActorScore)
                                {
                                    _getBookActorScore = _score;
                                    _getBookActorId = _actorId;
                                }
                            }
                        }
                        // 奇书ID
                        int _bookId = new List<int>(DateFile.instance.legendBookId.Keys)[storysystemstoryId - 11001];
                        // 如果奇遇区块有人则要抢, 否则直接得到奇书
                        if (_getBookActorId > 0)
                        {
                            DateFile.instance.SetEvent(new int[5]
                            {
                                0,
                                _getBookActorId,
                                endeventid + DateFile.instance.GetActorGoodness(_getBookActorId),
                                _getBookActorId,
                                _bookId
                            }, true, true);
                        }
                        else
                        {
                            DateFile.instance.SetEvent(new int[4]
                            {
                                0,
                                -1,
                                2312,
                                _bookId
                            }, true, true);
                        }
                        break;
                    // 天材地宝奇遇
                    case 2325:
                        DateFile.instance.SetEvent(new int[5]
                        {
                            0,
                            -1,
                            endeventid,
                            // 最高品级（二品）材料的ID
                            storysystemstoryId,
                            // 得到高品级的概率，计算公式在MessageWindow.EndEvent2325_1()
                            Mathf.Max(100 + DateFile.instance.storyBuffs[-3] - DateFile.instance.storyDebuffs[-3], 0)
                        }, true, true);
                        break;
                    // 私奔奇遇终点
                    // 中庸
                    case 9606:
                    // 仁善
                    case 9607:
                    // 刚正
                    case 9608:
                        DateFile.instance.SetEvent(new int[4]
                        {
                            0,
                            _storyValue,
                            endeventid,
                            _storyValue
                        }, true, true);
                        break;
                    // 叛逆
                    case 9609:
                    // 唯我
                    case 9610:
                        DateFile.instance.SetEvent(new int[4]
                        {
                            0,
                            7010 + int.Parse(DateFile.instance.GetActorDate(_storyValue, 19, false)) * 100,
                            endeventid,
                            _storyValue
                        }, true, true);
                        break;
                    // 其他情况
                    default:
                        if (!storyBaseManager.StoryBases.ContainsKey(storysystemstoryId))
                        {
                            DateFile.instance.SetEvent(new int[3]
                            {
                                0,
                                -1,
                                endeventid
                            }, true, true);
                        }
                        else
                        {
                            GEvent.OnEvent(eEvents.StoryEndEvent, StorySystem.instance.storySystemStoryId, endeventid);
                        }
                        break;
                }
                return true;
            }
            else
            {
                // 如果奇遇终点不存在，直接移除奇遇
                DateFile.instance.SetStory(true, storysystempartid, storysystemplaceid, 0, 0);
                StorySystem.instance.StoryEnd();
                return false;
            }
        }

        /// <summary>
        /// 字符串转为整型
        /// </summary>
        /// <param name="text">字符串</param>
        /// <param name="integer">输出的整型</param>
        /// <returns>是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseInt(string text, out int integer) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer);
    }

    internal static class ExtendedMethods
    {
        /// <summary>
        /// 尝试获取两个嵌套字典中位于内部的字典的值
        /// </summary>
        /// <typeparam name="T">嵌套字典的类型</typeparam>
        /// <typeparam name="Tkey1">外部字典键的类型</typeparam>
        /// <typeparam name="Tkey2">内部字典键的类型</typeparam>
        /// <typeparam name="TValue1">内部字典的类型</typeparam>
        /// <typeparam name="TValue2">内部字典值的类型</typeparam>
        /// <param name="iDictionary">外部字典</param>
        /// <param name="key1">外部字典的键值</param>
        /// <param name="key2">对应外部字典键值的内部字典的键值</param>
        /// <param name="value">内部字典的键值对应的值，失败则取默认值</param>
        /// <param name="iDictionaryValue">只为省略中括号泛型类型推断方便，无实际意义，只要类型与内部字典类型相同即可</param>
        /// <returns>True：成功；False:失败，value取类型默认值</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T, Tkey1, Tkey2, TValue1, TValue2>(this T iDictionary, Tkey1 key1, Tkey2 key2, out TValue2 value, TValue1 iDictionaryValue = default)
            where T : IDictionary<Tkey1, TValue1>
            where TValue1 : IDictionary<Tkey2, TValue2>
        {
            value = default;
            return iDictionary.TryGetValue(key1, out var value1) && value1.TryGetValue(key2, out value);
        }

        /// <summary>
        /// 尝试获取两个嵌套字典中位于内部的字典的值，并转换为整型
        /// </summary>
        /// <typeparam name="T">嵌套字典的类型</typeparam>
        /// <typeparam name="Tkey1">外部字典键的类型</typeparam>
        /// <typeparam name="Tkey2">内部字典键的类型</typeparam>
        /// <typeparam name="TValue1">内部字典的类型</typeparam>
        /// <param name="iDictionary">外部字典</param>
        /// <param name="key1">外部字典的键值</param>
        /// <param name="key2">对应外部字典键值的内部字典的键值</param>
        /// <param name="value">内部字典的键值转换为整型，失败则为-1</param>
        /// <param name="iDictionaryValue">只为省略中括号泛型类型推断方便，无实际意义，只要类型与内部字典类型相同即可</param>
        /// <returns>True：成功；False:失败</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T, Tkey1, Tkey2, TValue1>(this T iDictionary, Tkey1 key1, Tkey2 key2, out int value, TValue1 iDictionaryValue = default)
            where T : IDictionary<Tkey1, TValue1>
            where TValue1 : IDictionary<Tkey2, string>
        {
            value = -1;
            return iDictionary.TryGetValue(key1, out var value1) && value1.TryGetValue(key2, out string text) && HelperBase.TryParseInt(text, out value);
        }

        /// <summary>
        /// 尝试根据键值获取值,并将值转换为整型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Tkey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="iDictionary">继承iDictionary的类</param>
        /// <param name="key">键值</param>
        /// <param name="value">值，若失败则为-1</param>
        /// <returns>True获取成功，False获取失败</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T, Tkey>(this T iDictionary, Tkey key, out int value) where T : IDictionary<Tkey, string>
        {
            value = -1;
            return iDictionary.TryGetValue(key, out string text) && HelperBase.TryParseInt(text, out value);
        }
    }

    // 借用Sth4nothing的反射类
    internal static class ReflectionMethod
    {
        private const BindingFlags Flags = BindingFlags.Instance
                                           | BindingFlags.Static
                                           | BindingFlags.NonPublic
                                           | BindingFlags.Public;

        /// <summary>
        /// 调用实例中的方法并返回结果
        /// </summary>
        /// <typeparam name="TSource">实例的类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="instance">类实例</param>
        /// <param name="method">类实例方法的名称</param>
        /// <param name="args">类实例方法的参数</param>
        /// <returns>方法返回值</returns>
        public static TResult Invoke<TSource, TResult>(this TSource instance, string method, params object[] args) => (TResult)typeof(TSource).GetMethod(method, Flags)?.Invoke(instance, args);

        /// <summary>
        /// 调用实例中的方法
        /// </summary>
        /// <typeparam name="T">实例的类型</typeparam>
        /// <param name="instance">类实例</param>
        /// <param name="method">类实例方法的名称</param>
        /// <param name="args">类实例方法的参数</param>
        public static void Invoke<T>(this T instance, string method, params object[] args) => typeof(T).GetMethod(method, Flags)?.Invoke(instance, args);

        /// <summary>
        /// 调用实例中的方法并返回结果
        /// </summary>
        /// <typeparam name="T">实例的类型</typeparam>
        /// <param name="instance">类实例</param>
        /// <param name="method">类实例方法的名称</param>
        /// <param name="argTypes">类实例方法的参数的类型</param>
        /// <param name="args">类实例方法的参数</param>
        /// <returns>方法返回值</returns>
        public static object Invoke<T>(this T instance, string method, System.Type[] argTypes, params object[] args)
        {
            argTypes = argTypes ?? new System.Type[0];
            var methods = typeof(T).GetMethods(Flags).Where(m =>
            {
                if (m.Name != method)
                {
                    return false;
                }

                return m.GetParameters()
                    .Select(p => p.ParameterType)
                    .SequenceEqual(argTypes);
            });

            if (methods.Count() != 1)
            {
                throw new AmbiguousMatchException("cannot find method to invoke");
            }

            return methods.First()?.Invoke(instance, args);
        }

        /// <summary>
        /// 获取类实例中域(Field)的值
        /// </summary>
        /// <typeparam name="TSource">实例的类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="instance">类实例</param>
        /// <param name="field"></param>
        /// <returns>域的值</returns>
        public static TResult GetValue<TSource, TResult>(this TSource instance, string field) => (TResult)typeof(TSource).GetField(field, Flags)?.GetValue(instance);

        /// <summary>
        /// 获取类实例中域(Field)的值
        /// </summary>
        /// <typeparam name="T">实例的类型</typeparam>
        /// <param name="instance">类实例</param>
        /// <param name="field">域名称</param>
        /// <returns>域的值</returns>
        public static object GetValue<T>(this T instance, string field) => typeof(T).GetField(field, Flags)?.GetValue(instance);
    }
}
