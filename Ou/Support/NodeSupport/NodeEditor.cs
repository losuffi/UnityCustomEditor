﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.VersionControl;
using UnityEngine;
namespace Ou.Support.NodeSupport
{
    public static class NodeEditor
    {
        private static Texture2D texture2D;
        public static NodeGraph curNodeGraph;
        public static NodeEditorState curNodeEditorState;
        public static NodeInputInfo CurNodeInputInfo;
        public static NodeManager NodeManager;
        public static TreeNodeManager TreeNodeManager;
        public static TreeNodeGUIManager TreeNodeGUIManager;
        public static string Path = @"Assets/Ou/Property/Node/Default.asset";
        public static Node memoryNode;
        public static Stack<Node> selectNodes=new Stack<Node>();
        public static string Message = string.Empty;
        public static Rect Messagerect= new Rect(0, 0, 100, 20);
        public static void Clear()
        {
            curNodeGraph.Clear();
        }

        public static void Refresh()
        {
            GetCache();
        }
        #region GUIDraw
        public static void DrawCanvas(Rect viewRect)
        {
            DrawBackground();
            if (curNodeGraph == null || curNodeEditorState == null)
            {
                return;
            }
            curNodeEditorState.CurGraphRect = viewRect;
            if (!curNodeGraph.nodes.Exists(res => res.GetId.Equals("初始化")))
            {
                InitGraphNode();
            }
            for (int nodeCnt = 0; nodeCnt < curNodeGraph.nodes.Count; nodeCnt++)
            {
                if (curNodeGraph.nodes[nodeCnt] == null)
                {
                    curNodeGraph.nodes.RemoveAt(nodeCnt);
                    nodeCnt--;
                }
                else
                {
                    try
                    {
                        curNodeGraph.nodes[nodeCnt].Draw();
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        curNodeGraph.Remove(curNodeGraph.nodes[nodeCnt]);
                        nodeCnt--;
                    }
                }
                //try
                //{
                //    curNodeGraph.nodes[nodeCnt].Draw();
                //}
                //catch (NullReferenceException e)
                //{
                //    curNodeEditorState = null;
                //    curNodeGraph = null;
                //    return;
                //}
            }

            if (CurNodeInputInfo == null || CurNodeInputInfo.EdState == null) 
            {
                CurNodeInputInfo = new NodeInputInfo("test", curNodeEditorState);
            }
            DrawLink();
            DrawMessage();
            NodeInputSystem.DynamicInvoke(CurNodeInputInfo);
        }

        public static void DrawMessage()
        {
            Vector2 pos = CurNodeInputInfo.InputPos;
            GUI.Label(Messagerect, Message);
        }

        public static void DrawBackground()
        {
            if (texture2D == null)
            {
                texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Ou/OuSource/backgroundC.png");
                return;
            }
            float offsetX = curNodeEditorState.PanOffset.x % texture2D.width;
            float offsetY = curNodeEditorState.PanOffset.y % texture2D.height;
            Vector2 offset = new Vector2(-offsetX / texture2D.width, offsetY / texture2D.height);
            Vector2 Scale =
                new Vector2((curNodeEditorState.CurGraphRect.width / texture2D.width),
                    curNodeEditorState.CurGraphRect.height / texture2D.height);
            Rect uiRect = new Rect(
                offset,Scale/curNodeEditorState.GraphZoom
                );
            Rect rect = curNodeEditorState.CurGraphRect;
            rect.position=Vector2.zero;
            GUI.DrawTextureWithTexCoords(rect, texture2D, uiRect);
        }

        public static void RectConverting(ref Rect rect)
        {
            rect = new Rect(rect.position * curNodeEditorState.GraphZoom, rect.size * curNodeEditorState.GraphZoom);
            rect.position += curNodeEditorState.PanOffset + curNodeEditorState.PanAdjust;
        }

        public static void DrawLink()
        {
            if (curNodeEditorState.IsLinkSetting)
            {
                Vector3 startPos = new Vector3(curNodeEditorState.SelectedKnob.rect.center.x,
                    curNodeEditorState.SelectedKnob.rect.center.y, 0);
                Vector3 endPos = new Vector3(CurNodeInputInfo.InputPos.x, CurNodeInputInfo.InputPos.y, 0);
                OuUIUtility.DrawLineA(startPos, endPos);
            }
        }

