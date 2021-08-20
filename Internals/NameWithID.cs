using System;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.Internals
{
    public sealed class NameWithID
    {
        public string Name { get; }
        public int Id { get; }

        public NameWithID(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public override string ToString() => $"name: {Name} | id: {Id}";

        public override int GetHashCode() => Id;
    }
}