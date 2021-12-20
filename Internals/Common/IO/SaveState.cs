using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WiiPlayTanksRemake.Internals.Common.IO
{
    // this is very wip and does not work
    internal class SaveState
    {
        public Dictionary<int, string> savedTypes = new();

        public Dictionary<int, ValueTuple<string, object>> savedTypeFields = new();
        public Dictionary<int, ValueTuple<string, object>> savedTypeProperties = new();

        private SaveState(Assembly assembly)
        {
            for (int i = 0; i < assembly.GetTypes().Length; i++)
            {
                savedTypes.Add(i, assembly.GetTypes()[i].Name);

                for (int j = 0; j < assembly.GetTypes()[i].GetFields().Length; j++)
                {
                    var fld = assembly.GetTypes()[i].GetFields()[j];
                    savedTypeFields.Add(i, new(fld.Name, fld.GetValue(assembly.GetTypes()[i])));
                }
                for (int k = 0; k < assembly.GetTypes()[i].GetProperties().Length; k++)
                {
                    var prop = assembly.GetTypes()[i].GetProperties()[k];
                    savedTypeProperties.Add(i, new(prop.Name, prop.GetValue(assembly.GetTypes()[i])));
                }
            }
        }

        public static SaveState Save(Assembly assembly)
        {
            var ss = new SaveState(assembly);

            var hndlr = new JsonHandler(ss, Path.Combine(Environment.SpecialFolder.Desktop.ToString(), "savestate"));

            var opts = new System.Text.Json.JsonSerializerOptions() { WriteIndented = true, IncludeFields = true };

            hndlr.Serialize(opts, true);

            return ss;
        }

        public static void Load(SaveState state)
        {
            // help me what do i write
        }
    }
}
