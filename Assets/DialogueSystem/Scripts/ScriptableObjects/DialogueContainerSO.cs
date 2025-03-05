using System.Collections.Generic;
using UnityEngine;

public class DialogueContainerSO : ScriptableObject
{
    [field: SerializeField] public string FileName { get; set; }
    [field: SerializeField] public SerializableDictionary<DialogueGroupSO, List<DialogueSO>> DialogueGroups { get; set; }
    [field: SerializeField] public List<DialogueSO> UngroupedDialogues { get; set; }


    public void Initialize(string fileName)
    {
        FileName = fileName;
        DialogueGroups = new SerializableDictionary<DialogueGroupSO, List<DialogueSO>>();
        UngroupedDialogues = new List<DialogueSO>();
    }

    public List<string> GetdialogueGroupNames()
    {
        List<string> groupNames = new();
        foreach (var dialogueGroup in DialogueGroups.Keys)
        {
            groupNames.Add(dialogueGroup.GroupName);
        }
        return groupNames;
    }

    public List<string> GetGroupedDialogueNames(DialogueGroupSO dialogueGroup, bool startingDialoguesOnly)
    {
        List<string> dialogueNames = new();
        foreach (var dialogue in DialogueGroups[dialogueGroup])
        {
            if (startingDialoguesOnly && !dialogue.IsStartingDialogue)
            {
                continue;
            }
            dialogueNames.Add(dialogue.DialogueName);
        }
        return dialogueNames;
    }

    public List<string> GetUngroupedDialogueNames(bool startingDialoguesOnly)
    {
        List<string> dialogueNames = new();

        foreach (var dialogue in UngroupedDialogues)
        {
            if (startingDialoguesOnly && !dialogue.IsStartingDialogue)
            {
                continue;
            }
            dialogueNames.Add(dialogue.DialogueName);
        }

        return dialogueNames;
    }
}