        public static GenericMenu GetGenericMenu()
        {
            GenericMenu menu=new GenericMenu();
            if (memoryNode!=null)
            {
                menu.AddItem(new GUIContent("粘贴节点"), false, SelectedNodeMenuCallback, "Paste");
            }
            foreach (var node in NodeTypes.nodes)
            {
                if (NodeAdjust.selectedEditorTypeName.Equals(string.Empty) ||
                    NodeAdjust.selectedNodeTypeName.Equals(string.Empty))
                {
                    break;
                }
                if (node.Value.type.IsSubclassOf(NodeAdjust.nodeTypeDatas[NodeAdjust.selectedNodeTypeName].type))
                {
                    menu.AddItem(new GUIContent(node.Value.Name), false, CallBack, node.Key); //需要修改 装入InputControls中。
                }
            }
            return menu;
        }

        static void CallBack(object obj)
        {
            Node node = obj as Node;
            if (curNodeGraph != null)
            {
                Vector2 pos = CurNodeInputInfo.InputPos - curNodeEditorState.CurGraphRect.position;
                curNodeGraph.AddNode(node, pos);
            }
        }

        public static void DrawSelectedNodeMenu()
        {
            GenericMenu menu=new GenericMenu();
            if (curNodeEditorState.SelectedNode.GetType() != typeof(TreeInitNode))
            {
                menu.AddItem(new GUIContent("复制节点"), false, SelectedNodeMenuCallback, "Copy");
            }
            menu.AddItem(new GUIContent("删除节点"),false,SelectedNodeMenuCallback,"Remov");
            menu.AddItem(new GUIContent("剔除入口线"), false, SelectedNodeMenuCallback, "RemovInputLine");
            menu.AddItem(new GUIContent("剔除出口线"), false, SelectedNodeMenuCallback, "RemovOutputLine");
            menu.ShowAsContext();
        }

        static void SelectedNodeMenuCallback(object obj)
        {
            if (obj.ToString().Equals("Remov"))
            {
                var node = curNodeEditorState.SelectedNode;
                curNodeGraph.Remove(node);
            }else if (obj.ToString().Equals("RemovInputLine"))
            {
                var node = curNodeEditorState.SelectedNode;
                node.RemoveLink(typeof(NodeInput));
            }
            else if(obj.ToString().Equals("RemovOutputLine"))
            {
                var node = curNodeEditorState.SelectedNode;
                node.RemoveLink(typeof(NodeOutput));
            }
            else if(obj.ToString().Equals("Copy"))
            {
                memoryNode = curNodeEditorState.SelectedNode;
            }else if (obj.ToString().Equals("Paste"))
            {
                PasteNode();
            }
        }

        static void InitGraphNode()
        {
            TreeInitNode initNode = ScriptableObject.CreateInstance<TreeInitNode>();
            curNodeGraph.InitNode(initNode,Vector2.zero);
        }
        #endregion 
        #region DataSave

        public static void SaveCurrentCanvas()
        {

            EditorUtility.SetDirty(curNodeEditorState);
            AssetDatabase.SaveAssets();
        }
        public static void SaveAs()
        {
            string path = EditorUtility.SaveFilePanel("Save unit", Application.dataPath + "/Ou/Property/Node",
                "Duplicate", "asset");
            path = Regex.Replace(path, @"^.+/Assets", "Assets");
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(curNodeEditorState), path);
            AssetDatabase.Refresh();
            NodeEditorState state = AssetDatabase.LoadAssetAtPath<NodeEditorState>(path);
            state.Name = Regex.Match(path, @"/.+?\.", RegexOptions.RightToLeft).Value.TrimStart('/').TrimEnd('.');
            TriggerEditorUtility.Init();
            NodeGraph graph = null;
            if (state != null)
            {
                graph = state.CurGraph;
            }
            CurNodeInputInfo = null;
            curNodeGraph = graph;
            curNodeEditorState = state;
            CreateCache(path);
        }
        public static void RemDataAsset()
        {
            var newPath = @"Assets/Ou/Property/Node/" + curNodeEditorState.Name + ".asset";
            AssetDatabase.MoveAsset(Path, newPath);
        }

        public static void InitAssetData()
        {
            TriggerEditorUtility.Init();
            NodeEditorState state = AssetDatabase.LoadAssetAtPath<NodeEditorState>(Path);
            NodeGraph graph = null;
            if (state!=null)
            {
                graph = state.CurGraph;
            }
            else
            {
                state = ScriptableObject.CreateInstance<NodeEditorState>();
                graph = ScriptableObject.CreateInstance<NodeGraph>();
                state.CurGraph = graph;
                AssetDatabase.CreateAsset(state, Path);
                AssetDatabase.AddObjectToAsset(graph, state);
            }
            CurNodeInputInfo = null;
            curNodeGraph = graph;
            curNodeEditorState = state;
        }

