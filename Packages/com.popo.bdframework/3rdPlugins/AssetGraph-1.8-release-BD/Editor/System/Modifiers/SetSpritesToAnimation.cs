using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine.AssetGraph;

[CustomModifier("Set Sprites to Animation", typeof(AnimationClip))]
public class SetSpritesToAnimation : IModifier {

    public enum AnimationType : int {
        SpriteAnimation,
        UIAnimation,
    }

    [SerializeField] int m_frameRate = 60;
    [SerializeField] bool m_loopTime;
    [SerializeField] AnimationType m_animType;

    public void OnValidate () {
    }

	// Test if asset is different from intended configuration 
	public bool IsModified (UnityEngine.Object[] assets, List<AssetReference> group) {

        var anim = assets.Where (a => a is AnimationClip).FirstOrDefault() as AnimationClip;

        return anim != null;
	}

	// Actually change asset configurations. 
	public void Modify (UnityEngine.Object[] assets, List<AssetReference> group) {

        var animClip = assets.Where (a => a is AnimationClip).FirstOrDefault() as AnimationClip;

        var spriteAssets = group.Where (a => a.importerType == typeof(TextureImporter)).OrderBy(a => a.fileName);

        List<Sprite> sprites = new List<Sprite>();

        foreach (var a in spriteAssets) {
            foreach (var o in a.allData) {
                var s = o as Sprite;
                if (s != null) {
                    sprites.Add (s);
                }
            }
        }
        animClip.frameRate = m_frameRate;

        var setting = AnimationUtility.GetAnimationClipSettings (animClip);
        setting.loopTime = m_loopTime;

        AnimationUtility.SetAnimationClipSettings (animClip, setting);

        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite"; 

        EditorCurveBinding uiBinding = new EditorCurveBinding();
        uiBinding.type = typeof(UnityEngine.UI.Image);
        uiBinding.path = "";
        uiBinding.propertyName = "m_Sprite"; 

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for(int i = 0; i < sprites.Count; i++) {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = i/(float)m_frameRate;
            keyframes[i].value = sprites[i];
        }

        if (m_animType == AnimationType.SpriteAnimation) {
            AnimationUtility.SetObjectReferenceCurve(animClip, spriteBinding, keyframes);
            AnimationUtility.SetObjectReferenceCurve(animClip, uiBinding, null);
        } else {
            AnimationUtility.SetObjectReferenceCurve(animClip, spriteBinding, null);
            AnimationUtility.SetObjectReferenceCurve(animClip, uiBinding, keyframes);
        }
	}

	// Draw inspector gui 
	public void OnInspectorGUI (Action onValueChanged) {

        bool valueChanged = false;

        var newType = (AnimationType)EditorGUILayout.EnumPopup ("Animation Type", m_animType);
        if (newType != m_animType) {
            m_animType = newType;
            valueChanged = true;
        }

        var newFR = EditorGUILayout.IntSlider ("Frame Rate", m_frameRate, 1, 60);

        if(newFR != m_frameRate) {
            m_frameRate = newFR;
            valueChanged = true;
		}

        var newLoopTime = EditorGUILayout.ToggleLeft ("Loop Time", m_loopTime);

        if(newLoopTime != m_loopTime) {
            m_loopTime = newLoopTime;
            valueChanged = true;
        }

        if (valueChanged) {
            onValueChanged();
        }
	}
}
