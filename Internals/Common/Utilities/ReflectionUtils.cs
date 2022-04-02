using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities
{
    public class ReflectionUtils
    {
        public static List<T> GetInheritedTypesOf<T>(Assembly assembly) where T : class
        {
            var types = assembly.GetTypes();
            var buffer = new List<T>();
            foreach (var type in types)
                if (type.IsSubclassOf(typeof(T)) && !type.IsAbstract)
                    buffer.Add(Activator.CreateInstance(type) as T);
            return buffer;
        }
    }
}
