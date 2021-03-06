﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ou.Support.UnitSupport;
using UnityEngine;
using UnityEngine.UI;

namespace Ou.Support.NodeSupport
{
    [Node(false, "Unit-图片设置", "Node")]
    public class TreeNodeUGUIUnitImage : TreeNodeGUI
    {
        public override string GetId { get { return "Unit-图片设置"; } }
        // [SerializeField] private string content = string.Empty;
        protected internal override void Evaluator()
        {
            var img = variables[0].obj as Image;
            UnitBase unit = curGraph.ReadGlobalVariable(variables[1]).obj as UnitBase;
            GlobalVariable res = unit.ReadGlobalVariable(variables[2].name);
            img.sprite = (Sprite) res.obj;
            base.Evaluator();
        }

        protected internal override void NodeGUI()
        {
            OuUIUtility.FormatLabel("UI目标");
            DrawFillsLayout(variables[0]);
            OuUIUtility.FormatLabel("Unit:");
            DrawFillsLayout(variables[1]);
            if (variables[1].obj != null && variables[1].obj.GetType() == typeof(UnitBase))
            {
                UnitBase tar = variables[1].obj as UnitBase;
                variables[2].setRangeType(tar, "UISprite");
                DrawUnitHandle();
            }
        }

        private void DrawUnitHandle()
        {
            OuUIUtility.FormatLabel("读取属性：");
           DrawUnitLayout(variables[2]);
        }

        public override Node Create(Vector2 pos)
        {
            TreeNode node = CreateInstance<TreeNodeUGUIUnitImage>();
            node.Title = "Unit-图片设置";
            node.rect = new Rect(pos, new Vector2(150, 240));
            node.CreateNodeInput("PreIn", "工作状态");
            node.CreateNodeOutput("Nextout", "工作状态");
            node.CreateVariable();
            node.CreateVariable();
            node.CreateVariable();
            return node;

        }

        protected internal override TreeNodeResult OnUpdate()
        {
            return TreeNodeResult.Done;
        }

        protected internal override void OnStart()
        {
            base.OnStart();
        }

        protected internal override void Start()
        {
            //  ext = new GlobalVariable(typeof(Text), null, "TextUI", "text");
            variables[0].setRangeType(this, "ImageUI");
            variables[1].setRangeType(this, "Unit");
            base.Start();
        }
    }
}
