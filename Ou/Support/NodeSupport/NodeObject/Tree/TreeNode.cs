﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ou.Support.UnitSupport;
using UnityEditor;
using UnityEngine;

namespace Ou.Support.NodeSupport
{
    [Node(false,"树型","EditorType")]
    public class TreeNode:Node
    {
        protected internal virtual  void FeedBack()
        {
            foreach (NodeInput input in inputKnobs)
            {
                if(input.connection==null)
                    continue;
                var parent = input.connection.Body as TreeNode;
                parent.feedback = feedback;
                parent.FeedBack();
            }
        }

        protected internal void CallFeedBack()
        {
            if (!outputKnobs.Any())
            {
                feedback = state;
                FeedBack();
            }
            else 
            {
                foreach (NodeOutput output in outputKnobs)
                {
                    if(output.connections.Any())
                        return;
                }
                feedback = state;
                FeedBack();
            }
        }

        protected internal void StateReset(string outputName)
        {
            var outputs = outputKnobs.FindAll(T => T.Name.Equals(outputName));
            ClearKnobMessage();
            feedback = TreeNodeResult.Idle;
            if (!outputs.Any())
            {
                return;
            }
            foreach (NodeOutput output in outputs)
            {
                if(!output.connections.Any())
                    continue;
                foreach (NodeInput input in output.connections)
                {
                    (input.Body as TreeNode).state = TreeNodeResult.Idle;
                    (input.Body as TreeNode).StateReset("Nextout");
                }
            }
        }

        protected internal virtual TreeNodeResult OnUpdate()
        {
            return TreeNodeResult.Idle;
        }

        protected enum GotoType
        {
            Single,
            All,
        }

        protected void Goto(GotoType type = GotoType.All, string des ="")
        {
            if (type == GotoType.All)
            {
                if (outputKnobs.Any())
                {
                    foreach (var output in outputKnobs.FindAll(T => T.outputType.Equals("工作状态")))
                    {
                        output.SetValue<TreeNodeResult>(TreeNodeResult.Start);
                    }

                }
            }
            else if(type== GotoType.Single)
            {
                if (outputKnobs.Any())
                {
                    var output = outputKnobs.Find(T => T.Name.Equals(des));
                    output.SetValue<TreeNodeResult>(TreeNodeResult.Start);
                }
            }
        }

        protected void Goto(int index)
        {
            var output = outputKnobs[index];
            output.SetValue<TreeNodeResult>(TreeNodeResult.Running);
        }
        protected internal virtual void OnStart()
        {
            //for (int i = 0; i < variables.Count; i++)
            //{
            //    variables[i] = curGraph.ReadGlobalVariable(variables[i]);
            //}
        }
        protected internal override void Evaluator()
        {
            CallFeedBack();
            Goto();
        }

        protected internal void SetEnd()
        {
            state = TreeNodeResult.End;
        }
        protected internal virtual void CheckStart()
        {
            if(state==TreeNodeResult.Running)
                return;
            if (inputKnobs.Any())
            {
                foreach (var input in inputKnobs.FindAll(T => T.InputType.Equals("工作状态")))
                {
                    state = input.GetValue<TreeNodeResult>();
                    if (state == TreeNodeResult.Start)
                    {
                        OnStart();
                        state = TreeNodeResult.Running;
                        break;
                    }
                }
            }
        }

        protected internal virtual bool IsCompelete
        {
            get
            {
                stateWithDraw = state;
                if (state == TreeNodeResult.Done || state == TreeNodeResult.Failed||state== TreeNodeResult.Break)
                {

                    return true;
                }
                return false;
            }
        }
        protected internal virtual bool FeedIsCompelete
        {
            get
            {
                if (feedback == TreeNodeResult.Done || feedback == TreeNodeResult.Failed || feedback == TreeNodeResult.Break)
                {

                    return true;
                }
                return false;
            }
        }


        protected internal override void NodeGUI()
        {
        }

        protected internal void ClearKnobMessage()
        {
            foreach (NodeOutput knob in outputKnobs)
            {
                knob.SetValue<TreeNodeResult>(TreeNodeResult.Idle);
            }
        }

        protected internal GlobalVariable CreateVariable()
        {
            var gv=new GlobalVariable();
            variables.Add(gv);
            return gv;
        }

        protected internal GlobalVariable CreateVariable(Type type, object obj, string id, string name)
        {
            var gv = new GlobalVariable(type, obj, id, name);
            variables.Add(gv);
            return gv;
        }

        protected internal PopupStructer SetVariableTypeRange(params string[] str)
        {
            return new PopupStructer(curGraph,str);
        }

        protected internal void DrawFillsLayout(GlobalVariable gv)
        {
            if (gv.structerTypeRange == null)
            {
                throw new NullReferenceException("gv 设置错误");
            }

            gv.isSetVariable = GUILayout.Toggle(gv.isSetVariable, "全局变量？");
            if (gv.isSetVariable)
            {
                OuUIUtility.FormatSelectedVariable_TypeFit(ref gv, ref gv.FillIndex, gv.structerTypeRange);
            }
            else
            {
                OuUIUtility.FormatFillVariable_SelectedType(ref gv, ref gv.FillIndex);
            }
        }

