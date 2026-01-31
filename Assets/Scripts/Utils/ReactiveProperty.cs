using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A minimal ReactiveProperty implementation to avoid external dependencies like UniRx/R3.
/// Allows UI to subscribe to data changes.
/// </summary>
public class ReactiveProperty<T>
{
    private T _value;
    private readonly EqualityComparer<T> _comparer;
    private event Action<T> _onValueChanged;

    public T Value
    {
        get => _value;
        set
        {
            if (!_comparer.Equals(_value, value))
            {
                _value = value;
                _onValueChanged?.Invoke(_value);
            }
        }
    }

    public ReactiveProperty(T initialValue = default)
    {
        _value = initialValue;
        _comparer = EqualityComparer<T>.Default;
    }

    public IDisposable Subscribe(Action<T> action)
    {
        action(_value); // Emit current value on subscribe
        _onValueChanged += action;
        return new Subscription(this, action);
    }

    private class Subscription : IDisposable
    {
        private ReactiveProperty<T> _source;
        private Action<T> _action;

        public Subscription(ReactiveProperty<T> source, Action<T> action)
        {
            _source = source;
            _action = action;
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source._onValueChanged -= _action;
                _source = null;
                _action = null;
            }
        }
    }
}
