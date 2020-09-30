using System;

namespace FreeMarketApp.Helpers
{
    public class TextHelper
    {
        private const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";

        public bool IsNumberValid(string text)
        {
            float number;

            if (float.TryParse(text, out number))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsTextValid(string text, bool allowSpaceDotComma = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    var charTest = text.Substring(i, 1);
                    if (!CHARS.Contains(charTest))
                    {
                        if ((charTest == " ") && (allowSpaceDotComma == true) ||
                            (charTest == ",") && (allowSpaceDotComma == true) ||
                            (charTest == ".") && (allowSpaceDotComma == true))
                        {
                            //silence
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
