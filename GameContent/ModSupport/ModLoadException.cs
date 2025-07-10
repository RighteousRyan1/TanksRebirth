using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.GameContent.ModSupport;

internal class ModLoadException : Exception
{
	public ModLoadException(string message) : base(message) { }
	public ModLoadException(string message, Exception inner) : base(message, inner) { }

}
