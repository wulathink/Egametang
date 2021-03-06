﻿using System;
using System.Collections.Generic;
using Base;
using Model;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyEditor
{
	public class PropertyDesigner: Editor
	{
		private readonly string[] mBehaviorToolbarStrings = { "属性值", "节点", "工具", "调试" };
		private int mBehaviorToolbarSelection;
		private float mWidth = 380f;
		private BehaviorNodeData mCurBehaviorNode;
		private string mSearchNode = "";
		private FoldoutNode mCurNode;

		public PropertyDesigner()
		{
			Init();
		}

		private void Init()
		{
			UpdateList();
		}

		private bool mDragingBorder;

		public void HandleEvents()
		{
			var e = Event.current;
			switch (e.type)
			{
				case EventType.MouseDown:
					if (e.button == 0 && e.mousePosition.x < mWidth + 30 && e.mousePosition.x > mWidth)
					{
						mDragingBorder = true;
					}
					break;
				case EventType.MouseDrag:
					if (mDragingBorder)
					{
						mWidth += e.delta.x;
					}
					break;
				case EventType.MouseUp:
					mDragingBorder = false;
					break;
			}
		}

		Rect toolbarRect = new Rect(0f, 0f, 0, 0);

		public void Draw()
		{
			HandleEvents();
			toolbarRect = new Rect(0f, 0f, mWidth, 18f);
			Rect boxRect = new Rect(0f, toolbarRect.height, this.mWidth, (Screen.height - toolbarRect.height) - 21f);
			GUILayout.BeginArea(toolbarRect, EditorStyles.toolbar);
			this.mBehaviorToolbarSelection = GUILayout.Toolbar(this.mBehaviorToolbarSelection, this.mBehaviorToolbarStrings, EditorStyles.toolbarButton);
			GUILayout.EndArea();
			GUILayout.BeginArea(boxRect);
			Filter();
			if (mBehaviorToolbarSelection == 0)
			{
				DrawValueView();
			}
			else if (mBehaviorToolbarSelection == 1)
			{
				DrawNodes();
			}
			else if (mBehaviorToolbarSelection == 2)
			{
			}
			else if (mBehaviorToolbarSelection == 3)
			{
				DrawDebugView();
			}
			GUILayout.EndArea();
		}

		public void SetToolBar(int select)
		{
			this.mBehaviorToolbarSelection = select;
		}

		public void SelectNodeFolderCallback(FoldoutFolder folder)
		{
			folder.Select = true;
			if (mCurNodeFolder != null && mCurNodeFolder != folder)
			{
				mCurNodeFolder.Select = false;
				mCurNodeFolder = null;
			}
			mCurNodeFolder = folder;
		}

		public void SelectNodeCallback(FoldoutNode node)
		{
			node.Select = true;
			if (mCurNode != null && mCurNode != node)
			{
				mCurNode.Select = false;
				mCurNode = null;
			}
			mCurNode = node;
		}

		private void UpdateList()
		{
			mNodeFoldout = new FoldoutFolder("所有节点", SelectNodeFolderCallback);
			mNodeFoldout.Fold = true;

			foreach (var kv in BehaviorManager.GetInstance().Classify2NodeProtoList)
			{
				string classify = kv.Key;
				var nodeTypeList = kv.Value;
				FoldoutFolder folder = mNodeFoldout.AddFolder(classify, SelectNodeFolderCallback);
				folder.Fold = true;

				mNodeCount++;
				foreach (var nodeType in nodeTypeList)
				{
					folder.AddNode(classify, nodeType.name + " (" + nodeType.describe + ")", SelectNodeCallback);
					mNodeCount++;
				}
			}
		}

		private Vector2 mTreeScrollPos = Vector2.zero;
		private int mNodeCount;
		private FoldoutFolder mNodeFoldout;
		private FoldoutFolder mCurNodeFolder;

		private void DrawNodes()
		{
			float offset = 190f;
			if (mCurNode == null)
			{
				offset = 55f;
			}
			DrawSearchList(offset);
			DrawNodeFunctions(offset);
		}

		private int mNodeTypeSelection;
		private string[] mNodeTypeToolbarStrings = { "All", "Composite", "Decorator", "Action", "Condition", "Root", "DataTrans" };
		private int mEnumNodeTypeSelection;
		string[] mEnumNodeTypeArr;

		private string DrawSearchList(float offset)
		{
			GUILayout.BeginHorizontal();
			GUI.SetNextControlName("Search");
			this.mSearchNode = GUILayout.TextField(this.mSearchNode, GUI.skin.FindStyle("ToolbarSeachTextField"));
			GUILayout.EndHorizontal();

			toolbarRect = new Rect(0f, 15f, mWidth, 25f);
			Rect boxRect = new Rect(0f, toolbarRect.height, this.mWidth, (Screen.height - toolbarRect.height) - 21f + 10);
			GUILayout.BeginArea(toolbarRect, EditorStyles.toolbar);
			GUILayout.BeginHorizontal();

			GUILayout.Label("Filter");
			Array strArr = Enum.GetValues(typeof (NodeClassifyType));
			List<string> strList = new List<string>();
			strList.Add("All");
			foreach (var str in strArr)
			{
				strList.Add(str.ToString());
			}
			mEnumNodeTypeArr = strList.ToArray();
			mEnumNodeTypeSelection = EditorGUILayout.Popup(mEnumNodeTypeSelection, mEnumNodeTypeArr);
			if (GUILayout.Button("Clear"))
			{
				ClearNodes();
			}
			GUILayout.EndHorizontal();
			GUILayout.EndArea();

			GUILayout.BeginArea(new Rect(0f, 15f + 20, this.mWidth, Screen.height - offset));
			this.mTreeScrollPos = GUI.BeginScrollView(new Rect(0f, 0f, this.mWidth, Screen.height - offset), this.mTreeScrollPos,
					new Rect(0f, 0f, this.mWidth - 20f, mNodeCount * 19), false, false);
			mNodeFoldout.Draw();
			GUI.EndScrollView();
			GUILayout.EndArea();
			if (mCurNode != null)
			{
				string[] arr = mCurNode.Text.Split(' ');
				string name = arr[0];
				return name;
			}
			return "";
		}

		private void DrawNodeFunctions(float offset)
		{
			Rect boxRect = new Rect(0f, Screen.height - offset + 15f, this.mWidth, 200f);
			GUILayout.BeginArea(boxRect);
			BehaviorManager.GetInstance().selectNodeName = "";
			if (mCurNode != null)
			{
				string[] arr = mCurNode.Text.Split(' ');
				string name = arr[0];
				BehaviorManager.GetInstance().selectNodeName = name;
				BehaviorManager.GetInstance().selectNodeType = mCurNode.folderName;
				if (mCurNode.folderName != NodeClassifyType.Root.ToString())
				{
					if (GUILayout.Button("新建"))
					{
						Game.Scene.GetComponent<EventComponent>().Run(EventIdType.BehaviorTreePropertyDesignerNewCreateClick, name, Vector2.zero);
					}
				}
				if (mCurNode.folderName != NodeClassifyType.Root.ToString() ||
				    (mCurNode.folderName == NodeClassifyType.Root.ToString() && mCurBehaviorNode.IsRoot()))
				{
					if (GUILayout.Button("替换"))
					{
						Game.Scene.GetComponent<EventComponent>().Run(EventIdType.BehaviorTreeReplaceClick, name, Vector2.zero);
					}
				}

				if (GUILayout.Button("保存"))
				{
					BehaviorManager.GetInstance().SaveAll();
				}
				var node = BehaviorManager.GetInstance().GetNodeTypeProto(name);
				GUILayout.Label("节点名:" + node.name);
				GUILayout.Label("描述:" + node.describe);
			}

			GUILayout.EndArea();
		}

		private void ClearNodes()
		{
			BehaviorManager.GetInstance().selectNodeName = "";
			mEnumNodeTypeSelection = 0;
			mSearchNode = "";
			foreach (FoldoutFolder folder in mNodeFoldout.Folders)
			{
				foreach (FoldoutNode node in folder.Nodes)
				{
					node.Hide = false;
				}
			}
		}

		private void Filter()
		{
			foreach (FoldoutFolder folder in mNodeFoldout.Folders)
			{
				string selectType;
				if (mEnumNodeTypeSelection == 0)
				{
					selectType = "All";
				}
				else
				{
					selectType = Enum.GetName(typeof (NodeClassifyType), mEnumNodeTypeSelection - 1);
				}

				if (selectType == folder.Text || selectType == "All")
				{
					folder.Hide = false;
					foreach (FoldoutNode node in folder.Nodes)
					{
						if (node.Text.ToUpper().IndexOf(mSearchNode.ToUpper()) == -1)
						{
							node.Hide = true;
						}
						else
						{
							node.Hide = false;
						}
					}
				}
				else
				{
					foreach (FoldoutNode node in folder.Nodes)
					{
						node.Hide = true;
					}
					folder.Hide = true;
				}
			}
		}

		private string searchNodeName = "";
		private BehaviorTreeConfig searchTree;
		private readonly GameObject[] searchGoArr = new GameObject[0];
		private Rect mBorderRect; //边框
		private Vector2 mScrollPosition = Vector2.zero;
		private Rect mGraphRect = new Rect(0, 0, 50, 50); //绘图区域
		private Vector2 scrollPosition = Vector2.zero;

		private void ShowResult()
		{
			Rect boxRect1 = new Rect(0f, 100, this.mWidth, Screen.height);
			GUILayout.BeginArea(boxRect1);

			scrollPosition = GUI.BeginScrollView(new Rect(0, 0, this.mWidth, 220), scrollPosition, new Rect(0, 0, this.mWidth - 20, searchGoArr.Length * 20));
			for (int i = 0; i < this.searchGoArr.Length; i++)
			{
				searchGoArr[i] = BehaviourTreeField((i + 1).ToString(), searchGoArr[i]);
			}

			GUI.EndScrollView();
			GUILayout.EndArea();
		}

		private BehaviorTreeConfig treeConfig;

		public GameObject BehaviourTreeField(string desc, GameObject value)
		{
			EditorGUILayout.BeginHorizontal();
			value = (GameObject) EditorGUILayout.ObjectField(desc, value, typeof (GameObject), false);
			if (value.GetComponent<BehaviorTreeConfig>() != null && GUILayout.Button("打开行为树"))
			{
				BehaviorManager.GetInstance().OpenBehaviorEditor(value);
				SetToolBar(2);
			}
			EditorGUILayout.EndHorizontal();
			return value;
		}

		private void DrawValueView()
		{
			if (mCurBehaviorNode == null || mCurBehaviorNode.Proto == null)
			{
				return;
			}
			if (GUILayout.Button("保存行为树"))
			{
				BehaviorManager.GetInstance().SaveAll();
			}
			ClientNodeTypeProto proto = mCurBehaviorNode.Proto;
			GUILayout.Space(10f);
			GUILayout.Label("节点ID:" + mCurBehaviorNode.nodeId);
			GUILayout.Label("节点名:" + mCurBehaviorNode.name);
			GUILayout.Label("说明:");
			GUILayout.Label(proto.describe);
			GUILayout.Label("描述:");
			mCurBehaviorNode.describe = EditorGUILayout.TextArea(mCurBehaviorNode.describe, GUILayout.Height(50f));

			DrawAllValue(proto);
		}

		private bool mFoldParam = true;
		private bool mFoldInput = true;
		private bool mFoldOutput = true;

		private void DrawAllValue(ClientNodeTypeProto proto)
		{
			List<NodeFieldDesc> paramFieldList = GetFieldDescList(proto.new_args_desc, typeof (NodeFieldAttribute));
			List<NodeFieldDesc> inputFieldList = GetFieldDescList(proto.new_args_desc, typeof (NodeInputAttribute));
			List<NodeFieldDesc> outputFieldList = GetFieldDescList(proto.new_args_desc, typeof (NodeOutputAttribute));
			mFoldParam = EditorGUILayout.Foldout(mFoldParam, "参数");
			if (mFoldParam)
			{
				DrawProp(proto.name, paramFieldList, NodeParamType.None);
			}
			mFoldInput = EditorGUILayout.Foldout(mFoldInput, "输入");
			if (mFoldInput)
			{
				DrawProp(proto.name, inputFieldList, NodeParamType.Input);
			}
			mFoldOutput = EditorGUILayout.Foldout(mFoldOutput, "输出");
			if (mFoldOutput)
			{
				DrawProp(proto.name, outputFieldList, NodeParamType.Output);
			}
		}

		private List<NodeFieldDesc> GetFieldDescList(List<NodeFieldDesc> fieldList, Type fieldAttributeType)
		{
			List<NodeFieldDesc> newFieldList = new List<NodeFieldDesc>();
			for (int i = 0; i < fieldList.Count; i++)
			{
				if (fieldList[i].attributeType == fieldAttributeType)
				{
					newFieldList.Add(fieldList[i]);
				}
			}
			return newFieldList;
		}

		private void DrawProp(string nodeName, List<NodeFieldDesc> fieldList, NodeParamType nodeParamType)
		{
			for (int i = 0; i < fieldList.Count; i++)
			{
				NodeFieldDesc desc = fieldList[i];
				Type fieldType = ExportNodeTypeConfig.GetFieldType(nodeName, desc.name);
				ClientNodeTypeProto clientNode = ExportNodeTypeConfig.GetNodeTypeProtoFromDll(nodeName);
				object newValue = null;
				if (!mCurBehaviorNode.args_dict.ContainsKey(desc.name))
				{
					mCurBehaviorNode.args_dict.Add(desc.name, new ValueBase());
				}
				if (BehaviorTreeArgsDict.IsStringType(fieldType))
				{
					if (nodeParamType == NodeParamType.Input)
					{
						newValue = InputEnumFieldValue(desc);
					}
					else if (nodeParamType == NodeParamType.Output && clientNode.classify == NodeClassifyType.Root.ToString())
					{
						newValue = ConstTextFieldValue(desc);
					}
					else
					{
						newValue = TextFieldValue(desc);
					}
				}
				else if (BehaviorTreeArgsDict.IsFloatType(fieldType))
				{
					newValue = FloatFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsDoubleType(fieldType))
				{
					newValue = DoubletFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsIntType(fieldType))
				{
					newValue = IntFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsLongType(fieldType))
				{
					newValue = LongFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsBoolType(fieldType))
				{
					newValue = BoolFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsObjectType(fieldType))
				{
					newValue = ObjectFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsIntArrType(fieldType))
				{
					newValue = IntArrFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsLongArrType(fieldType))
				{
					newValue = LongArrFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsStringArrType(fieldType))
				{
					newValue = StrArrFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsFloatArrType(fieldType))
				{
					newValue = FloatArrFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsDoubleArrType(fieldType))
				{
					newValue = DoubleArrFieldValue(desc);
				}
				else if (BehaviorTreeArgsDict.IsEnumType(fieldType))
				{
					if (nodeParamType == NodeParamType.Input)
					{
						newValue = InputEnumFieldValue(desc);
					}
					else if (nodeParamType == NodeParamType.Output)
					{
						newValue = OutPutEnumFieldValue(desc);
					}
					else
					{
						newValue = EnumFieldValue(desc);
					}
				}
				else if (BehaviorTreeArgsDict.IsObjectArrayType(fieldType))
				{
					newValue = ObjectArrFieldValue(desc);
				}
				else
				{
					Log.Error($"行为树节点暂时未支持此类型:{fieldType}！");
					return;
				}
				mCurBehaviorNode.args_dict.SetKeyValueComp(fieldType, desc.name, newValue);
			}
		}

		private object ObjectFieldValue(NodeFieldDesc desc)
		{
			Object oldValue = (Object) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			EditorGUILayout.LabelField(GetPropDesc(desc));
			Object newValue = EditorGUILayout.ObjectField("", oldValue, desc.type, false);
			if (newValue == null)
			{
				return null;
			}
			if (BehaviorTreeArgsDict.IsGameObjectType(desc.type) && !BehaviorTreeArgsDict.SatisfyCondition((GameObject) newValue, desc.constraintTypes))
			{
				return null;
			}
			return newValue;
		}

		private object ConstTextFieldValue(NodeFieldDesc desc)
		{
			string oldValue = desc.value.ToString();
			EditorGUILayout.LabelField(GetPropDesc(desc));
			EditorGUILayout.LabelField("", oldValue);
			return oldValue;
		}

		private object TextFieldValue(NodeFieldDesc desc)
		{
			string oldValue = (string) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			EditorGUILayout.LabelField(GetPropDesc(desc));
			object newValue = EditorGUILayout.TextField("", oldValue);
			return newValue;
		}

		private object BoolFieldValue(NodeFieldDesc desc)
		{
			bool oldValue = (bool) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			EditorGUILayout.LabelField(GetPropDesc(desc));
			object newValue = EditorGUILayout.Toggle("", oldValue);
			return newValue;
		}

		private object IntFieldValue(NodeFieldDesc desc)
		{
			int oldValue = (int) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			EditorGUILayout.LabelField(GetPropDesc(desc));
			object newValue = EditorGUILayout.IntField("", oldValue);
			return newValue;
		}

		private object LongFieldValue(NodeFieldDesc desc)
		{
			long oldValue = (long) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			EditorGUILayout.LabelField(GetPropDesc(desc));
			object newValue = EditorGUILayout.LongField("", oldValue);
			return newValue;
		}

		private object FloatFieldValue(NodeFieldDesc desc)
		{
			float oldValue = (float) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			EditorGUILayout.LabelField(GetPropDesc(desc));
			object newValue = EditorGUILayout.FloatField("", oldValue);
			return newValue;
		}

		private object DoubletFieldValue(NodeFieldDesc desc)
		{
			double oldValue = (double) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			EditorGUILayout.LabelField(GetPropDesc(desc));
			object newValue = EditorGUILayout.DoubleField("", oldValue);
			return newValue;
		}

		private bool foldStrArr;

		private object StrArrFieldValue(NodeFieldDesc desc)
		{
			string[] oldValue = (string[]) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			string[] newValue = CustomArrayField.StringArrFieldValue(ref foldStrArr, GetPropDesc(desc), oldValue);
			return newValue;
		}

		private bool foldIntArr;

		private object IntArrFieldValue(NodeFieldDesc desc)
		{
			int[] oldValue = (int[]) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			int[] newValue = CustomArrayField.IntArrFieldValue(ref foldIntArr, GetPropDesc(desc), oldValue);
			return newValue;
		}

		private bool foldLongArr;

		private object LongArrFieldValue(NodeFieldDesc desc)
		{
			long[] oldValue = (long[]) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			long[] newValue = CustomArrayField.LongArrFieldValue(ref foldLongArr, GetPropDesc(desc), oldValue);
			return newValue;
		}

		private bool foldFloatArr;

		private object FloatArrFieldValue(NodeFieldDesc desc)
		{
			float[] oldValue = (float[]) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			float[] newValue = CustomArrayField.FloatArrFieldValue(ref foldFloatArr, GetPropDesc(desc), oldValue);
			return newValue;
		}

		private bool foldDoubleArr;

		private object DoubleArrFieldValue(NodeFieldDesc desc)
		{
			double[] oldValue = (double[]) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			double[] newValue = CustomArrayField.DoubleArrFieldValue(ref foldDoubleArr, GetPropDesc(desc), oldValue);
			return newValue;
		}

		private bool foldObjectArr;

		private object ObjectArrFieldValue(NodeFieldDesc desc)
		{
			Object[] oldValue = (Object[]) mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name);
			Object[] newValue = CustomArrayField.ObjectArrFieldValue(ref foldObjectArr, GetPropDesc(desc), oldValue, desc);
			return newValue;
		}

		private object OutPutEnumFieldValue(NodeFieldDesc desc)
		{
			string oldValue = mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name)?.ToString();
			if (string.IsNullOrEmpty(oldValue))
			{
				oldValue = BTEnvKey.None;
			}
			string[] enumValueArr;
			if (mCurBehaviorNode.IsRoot() && desc.value.ToString() != BTEnvKey.None)
			{
				enumValueArr = new string[1] { desc.value.ToString() };
			}
			else
			{
				enumValueArr = BehaviorTreeInOutConstrain.GetEnvKeyEnum(typeof (BTEnvKey));
				if (enumValueArr.Length == 0)
				{
					enumValueArr = new string[1] { BTEnvKey.None };
				}
				if (oldValue == BTEnvKey.None)
				{
					oldValue = desc.value.ToString();
				}
			}

			int oldSelect = IndexInStringArr(enumValueArr, oldValue);
			string label = desc.name + (desc.desc == ""? "" : $"({desc.desc})") + $"({desc.envKeyType})";
			EditorGUILayout.LabelField(label);
			int selection = EditorGUILayout.Popup("", oldSelect, enumValueArr);
			string newValue = enumValueArr[selection];
			return newValue;
		}

		private object InputEnumFieldValue(NodeFieldDesc desc)
		{
			string oldValue = mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name)?.ToString();
			string[] enumValueArr = BehaviorManager.GetInstance().GetCanInPutEnvKeyArray(mCurBehaviorNode, desc);
			if (enumValueArr.Length == 0)
			{
				enumValueArr = new string[1] { BTEnvKey.None };
			}
			else if (string.IsNullOrEmpty(oldValue))
			{
				oldValue = enumValueArr[0];
			}
			int oldSelect = IndexInStringArr(enumValueArr, oldValue);
			string label = desc.name + (desc.desc == ""? "" : $"({desc.desc})") + $"({desc.envKeyType})";
			EditorGUILayout.LabelField(label);
			int selection = EditorGUILayout.Popup("", oldSelect, enumValueArr);
			string newValue = enumValueArr[selection];
			return newValue;
		}

		private int IndexInStringArr(string[] strArr, string str)
		{
			for (int i = 0; i < strArr.Length; i++)
			{
				if (strArr[i] == str)
				{
					return i;
				}
			}
			return 0;
		}

		private object EnumFieldValue(NodeFieldDesc desc)
		{
			string oldValue = mCurBehaviorNode.args_dict.GetTreeDictValue(desc.type, desc.name)?.ToString();
			if (string.IsNullOrEmpty(oldValue))
			{
				oldValue = GetDefaultEnumValue(desc.type);
			}
			Enum oldValueEnum = (Enum) Enum.Parse(desc.type, oldValue);
			Enum newValueEnum;
			EditorGUILayout.LabelField(desc.type.ToString());
			newValueEnum = EditorGUILayout.EnumPopup(oldValueEnum);
			return newValueEnum.ToString();
		}

		private string GetDefaultEnumValue(Type type)
		{
			Array array = Enum.GetValues(type);
			string value = array.GetValue(0).ToString();
			return value;
		}

		public string[] GetEnumValues(Type enumType)
		{
			List<string> enumValueList = new List<string>();
			foreach (int myCode in Enum.GetValues(enumType))
			{
				string strName = Enum.GetName(enumType, myCode);
				enumValueList.Add(strName);
			}
			return enumValueList.ToArray();
		}

		public string GetPropDesc(NodeFieldDesc desc)
		{
			string typeDesc = desc.type.ToString().Split('.')[1].ToLower();
			return desc.name + desc.desc + "(" + typeDesc + ")";
		}

		public void onSelectNode(params object[] list)
		{
			mCurBehaviorNode = (BehaviorNodeData) list[0];
			GUI.FocusControl("");
		}

		public void DrawDebugView()
		{
			if (BehaviorManager.GetInstance().CurBehaviorTree == null)
			{
				return;
			}
			if (GUILayout.Button($"清空执行记录"))
			{
				BehaviorManager.treePathList.Clear();
				BehaviorManager.GetInstance().ClearDebugState();
			}
			float offset = 55f;
			GUILayout.BeginArea(new Rect(0f, 20f, this.mWidth, Screen.height - offset));
			this.mTreeScrollPos = GUI.BeginScrollView(new Rect(0f, 0f, this.mWidth, Screen.height - offset), this.mTreeScrollPos,
					new Rect(0f, 0f, this.mWidth - 20f, BehaviorManager.treePathList.Count * 22), false, false);

			for (int i = 0; i < BehaviorManager.treePathList.Count; i++)
			{
				if (GUILayout.Button($"frame{i}"))
				{
					BehaviorManager.GetInstance().ClearDebugState();
					BehaviorManager.GetInstance().SetDebugState(BehaviorManager.GetInstance().CurBehaviorTree, BehaviorManager.treePathList[i]);
				}
			}
			GUI.EndScrollView();
			GUILayout.EndArea();
		}
	}
}