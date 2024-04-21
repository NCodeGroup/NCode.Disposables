#region Copyright Preamble

// Copyright @ 2024 NCode Group
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

#endregion

namespace NCode.Disposables.Tests;

public class ImmutableCounterTests
{
    [Fact]
    public void Count_Default_Valid()
    {
        var counter = new ImmutableCounter();
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public void Count_Specific_Valid()
    {
        var count = Random.Shared.Next();
        var counter = new ImmutableCounter(count);
        Assert.Equal(count, counter.Count);
    }

    [Fact]
    public void Increment_Valid()
    {
        var count = Random.Shared.Next();
        var counter = new ImmutableCounter(count);
        var incremented = counter.Increment();
        Assert.Equal(count + 1, incremented.Count);
        Assert.Equal(count, counter.Count);
    }

    [Fact]
    public void Decrement_Valid()
    {
        var count = Random.Shared.Next();
        var counter = new ImmutableCounter(count);
        var decremented = counter.Decrement();
        Assert.Equal(count - 1, decremented.Count);
        Assert.Equal(count, counter.Count);
    }
}