        protected internal void DrawLocalLayout(GlobalVariable gv)
        {
            if (gv.structerTypeRange == null)
                return;
            OuUIUtility.FormatFillVariable_SelectedType(ref gv, ref gv.FillIndex);
        }

        protected internal void DrawGlobalLayout(GlobalVariable gv)
        {
            if (gv.structerTypeRange == null)
                return;
            OuUIUtility.FormatSelectedVariable_TypeFit(ref gv, ref gv.FillIndex, gv.structerTypeRange);
        }
        protected internal void DrawUnitLayout(GlobalVariable gv)
        {
            if (gv.structerTypeRange == null)
                return;
            OuUIUtility.FormatSelectedVariableUnit_TypeFit(ref gv, gv.structerTypeRange);
        }


        public override Node Create(Vector2 pos)
        {
            throw new NotImplementedException();
        }
        [SerializeField]
        protected internal TreeNodeResult state = TreeNodeResult.Idle;
        [SerializeField]
        protected internal TreeNodeResult feedback = TreeNodeResult.Idle;
        [SerializeField]
        protected internal List<GlobalVariable> variables=new List<GlobalVariable>();
        private const string nodeId = "树型";
        protected internal TreeNodeResult stateWithDraw = TreeNodeResult.Done;
        public override string GetId { get { return nodeId; } }
        protected internal override void Draw()
        {
            base.Draw();
            DrawState();
        }

        void DrawState()
        {
            if(!EditorApplication.isPlaying)
                return;
            Color normalColor = Handles.color;
            if (stateWithDraw == TreeNodeResult.Failed)
            {
                Handles.color = Color.yellow;
            }
            else if(stateWithDraw == TreeNodeResult.Running)
            {
                Handles.color=Color.green;
            }
            else if(stateWithDraw == TreeNodeResult.Break|| stateWithDraw == TreeNodeResult.Done)
            {
                Handles.color= Color.black;
            }
            else
            {
                Handles.color = Color.gray;
            }
            Handles.DrawAAPolyLine(6,new Vector3(nodeRect.x, nodeRect.y, 0),
                new Vector3(nodeRect.x + nodeRect.width, nodeRect.y, 0),
                new Vector3(nodeRect.x + nodeRect.width, nodeRect.y + nodeRect.height, 0),
                new Vector3(nodeRect.x, nodeRect.y + nodeRect.height, 0),
                new Vector3(nodeRect.x, nodeRect.y, 0));
            Handles.color = normalColor;
        }
    }
    public enum TreeNodeResult
    {
        Idle,
        Start,
        Done,
        Failed,
        Running,
        Break,
        End,
    }

    public enum SettingType
    {
        填充,
        全局变量,
    }

    public enum DataModel
    {
        Editor,
        Runtime,
    }

    [Serializable]
    public class GlobalVariable:ISerializationCallbackReceiver
    { 


        public Type type;
        public object obj;
        public string identity;
        public string name;
        public int FillIndex=0;

        public bool isSetVariable = false;
        public PopupStructer structerTypeRange = null;
        // [SerializeField] private string objMessag;
        [SerializeField] public string objMessage;

        public bool isFromGlobaldatas = false;
        public GlobalVariable(Type type, object obj,string id,string name)
        {
            isSetVariable = false;
            this.type = type;
            this.obj = obj;
            this.identity = id;
            this.name = name;
            FillIndex = 0;
        }

        public GlobalVariable(GlobalVariable variable)
        {
            isSetVariable = false;
            type = variable.type;
            obj = variable.obj;
            identity = variable.identity;
            name = variable.name;
            objMessage = variable.objMessage;
            FillIndex = variable.FillIndex;
            structerTypeRange = variable.structerTypeRange;
        }

        public GlobalVariable()
        {
            isSetVariable = false;
            this.type = typeof(string);
            this.obj = string.Empty;
            this.identity = "字符串";
            this.name = string.Empty;
            FillIndex = 0;
        }

        public void setRangeType(TreeNode node,params string[] strs)
        {
            structerTypeRange = node.SetVariableTypeRange(strs);
        }
        public void setRangeType(NodeGraph graph, params string[] strs)
        {
            structerTypeRange = new PopupStructer(graph,strs);
        }
        public void setRangeType(params string[] strs)
        {
            structerTypeRange = new PopupStructer(strs);
        }
        public void setRangeType(UnitBase unit, params string[] strs)
        {
            if (structerTypeRange != null)
            {
                structerTypeRange.Update(unit, strs);
            }
            else
            {
                structerTypeRange = new PopupStructer(unit, strs);
            }
        }

        public void ConvertString()
        {
            objMessage = ConnectionType.types[this.identity].ObjtoString(this.obj);
        }

        public void ConvertObject()
        {
            obj = ConnectionType.types[this.identity].StringtoObj(objMessage);
        }

        public void OnBeforeSerialize()
        {
            if(this.obj==null||this.obj.Equals(string.Empty))
                return;
            objMessage= ConnectionType.types[this.identity].ObjtoString(this.obj);
        }

        public void OnAfterDeserialize()
        {
            if(objMessage.Equals(string.Empty))
                return;
            TriggerEditorUtility.TrigInit += delegate
            {
                obj = ConnectionType.types[this.identity].StringtoObj(this.objMessage);
            };
        }
    }

}
