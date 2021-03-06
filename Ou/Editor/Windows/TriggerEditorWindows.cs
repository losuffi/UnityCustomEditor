﻿using System;
using Ou.Editor.Views;
using Ou.Support.NodeSupport;
using UnityEditor;
using UnityEngine;

namespace Ou.Editor.Windows
{
    public class TriggerEditorWindows:EditorWindow
    {
        public static TriggerEditorWindows Instance;

        public TriggerEditorAdjustView AdjustView;
        public TriggerEditorCanvasView CanvasView;
        public TriggerEditorToolBarView ToolBarView;

        private bool IsPaintDone;
        public static void Init()
        {
            Instance = GetWindow<TriggerEditorWindows>(true);
            Instance.titleContent = new GUIContent("TriggerEditor");
            Instance.maxSize = new Vector2(1400, 860);
            Instance.minSize = new Vector2(1400, 860);
            NodeEditor.Refresh();
        }

        private void OnEnable()
        {
            if(NodeEditor.curNodeGraph==null)
                NodeEditor.Refresh();
            IsPaintDone = false;
        }
        private void OnGUI()
        {
            if (!CheckView())
            {
                return;
            }
            Event e= Event.current;
            {
                if(e.type==EventType.Repaint&&!IsPaintDone)
                    return;
                //Draw SubWindow
                try
                {
                    DrawViews(e);
                }
                catch (ArgumentException exception)
                {
                    return;
                }
                if (!IsPaintDone && e.type == EventType.Layout)
                {
                    IsPaintDone = true;
                }
            }
            Repaint();
        }

        private void DrawViews(Event e)
        {
            if (NodeEditor.curNodeEditorState != null)
            {
                Instance.titleContent = new GUIContent(NodeEditor.curNodeEditorState.Name);
            }
            CanvasView.UpdateView(new Rect(position.width, position.height, position.width, position.height),
                new Rect(0.201f, 0.05f, 0.799f, 0.951f),
                e);
            AdjustView.UpdateView(new Rect(position.width, position.height, position.width, position.height),
                new Rect(0, 0.05f, 0.2f, 0.951f),
                e);
            ToolBarView.UpdateView(position,
                new Rect(0, 0, 1, 0.049f),
                e);
        }
        bool CheckView()
        {
            if (Instance == null)
            {
                Init();
                return false;
            }
            if (CanvasView == null)
            {
                CanvasView = new TriggerEditorCanvasView("Canvas");
            }
            if (AdjustView == null)
            {
                AdjustView=new TriggerEditorAdjustView("Adjust");
            }
            if (ToolBarView == null)
            {
                ToolBarView=new TriggerEditorToolBarView("ToolBar");
            }
            return true;
        }
    }
}
