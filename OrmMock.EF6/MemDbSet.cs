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

    public class MemDbSet<T> : IDbSet<T>
        where T : class
    {
        private readonly IMemDb memDb;

        public MemDbSet(IMemDb memDb)
        {
            this.memDb = memDb;
            this.Local = new ObservableCollection<T>();

#pragma warning disable CS0219 // Variable is assigned but its value is never used
            var ignored = nameof(this.AddOrUpdate); // Dummy reference so that the method is not removed.
#pragma warning restore CS0219 // Variable is assigned but its value is never used
        }

        private IQueryable<T> Queryable() => new MemDbAsyncEnumerable<T>(this.memDb.Get<T>());

        public IEnumerator<T> GetEnumerator() => this.Queryable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public Expression Expression => this.Queryable().Expression;

        public Type ElementType => typeof(T);

        public IQueryProvider Provider => new MemDbAsyncQueryProvider<T>(this.Queryable().Provider);

        public T Find(params object[] keyValues) => this.memDb.Get<T>(new KeyHolder(keyValues));

        public T Add(T entity)
        {
            this.memDb.Add(entity);

            return entity;
        }

        public T Remove(T entity)
        {
            this.memDb.Remove(entity);

            return entity;
        }

        public T Attach(T entity)
        {
            throw new NotImplementedException();
        }

        public T Create()
        {
            throw new NotImplementedException();
        }

        public TDerivedEntity Create<TDerivedEntity>() where TDerivedEntity : class, T
        {
            throw new NotImplementedException();
        }

        public ObservableCollection<T> Local { get; }

        /// <summary>
        /// Seeds initial data. Called by reflection. Do not remove.
        /// </summary>
        /// <param name="values">The initial seed values.</param>
        public void AddOrUpdate(params T[] values)
        {
            foreach (var remove in this.Queryable().ToList())
            {
                this.memDb.Remove(remove);
            }

            this.memDb.Commit();

            foreach (var add in values)
            {
                this.Add(add);
            }
        }
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
