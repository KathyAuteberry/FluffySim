using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

// Preserve the imported assertions and cases using the already-cached test runner.
namespace Xunit;
public class FactAttribute : TestAttribute { }
public class TheoryAttribute : Attribute { }
public class InlineDataAttribute(params object?[] args) : TestCaseAttribute(args) { }
public class MemberDataAttribute(string name) : TestCaseSourceAttribute(name) { }
public static class Assert
{
    public static void True(bool value, string? message = null) => NUnit.Framework.Assert.That(value, Is.True, message);
    public static void False(bool value, string? message = null) => NUnit.Framework.Assert.That(value, Is.False, message);
    public static void Null(object? value) => NUnit.Framework.Assert.That(value, Is.Null);
    public static void NotNull(object? value) => NUnit.Framework.Assert.That(value, Is.Not.Null);
    public static void Empty(IEnumerable value) => NUnit.Framework.Assert.That(value, Is.Empty);
    public static void NotEmpty(IEnumerable value) => NUnit.Framework.Assert.That(value, Is.Not.Empty);
    public static void Equal<T>(T expected, T actual) => NUnit.Framework.Assert.That(actual, Is.EqualTo(expected));
    public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual) => NUnit.Framework.Assert.That(actual, Is.EqualTo(expected));
    public static T Single<T>(IEnumerable<T> value) { var a = value.ToArray(); NUnit.Framework.Assert.That(a, Has.Length.EqualTo(1)); return a[0]; }
    public static void Contains(string expected, string? actual) => NUnit.Framework.Assert.That(actual, Does.Contain(expected));
    public static void Contains<T>(T expected, IEnumerable<T> actual) => NUnit.Framework.Assert.That(actual, Does.Contain(expected));
    public static void Contains<T>(IEnumerable<T> actual, Predicate<T> predicate) => True(actual.Any(v => predicate(v)));
    public static void DoesNotContain<T>(IEnumerable<T> actual, Predicate<T> predicate) => False(actual.Any(v => predicate(v)));
    public static void DoesNotContain<T>(T expected, IEnumerable<T> actual) => NUnit.Framework.Assert.That(actual, Does.Not.Contain(expected));
    public static void All<T>(IEnumerable<T> actual, Action<T> assertion) { foreach (var value in actual) assertion(value); }
    public static T Throws<T>(Action action) where T : Exception => NUnit.Framework.Assert.Throws<T>(() => action())!;
    public static T Throws<T>(Func<object?> action) where T : Exception => Throws<T>(() => { action(); });
    public static T ThrowsAny<T>(Action action) where T : Exception => (T)NUnit.Framework.Assert.Catch<T>(() => action())!;
    public static T ThrowsAny<T>(Func<object?> action) where T : Exception => ThrowsAny<T>(() => { action(); });
    public static void Fail(string message) => NUnit.Framework.Assert.Fail(message);
}

public class TheoryData<T> : List<object?[]> { public void Add(T value) => base.Add([value]); }
public class TheoryData<T1,T2> : List<object?[]> { public void Add(T1 first,T2 second) => base.Add([first,second]); }
