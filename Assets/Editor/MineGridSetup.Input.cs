using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public static partial class MineGridSetup
{
    private static void BuildEventSystem()
    {
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (!actions)
        {
            throw new System.InvalidOperationException("The project UI input actions are missing.");
        }

        // Configure while inactive so the module does not create disposable default actions.
        var events = new GameObject("EventSystem");
        events.SetActive(false);
        events.AddComponent<EventSystem>();
        var module = events.AddComponent<InputSystemUIInputModule>();
        module.actionsAsset = actions;
        module.point = InputReference(actions, "Point");
        module.leftClick = InputReference(actions, "Click");
        module.rightClick = InputReference(actions, "RightClick");
        module.middleClick = InputReference(actions, "MiddleClick");
        module.scrollWheel = InputReference(actions, "ScrollWheel");
        module.move = InputReference(actions, "Navigate");
        module.submit = InputReference(actions, "Submit");
        module.cancel = InputReference(actions, "Cancel");
        module.trackedDevicePosition = InputReference(actions, "TrackedDevicePosition");
        module.trackedDeviceOrientation = InputReference(actions, "TrackedDeviceOrientation");
        events.SetActive(true);
    }

    private static InputActionReference InputReference(InputActionAsset actions, string name)
    {
        Directory.CreateDirectory(Root + "/Input");
        string path = Root + "/Input/UI_" + name + ".asset";
        var reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
        if (!reference)
        {
            reference = InputActionReference.Create(actions.FindAction("UI/" + name, true));
            AssetDatabase.CreateAsset(reference, path);
        }
        else
        {
            reference.Set(actions.FindAction("UI/" + name, true));
        }

        EditorUtility.SetDirty(reference);
        return reference;
    }
}
