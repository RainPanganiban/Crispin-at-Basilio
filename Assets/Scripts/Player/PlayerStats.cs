using UnityEngine;
using System;

[System.Serializable]
public class Stat
{
    public string name;
    public float maxValue = 100f;
    public float currentValue;

    public event Action<float, float> OnValueChanged; // current, max

    public Stat(string statName, float max)
    {
        name = statName;
        maxValue = max;
        currentValue = max;
    }

    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, 0, maxValue);
        OnValueChanged?.Invoke(currentValue, maxValue);
    }

    public void ChangeValue(float delta)
    {
        SetValue(currentValue + delta);
    }

    public float GetPercent()
    {
        return currentValue / maxValue;
    }
}
