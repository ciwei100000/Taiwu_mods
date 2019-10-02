using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GameData;
using UnityEngine;

namespace TaiwuEditor
{
    /// <summary>
    /// 角色属性修改工具类
    /// </summary>
    class ActorPropertyHelper : HelperBase
    {
        private const string errorString = "无人物数据";
        /// <summary>属性ID名称</summary>
        private static readonly Dictionary<int, string> fieldNames = new Dictionary<int, string>()
        {
            { -1, "历练"},
            { 11, "年龄"},
            { 12, "健康"},
            { 13, "基础寿命"},
            { 61, "膂力"},
            { 62, "体质"},
            { 63, "灵敏"},
            { 64, "根骨"},
            { 65, "悟性"},
            { 66, "定力"},
            {401, "食材"},
            {402, "木材"},
            {403, "金石"},
            {404, "织物"},
            {405, "草药"},
            {406, "金钱"},
            {407, "威望"},
            {501, "音律"},
            {502, "弈棋"},
            {503, "诗书"},
            {504, "绘画"},
            {505, "术数"},
            {506, "品鉴"},
            {507, "锻造"},
            {508, "制木"},
            {509, "医术"},
            {510, "毒术"},
            {511, "织锦"},
            {512, "巧匠"},
            {513, "道法"},
            {514, "佛学"},
            {515, "厨艺"},
            {516, "杂学"},
            {601, "内功"},
            {602, "身法"},
            {603, "绝技"},
            {604, "拳掌"},
            {605, "指法"},
            {606, "腿法"},
            {607, "暗器"},
            {608, "剑法"},
            {609, "刀法"},
            {610, "长兵"},
            {611, "奇门"},
            {612, "软兵"},
            {613, "御射"},
            {614, "乐器"},
            {706, "无属性内力"}
        };
        private static ActorPropertyHelper instance;
        /// <summary>修改框数值缓存</summary>
        private readonly Dictionary<int, string> fieldValuesCache;
        /*/// <summary>当前编辑的角色在游戏中的数据</summary>
        private Dictionary<int, string> currentActorDate;*/
        /// <summary>修改框标签宽度</summary>
        public static float fieldHelperLblWidth = 90f;
        /// <summary>修改框宽度</summary>
        public static float fieldHelperTextWidth = 120f;
        /// <summary>修改框修改按钮宽度</summary>
        public static float fieldHelperBtnWidth = 80f;

        public static ActorPropertyHelper Instance
        {
            get
            {
                if (instance == null)
                    instance = new ActorPropertyHelper();
                return instance;
            }
        }

        protected ActorPropertyHelper()
        {
            fieldValuesCache = new Dictionary<int, string>(fieldNames.Count);
        }

        /// <summary>
        /// 定义需要每帧执行的方法
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Update(int actorId)
        {
            UpdateAllFields(actorId);
        }

        /// <summary>
        /// 属性修改框框架
        /// </summary>
        /// <param name="resid">对应属性的编号</param>
        public void FieldHelper(int resid)
        {
            GUILayout.BeginHorizontal("Box");
            if (fieldNames.TryGetValue(resid, out string fieldname))
            {
                GUILayout.Label(fieldname, LabelStyle ?? GUI.skin.label, GUILayout.Width(fieldHelperLblWidth));
                if (fieldValuesCache.TryGetValue(resid, out string fieldValue))
                {
                    fieldValuesCache[resid] = GUILayout.TextField(fieldValue, TextFieldStyle ?? GUI.skin.textField, GUILayout.Width(fieldHelperTextWidth));
                    if (GUILayout.Button("修改", ButtonStyle ?? GUI.skin.button, GUILayout.Width(fieldHelperBtnWidth)))
                    {
                        SetFieldValue(resid);
                        UpdateField(resid);
                    }
                }
                else
                {
                    GUILayout.TextField(errorString, TextFieldStyle ?? GUI.skin.textField, GUILayout.Width(fieldHelperTextWidth));
                }
            }
            else
            {
                GUILayout.Label("FieldName不存在");
            }
            GUILayout.EndHorizontal();
        }

