using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common
{
    // it may not be modular but it works 
    // TODO: allow generic usage
    // TODO: also finish
    public class Invokable : IInvokable
    {
        public InvocationLocation Location { get; set; }
        internal Action action;

        public Invokable(Action action)
        {
            this.action = action;
        }

        public void Invoke()
        {
            action?.Invoke();
        }
    }

    public interface IInvokable
    {
        void Invoke();
    }

    public enum InvocationLocation
    {
        Update,
        Draw
    }
}
