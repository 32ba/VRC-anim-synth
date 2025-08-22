using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnimSynth.Editor
{
  public class AnimSynthWindow : EditorWindow
  {
    private AnimationClip baseAnimation;
    private List<AnimationClip> targetAnimations = new List<AnimationClip>();
    private string outputDirectory = "Assets";
    private string prefix = "";
    private string suffix = "_synth";
    private Vector2 scrollPosition;
    private ReorderableList reorderableList;

    private void OnEnable()
    {
      reorderableList = new ReorderableList(targetAnimations, typeof(AnimationClip), true, true, true, true);
      reorderableList.drawHeaderCallback = (Rect rect) =>
      {
        EditorGUI.LabelField(rect, "Target Animations");
      };
      reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
      {
        rect.y += 2;
        rect.height = EditorGUIUtility.singleLineHeight;
        targetAnimations[index] = (AnimationClip)EditorGUI.ObjectField(
          rect,
          targetAnimations[index],
          typeof(AnimationClip),
          false
        );
      };
      reorderableList.onAddCallback = (ReorderableList list) =>
      {
        targetAnimations.Add(null);
      };
      reorderableList.onRemoveCallback = (ReorderableList list) =>
      {
        targetAnimations.RemoveAt(list.index);
      };

      // Add drag and drop support to the ReorderableList
      reorderableList.drawNoneElementCallback = (Rect rect) =>
      {
        EditorGUI.LabelField(rect, "Drag & Drop Animations Here");
        HandleDragAndDrop(rect);
      };

      reorderableList.drawFooterCallback = (Rect rect) =>
      {
        ReorderableList.defaultBehaviours.DrawFooter(rect, reorderableList);
        HandleDragAndDrop(rect);
      };
    }

    private void HandleDragAndDrop(Rect rect)
    {
      Event evt = Event.current;
      switch (evt.type)
      {
        case EventType.DragUpdated:
        case EventType.DragPerform:
          if (!rect.Contains(evt.mousePosition))
            break;

          DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

          if (evt.type == EventType.DragPerform)
          {
            DragAndDrop.AcceptDrag();

            foreach (var draggedObject in DragAndDrop.objectReferences)
            {
              if (draggedObject is AnimationClip clip)
              {
                targetAnimations.Add(clip);
              }
            }
            Repaint();
          }
          evt.Use();
          break;
      }
    }

    [MenuItem("Window/VRC Anim Synth")]
    public static void ShowWindow()
    {
      GetWindow<AnimSynthWindow>("VRC Anim Synth");
    }

    private void OnGUI()
    {
      scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

      EditorGUILayout.Space(10);
      DrawBaseAnimationField();
      EditorGUILayout.Space(10);
      DrawTargetAnimationsField();
      EditorGUILayout.Space(10);
      DrawOutputSettings();
      EditorGUILayout.Space(10);
      DrawSynthButton();

      EditorGUILayout.EndScrollView();
    }

    private void DrawBaseAnimationField()
    {
      EditorGUILayout.LabelField("Base Animation", EditorStyles.boldLabel);
      baseAnimation = (AnimationClip)EditorGUILayout.ObjectField(
        baseAnimation,
        typeof(AnimationClip),
        false
      );
    }

    private void DrawTargetAnimationsField()
    {
      EditorGUILayout.LabelField("Target Animations", EditorStyles.boldLabel);
      reorderableList.DoLayoutList();
    }

    private void DrawOutputSettings()
    {
      EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);

      // Output directory selection
      if (GUILayout.Button("Select Output Directory"))
      {
        string selectedPath = EditorUtility.OpenFolderPanel("Select Output Directory", "Assets", "");
        if (!string.IsNullOrEmpty(selectedPath))
        {
          // Convert to project relative path
          if (selectedPath.StartsWith(Application.dataPath))
          {
            outputDirectory = "Assets" + selectedPath.Substring(Application.dataPath.Length);
          }
        }
      }
      EditorGUILayout.LabelField("Current Output Directory:", outputDirectory);

      // Prefix and suffix settings
      prefix = EditorGUILayout.TextField("Prefix", prefix);
      suffix = EditorGUILayout.TextField("Suffix", suffix);
    }

    private void DrawSynthButton()
    {
      GUI.enabled = IsValid();
      if (GUILayout.Button("Synthesize Animations"))
      {
        SynthAnimations();
      }
      GUI.enabled = true;

      if (!IsValid())
      {
        EditorGUILayout.HelpBox("Please select a base animation and at least one target animation.", MessageType.Warning);
      }
    }

    private bool IsValid()
    {
      return baseAnimation != null && targetAnimations.Count > 0 && targetAnimations.Exists(a => a != null);
    }

    private void SynthAnimations()
    {
      if (!IsValid()) return;

      // Remove null animations from the list
      var validAnimations = targetAnimations.Where(a => a != null).ToList();

      // Synthesize each target animation separately
      foreach (var targetAnim in validAnimations)
      {
        string outputPath = Path.Combine(
          outputDirectory,
          $"{prefix}{targetAnim.name}{suffix}.anim"
        );

        var newClip = AnimationSynthesizer.Synthesize(
          baseAnimation,
          new List<AnimationClip> { targetAnim },
          outputPath,
          prefix,
          suffix
        );

        if (newClip != null)
        {
          Debug.Log($"Animation synthesis completed: {outputPath}");
          Selection.activeObject = newClip;
        }
      }
    }
  }
}
