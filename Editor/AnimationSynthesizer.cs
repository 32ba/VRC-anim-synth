using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnimSynth.Editor
{
  public class AnimationSynthesizer
  {
    public static AnimationClip Synthesize(AnimationClip baseAnimation, List<AnimationClip> targetAnimations, string outputPath, string prefix = "", string suffix = "_synth")
    {
      if (baseAnimation == null)
      {
        Debug.LogError("Base animation is not specified.");
        return null;
      }

      if (targetAnimations == null || targetAnimations.Count == 0)
      {
        Debug.LogError("Target animations are not specified.");
        return null;
      }

      // Create new AnimationClip
      var newClip = new AnimationClip();
      newClip.name = $"{prefix}{targetAnimations[0].name}{suffix}";

      // Get curves from base animation
      var baseBindings = AnimationUtility.GetCurveBindings(baseAnimation);
      var blendShapeData = new Dictionary<string, (float value, string path)>();

      // Get blendshape values and paths from base animation
      foreach (var binding in baseBindings)
      {
        if (binding.propertyName.StartsWith("blendShape."))
        {
          var curve = AnimationUtility.GetEditorCurve(baseAnimation, binding);
          if (curve != null && curve.keys.Length > 0)
          {
            blendShapeData[binding.propertyName] = (curve.keys[0].value, binding.path);
          }
        }
      }

      // Process target animation
      var targetAnim = targetAnimations[0];
      var targetBindings = AnimationUtility.GetCurveBindings(targetAnim);
      foreach (var binding in targetBindings)
      {
        if (!binding.propertyName.StartsWith("blendShape.")) continue;

        var curve = AnimationUtility.GetEditorCurve(targetAnim, binding);
        if (curve == null || curve.keys.Length == 0) continue;

        var value = curve.keys[0].value;
        var path = binding.path; // Get path from target animation as well

        // If the property doesn't exist in the dictionary, add it
        if (!blendShapeData.ContainsKey(binding.propertyName))
        {
          blendShapeData[binding.propertyName] = (value, path);
          continue;
        }

        // Compare and take the maximum value. Prioritize base animation's path.
        var existingData = blendShapeData[binding.propertyName];
        string finalPath = existingData.path; // Default to base animation's path
        if (string.IsNullOrEmpty(finalPath)) // If base path is empty, use target's path
        {
          finalPath = path;
        }

        if (value > existingData.value)
        {
          blendShapeData[binding.propertyName] = (value, finalPath);
        }
        else // If target value is not greater, still ensure path is set if it was empty
        {
          blendShapeData[binding.propertyName] = (existingData.value, finalPath);
        }
      }

      // Set curves to new animation clip (skip zero values)
      foreach (var kvp in blendShapeData)
      {
        if (Mathf.Approximately(kvp.Value.value, 0f)) continue;

        var curve = new AnimationCurve(new Keyframe(0, kvp.Value.value));
        var newBinding = new EditorCurveBinding
        {
          propertyName = kvp.Key,
          type = typeof(SkinnedMeshRenderer),
          path = kvp.Value.path // Use the stored path
        };
        AnimationUtility.SetEditorCurve(newClip, newBinding, curve);
      }

      // Debug log for verification
      Debug.Log($"Animation synthesis details for {newClip.name}:");
      Debug.Log($"Total blendshapes: {blendShapeData.Count}");
      Debug.Log($"Non-zero blendshapes: {blendShapeData.Count(x => !Mathf.Approximately(x.Value.value, 0f))}");

      // Save animation clip
      if (!string.IsNullOrEmpty(outputPath))
      {
        var directory = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        AssetDatabase.CreateAsset(newClip, outputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
      }

      return newClip;
    }
  }
}
