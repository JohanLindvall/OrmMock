using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OrmMock
{
    public interface IBuildContext<T>
    {
        IBuildContext<T> With<T2>(Expression<Func<T, T2>> e, T2 value);

        T Create();

        IEnumerable<T> CreateMany(int count = 3);
    }
}
