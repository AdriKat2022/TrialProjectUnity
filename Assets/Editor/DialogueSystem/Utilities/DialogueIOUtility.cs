using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public static class DialogueIOUtility
{
    private static DialogueGraphView graphView;
    private static string graphFileName;
    private static string containerFolderPath;

    private static List<DialogueGroup> groups;
    private static List<DialogueNode> nodes;

    private static Dictionary<string, DialogueGroupSO> createdDialogueGroups;
    private static Dictionary<string, DialogueSO> createdDialogues;

    private static Dictionary<string, DialogueGroup> loadedGroups;
    private static Dictionary<string, DialogueNode> loadedNodes;

    public static void Initialize(DialogueGraphView graphView, string graphName)
    {
        DialogueIOUtility.graphView = graphView;
        graphFileName = graphName;
        containerFolderPath = $"Assets/DialogueSystem/Dialogues/{graphFileName}";

        groups = new List<DialogueGroup>();
        nodes = new List<DialogueNode>();
        createdDialogueGroups = new Dictionary<string, DialogueGroupSO>();
        createdDialogues = new Dictionary<string, DialogueSO>();
        loadedGroups = new Dictionary<string, DialogueGroup>();
        loadedNodes = new Dictionary<string, DialogueNode>();
    }

    #region Save Methods
    public static void Save()
    {
        CreateStaticFolders();
        GetElementsFromGraphView();

        DialogueGraphSaveDataSO graphData = CreateAsset<DialogueGraphSaveDataSO>("Assets/Editor/DialogueSystem/Graphs", $"{graphFileName}Graph");
        graphData.Initialize(graphFileName);

        DialogueContainerSO dialogueContainer = CreateAsset<DialogueContainerSO>(containerFolderPath, graphFileName);
        dialogueContainer.Initialize(graphFileName);

        SaveGroups(graphData, dialogueContainer);

        SaveNodes(graphData, dialogueContainer);

        SaveAsset(graphData);
        SaveAsset(dialogueContainer);
    }

    #region Groups
    private static void SaveGroups(DialogueGraphSaveDataSO graphData, DialogueContainerSO dialogueContainer)
    {
        List<string> groupNames = new();
        foreach (DialogueGroup group in groups)
        {
            SaveGroupToGraph(group, graphData);
            SaveGroupToScriptableObject(group, dialogueContainer);

            groupNames.Add(group.title);
        }

        UpdateOldGroups(groupNames, graphData);
    }

    private static void UpdateOldGroups(List<string> currentGroupNames, DialogueGraphSaveDataSO graphData)
    {
        if (graphData.OldGroupNames != null && graphData.OldGroupNames.Count > 0)
        {
            List<string> groupsToRemove = graphData.OldGroupNames.Except(currentGroupNames).ToList();

            foreach (string group in groupsToRemove)
            {
                RemoveFolder($"{containerFolderPath}/Groups/{group}");
            }
        }

        graphData.OldGroupNames = new List<string>(currentGroupNames);
    }

    private static void SaveGroupToScriptableObject(DialogueGroup group, DialogueContainerSO dialogueContainer)
    {
        string groupName = group.title;
        CreateFolder($"{containerFolderPath}/Groups", groupName);
        CreateFolder($"{containerFolderPath}/Groups/{groupName}", "Dialogues");

        DialogueGroupSO dialogueGroup = CreateAsset<DialogueGroupSO>($"{containerFolderPath}/Groups/{groupName}", groupName);
        dialogueGroup.Initialize(groupName);
        createdDialogueGroups.Add(group.ID, dialogueGroup);
        dialogueContainer.DialogueGroups.Add(dialogueGroup, new List<DialogueSO>());

        SaveAsset(dialogueGroup);
    }

    private static void SaveGroupToGraph(DialogueGroup group, DialogueGraphSaveDataSO graphData)
    {
        DialogueGroupSaveData groupSaveData = new()
        {
            ID = group.ID,
            Name = group.title,
            Position = group.GetPosition().position
        };
        Debug.Log(graphData);
        Debug.Log(graphData.Groups);
        graphData.Groups.Add(groupSaveData);
    }
    #endregion

    #region Nodes

    private static void SaveNodes(DialogueGraphSaveDataSO graphData, DialogueContainerSO dialogueContainer)
    {
        SerializableDictionary<string, List<string>> groupedNodeNames = new();
        List<string> ungroupedNodeNames = new();

        foreach (DialogueNode node in nodes)
        {
            SaveNodeToGraph(node, graphData);
            SaveNodeToScriptableObject(node, dialogueContainer);

            if (node.Group != null)
            {
                groupedNodeNames.AddItem(node.Group.title, node.DialogueName);
            }
            else
            {
                ungroupedNodeNames.Add(node.DialogueName);
            }
        }

        UpdateDialogueChoicesConnections();
        UpdateOldGroupedNodes(groupedNodeNames, graphData);
        UpdateOldUngroupedNodes(ungroupedNodeNames, graphData);
    }

    private static void UpdateOldGroupedNodes(SerializableDictionary<string, List<string>> currentGroupedNodeNames, DialogueGraphSaveDataSO graphData)
    {
        if (graphData.OldGroupedNodeNames != null && graphData.OldGroupedNodeNames.Count > 0)
        {
            foreach (KeyValuePair<string, List<string>> oldGroupNode in graphData.OldGroupedNodeNames)
            {
                List<string> nodesToRemove = oldGroupNode.Value.Except(currentGroupedNodeNames[oldGroupNode.Key]).ToList();

                if (currentGroupedNodeNames.ContainsKey(oldGroupNode.Key))
                {
                    nodesToRemove = oldGroupNode.Value.Except(currentGroupedNodeNames[oldGroupNode.Key]).ToList();
                }

                foreach (string node in nodesToRemove)
                {
                    RemoveAsset($"{containerFolderPath}/Groups/{oldGroupNode.Key}/Dialogues", node);
                }
            }
        }

        graphData.OldGroupedNodeNames = new SerializableDictionary<string, List<string>>(currentGroupedNodeNames);
    }

    private static void UpdateOldUngroupedNodes(List<string> currentUngroupedNodeNames, DialogueGraphSaveDataSO graphData)
    {
        if (graphData.OldUngroupedNodeNames != null && graphData.OldUngroupedNodeNames.Count > 0)
        {
            List<string> nodesToRemove = graphData.OldUngroupedNodeNames.Except(currentUngroupedNodeNames).ToList();
            foreach (string node in nodesToRemove)
            {
                RemoveAsset($"{containerFolderPath}/Global/Dialogues", node);
            }
        }

        graphData.OldUngroupedNodeNames = new List<string>(currentUngroupedNodeNames);
    }

    private static void SaveNodeToGraph(DialogueNode node, DialogueGraphSaveDataSO graphData)
    {
        List<DialogueChoiceSaveData> choices = CloneNodeChoices(node.Choices);

        DialogueNodeSaveData nodeSaveData = new()
        {
            ID = node.ID,
            Name = node.DialogueName,
            Choices = choices,
            Text = node.DialogueText,
            Type = node.Type,
            GroupID = node.Group?.ID,
            Position = node.GetPosition().position
        };

        graphData.Nodes.Add(nodeSaveData);
    }

    private static void SaveNodeToScriptableObject(DialogueNode node, DialogueContainerSO dialogueContainer)
    {
        DialogueSO dialogue;

        if (node.Group != null)
        {
            dialogue = CreateAsset<DialogueSO>($"{containerFolderPath}/Groups/{node.Group.title}/Dialogues", node.DialogueName);
            dialogueContainer.DialogueGroups.AddItem(createdDialogueGroups[node.Group.ID], dialogue);
        }
        else
        {
            dialogue = CreateAsset<DialogueSO>($"{containerFolderPath}/Global/Dialogues", node.DialogueName);
            dialogueContainer.UngroupedDialogues.Add(dialogue);
        }

        dialogue.Initialize(
            node.DialogueName,
            node.DialogueText,
            ConvertNodeChoicesToDialogueChoices(node.Choices),
            node.Type,
            node.IsStartingNode()
        );

        SaveAsset(dialogue);
        createdDialogues.Add(node.ID, dialogue);
    }

    private static List<DialogueChoiceData> ConvertNodeChoicesToDialogueChoices(List<DialogueChoiceSaveData> choices)
    {
        List<DialogueChoiceData> dialogueChoices = new();

        foreach (DialogueChoiceSaveData choice in choices)
        {
            dialogueChoices.Add(new DialogueChoiceData
            {
                Text = choice.Text,
            });
        }

        return dialogueChoices;
    }

    private static void UpdateDialogueChoicesConnections()
    {
        foreach (var node in nodes)
        {
            DialogueSO dialogue = createdDialogues[node.ID];
            for (int choiceIndex = 0; choiceIndex < node.Choices.Count; choiceIndex++)
            {
                DialogueChoiceSaveData choice = node.Choices[choiceIndex];

                if (string.IsNullOrEmpty(choice.NodeID))
                {
                    continue;
                }

                dialogue.Choices[choiceIndex].NextDialogue = createdDialogues[choice.NodeID];
                SaveAsset(dialogue);
            }
        }
    }

    #endregion

    #endregion

    #region Load Methods

    public static void Load()
    {
        DialogueGraphSaveDataSO graphData = LoadAsset<DialogueGraphSaveDataSO>("Assets/Editor/DialogueSystem/Graphs", graphFileName);

        if (graphData == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "No graph data found!\n\n" +
                $"At \"Assets/Editor/DialogueSystem/Graphs/{graphFileName}\"",
                "Ok"
            );
            return;
        }

        Debug.Log("Found graph data: " + graphData);

        DialogueEditorWindow.UpdateFileName(graphData.FileName);
        LoadGroups(graphData.Groups);
        LoadNodes(graphData.Nodes);
        LoadNodesConnections();
    }

    private static void LoadNodesConnections()
    {
        foreach (KeyValuePair<string, DialogueNode> loadedNode in loadedNodes)
        {
            foreach (Port choicePort in loadedNode.Value.outputContainer.Children())
            {
                DialogueChoiceSaveData choiceData = (DialogueChoiceSaveData)choicePort.userData;
                if (string.IsNullOrEmpty(choiceData.NodeID))
                {
                    continue;
                }

                DialogueNode nextNode = loadedNodes[choiceData.NodeID];
                Port nexNodeInputPort = nextNode.inputContainer.Children().First() as Port;

                Edge edge = choicePort.ConnectTo(nexNodeInputPort);
                graphView.AddElement(edge);
                loadedNode.Value.RefreshPorts();
            }
        }
    }

    private static void LoadNodes(List<DialogueNodeSaveData> nodes)
    {
        foreach (DialogueNodeSaveData nodeData in nodes)
        {
            DialogueNode node = graphView.CreateNode(nodeData.Name, nodeData.Type, nodeData.Position, false);

            node.ID = nodeData.ID;
            node.Choices = CloneNodeChoices(nodeData.Choices);
            node.DialogueText = nodeData.Text;

            node.Draw();
            graphView.AddElement(node);
            loadedNodes.Add(node.ID, node);

            if (string.IsNullOrEmpty(nodeData.GroupID))
            {
                continue;
            }

            DialogueGroup group = loadedGroups[nodeData.GroupID];
            node.Group = group;
            group.AddElement(node);
        }
    }

    private static void LoadGroups(List<DialogueGroupSaveData> groups)
    {
        foreach (DialogueGroupSaveData groupData in groups)
        {
            DialogueGroup group = graphView.CreateGroup(groupData.Name, groupData.Position);

            group.ID = groupData.ID;

            loadedGroups.Add(group.ID, group);
        }
    }

    #endregion

    #region Fetch Methods
    private static void GetElementsFromGraphView()
    {
        Type groupType = typeof(DialogueGroup);
        graphView.graphElements.ForEach(element =>
        {
            if (element is DialogueNode dialogueNode)
            {
                nodes.Add(dialogueNode);
                return;
            }

            if (element.GetType() == groupType)
            {
                DialogueGroup group = (DialogueGroup)element;
                groups.Add(group);
                return;
            }

        });
    }

    #endregion

    #region Creation Methods
    private static void CreateStaticFolders()
    {
        CreateFolder("Assets/Editor/DialogueSystem", "Graphs");
        CreateFolder("Assets", "DialogueSystem");
        CreateFolder("Assets/DialogueSystem", "Dialogues");
        CreateFolder("Assets/DialogueSystem/Dialogues", graphFileName);
        CreateFolder(containerFolderPath, "Global");
        CreateFolder(containerFolderPath, "Groups");
        CreateFolder($"{containerFolderPath}/Global", "Dialogues");
    }
    #endregion

    #region Utility Methods

    private static void SaveAsset(UnityEngine.Object asset)
    {
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void RemoveAsset(string path, string assetName)
    {
        AssetDatabase.DeleteAsset($"{path}/{assetName}.asset");
    }

    private static void CreateFolder(string path, string foldername)
    {
        if (AssetDatabase.IsValidFolder($"{path}/{foldername}"))
        {
            return;
        }

        AssetDatabase.CreateFolder(path, foldername);
    }

    private static void RemoveFolder(string fullPath)
    {
        FileUtil.DeleteFileOrDirectory($"{fullPath}.meta");
        FileUtil.DeleteFileOrDirectory($"{fullPath}/");
    }

    private static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
    {
        string fullPath = $"{path}/{assetName}.asset";

        T asset = LoadAsset<T>(path, assetName);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, fullPath);
        }

        return asset;
    }

    private static T LoadAsset<T>(string path, string assetName) where T : ScriptableObject
    {
        return AssetDatabase.LoadAssetAtPath<T>($"{path}/{assetName}.asset");
    }

    private static List<DialogueChoiceSaveData> CloneNodeChoices(List<DialogueChoiceSaveData> choices)
    {
        List<DialogueChoiceSaveData> clonedChoices = new();

        // Copy choices to avoid reference issues (the serialized object need to be updated only when saving the graph)
        foreach (DialogueChoiceSaveData choice in choices)
        {
            clonedChoices.Add(new DialogueChoiceSaveData
            {
                Text = choice.Text,
                NodeID = choice.NodeID
            });
        }

        return clonedChoices;
    }
    #endregion
}
