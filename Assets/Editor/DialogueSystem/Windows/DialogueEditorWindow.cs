using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class DialogueEditorWindow : EditorWindow
{
    private DialogueGraphView graphView;
    private readonly string defaultFileName = "DialogueFileName";
    private static TextField fileNameTextField;
    private Button saveButton;

    [MenuItem("Window/DialogueEditorWindow")]
    public static void ShowExample()
    {
        DialogueEditorWindow wnd = GetWindow<DialogueEditorWindow>("DialogueEditorWindow");
        //wnd.titleContent = new GUIContent();
    }

    private void OnEnable()
    {
        AddGraphView();
        AddToolBar();
        AddStyles();
    }

    public static void UpdateFileName(string fileName)
    {
        fileNameTextField.value = fileName;
    }

    private void AddToolBar()
    {
        Toolbar toolbar = new();
        fileNameTextField = DialogueElementUtility.CreateTextField(defaultFileName, "File Name:", callback =>
        {
            fileNameTextField.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();
        });

        saveButton = DialogueElementUtility.CreateButton("Save", () => Save());
        Button loadButton = DialogueElementUtility.CreateButton("Load", () => Load());
        Button clearButton = DialogueElementUtility.CreateButton("Clear", () => Clear());

        toolbar.Add(fileNameTextField);
        toolbar.Add(saveButton);
        toolbar.Add(loadButton);
        toolbar.Add(clearButton);

        toolbar.AddStyleSheets("DialogueSystem/DialogueToolBarStyles.uss");
        rootVisualElement.Add(toolbar);
    }


    #region Toolbar Methods
    private void Save()
    {
        if (string.IsNullOrEmpty(fileNameTextField.value))
        {
            EditorUtility.DisplayDialog("Invalid file name", "Please enter a valid file name", "Ok");
            return;
        }

        DialogueIOUtility.Initialize(graphView, fileNameTextField.value);
        DialogueIOUtility.Save();
    }

    private void Load()
    {
        string path = EditorUtility.OpenFilePanel("Dialogue Graphs", "Assets/Editor/DialogueSystem/Graphs", "asset");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        Clear();
        DialogueIOUtility.Initialize(graphView, Path.GetFileNameWithoutExtension(path));
        DialogueIOUtility.Load();
    }

    private void Clear()
    {
        graphView.ClearGraph();
        fileNameTextField.value = defaultFileName;
    }
    #endregion

    private void AddStyles()
    {
        rootVisualElement.AddStyleSheets("DialogueSystem/DialogueVariables.uss");
    }

    private void AddGraphView()
    {
        graphView = new DialogueGraphView(this);
        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
    }

    public void EnableSaving()
    {
        saveButton.SetEnabled(true);
    }

    public void DisableSaving()
    {
        saveButton.SetEnabled(false);
    }
}