        public void FieldHelper(int resid, int max)
        {
            GUILayout.BeginHorizontal("Box");
            if (fieldNames.TryGetValue(resid, out string fieldname))
            {
                GUILayout.Label(fieldname, LabelStyle ?? GUI.skin.label, GUILayout.Width(fieldHelperLblWidth));
                if (fieldValuesCache.TryGetValue(resid, out string fieldValue))
                {
                    GUILayout.BeginHorizontal();
                    fieldValuesCache[resid] = GUILayout.TextField(fieldValue, TextFieldStyle ?? GUI.skin.textField, GUILayout.Width(fieldHelperTextWidth));
                    if (LabelStyle != null)
                    {
                        var fontStyle = LabelStyle.fontStyle;
                        LabelStyle.fontStyle = FontStyle.Bold;
                        GUILayout.Label($"/<color=#FFA500>{max}</color>", LabelStyle ?? GUI.skin.label, GUILayout.ExpandWidth(false));
                        LabelStyle.fontStyle = fontStyle;
                    }
                    else
                    {
                        GUILayout.Label($"/<color=#FFA500>{max}</color>", GUILayout.ExpandWidth(false));
                    }
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("修改", ButtonStyle ?? GUI.skin.button, GUILayout.Width(fieldHelperBtnWidth)))
                    {
                        SetFieldValue(resid, 0, max);
                        UpdateField(resid);
                    }
                }
                else
                {
                    GUILayout.TextField(errorString, TextFieldStyle ?? GUI.skin.textField, GUILayout.Width(fieldHelperTextWidth));
                }
            }
            else
            {
                GUILayout.Label("FieldName不存在");
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 属性修改框框架
        /// </summary>
        /// <param name="resid">对应属性的编号</param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public void FieldHelper(int resid, int min, int max)
        {
            GUILayout.BeginHorizontal("Box");
            if (fieldNames.TryGetValue(resid, out string fieldname))
            {
                GUILayout.Label($"{fieldname}(<color=#FFA500>{min}~{max}</color>)", LabelStyle ?? GUI.skin.label, GUILayout.Width(fieldHelperLblWidth));
                if (fieldValuesCache.TryGetValue(resid, out string fieldValue))
                {
                    fieldValuesCache[resid] = GUILayout.TextField(fieldValue, TextFieldStyle ?? GUI.skin.textField, GUILayout.Width(fieldHelperTextWidth));
                    if (GUILayout.Button("修改", ButtonStyle ?? GUI.skin.button, GUILayout.Width(fieldHelperBtnWidth)))
                    {
                        SetFieldValue(resid, min, max);
                        UpdateField(resid);
                    }
                }
                else
                {
                    GUILayout.TextField(errorString, TextFieldStyle ?? GUI.skin.textField, GUILayout.Width(fieldHelperTextWidth));
                }
            }
            else
            {
                GUILayout.Label("FieldName不存在");
            }
            GUILayout.EndHorizontal();
        }

        /*/// <summary>
        /// 获得当前编辑的角色属性数据
        /// </summary>
        /// <param name="index">对应属性的编号</param>
        /// <param name="text">获取的属性数据，获取失败则为空</param>
        /// <returns>成功获取返回True，否则返回false</returns>
        public bool GetLastActorData(int index, out string text)
        {
            if (currentActorDate != null && currentActorDate.TryGetValue(index, out text))
            {
                return true;
            }
            text = "";
            return false;
        }*/

        /// <summary>
        /// 当前编辑角色改变时将游戏数值同步到所有属性修改框里
        /// </summary>
        /// <param name="dateFileInstance">DateFile实例</param>
        /// <param name="actorMenuInstance">ActorMenu实例</param>
        /// <param name="actorId">需要同步数据的角色Id</param>
        private void UpdateAllFields(int actorId)
        {
            if (DateFile.instance == null)
            {
                if (fieldValuesCache.Count > 0)
                    foreach (var field in fieldNames)
                    {
                        fieldValuesCache.Remove(field.Key);
                    }
                currentActorId = -1;
            }
            else if (actorId != currentActorId)
            {
                currentActorId = actorId;
                foreach (var field in fieldNames)
                {
                    FetchFieldValueHelper(DateFile.instance, field.Key);
                }
            }
        }

        /// <summary>
        /// 重置所有修改框为需要更新的状态，需要配合Update()才能更新数值
        /// </summary>
        public void SetAllFieldsNeedToUpdate() => currentActorId = -1;

        /// <summary>
        /// 将游戏数值同步到属性修改框里
        /// </summary>
        /// <param name="resid">对应属性的编号</param>
        private void UpdateField(int resid)
        {
            var dateFile = DateFile.instance;
            if (dateFile == null)
            {
                fieldValuesCache.Remove(resid);
            }
            else
            {
                FetchFieldValueHelper(dateFile, resid);
            }
        }

        /// <summary>
        /// 获取游戏属性数据到修改框数值缓存
        /// </summary>
        /// <param name="dateFileInstance">不能为null</param>
        /// <param name="actorMenuInstance">不能为null</param>
        /// <param name="actorDate">不能为null</param>
        /// <param name="resid"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FetchFieldValueHelper(DateFile dateFileInstance, int resid)
        {
            switch (resid)
            {
                case -1:
                    fieldValuesCache[resid] = dateFileInstance.gongFaExperienceP.ToString();
                    break;
                case 12: // 获取健康数据
                    fieldValuesCache[resid] = dateFileInstance.Health(currentActorId).ToString();
                    break;
                default:
                    var value = Characters.GetCharProperty(currentActorId, resid);
                    if (value == null)
                    {
                        int id;
                        if (Characters.HasChar(currentActorId))
                        {
                            string charProperty = Characters.GetCharProperty(currentActorId, 997);
                            id = (charProperty != null) ? int.Parse(charProperty) : currentActorId;
                        }
                        else
                        {
                            id = currentActorId;
                        }
                        Dictionary<int, string> tmpDict = null;  // 无意义，只为了省略泛型函数的参数
                        if (!DateFile.instance.presetActorDate.TryGetValue(id, resid, out value, tmpDict))
                        {
                            value = "0";
                        }
                    }
                    fieldValuesCache[resid] = value;
                    break;
            }
        }

        /// <summary>
        /// 将修改框里的数据设定为游戏数据
        /// </summary>
        /// <param name="resid">对应属性的编号</param>
        private void SetFieldValue(int resid)
        {
            var dateFile = DateFile.instance;
            if (dateFile != null && currentActorId != -1 && fieldValuesCache.TryGetValue(resid, out string text))
            {
                if (TryParseInt(text, out int value) && value >= 0)
                {
                    SetValueHelper(dateFile, resid, text, value);
                }
            }
        }

        /// <summary>
        /// 将修改框里的数据设定为游戏数据
        /// </summary>
        /// <param name="resid">对应属性的编号</param>
        /// <param name="min">数值最小值</param>
        /// <param name="max">数值最大值</param>
        private void SetFieldValue(int resid, int min, int max)
        {
            var dateFile = DateFile.instance;
            if (dateFile != null && currentActorId != -1 && fieldValuesCache.TryGetValue(resid, out string text))
            {
                if (TryParseInt(text, out int value) && value >= min && value <= max)
                {
                    SetValueHelper(dateFile, resid, text, value);
                }
            }
        }

        /// <summary>
        /// 为了简化代码，将设定值的部分另写一个方法
        /// </summary>
        /// <param name="resid">对应属性的编号</param>
        /// <param name="text">目标数值的字符串形式</param>
        /// <param name="value">目标数值的整型形式，值必须与text相同</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValueHelper(DateFile instance, int resid, string text, int value)
        {
            switch (resid)
            {
                case -1:
                    instance.gongFaExperienceP = value;
                    break;
                default:
                    Characters.SetCharProperty(currentActorId, resid, text);
                    break;
            }
        }
    }
}
