#define DEBUG
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TaiwuEditor
{
    /// <summary>
    /// 下拉菜单类
    /// </summary>
    internal class DropDownMenu
    {
        #region Static Member
        /// <summary>下拉菜单实例集合</summary>
        private static Dictionary<string, DropDownMenu> instances;
        /// <summary>当前下拉菜单最大长度</summary>
        private static float currentHeightLimit;
        #endregion

        #region Style
        /// <summary>下拉菜单中的选项的样式</summary>
        public static GUIStyle MenuItemStyle { get; set; }
        public static GUIStyle ListStyle { get; set; }
        public static GUIStyle ListBottomStyle { get; set; }
        #endregion

        /// <summary>控件按钮的大小和位置</summary>
        private Rect rect;
        /// <summary>Rect.height的倒数</summary>
        private float reciprocalRectHeight;
        /// <summary>下拉菜单最多可以同时显示出多少选项</summary>
        private int listItemCount;
        /// <summary>下拉菜单滚动轴</summary>
        private Vector2 scrollPosition = new Vector2();
        /// <summary>下拉菜单数据源, key列表项显示的内容，value列表项事件传入的值</summary>
        private readonly List<string> dataSource;
        /// <summary>选项被点击时执行的方法</summary>
        private readonly Action<string> OnSelectItemChanged;

        /// <summary>是否显示菜单</summary>
        public bool ShowList { get; set; }

        /// <summary>
        /// 创建一个下拉菜单实例
        /// </summary>
        /// <param name="dataSource">下拉菜单数据源</param>
        /// <param name="onSelectItemChanged">选项被点击时执行的方法</param>
        private DropDownMenu(List<string> dataSource, Action<string> onSelectItemChanged)
        {
            this.dataSource = dataSource;
            OnSelectItemChanged = onSelectItemChanged;
        }

        /// <summary>
        /// 将下拉列表按钮的大小和位置信息作为下拉列表框大小和位置的参照
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetRect(float x, float y, float width, float height)
        {
            if (rect.height != height)
            {
                rect.height = height;
                reciprocalRectHeight = 1 / height;
                CalculateItemCount();
            }
            rect.x = x;
            rect.y = y;
            rect.width = width;
        }


        /// <summary>
        /// 绘制列表项
        /// </summary>
        private void DrawList()
        {
            if (ShowList)
            {
                Rect rectList;
                if (listItemCount > 0 && listItemCount < dataSource.Count)
                {
                    // 下拉列表当前显示的区域的大小和位置, 为了留出最方下的边框线, 这里高度减1
                    rectList = new Rect(rect.x, rect.y + rect.height, rect.width, rect.height * listItemCount - 1);
                    // 下拉列表如果全部显示的大小和位置
                    var rectListView = new Rect(rectList.x, rectList.y, rectList.width - GUI.skin.verticalScrollbar.fixedWidth, rect.height * dataSource.Count);
                    // 下拉菜单边框的大小和位置
                    var rectListViewGroup = new Rect(rectList.x, rectList.y, rectList.width, rectList.height + 1 - rect.height);
                    // 下拉菜单拉到最底部的留白
                    var rectListViewGroupBottom = new Rect(rectList.x, rectList.y + rectListViewGroup.height, rectList.width, rect.height);
                    GUI.Box(rectListViewGroup, "", ListStyle ?? GUI.skin.box);
                    GUI.Box(rectListViewGroupBottom, "", ListBottomStyle ?? GUI.skin.box);
                    scrollPosition = Vector2.Lerp(scrollPosition, GUI.BeginScrollView(rectList, scrollPosition, rectListView, false, false), 0.5f);
                    {
                        GetFirstAndLastRowVisible(scrollPosition, out var firstRowVisible, out var lastRowVisible);
                        for (int i = firstRowVisible; i <= lastRowVisible; i++)
                        {
                            float top = rectList.y + i * rect.height;
                            DrawItem(new Rect(rectList.x, top, rect.width, rect.height), dataSource[i], MenuItemStyle);
                        }
                    }
                    GUI.EndScrollView();
                }
                else
                {
                    // 下拉列表当前显示的区域的大小和位置, 为了留出最方下的边框线, 这里高度减1
                    rectList = new Rect(rect.x, rect.y + rect.height, rect.width, rect.height * dataSource.Count - 1);
                    // 下拉菜单边框的大小和位置
                    var rectListViewGroup = new Rect(rectList.x, rectList.y, rectList.width, rectList.height + 1 - rect.height);
                    // 下拉菜单拉到最底部的留白
                    var rectListViewGroupBottom = new Rect(rectList.x, rectList.y + rectListViewGroup.height, rectList.width, rect.height);
                    float top = 0;
                    GUI.Box(rectListViewGroup, "", ListStyle ?? GUI.skin.box);
                    GUI.Box(rectListViewGroupBottom, "", ListBottomStyle ?? GUI.skin.box);
                    GUI.BeginGroup(rectList);
                    {
                        foreach (var data in dataSource)
                        {
                            DrawItem(new Rect(0, top, rect.width, rect.height), data, MenuItemStyle);
                            top += rect.height;
                        }
                    }
                    GUI.EndGroup();
                }
                // 鼠标左键或右键点击到下拉菜单以外的地方关闭菜单
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(Main.settings.hotKey) && !rectList.Contains(Event.current.mousePosition))
                {
                    ShowList = false;
                }
            }
        }

        /// <summary>
        /// 绘制内容项,并响应事件
        /// </summary>
        /// <param name="r">列表项大小位置</param>
        /// <param name="displayedContent">列表项显示的内容</param>
        /// <param name="itemStyle">列表项样式</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawItem(Rect r, string displayedContent, GUIStyle itemStyle)
        {
            if (GUI.Button(r, displayedContent, itemStyle ?? GUI.skin.label))
            {
                ShowList = false;
                OnSelectItemChanged?.Invoke(displayedContent);
            }
        }

        /// <summary>
        /// 计算可以容纳的数据行数
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculateItemCount() => listItemCount = (int)(currentHeightLimit * reciprocalRectHeight);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scrollPosition"></param>
        /// <param name="firstRowVisible"></param>
        /// <param name="lastRowVisible"></param>
        private void GetFirstAndLastRowVisible(Vector2 scrollPosition, out int firstRowVisible, out int lastRowVisible)
        {
            if (dataSource.Count == 0 && listItemCount <= 0)
            {
                firstRowVisible = lastRowVisible = -1;
            }
            else
            {
                firstRowVisible = (int)(scrollPosition.y * reciprocalRectHeight);
                lastRowVisible = firstRowVisible + listItemCount;
                firstRowVisible = Math.Max(firstRowVisible, 0);
                lastRowVisible = Math.Min(lastRowVisible, dataSource.Count - 1);
                if (firstRowVisible >= dataSource.Count && firstRowVisible > 0)
                {
                    scrollPosition.y = 0f;
                    GetFirstAndLastRowVisible(scrollPosition, out firstRowVisible, out lastRowVisible);
                }
            }
        }

        /// <summary>
        /// 创建下拉菜单的实例
        /// </summary>
        /// <param name="name">下拉菜单的名称</param>
        /// <param name="dataSource">下拉菜单数据源, key列表项显示的内容，value列表项事件传入的值</param>
        /// <param name="onSelectItemChanged">选项被点击时执行的方法</param>
        public static void CreateMenuInstance(string name, List<string> dataSource, Action<string> onSelectItemChanged)
        {
            if (instances == null)
                instances = new Dictionary<string, DropDownMenu>();
            if (!instances.ContainsKey(name))
            {
                instances.Add(name, new DropDownMenu(dataSource, onSelectItemChanged));
            }
            else
            {
                Main.logger.Log($"[CreateMenuInstance]{name} already exits");
            }
        }

        /// <summary>
        /// 根据名称获取下拉菜单实例
        /// </summary>
        /// <param name="name">下拉菜单</param>
        /// <returns></returns>
        public static DropDownMenu GetMenuInstance(string name) => (instances == null || !instances.TryGetValue(name, out var dropDownMenu)) ? null : dropDownMenu;

        /// <summary>
        /// 绘制所有创建实例的下拉菜单
        /// </summary>
        /// <param name="availableAreaHeight">下拉菜单可以允许达到的最大长度</param>
        public static void Draw(float availableAreaHeight)
        {
            if (instances == null || instances.Count < 0)
                return;

            if (availableAreaHeight < 1)
            {
#if DEBUG
                Main.logger.Log($"[DrawLists]availableAreaHeight({availableAreaHeight}) is too small");
#endif
                return;
            }

            if (currentHeightLimit != availableAreaHeight)
            {
                currentHeightLimit = availableAreaHeight;
                foreach (var dropDownMenu in instances.Values)
                {
                    dropDownMenu.CalculateItemCount();
                    dropDownMenu.DrawList();
                }
            }
            else
            {
                foreach (var dropDownMenu in instances.Values)
                {
                    dropDownMenu.DrawList();
                }
            }
        }

        /// <summary>
        /// 收起所有菜单
        /// </summary>
        /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CollapseAllMenus()
        {
            foreach (var dropDownMenu in instances)
            {
                dropDownMenu.Value.ShowList = false;
            }
        }*/
    }
}
