// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public abstract class UnitTestBase : IDisposable
{
    private MockRepository Mocks { get; } = new(MockBehavior.Strict);

    protected Mock<T> CreateStrictMock<T>(params IEnumerable<object> args)
        where T : class => Mocks.Create<T>([.. args]);

    protected Mock<T> CreateLooseMock<T>(params IEnumerable<object> args)
        where T : class => Mocks.Create<T>(MockBehavior.Loose, [.. args]);

    protected Mock<T> CreatePartialMock<T>(params IEnumerable<object> args)
        where T : class
    {
        var mock = CreateLooseMock<T>(args);
        mock.CallBase = true;
        return mock;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            Mocks.Verify();
    }
}
