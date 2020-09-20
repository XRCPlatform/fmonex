namespace FreeMarketApp.Helpers
{
    internal static class ValidationHelper
    {
        private const string VALIDCHARS = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";

        internal static bool IsNumberValid(string text)
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

        internal static bool IsTextValid(string text, bool allowSpaceDotComma = false)
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
                    if (!VALIDCHARS.Contains(charTest))
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
