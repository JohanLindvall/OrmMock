// Copyright(c) 2017, 2018 Johan Lindvall
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace OrmMock.EF6
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;

    using MemDb;

    using Shared;

    /// <summary>
    /// Implements a MemDbSet.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MemDbSet<T> : DbSet<T>, IDbSet<T>, IQueryable<T>, IEnumerable<T>, IQueryable, IEnumerable
        where T : class
    {
        /// <summary>
        /// Holds the memory database reference.
        /// </summary>
        private readonly IMemDb memDb;

        /// <summary>
        /// Initializes a new instance of the MemDbSet class.
        /// </summary>
        /// <param name="memDb">The memory db instance.</param>
        public MemDbSet(IMemDb memDb)
        {
            this.memDb = memDb;
            this.Local = new ObservableCollection<T>();

#pragma warning disable CS0219 // Variable is assigned but its value is never used
            var ignored = nameof(this.AddOrUpdate); // Dummy reference so that the method is not removed.
#pragma warning restore CS0219 // Variable is assigned but its value is never used
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => this.Queryable().GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <inheritdoc />
        public Expression Expression => this.Queryable().Expression;

        /// <inheritdoc />
        public Type ElementType => typeof(T);

        /// <inheritdoc />
        public IQueryProvider Provider => new MemDbAsyncQueryProvider<T>(this.Queryable().Provider);

        /// <inheritdoc />
        public override T Find(params object[] keyValues) => this.memDb.Get<T>(new Keys(keyValues));

        /// <inheritdoc />
        public override Task<T> FindAsync(CancellationToken cancellationToken, params object[] keyValues) => Task.FromResult(this.Find(keyValues));

        /// <inheritdoc />
        public override Task<T> FindAsync(params object[] keyValues) => Task.FromResult(this.Find(keyValues));

        /// <inheritdoc />
        public override T Add(T entity)
        {
            this.memDb.Add(entity);

            return entity;
        }

        /// <inheritdoc />
        public override T Remove(T entity)
        {
            this.memDb.Remove(entity);

            return entity;
        }

        /// <inheritdoc />
        public override T Attach(T entity) => entity; // no-op.

        /// <inheritdoc />
        public override T Create() => this.Create<T>();

        /// <inheritdoc />
        public override TDerivedEntity Create<TDerivedEntity>()
        {
            return this.memDb.Create<TDerivedEntity>();
        }

        /// <inheritdoc />
        public override ObservableCollection<T> Local { get; }

        /// <summary>
        /// Seeds initial data. Called by reflection. Do not remove.
        /// </summary>
        /// <param name="values">The initial seed values.</param>
        public void AddOrUpdate(params T[] values)
        {
            foreach (var remove in this.memDb.Get<T>().ToList())
            {
                this.memDb.Remove(remove);
            }

            this.memDb.Commit();

            foreach (var add in values)
            {
                this.Add(add);
            }

            this.memDb.Commit();
        }

        /// <summary>
        /// Gets a queryable for the current set type.
        /// </summary>
        /// <returns>A queryable for the current set type.</returns>
        private IQueryable<T> Queryable() => new MemDbAsyncEnumerable<T>(this.memDb.Get<T>());
    }

    // See https://github.com/rowanmiller/EntityFramework.Testing/tree/master/src/EntityFramework.Testing
    // https://www.andrewhoefling.com/Home/post/moq-entity-framework-dbset

    internal class MemDbAsyncQueryProvider<TEntity> : IDbAsyncQueryProvider
    {
        private readonly IQueryProvider inner;

        internal MemDbAsyncQueryProvider(IQueryProvider inner)
        {
            this.inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new MemDbAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new MemDbAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return this.inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return this.inner.Execute<TResult>(expression);
        }

        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.Execute(expression));
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.Execute<TResult>(expression));
        }
    }

    internal class MemDbAsyncEnumerable<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
    {
        public MemDbAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public MemDbAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IDbAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new MemDbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
        {
            return this.GetAsyncEnumerator();
        }

        IQueryProvider IQueryable.Provider => new MemDbAsyncQueryProvider<T>(this);
    }

    internal class MemDbAsyncEnumerator<T> : IDbAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> inner;

        public MemDbAsyncEnumerator(IEnumerator<T> inner)
        {
            this.inner = inner;
        }

        public void Dispose()
        {
            this.inner.Dispose();
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this.inner.MoveNext());
        }

        public T Current => this.inner.Current;

        object IDbAsyncEnumerator.Current => this.Current;
    }
}
