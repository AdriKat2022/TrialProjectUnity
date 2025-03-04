using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(Dialogue))]
public class DialogueInspector : Editor
{
    private SerializedProperty dialogueContainerProperty;
    private SerializedProperty dialogueGroupProperty;
    private SerializedProperty dialogueProperty;

    private SerializedProperty groupedDialogueProperty;
    private SerializedProperty startingDialogueOnlyProperty;

    private SerializedProperty selectedDialogueGroupIndexProperty;
    private SerializedProperty selectedDialogueIndexProperty;


    private void OnEnable()
    {
        dialogueContainerProperty = serializedObject.FindProperty("dialogueContainer");
        dialogueGroupProperty = serializedObject.FindProperty("dialogueGroup");
        dialogueProperty = serializedObject.FindProperty("dialogue");
        groupedDialogueProperty = serializedObject.FindProperty("groupedDialogues");
        startingDialogueOnlyProperty = serializedObject.FindProperty("startingDialogueOnly");
        selectedDialogueGroupIndexProperty = serializedObject.FindProperty("selectedDialogueGroupIndex");
        selectedDialogueIndexProperty = serializedObject.FindProperty("selectedDialogueIndex");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDialogueContainerArea();
        DialogueInspectorUtility.DrawSpace();

        DialogueContainerSO dialogueContainer = dialogueContainerProperty.objectReferenceValue as DialogueContainerSO;

        if (dialogueContainer == null)
        {
            StopDrawing("Start by assigning a dialogue container. A dialogue container is equivalent to a dialogue graph.");
            return;
        }

        DrawFiltersArea();
        DialogueInspectorUtility.DrawSpace();

        bool currentStartingDialoguesOnlyFilter = startingDialogueOnlyProperty.boolValue;

        List<string> dialogueNames;
        string dialogueFolderPath = $"Assets/DialogueSystem/Dialogues/{dialogueContainer.FileName}";
        string dialogueInfoMessage;

        if (groupedDialogueProperty.boolValue)
        {
            List<string> dialogueGroupNames = dialogueContainer.GetdialogueGroupNames();

            if (dialogueGroupNames.Count == 0)
            {
                StopDrawing("There are no dialogue groups found in the dialogue container.");
                return;
            }

            DrawDialogueGroupArea(dialogueContainer, dialogueGroupNames);
            DialogueInspectorUtility.DrawSpace();

            DialogueGroupSO dialogueGroup = (DialogueGroupSO)dialogueGroupProperty.objectReferenceValue;
            dialogueNames = dialogueContainer.GetGroupedDialogueNames(dialogueGroup, currentStartingDialoguesOnlyFilter);
            dialogueFolderPath += $"/Groups/{dialogueGroup.GroupName}/Dialogues";
            dialogueInfoMessage = "There are no " + (currentStartingDialoguesOnlyFilter ? "starting" : "") + " dialogues in the selected group!";
        }
        else
        {
            dialogueNames = dialogueContainer.GetUngroupedDialogueNames(currentStartingDialoguesOnlyFilter);
            dialogueFolderPath += "/Global/Dialogues";
            dialogueInfoMessage = "There are no " + (currentStartingDialoguesOnlyFilter ? "starting " : "") + "dialogues in the global space of this container!";
        }

        if (dialogueNames.Count == 0)
        {
            StopDrawing(dialogueInfoMessage);
            return;
        }

        DrawDialogueArea(dialogueNames, dialogueFolderPath);

        serializedObject.ApplyModifiedProperties();
    }

    #region Draw Methods

    private void DrawDialogueContainerArea()
    {
        DialogueInspectorUtility.DrawHeader("Dialogue Container");
        dialogueContainerProperty.DrawPropertyField();
    }

    private void DrawFiltersArea()
    {
        DialogueInspectorUtility.DrawHeader("Filters");
        groupedDialogueProperty.DrawPropertyField();
        startingDialogueOnlyProperty.DrawPropertyField();
    }

