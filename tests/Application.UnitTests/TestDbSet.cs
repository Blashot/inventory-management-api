using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Application.UnitTests;

/// <summary>
/// An in-memory DbSet that supports async LINQ operations (AsNoTracking, SingleOrDefaultAsync, Where, ToListAsync, Select).
/// Avoids NSubstitute context pollution from MockQueryable.
/// </summary>
internal sealed class TestDbSet<T> : DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>
    where T : class
{
    private readonly List<T> _data;

    internal TestDbSet(IEnumerable<T> data) => _data = data.ToList();

    /// <summary>All entities currently held by this in-memory set (including those added via <see cref="Add"/>).</summary>
    internal IReadOnlyList<T> Entities => _data.AsReadOnly();

    public override IEntityType EntityType => throw new NotSupportedException();

    IQueryProvider IQueryable.Provider =>
        new TestAsyncQueryProvider<T>(_data.AsQueryable().Provider);

    Expression IQueryable.Expression => _data.AsQueryable().Expression;

    Type IQueryable.ElementType => _data.AsQueryable().ElementType;

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _data.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _data.GetEnumerator();

    public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(_data.GetEnumerator());

    public override EntityEntry<T> Add(T entity)
    {
        _data.Add(entity);
        return null!; // Return value is not used in handlers
    }
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) =>
        new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object Execute(Expression expression) => inner.Execute(expression)!;

    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

    TResult IAsyncQueryProvider.ExecuteAsync<TResult>(
        Expression expression,
        CancellationToken cancellationToken)
    {
        Type resultType = typeof(TResult).GetGenericArguments()[0];
        object? executionResult = Execute(expression);
        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [executionResult])!;
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

