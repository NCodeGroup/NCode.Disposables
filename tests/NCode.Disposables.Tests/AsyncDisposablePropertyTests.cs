// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using FsCheck.Xunit;

namespace NCode.Disposables.Tests;

public class AsyncDisposablePropertyTests
{
    private const string Seed = "20261125,12345";

    #region Collection Tests

    [Property(Replay = Seed)]
    public async Task<bool> Collection_GivenAnyItems_DisposesEachOnceInReverseOrder(byte count)
    {
        var disposed = new List<int>();
        var collection = AsyncDisposable.Collection();
        for (var index = 0; index < count; index++)
        {
            var item = index;
            var disposable = AsyncDisposable.CreateRepeatable(() => disposed.Add(item));
            collection.Add(disposable);
        }
        var expected = Enumerable.Range(0, count).Reverse();

        await collection.DisposeAsync();
        await collection.DisposeAsync();

        return disposed.SequenceEqual(expected);
    }

    [Property(Replay = Seed)]
    public async Task<bool> Collection_GivenItemsThatThrow_DisposesEveryItemInReverseOrder(
        bool[] throws
    )
    {
        var disposed = new List<int>();
        var collection = CreateCollection(throws, disposed);
        var expected = Enumerable.Range(0, throws.Length).Reverse();

        await Record.ExceptionAsync(async () => await collection.DisposeAsync());

        return disposed.SequenceEqual(expected);
    }

    [Property(Replay = Seed)]
    public async Task<bool> Collection_GivenItemsThatThrow_ThrowsTheirExceptionsInDisposalOrder(
        bool[] throws
    )
    {
        var collection = CreateCollection(throws, []);
        var expected = ThrowingItemsInDisposalOrder(throws);

        var thrown = await Record.ExceptionAsync(async () => await collection.DisposeAsync());

        return MessagesOf(thrown).SequenceEqual(expected);
    }

    [Property(Replay = Seed)]
    public async Task<bool> Collection_GivenItemsThatThrow_ThrowsAnAggregateExceptionOnlyForSeveral(
        bool[] throws
    )
    {
        var collection = CreateCollection(throws, []);
        var throwingCount = throws.Count(shouldThrow => shouldThrow);

        var thrown = await Record.ExceptionAsync(async () => await collection.DisposeAsync());
        var isAggregate = thrown is AggregateException;

        return isAggregate == throwingCount > 1;
    }

    #endregion

    #region Create Tests

    [Property(Replay = Seed)]
    public async Task<bool> Create_GivenAnyNumberOfDisposeCalls_RunsTheActionOnce(byte extraCalls)
    {
        var count = 0;
        var disposable = AsyncDisposable.Create(() => count++);

        for (var call = 0; call <= extraCalls; call++)
        {
            await disposable.DisposeAsync();
        }

        return count == 1;
    }

    #endregion

    #region CreateRepeatable Tests

    [Property(Replay = Seed)]
    public async Task<bool> CreateRepeatable_GivenAnyNumberOfDisposeCalls_RunsTheActionEachTime(
        byte calls
    )
    {
        var count = 0;
        var disposable = AsyncDisposable.CreateRepeatable(() => count++);

        for (var call = 0; call < calls; call++)
        {
            await disposable.DisposeAsync();
        }

        return count == calls;
    }

    #endregion

    #region Serial Tests

    [Property(Replay = Seed)]
    public async Task<bool> Serial_GivenAnySequenceOfResources_DisposesEachOnceInTheOrderItIsReplaced(
        byte count
    )
    {
        var disposed = new List<int>();
        var serial = AsyncDisposable.Serial();
        for (var index = 0; index < count; index++)
        {
            var item = index;
            var disposable = AsyncDisposable.CreateRepeatable(() => disposed.Add(item));
            await serial.SetDisposableAsync(disposable);
        }
        var expected = Enumerable.Range(0, count);

        await serial.DisposeAsync();
        await serial.DisposeAsync();

        return disposed.SequenceEqual(expected);
    }

    #endregion

    #region Shared Tests

    [Property(Replay = Seed)]
    public async Task<bool> Shared_GivenAnyNumberOfLeases_ReleasesTheValueOnceAfterTheLastLease(
        byte extraLeases
    )
    {
        var released = 0;
        var value = new object();
        var lease = AsyncDisposable.Shared(value, _ =>
        {
            released++;
            return ValueTask.CompletedTask;
        });
        List<AsyncSharedReferenceLease<object>> otherLeases = [];
        for (var index = 0; index < extraLeases; index++)
        {
            var other = lease.AddReference();
            otherLeases.Add(other);
        }

        foreach (var other in otherLeases)
        {
            await other.DisposeAsync();
        }

        var releasedBeforeTheLast = released;
        await lease.DisposeAsync();

        return releasedBeforeTheLast == 0 && released == 1;
    }

    #endregion

    private static AsyncDisposableCollection CreateCollection(bool[] throws, List<int> disposed)
    {
        var collection = AsyncDisposable.Collection();
        for (var index = 0; index < throws.Length; index++)
        {
            var item = index;
            var shouldThrow = throws[index];
            var disposable = AsyncDisposable.Create(() =>
            {
                disposed.Add(item);
                if (shouldThrow)
                {
                    var message = item.ToString(CultureInfo.InvariantCulture);
                    throw new InvalidOperationException(message);
                }
            });
            collection.Add(disposable);
        }

        return collection;
    }

    private static List<string> ThrowingItemsInDisposalOrder(bool[] throws) =>
        Enumerable
            .Range(0, throws.Length)
            .Reverse()
            .Where(index => throws[index])
            .Select(index => index.ToString(CultureInfo.InvariantCulture))
            .ToList();

    private static List<string> MessagesOf(Exception? thrown) =>
        thrown switch
        {
            null => [],
            AggregateException aggregate =>
                aggregate.InnerExceptions.Select(exception => exception.Message).ToList(),
            _ => [thrown.Message],
        };
}
