﻿using UnityEngine;
using System.Collections;

namespace Leap.Unity {
  public class HandDrop : HandTransitionBehavior {
    private Vector3 startingPalmPosition;
    private Quaternion startingOrientation;
    private Transform palm;

    // Use this for initialization
    protected override void Awake() {
      base.Awake();
      palm = GetComponent<HandModel>().palm;
      startingPalmPosition = palm.localPosition;
      startingOrientation = palm.localRotation;
    }

    protected override void HandFinish() {
      StartCoroutine(LerpToStart());
    }
    protected override void HandReset() {
      StopAllCoroutines();
    }

    private IEnumerator LerpToStart() {
      Vector3 droppedPosition = palm.localPosition;
      Quaternion droppedOrientation = palm.localRotation;
      float duration = 1.0f;
      float startTime = Time.time;
      float endTime = startTime + duration;

      while (Time.time <= endTime) {
        float t = (Time.time - startTime) / duration;
        palm.localPosition = Vector3.Lerp(droppedPosition, startingPalmPosition, t);
        palm.localRotation = Quaternion.Lerp(droppedOrientation, startingOrientation, t);
        yield return null;
      }
    }
  }
}
