using System;

namespace FreeMarketOne.Extensions.Helpers
{
    public class TextHelper
    {
        public const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
        public const string CHARS_DANGEROUS = "'|\\";

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

        public bool IsCleanTextValid(string text, bool allowSpaceDotComma = false)
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

        public bool IsTextNotDangerous(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }
            else
            {
                if (text.Contains(Environment.NewLine))
                {
                    return false;
                }

                for (int i = 0; i < text.Length; i++)
                {
                    var charTest = text.Substring(i, 1);
                    if (CHARS_DANGEROUS.Contains(charTest))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
