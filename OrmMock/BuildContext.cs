using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OrmMock
{
    public class BuildContext<T> : IBuildContext<T>
    {
        private readonly BuildContext<T> previous;

        private readonly ObjectContext objectContext;

        private readonly Action<T> modifier;

        public BuildContext(BuildContext<T> previous, Action<T> modifier)
        {
            this.previous = previous;
            this.modifier = modifier;
        }

        public BuildContext(ObjectContext objectContext)
        {
            this.objectContext = objectContext;
        }

        public IBuildContext<T> With<T2>(Expression<Func<T, T2>> e, T2 value)
        {
            var property = ExpressionUtility.GetPropertyInfo(e).Single();

            return new BuildContext<T>(this, v => property.SetMethod.Invoke(v, new object[] { value }));
        }


        public T Create()
        {
            var result = this.objectContext != null ? this.objectContext.Create<T>() : this.previous.Create();

            this.modifier?.Invoke(result);

            return result;
        }

        public IEnumerable<T> CreateMany(int count = 3)
        {
            while (count-- > 0)
            {
                yield return this.Create();
            }
        }
    }
}
