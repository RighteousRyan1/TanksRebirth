using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Collections.Generic;
using static Microsoft.Xna.Framework.Input.Keys;

namespace WiiPlayTanksRemake.Internals.Common.GameInput
{
    public static class KeybindParser
    {
        public static List<int> nums = new()
        {
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            0,
        };

        public static bool IsNum(this Keys key, out int num) {
            num = -1;
            foreach (var number in nums) {
                if (key.ToString() == $"D{number}") {
                    num = number;
                    return true;
                }
            }
            return false;
        }
        public static string ParseKey(this Keys key) {
            foreach (var num in nums) {
                if (key.ToString() == $"D{num}") {
                    return num.ToString();
                }
            }
            switch (key) {
                case OemPlus:
                    return "+";
                case OemMinus:
                    return "-";
                case OemCloseBrackets:
                    return "]";
                case OemOpenBrackets:
                    return "[";
                case OemSemicolon:
                    return ";";
                case OemBackslash:
                    return "/";
                case OemComma:
                    return ",";
                case OemPeriod:
                    return ".";
                case OemQuestion:
                    return "?";
                case OemQuotes:
                    return "'";
                case OemPipe:
                    return @"\";
                case OemTilde:
                    return "`";
            }
            return key.ToString();
        }
    }
}