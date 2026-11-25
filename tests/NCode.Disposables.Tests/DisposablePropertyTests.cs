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
            collection.Add(Disposable.CreateRepeatable(() => disposed.Add(item)));
        }

        collection.Dispose();
        collection.Dispose();

        return disposed.SequenceEqual(Enumerable.Range(0, count).Reverse());
    }

    [Property(Replay = Seed)]
    public bool Collection_GivenItemsThatThrow_DisposesEveryItemAndThrowsWhatTheyThrew(
        bool[] throws
    )
    {
        var disposed = new List<int>();
        var collection = Disposable.Collection();
        for (var index = 0; index < throws.Length; index++)
        {
            var item = index;
            var shouldThrow = throws[index];
            collection.Add(
                Disposable.Create(() =>
                {
                    disposed.Add(item);
                    if (shouldThrow)
                    {
                        throw new InvalidOperationException(
                            item.ToString(CultureInfo.InvariantCulture)
                        );
                    }
                })
            );
        }

        var thrown = Record.Exception(collection.Dispose);

        var expected = Enumerable
            .Range(0, throws.Length)
            .Reverse()
            .Where(index => throws[index])
            .Select(index => index.ToString(CultureInfo.InvariantCulture))
            .ToList();
        List<string> actual = thrown switch
        {
            null => [],
            AggregateException aggregate => aggregate
                .InnerExceptions.Select(exception => exception.Message)
                .ToList(),
            _ => [thrown.Message],
        };
        var isAggregate = thrown is AggregateException;
        return disposed.SequenceEqual(Enumerable.Range(0, throws.Length).Reverse())
            && actual.SequenceEqual(expected)
            && isAggregate == expected.Count > 1;
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

        return count == 1 && disposable.IsDisposed;
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
            serial.Disposable = Disposable.CreateRepeatable(() => disposed.Add(item));
        }

        serial.Dispose();
        serial.Dispose();

        return disposed.SequenceEqual(Enumerable.Range(0, count));
    }

    #endregion

    #region Shared Tests

    [Property(Replay = Seed)]
    public bool Shared_GivenAnyNumberOfLeases_ReleasesTheValueOnceAfterTheLastLease(
        byte extraLeases
    )
    {
        var released = 0;
        var lease = Disposable.Shared(new object(), _ => released++);
        List<SharedReferenceLease<object>> leases = [lease];
        for (var index = 0; index < extraLeases; index++)
        {
            leases.Add(lease.AddReference());
        }

        foreach (var other in leases.Skip(1))
        {
            other.Dispose();
        }

        var releasedBeforeTheLast = released;
        lease.Dispose();

        return releasedBeforeTheLast == 0 && released == 1;
    }

    #endregion
}
