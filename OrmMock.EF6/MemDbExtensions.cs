using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrmMock.EF6
{
    public static class MemDbExtensions
    {
        public static MemDbSet<T> DbSet<T>(this MemDb.MemDb memDb)
        where T : class
        {
            return new MemDbSet<T>(memDb);
        }
    }
}

//IDbSet<T>, IDbAsyncEnumerable<T>