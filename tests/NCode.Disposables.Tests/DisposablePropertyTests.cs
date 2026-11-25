// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using FsCheck.Xunit;

namespace NCode.Disposables.Tests;

public class DisposablePropertyTests
{
    private const string Seed = "20261125,12345";

    #region Collection Tests

    [Property(Replay = Seed)]
    public bool Collection_GivenAnyItems_DisposesEachOnceInReverseOrder(byte count)
    {
        var disposed = new List<int>();
        var collection = Disposable.Collection();
        for (var index = 0; index < count; index++)
        {
            var item = index;
            var disposable = Disposable.CreateRepeatable(() => disposed.Add(item));
            collection.Add(disposable);
        }
        var expected = Enumerable.Range(0, count).Reverse();

        collection.Dispose();
        collection.Dispose();

        return disposed.SequenceEqual(expected);
    }

    [Property(Replay = Seed)]
    public bool Collection_GivenItemsThatThrow_DisposesEveryItemInReverseOrder(bool[] throws)
    {
        var disposed = new List<int>();
        var collection = CreateCollection(throws, disposed);
        var expected = Enumerable.Range(0, throws.Length).Reverse();

        Record.Exception(collection.Dispose);

        return disposed.SequenceEqual(expected);
    }

    [Property(Replay = Seed)]
    public bool Collection_GivenItemsThatThrow_ThrowsTheirExceptionsInDisposalOrder(bool[] throws)
    {
        var collection = CreateCollection(throws, []);
        var expected = ThrowingItemsInDisposalOrder(throws);

        var thrown = Record.Exception(collection.Dispose);

        var actual = MessagesOf(thrown);
        return actual.SequenceEqual(expected);
    }

    [Property(Replay = Seed)]
    public bool Collection_GivenItemsThatThrow_ThrowsAnAggregateExceptionOnlyForSeveral(
        bool[] throws
    )
    {
        var collection = CreateCollection(throws, []);
        var throwingCount = throws.Count(shouldThrow => shouldThrow);

        var thrown = Record.Exception(collection.Dispose);

        var isAggregate = thrown is AggregateException;
        return isAggregate == throwingCount > 1;
    }

    #endregion

    #region Create Tests

    [Property(Replay = Seed)]
    public bool Create_GivenAnyNumberOfDisposeCalls_RunsTheActionOnce(byte extraCalls)
    {
        var count = 0;
        var disposable = Disposable.Create(() => count++);

        for (var call = 0; call <= extraCalls; call++)
        {
            disposable.Dispose();
        }

        return count == 1;
    }

    #endregion

    #region CreateRepeatable Tests

    [Property(Replay = Seed)]
    public bool CreateRepeatable_GivenAnyNumberOfDisposeCalls_RunsTheActionEachTime(byte calls)
    {
        var count = 0;
        var disposable = Disposable.CreateRepeatable(() => count++);

        for (var call = 0; call < calls; call++)
        {
            disposable.Dispose();
        }

        return count == calls;
    }

    #endregion

    #region Serial Tests

    [Property(Replay = Seed)]
    public bool Serial_GivenAnySequenceOfResources_DisposesEachOnceInTheOrderItIsReplaced(
        byte count
    )
    {
        var disposed = new List<int>();
        var serial = Disposable.Serial();
        for (var index = 0; index < count; index++)
        {
            var item = index;
            var disposable = Disposable.CreateRepeatable(() => disposed.Add(item));
            serial.Disposable = disposable;
        }
        var expected = Enumerable.Range(0, count);

        serial.Dispose();
        serial.Dispose();

        return disposed.SequenceEqual(expected);
    }

    #endregion

    #region Shared Tests

    [Property(Replay = Seed)]
    public bool Shared_GivenAnyNumberOfLeases_ReleasesTheValueOnceAfterTheLastLease(
        byte extraLeases
    )
    {
        var released = 0;
        var value = new object();
        var lease = Disposable.Shared(value, _ => released++);
        List<SharedReferenceLease<object>> otherLeases = [];
        for (var index = 0; index < extraLeases; index++)
        {
            var other = lease.AddReference();
            otherLeases.Add(other);
        }

        foreach (var other in otherLeases)
        {
            other.Dispose();
        }

        var releasedBeforeTheLast = released;
        lease.Dispose();

        return releasedBeforeTheLast == 0 && released == 1;
    }

    #endregion

    private static DisposableCollection CreateCollection(bool[] throws, List<int> disposed)
    {
        var collection = Disposable.Collection();
        for (var index = 0; index < throws.Length; index++)
        {
            var item = index;
            var shouldThrow = throws[index];
            var disposable = Disposable.Create(() =>
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
            [
                .. aggregate.InnerExceptions.Select(exception => exception.Message),
            ],
            _ => [thrown.Message],
        };
}