        static void CreateCache(string canvasPath)
        {
            if (EditorPrefs.HasKey("Path"))
            {
                EditorPrefs.DeleteKey("Path");
            }
            EditorPrefs.SetString("Path", canvasPath);
        }

        static void GetCache()
        {
            if (EditorPrefs.HasKey("Path"))
            {
                LoadCanvas(EditorPrefs.GetString("Path"));
            }
        }
        public static void LoadCanvas()
        {
            string path = EditorUtility.OpenFilePanel("Load Canvas", Application.dataPath + "/Ou/Property/Node", "asset");
            path = Regex.Replace(path, @"^.+/Assets", "Assets");
            TriggerEditorUtility.Init();
            NodeEditorState state = AssetDatabase.LoadAssetAtPath<NodeEditorState>(path);
            NodeGraph graph = null;
            if (state != null)
            {
                graph = state.CurGraph;
            }
            CurNodeInputInfo = null;
            curNodeGraph = graph;
            curNodeEditorState = state;
            CreateCache(path);
        }

        public static void LoadCanvas(string path)
        {
            NodeEditorState state = AssetDatabase.LoadAssetAtPath<NodeEditorState>(path);
            TriggerEditorUtility.Init();
            NodeGraph graph = null;
            if (state != null)
            {
                graph = state.CurGraph;
            }
            CurNodeInputInfo = null;
            curNodeGraph = graph;
            curNodeEditorState = state;
            CreateCache(path);
        }
        public static void NewCanvas()
        {
            Path = @"Assets/Ou/Property/Node/Default.asset";
            if (curNodeEditorState != null)
            {
                if (AssetDatabase.GetAssetPath(curNodeEditorState).Contains("Default"))
                {
                    curNodeEditorState.CurGraph.Clear();
                    return;
                }
            }
            NodeEditorState state = AssetDatabase.LoadAssetAtPath<NodeEditorState>(Path);
            NodeGraph graph = null;
            if (state != null)
            {
                graph = state.CurGraph;
                graph.Clear();
            }
            else
            {
                TriggerEditorUtility.Init();
                state = ScriptableObject.CreateInstance<NodeEditorState>();
                graph = ScriptableObject.CreateInstance<NodeGraph>();
                state.CurGraph = graph;
                AssetDatabase.CreateAsset(state, Path);
                AssetDatabase.AddObjectToAsset(graph, state);
            }
            CurNodeInputInfo = null;
            curNodeGraph = graph;
            curNodeEditorState = state;
            CreateCache(Path);
        }
        #endregion

        #region App

        public static void CreateManager()
        {
            var obj = GameObject.Find("_nodeTreeManager");
            var cam = GameObject.Find("Canvas/drawline");
            if (obj == null)
            {
                obj = new GameObject("_nodeTreeManager");
                TreeNodeManager = obj.AddComponent<TreeNodeManager>();
                TreeNodeGUIManager = obj.AddComponent<TreeNodeGUIManager>();
            }
            if (cam == null)
            {
                cam = new GameObject("drawline", typeof(DrawLine));
                var canvas = GameObject.Find("Canvas");
                if (canvas == null)
                {
                    throw new InvalidCastException("目前，需要一个名字为Canvas的UGUI_Canvas");
                }
                cam.transform.SetParent(canvas.transform);
                cam.transform.localPosition=Vector3.zero;
            }
        }

        public static void RegisterManager()
        {

        }

        public static void RegisterTreeManager()
        {
            if (GameObject.Find("_nodeTreeManager") == null)
            {
                CreateManager();
            }
            TreeNodeManager = TreeNodeManager ?? GameObject.Find("_nodeTreeManager").GetComponent<TreeNodeManager>();
            TreeNodeManager.RegisterGraph(curNodeGraph);
        }
        #endregion

        #region Handle

        public static void PasteNode()
        {
            CallBack(NodeTypes.getDefaultNode(memoryNode.GetId));
        }
        public static void ResetPos()
        {
            var nonuseful =
                curNodeGraph.nodes.Find(res => res.GetType() != typeof(TreeInitNode) && res.isNoneUsefulNode);
            curNodeEditorState.GraphZoom = 1;
            curNodeEditorState.ZoomPos=Vector2.zero;
            curNodeEditorState.DragOffset=Vector2.zero;
            curNodeEditorState.DragStart=Vector2.zero;
            curNodeEditorState.PanAdjust=Vector2.zero;
            Selection.activeObject = nonuseful;
			if(nonuseful!=null)
            	nonuseful.rect = new Rect(Vector2.zero, nonuseful.rect.size);
        }
        #endregion
    }
}
