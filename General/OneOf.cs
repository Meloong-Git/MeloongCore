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

    public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1) =>
        _index switch {0 => f0(_value0!), _ => f1(_value1!)};
    public void Switch(Action<T0> f0, Action<T1> f1) {
        if (_index == 0) f0(_value0!);
        else if (_index == 1) f1(_value1!);
    }
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

    public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1, Func<T2, TResult> f2) =>
        _index switch {0 => f0(_value0!), 1 => f1(_value1!), _ => f2(_value2!)};
    public void Switch(Action<T0> f0, Action<T1> f1, Action<T2> f2) {
        if (_index == 0) f0(_value0!);
        else if (_index == 1) f1(_value1!);
        else if (_index == 2) f2(_value2!);
    }
}
