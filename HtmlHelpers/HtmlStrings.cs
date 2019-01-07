using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Globalization;

namespace KS.Foundation.HtmlHelpers
{
    sealed class HtmlStrings
    {       
        public static string HtmlEncode(string StrToEncode)
        {
            return HttpUtility.HtmlEncode(StrToEncode);
        }

        public static string HtmlDecode(string EncodedString)
        {
            return HttpUtility.HtmlDecode(EncodedString);
        }

        public static string UrlEncode(string StrToEncode)
        {
            return HttpUtility.UrlEncode(StrToEncode);
        }

        public static string UrlDecode(string EncodedString)
        {
            return HttpUtility.UrlDecode(EncodedString);
        }

        private static int GetMonth(string UsaMonth)
        {
            switch (UsaMonth.ToUpper())
            {
                case "JAN":
                    return 1;
                
                case "FEB":
                    return 2;

                case "MAR":
                    return 3;

                case "APR":
                    return 4;

                case "MAY":
                    return 5;

                case "JUN":
                    return 6;

                case "JUL":
                    return 7;

                case "AUG":
                    return 8;

                case "SEP":
                    return 9;

                case "OCT":
                    return 10;

                case "NOV":
                    return 11;

                case "DEC":
                    return 12;

                default:
                    return 0;
            }            
        }

        public static System.DateTime ConvertDate(string ServerDate)
        {            
            //if (DateTime.TryParse(ServerDate, out dtRet))
            //    return dtRet;
            //else
            //    throw new Exception("Invalid DateTime");            

            int y = 0;
            int m = 0;
            int d = 0;
            int h = 0;
            int min = 0;
            int sek = 0;

            ServerDate = ServerDate.Trim();
            if (ServerDate == "")
                return new DateTime();

            string[] S = Strings.Split(ServerDate, ' ');
            if (S.Length != 5)
                return new DateTime();

            d = S[1].SafeInt();
            
            m = GetMonth(S[2]);
            if (m == 0)
                return new DateTime();

            y = S[3].SafeInt();

            string[] S2 = Strings.Split(S[4], ':');

            if (S2.Length == 3)
            {
                h = S2[0].SafeInt();
                min = S2[1].SafeInt();
                sek = S2[2].SafeInt();
            }

            DateTime dtRet;

            try
            {
                dtRet = new DateTime(y, m, d, h, min, sek);
            }
            catch (Exception)
            {
                dtRet = new DateTime();
            }

            return dtRet;
        }

        //public static string StripHtml(string S, bool bFlag)
        //{
        //    return S;
        //}

        public static string NormalizeLineBreaks(string input)
        {
            if (String.IsNullOrEmpty(input))
                return "";

            // Allow 10% as a rough guess of how much the string may grow.
            // If we're wrong we'll either waste space or have extra copies -
            // it will still work
            StringBuilder builder = new StringBuilder((int)(input.Length * 1.1));

            bool lastWasCR = false;

            foreach (char c in input)
            {
                if (lastWasCR)
                {
                    lastWasCR = false;
                    if (c == '\n')
                    {
                        continue; // Already written \r\n
                    }
                }
                switch (c)
                {
                    case '\r':
                        builder.Append("\r\n");
                        lastWasCR = true;
                        break;
                    case '\n':
                        builder.Append("\r\n");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}
