using System.ComponentModel;

namespace ThunderboltIoc;

/// <summary>
/// This is for source generator purposes. Do not make use of that.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Browsable(false)]
public readonly struct PrivateType
{
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    public readonly Type type;
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    public readonly Func<IServiceProvider, object>? factory;
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    public readonly ThunderboltServiceLifetime? registerLifetime;
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    public readonly Func<Type[], Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object>>? dictateFactory;

    private PrivateType(
        Type type,
        Func<IServiceProvider, object>? factory,
        ThunderboltServiceLifetime? registerLifetime,
        Func<Type[], Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object>>? dictateFactory)
    {
        this.type = type;
        this.factory = factory;

        this.registerLifetime = registerLifetime;
        this.dictateFactory = dictateFactory;
    }
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    public PrivateType(
        Type type,
        Func<IServiceProvider, object>? factory)
        : this(type, factory, null, null)
    {
    }

    //In C# 9, 'with expression' is supported for records only. It's available for structs since C# 10 only.
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    public PrivateType WithInitialization(ThunderboltServiceLifetime? registerLifetime, Func<Type[], Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object>> dictateFactory)
    {
        return new PrivateType(type, factory, registerLifetime, dictateFactory);
    }
}
