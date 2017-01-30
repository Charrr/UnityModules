﻿using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LeapGuiFeatureNameAttribute : Attribute {
  private static Dictionary<Type, string> _featureNameCache = new Dictionary<Type, string>();

  public readonly string featureName;

  public LeapGuiFeatureNameAttribute(string featureName) {
    this.featureName = featureName;
  }

  public static string GetFeatureName(Type type) {
    string featureName;
    if (!_featureNameCache.TryGetValue(type, out featureName)) {
      object[] attributes = type.GetCustomAttributes(typeof(LeapGuiFeatureNameAttribute), inherit: true);
      if (attributes.Length == 1) {
        featureName = (attributes[0] as LeapGuiFeatureNameAttribute).featureName;
      } else {
        featureName = type.Name;
      }
      _featureNameCache[type] = featureName;
    }

    return featureName;
  }
}

public abstract class LeapGuiFeatureBase : ScriptableObject {
  public abstract void ClearDataObjectReferences();
  public abstract void AddDataObjectReference(LeapGuiElementData data);

  public abstract LeapGuiElementData CreateDataObject(LeapGuiElement element);

#if UNITY_EDITOR
  public abstract void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused);
  public abstract float GetEditorHeight();
#endif
}

public abstract class LeapGuiElementData : ScriptableObject {
  [HideInInspector]
  public LeapGuiElement element;
  [HideInInspector]
  public LeapGuiFeatureBase feature;
}

public abstract class LeapGuiFeature<DataType> : LeapGuiFeatureBase
  where DataType : LeapGuiElementData {

  /// <summary>
  /// A list of all element data object.
  /// </summary>
  [HideInInspector]
  public List<DataType> data = new List<DataType>();

  public override void ClearDataObjectReferences() {
    data.Clear();
  }

  public override void AddDataObjectReference(LeapGuiElementData data) {
    this.data.Add(data as DataType);
  }

  public override LeapGuiElementData CreateDataObject(LeapGuiElement element) {
    var dataObj = ScriptableObject.CreateInstance<DataType>();
    dataObj.element = element;
    dataObj.feature = this;
    return dataObj;
  }
}

