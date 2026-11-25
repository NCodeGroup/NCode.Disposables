// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Guards a disposable value so that it is disposed unless ownership is transferred to the caller
/// by calling <see cref="Release"/>.
/// </summary>
/// <typeparam name="T">The type of the guarded value.</typeparam>
/// <remarks>
/// <para>
/// Disposing the guard disposes the value, unless <see cref="Release"/> has been called first.
/// Calling <see cref="Release"/> transfers ownership of the value to the caller, and disposing the
/// guard afterward does nothing to the value.
/// </para>
/// <para>
/// The guard is a class so that <c>using var guard</c> and a later call to <see cref="Release"/>
/// operate on the same instance. All operations are thread-safe using lock-free atomic operations.
/// </para>
/// </remarks>
public sealed class DisposableGuard<T> : IDisposable, IDisposalStatus
    where T : IDisposable
{
    private const int Owned = 0;
    private const int Released = 1;
    private const int Disposed = 2;
    private const int ReleasedAndDisposed = 3;

    private const string AlreadyReleasedMessage =
        "Ownership of the resource has already been released.";

    private int _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableGuard{T}"/> class.
    /// </summary>
    /// <param name="value">The value to guard.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public DisposableGuard(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Value = value;
    }

    /// <summary>
    /// Gets the guarded value.
    /// </summary>
    /// <value>The value that is disposed with the guard unless ownership has been released.</value>
    public T Value { get; }

    /// <summary>
    /// Gets a value indicating whether ownership of the value has been released to the caller.
    /// </summary>
    /// <value>
    /// <see langword="true"/> once <see cref="Release"/> has succeeded; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsReleased => Volatile.Read(ref _state) is Released or ReleasedAndDisposed;

    /// <inheritdoc />
    public bool IsDisposed => Volatile.Read(ref _state) is Disposed or ReleasedAndDisposed;

    /// <summary>
    /// Transfers ownership of the value to the caller so that disposing the guard does not dispose the value.
    /// </summary>
    /// <returns>The guarded value, which the caller is now responsible for disposing.</returns>
    /// <exception cref="ObjectDisposedException">The guard has been disposed and the value was disposed with it.</exception>
    /// <exception cref="InvalidOperationException">Ownership has already been released.</exception>
    public T Release()
    {
        var state = Interlocked.CompareExchange(ref _state, Released, Owned);
        if (state == Owned)
        {
            return Value;
        }

        ObjectDisposedException.ThrowIf(state == Disposed, this);

        throw new InvalidOperationException(AlreadyReleasedMessage);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Disposes the value unless ownership has been released. Calling this method more than once has no further effect.
    /// </remarks>
    public void Dispose()
    {
        var spinWait = new SpinWait();
        while (true)
        {
            var state = Volatile.Read(ref _state);
            switch (state)
            {
                case Owned:
                    if (Interlocked.CompareExchange(ref _state, Disposed, Owned) == Owned)
                    {
                        Value.Dispose();
                        return;
                    }

                    break;

                case Released:
                    if (
                        Interlocked.CompareExchange(ref _state, ReleasedAndDisposed, Released)
                        == Released
                    )
                    {
                        return;
                    }

                    break;

                default:
                    return;
            }

            spinWait.SpinOnce();
        }
    }
}
