using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.IO
{
    public interface IFileSerializable
    {
        string Directory { get; }
        string Name { get; }
        void Serialize();
        void Deserialize();
    }
}