    private void DrawDialogueGroupArea(DialogueContainerSO dialogueContainer, List<string> dialogueGroupNames)
    {
        DialogueInspectorUtility.DrawHeader("Dialogue Group");

        int oldSelectedDialogueGroupIndex = selectedDialogueGroupIndexProperty.intValue;

        DialogueGroupSO oldDialogueGroup = dialogueGroupProperty.objectReferenceValue as DialogueGroupSO;

        string oldDialogueGroupName = oldDialogueGroup == null ? string.Empty : oldDialogueGroup.name;
        UpdateIndexOnDialogueGroupUpdate(
            dialogueGroupNames,
            selectedDialogueGroupIndexProperty,
            oldSelectedDialogueGroupIndex,
            oldDialogueGroupName,
            oldDialogueGroup == null);

        selectedDialogueGroupIndexProperty.DrawPopup("Dialogue Group", dialogueGroupNames.ToArray());
        string selectedDialogueGroupName = dialogueGroupNames[selectedDialogueGroupIndexProperty.intValue];
        DialogueGroupSO selectedDialogueGroup = DialogueIOUtility.LoadAsset<DialogueGroupSO>($"Assets/DialogueSystem/Dialogues/{dialogueContainer.FileName}/Groups/{selectedDialogueGroupName}", selectedDialogueGroupName);
        dialogueGroupProperty.objectReferenceValue = selectedDialogueGroup;

        DialogueInspectorUtility.DrawDisabledFields(() => dialogueGroupProperty.DrawPropertyField());
    }

    private void DrawDialogueArea(List<string> dialogueNames, string dialogueFolderPath)
    {
        DialogueInspectorUtility.DrawHeader("Dialogue");
        int oldSelectedDialogueIndex = selectedDialogueIndexProperty.intValue;
        DialogueSO oldDialogue = dialogueProperty.objectReferenceValue as DialogueSO;
        string oldDialogueName = oldDialogue == null ? string.Empty : oldDialogue.name;
        UpdateIndexOnDialogueGroupUpdate(dialogueNames, selectedDialogueIndexProperty, oldSelectedDialogueIndex, oldDialogueName, oldDialogue == null);
        selectedDialogueIndexProperty.DrawPopup("Dialogue", new string[] { "1", "2" });
        string selectedDialogueName = dialogueNames[selectedDialogueIndexProperty.intValue];
        DialogueSO selectedDialogue = DialogueIOUtility.LoadAsset<DialogueSO>(dialogueFolderPath, selectedDialogueName);
        dialogueProperty.objectReferenceValue = selectedDialogue;
        DialogueInspectorUtility.DrawDisabledFields(() => dialogueProperty.DrawPropertyField());
    }

    private void StopDrawing(string reason)
    {
        EditorGUILayout.HelpBox(reason, MessageType.Info, true);
        DialogueInspectorUtility.DrawSpace();
        EditorGUILayout.HelpBox("You need to select a dialogue for this component to work properly at runtime!", MessageType.Warning, true);
        serializedObject.ApplyModifiedProperties();
    }
    #endregion

    private void UpdateIndexOnDialogueGroupUpdate(List<string> optionNames, SerializedProperty indexProperty, int oldSelectedPropertyIndex, string oldPropertyName, bool isOldPropertyNull)
    {
        if (isOldPropertyNull)
        {
            indexProperty.intValue = 0;
            return;
        }

        bool oldIndexIsOutOfBounds = oldSelectedPropertyIndex >= optionNames.Count;
        bool oldNameIsDifferentThanSelectedName = oldIndexIsOutOfBounds || oldPropertyName != optionNames[oldSelectedPropertyIndex];

        if (oldNameIsDifferentThanSelectedName)
        {
            if (optionNames.Contains(oldPropertyName))
            {
                indexProperty.intValue = optionNames.IndexOf(oldPropertyName);
            }
            else
            {
                indexProperty.intValue = 0;
            }
        }
    }
}
