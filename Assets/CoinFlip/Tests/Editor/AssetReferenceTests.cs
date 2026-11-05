using System;
using CoinFlip;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AssetReferenceTests
{
    [Test] public void AllRequiredIllustrationsAndAudioLoad()
    {
        foreach(string name in new[]{"Heads","Tails","Rounded","Circle","Ring","Sparkle","Shadow","Aura","Sound","Check","Close"})
            Assert.IsNotNull(Resources.Load<Sprite>("Art/"+name),name);
        foreach(string cue in Enum.GetNames(typeof(AudioManager.Cue)))Assert.IsNotNull(Resources.Load<AudioClip>("Audio/"+cue),cue);
        Assert.IsNotNull(Resources.Load<AudioClip>("Audio/Music"));Assert.IsNotNull(Resources.Load<GameSettings>("GameSettings"));
    }
    [Test] public void PlayableSceneHasRequiredSerializedReferences()
    {
        const string scenePath="Assets/CoinFlip/Scenes/CoinFlip.unity";
        var scene=SceneManager.GetSceneByPath(scenePath);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
        try
        {
            GameManager manager=null;foreach(var root in scene.GetRootGameObjects()){manager=root.GetComponent<GameManager>();if(manager!=null)break;}
            Assert.IsNotNull(manager);var serialized=new SerializedObject(manager);
            Assert.IsNotNull(serialized.FindProperty("settings").objectReferenceValue,"GameSettings must survive scene creation");
            Assert.IsNotNull(serialized.FindProperty("font").objectReferenceValue,"Static font atlas must be bound");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
