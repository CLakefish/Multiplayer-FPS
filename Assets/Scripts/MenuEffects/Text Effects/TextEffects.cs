using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TextEffects : MonoBehaviour
{
    #if UNITY_EDITOR

    [CustomEditor(typeof(TextEffects))]
    private class TextEffectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }

    #endif

    [System.Serializable]
    private enum WordEffect { Wiggle, Shake, Distort }

    [Header("References")]
    private TMPro.TMP_Text text;

    [Header("Parameters")]
    [SerializeField] private WordEffect effect;
    [SerializeField] private float speed = 2,
                                   waveWidth = 0.005f,
                                   intensity = 10;

    private void Awake() => text = GetComponent<TMPro.TMP_Text>();

    private void Update()
    {
        if (text == null || string.IsNullOrEmpty(text.text)) return;

        text.ForceMeshUpdate();

        switch (effect)
        {
            case WordEffect.Wiggle:
                Wiggle();
                break;
        }

        for (int i = 0; i < text.textInfo.meshInfo.Length; i++)
        {
            var meshInfo = text.textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            meshInfo.mesh.colors32 = meshInfo.colors32;
            text.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    private void Wiggle()
    {
        for (int i = 0; i < text.text.Length; i++)
        {
            var charInfo = text.textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;
            var verts = text.textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;

            for (int j = 0; j < 4; j++)
            {
                var orig = verts[charInfo.vertexIndex + j];
                verts[charInfo.vertexIndex + j] = orig + new Vector3(0, Mathf.Sin(Time.time * speed + orig.x * waveWidth) * intensity, 0);
            }
        }
    }
}
