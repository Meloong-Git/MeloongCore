namespace MeloongCore;
public readonly struct OneOf<T0, T1> {
    private readonly int _index;
    private readonly T0? _value0;
    private readonly T1? _value1;
    public static implicit operator OneOf<T0, T1>(T0 value) => new(0, value, default);
    public static implicit operator OneOf<T0, T1>(T1 value) => new(1, default, value);
    private OneOf(int index, T0? value0, T1? value1) {
        _index = index;
        _value0 = value0;
        _value1 = value1;
    }

    /// <summary>
    /// 对不同类型执行不同的操作，并返回同一个值。
    /// </summary>
    public TResult Switch<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1) => _index switch {
        0 => f0(_value0!), _ => f1(_value1!)
    };
    /// <summary>
    /// 对不同类型执行不同的操作。
    /// </summary>
    public void Switch(Action<T0> f0, Action<T1> f1) {
        if (_index == 0) f0(_value0!);
        else if (_index == 1) f1(_value1!);
    }
    /// <summary>
    /// 判断当前的类型是否为 T。
    /// </summary>
    public bool Is<T>() => _index switch {
        0 when typeof(T) == typeof(T0) => true, 
        1 when typeof(T) == typeof(T1) => true, 
        _ => false
    };
    /// <summary>
    /// 假定当前的类型为 T，并返回该值。
    /// 若当前值的类型不为 T，则抛出 InvalidOperationException。
    /// </summary>
    public T As<T>() => _index switch {
        0 when typeof(T) == typeof(T0) => (T)(object)_value0!, 
        1 when typeof(T) == typeof(T1) => (T)(object)_value1!, 
        _ => throw new InvalidOperationException()
    };
}

public readonly struct OneOf<T0, T1, T2> {
    private readonly int _index;
    private readonly T0? _value0;
    private readonly T1? _value1;
    private readonly T2? _value2;
    public static implicit operator OneOf<T0, T1, T2>(T0 value) => new(0, value, default, default);
    public static implicit operator OneOf<T0, T1, T2>(T1 value) => new(1, default, value, default);
    public static implicit operator OneOf<T0, T1, T2>(T2 value) => new(2, default, default, value);
    private OneOf(int index, T0? value0, T1? value1, T2? value2) {
        _index = index;
        _value0 = value0;
        _value1 = value1;
        _value2 = value2;
    }

    /// <summary>
    /// 对不同类型执行不同的操作，并返回同一个值。
    /// </summary>
    public TResult Switch<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1, Func<T2, TResult> f2) => _index switch {
        0 => f0(_value0!), 1 => f1(_value1!), _ => f2(_value2!)
    };
    /// <summary>
    /// 对不同类型执行不同的操作。
    /// </summary>
    public void Switch(Action<T0> f0, Action<T1> f1, Action<T2> f2) {
        if (_index == 0) f0(_value0!);
        else if (_index == 1) f1(_value1!);
        else if (_index == 2) f2(_value2!);
    }
    /// <summary>
    /// 判断当前的类型是否为 T。
    /// </summary>
    public bool Is<T>() => _index switch {
        0 when typeof(T) == typeof(T0) => true,
        1 when typeof(T) == typeof(T1) => true,
        2 when typeof(T) == typeof(T2) => true,
        _ => false
    };
    /// <summary>
    /// 假定当前的类型为 T，并返回该值。
    /// 若当前值的类型不为 T，则抛出 InvalidOperationException。
    /// </summary>
    public T As<T>() => _index switch {
        0 when typeof(T) == typeof(T0) => (T) (object) _value0!,
        1 when typeof(T) == typeof(T1) => (T) (object) _value1!,
        2 when typeof(T) == typeof(T2) => (T) (object) _value2!,
        _ => throw new InvalidOperationException()
    };
}
