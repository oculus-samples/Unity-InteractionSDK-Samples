// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapTargets : MonoBehaviour
{
    public enum TargetType { None, Dart, Plane, Frisbee, CornBag, BeerPong, Football, Bowling, Basketball }

    [System.Serializable]
    public class TargetConfig
    {
        public Transform target;
        public float activeScale = 1f;
    }

    public List<TargetConfig> targets = new List<TargetConfig>();
    public float defaultDuration = 0.5f;

    private TargetType _currentTarget;
    private Coroutine[] _activeCoroutines;

    void Start()
    {
        InitializeTargets();
        _activeCoroutines = new Coroutine[targets.Count];
    }

    void InitializeTargets()
    {
        foreach (var config in targets)
        {
            if (config.target != null)
            {
                config.target.localScale = Vector3.zero;
                config.target.gameObject.SetActive(false);
            }
        }
    }

    public void IsGrabbed(int index)
    {
        _currentTarget = (TargetType)index;
        StopAllActiveCoroutines();

        for (int i = 0; i < targets.Count; i++)
        {
            var config = targets[i];
            float targetScale = GetTargetScale(i);

            _activeCoroutines[i] = StartCoroutine(ScaleCoroutine(
                config.target,
                targetScale,
                defaultDuration,
                i == (int)_currentTarget - 1 // Only activate for current target
            ));
        }
    }

    float GetTargetScale(int index)
    {
        if (index == (int)_currentTarget - 1) return targets[index].activeScale;
        return 0f;
    }

    IEnumerator ScaleCoroutine(Transform target, float endValue, float duration, bool activate)
    {
        if (!target) yield break;

        // Set initial state
        target.gameObject.SetActive(activate);
        Vector3 start = target.localScale;
        Vector3 end = Vector3.one * endValue;
        float timeElapsed = 0;

        while (timeElapsed < duration)
        {
            target.localScale = Vector3.Lerp(start, end, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = end;

        // Ensure final state is correct
        if (!activate && target.localScale == Vector3.zero)
        {
            target.gameObject.SetActive(false);
        }
    }

    void StopAllActiveCoroutines()
    {
        foreach (var coroutine in _activeCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
    }
}
