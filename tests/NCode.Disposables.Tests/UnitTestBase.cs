// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public abstract class UnitTestBase : IDisposable
{
    private readonly MockRepository _mocks = new(MockBehavior.Strict);

    protected Mock<T> CreateStrictMock<T>(params object[] args)
        where T : class => _mocks.Create<T>(args);

    protected Mock<T> CreateLooseMock<T>(params object[] args)
        where T : class => _mocks.Create<T>(MockBehavior.Loose, args);

    protected Mock<T> CreatePartialMock<T>(params object[] args)
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
            _mocks.Verify();
    }
}
