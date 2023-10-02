using UnityEngine;

public class Property<T>
{
    private T __value;
    public T Value
    {
        get
        {
            return __value;
        }
        set
        {
            onValueChanged?.Invoke(__value, value);
            __value = value;
        }
    }
    public event System.Action<T, T> onValueChanged;

    public Property(T initialValue)
    {
        __value = initialValue;
    }
}
