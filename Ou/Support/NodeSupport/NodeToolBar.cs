﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Ou.Support.NodeSupport
{
    public static class NodeToolBar
    {
        public static void DrawToolBar(Rect rect,GUISkin skin)
        {
            GUILayout.BeginHorizontal();
            OuUIUtility.FormatButton("保存", NodeEditor.SaveCurrentCanvas,skin.GetStyle("ToolBarButton"));
            OuUIUtility.FormatButton("加载", NodeEditor.LoadCanvas, skin.GetStyle("ToolBarButton"));
            OuUIUtility.FormatButton("新建", NodeEditor.NewCanvas, skin.GetStyle("ToolBarButton"));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }
    }
}
