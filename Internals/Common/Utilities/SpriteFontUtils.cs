using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiPlayTanksRemake.Internals.Common.Utilities
{
	public static class SpriteFontUtils
	{
        /// <summary>
        /// Remove non-valid font characters from the given text to be safe-to-draw
        /// </summary>
        /// <param name="font">Font to check valid characters</param>
        /// <param name="text">Text to valid</param>
        /// <param name="validText">Variable to output the valid text</param>
        public static void GetSafeText(this SpriteFont font, string text, out string validText)
        {
            validText = "";

            foreach (char letter in text)
            {
                if (letter == '\n' || letter == '\r' || font.Characters.Contains(letter))
                {
                    validText += letter;
                }
                else
                {
                    validText += "?";
                }
            }
        }
    }
}
