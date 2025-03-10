using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Framework.Input;

// IConvertible meaning enum
/// <summary>An interface representing a method of input, tracking active input and allowing reassignment.</summary>
/// <typeparam name="T">An enum representing a list of 'buttons' or methods of input.</typeparam>
public interface IInputBind<T> where T : IConvertible {
    public string Name { get; }
    public bool JustPressed { get; }
    public bool IsPressed { get; }
    public bool PendReassign { get; set; }
    public T Assigned { get; internal set; }
    public Action OnPress { get; }
    public Action<T> OnReassign { get; set; }
    public void Fire();
}
