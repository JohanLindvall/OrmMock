using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrmMock
{
    public class KeyHolder
    {
        public KeyHolder(object[] keys)
        {
            this.Keys = keys;
        }

        public object[] Keys { get; }

        public bool Equals(KeyHolder other)
        {
            if (this.Keys.Length == other.Keys.Length)
            {
                for (var i = 0; i < this.Keys.Length; ++i)
                {
                    if (!this.Keys[i].Equals(other.Keys[i])) return false;
                }

                return true;
            }

            return false;
        }

        public override bool Equals(object other)
        {
            if (other is KeyHolder k)
            {
                return this.Equals(k);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hash = 17;

            foreach (var key in this.Keys)
            {
                hash = hash * 31 + key.GetHashCode();
            }

            return hash;
        }
    }
}
