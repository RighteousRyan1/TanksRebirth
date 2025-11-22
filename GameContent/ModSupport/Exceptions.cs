using System;

namespace TanksRebirth.GameContent.ModSupport;

class ModLoadException : Exception {
	public ModLoadException(string message) : base(message) { }
	public ModLoadException(string message, Exception inner) : base(message, inner) { }
}

class ModRuntimeException : Exception {
    public ModRuntimeException(string message) : base(message) { }
    public ModRuntimeException(string message, Exception inner) : base(message, inner) { }
}
