/*
 * csammisrun.utility.HttpUtility
 * Obtained from .NET 2.0 sources using Lutz Roeder's Reflector
 */

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace KS.Foundation.HtmlHelpers
{
  /// <summary>
  /// Provides methods for encoding and decoding URLs and HTML snippets without the additional weight of the System.Web assembly
  /// </summary>
  public sealed class HttpUtility
  {
    private static char[] s_entityEndingChars;

    /// <summary>
    /// Initializes a new instance of the HttpUtility class.
    /// </summary>
    static HttpUtility()
    {
      HttpUtility.s_entityEndingChars = new char[] { ';', '&' };
    }

    #region URL encode/decode
    /// <summary>
    /// Encodes a URL string.
    /// </summary>
    public static string UrlEncode(string str)
    {
      if (str == null)
      {
        return null;
      }
      return HttpUtility.UrlEncode(str, Encoding.UTF8);
    }

    /// <summary>
    /// Encodes a URL string using the specified encoding object.
    /// </summary>
    public static string UrlEncode(string str, Encoding e)
    {
      if (str == null)
      {
        return null;
      }
      return Encoding.ASCII.GetString(HttpUtility.UrlEncodeToBytes(str, e));
    }

    /// <summary>
    /// Converts a string into a URL-encoded array of bytes using the specified encoding object.
    /// </summary>
    public static byte[] UrlEncodeToBytes(string str, Encoding e)
    {
      if (str == null)
      {
        return null;
      }
      byte[] buffer1 = e.GetBytes(str);
      return HttpUtility.UrlEncodeBytesToBytesInternal(buffer1, 0, buffer1.Length, false);
    }

    /// <summary>
    /// Converts a string that has been encoded for transmission in a URL into a decoded string.
    /// </summary>
    public static string UrlDecode(string url)
    {
      return UrlDecode(url, Encoding.UTF8);
    }

    /// <summary>
    /// Converts a URL-encoded string into a decoded string, using the specified encoding object.
    /// </summary>
    public static string UrlDecode(string s, Encoding e)
    {
      int num1 = s.Length;
      HttpUtility.UrlDecoder decoder1 = new HttpUtility.UrlDecoder(num1, e);
      for (int num2 = 0; num2 < num1; num2++)
      {
        char ch1 = s[num2];
        if (ch1 == '+')
        {
          ch1 = ' ';
        }
        else if ((ch1 == '%') && (num2 < (num1 - 2)))
        {
          if ((s[num2 + 1] == 'u') && (num2 < (num1 - 5)))
          {
            int num3 = HttpUtility.HexToInt(s[num2 + 2]);
            int num4 = HttpUtility.HexToInt(s[num2 + 3]);
            int num5 = HttpUtility.HexToInt(s[num2 + 4]);
            int num6 = HttpUtility.HexToInt(s[num2 + 5]);
            if (((num3 < 0) || (num4 < 0)) || ((num5 < 0) || (num6 < 0)))
            {
              goto Label_0106;
            }
            ch1 = (char)((ushort)((((num3 << 12) | (num4 << 8)) | (num5 << 4)) | num6));
            num2 += 5;
            decoder1.AddChar(ch1);
            goto Label_0120;
          }
          int num7 = HttpUtility.HexToInt(s[num2 + 1]);
          int num8 = HttpUtility.HexToInt(s[num2 + 2]);
          if ((num7 >= 0) && (num8 >= 0))
          {
            byte num9 = (byte)((num7 << 4) | num8);
            num2 += 2;
            decoder1.AddByte(num9);
            goto Label_0120;
          }
        }
      Label_0106:
        if ((ch1 & 0xff80) == '\0')
        {
          decoder1.AddByte((byte)ch1);
        }
        else
        {
          decoder1.AddChar(ch1);
        }
      Label_0120: ;
      }
      return decoder1.GetString();
    }
    #endregion

    #region HTML encode/decode
    /// <summary>
    /// Converts a string that has been HTML-encoded for HTTP transmission into a decoded string.
    /// </summary>
    public static string HtmlDecode(string s)
    {
      if (s == null)
      {
        return null;
      }
      if (s.IndexOf('&') < 0)
      {
        return s;
      }
      StringBuilder builder1 = new StringBuilder();
      StringWriter writer1 = new StringWriter(builder1);
      HttpUtility.HtmlDecode(s, writer1);
      return builder1.ToString();
    }

    /// <summary>
    /// Converts a string that has been HTML-encoded into a decoded string, and sends the decoded string to a TextWriter output stream.
    /// </summary>
    public static void HtmlDecode(string s, TextWriter output)
    {
      if (s != null)
      {
        if (s.IndexOf('&') < 0)
        {
          output.Write(s);
        }
        else
        {
          int num1 = s.Length;
          for (int num2 = 0; num2 < num1; num2++)
          {
            char ch1 = s[num2];
            if (ch1 == '&')
            {
              int num3 = s.IndexOfAny(HttpUtility.s_entityEndingChars, num2 + 1);
              if ((num3 > 0) && (s[num3] == ';'))
              {
                string text1 = s.Substring(num2 + 1, (num3 - num2) - 1);
                if ((text1.Length > 1) && (text1[0] == '#'))
                {
                  try
                  {
                    if ((text1[1] == 'x') || (text1[1] == 'X'))
                    {
                      ch1 = (char)((ushort)int.Parse(text1.Substring(2), NumberStyles.AllowHexSpecifier));
                    }
                    else
                    {
                      ch1 = (char)((ushort)int.Parse(text1.Substring(1)));
                    }
                    num2 = num3;
                  }
                  catch (FormatException)
                  {
                    num2++;
                  }
                  catch (ArgumentException)
                  {
                    num2++;
                  }
                }
                else
                {
                  num2 = num3;
                  char ch2 = HtmlEntities.Lookup(text1);
                  if (ch2 != '\0')
                  {
                    ch1 = ch2;
                  }
                  else
                  {
                    output.Write('&');
                    output.Write(text1);
                    output.Write(';');
                    goto Label_0103;
                  }
                }
              }
            }
            output.Write(ch1);
          Label_0103: ;
          }
        }
      }
    }

    /// <summary>
    /// Converts a string to an HTML-encoded string.
    /// </summary>
    public static string HtmlEncode(string s)
    {
      if (s == null)
      {
        return null;
      }
      int num1 = HttpUtility.IndexOfHtmlEncodingChars(s, 0);
      if (num1 == -1)
      {
        return s;
      }
      StringBuilder builder1 = new StringBuilder(s.Length + 5);
      int num2 = s.Length;
      int num3 = 0;
    Label_002A:
      if (num1 > num3)
      {
        builder1.Append(s, num3, num1 - num3);
      }
      char ch1 = s[num1];
      if (ch1 > '>')
      {
        builder1.Append("&#");
        builder1.Append(ch1.ToString(NumberFormatInfo.InvariantInfo));
        builder1.Append(';');
      }
      else
      {
        char ch2 = ch1;
        if (ch2 != '"')
        {
          switch (ch2)
          {
            case '<':
              builder1.Append("&lt;");
              goto Label_00D5;

            case '=':
              goto Label_00D5;

            case '>':
              builder1.Append("&gt;");
              goto Label_00D5;

            case '&':
              builder1.Append("&amp;");
              goto Label_00D5;
          }
        }
        else
        {
          builder1.Append("&quot;");
        }
      }
    Label_00D5:
      num3 = num1 + 1;
      if (num3 < num2)
      {
        num1 = HttpUtility.IndexOfHtmlEncodingChars(s, num3);
        if (num1 != -1)
        {
          goto Label_002A;
        }
        builder1.Append(s, num3, num2 - num3);
      }
      return builder1.ToString();
    }
    #endregion

    #region Private/internal methods and classes
    private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue)
    {
      int num1 = 0;
      int num2 = 0;
      for (int num3 = 0; num3 < count; num3++)
      {
        char ch1 = (char)bytes[offset + num3];
        if (ch1 == ' ')
        {
          num1++;
        }
        else if (!HttpUtility.IsSafe(ch1))
        {
          num2++;
        }
      }
      if ((!alwaysCreateReturnValue && (num1 == 0)) && (num2 == 0))
      {
        return bytes;
      }
      byte[] buffer1 = new byte[count + (num2 * 2)];
      int num4 = 0;
      for (int num5 = 0; num5 < count; num5++)
      {
        byte num6 = bytes[offset + num5];
        char ch2 = (char)num6;
        if (HttpUtility.IsSafe(ch2))
        {
          buffer1[num4++] = num6;
        }
        else if (ch2 == ' ')
        {
          buffer1[num4++] = 0x2b;
        }
        else
        {
          buffer1[num4++] = 0x25;
          buffer1[num4++] = (byte)HttpUtility.IntToHex((num6 >> 4) & 15);
          buffer1[num4++] = (byte)HttpUtility.IntToHex(num6 & 15);
        }
      }
      return buffer1;
    }

    private static int HexToInt(char h)
    {
      if ((h >= '0') && (h <= '9'))
      {
        return (h - '0');
      }
      if ((h >= 'a') && (h <= 'f'))
      {
        return ((h - 'a') + '\n');
      }
      if ((h >= 'A') && (h <= 'F'))
      {
        return ((h - 'A') + '\n');
      }
      return -1;
    }

    internal static char IntToHex(int n)
    {
      if (n <= 9)
      {
        return (char)((ushort)(n + 0x30));
      }
      return (char)((ushort)((n - 10) + 0x61));
    }

    internal static bool IsSafe(char ch)
    {
      if ((((ch >= 'a') && (ch <= 'z')) || ((ch >= 'A') && (ch <= 'Z'))) || ((ch >= '0') && (ch <= '9')))
      {
        return true;
      }
      switch (ch)
      {
        case '\'':
        case '(':
        case ')':
        case '*':
        case '-':
        case '.':
        case '_':
        case '!':
          return true;
      }
      return false;
    }

    private static int IndexOfHtmlEncodingChars(string s, int startPos)
    {
      int num1 = s.Length - startPos;

      int text1 = 0;
      int chPtr1 = text1;
      int chPtr2 = chPtr1 + startPos;
      while (num1 > 0)
      {
        char ch1 = s[chPtr2 + 0];
        if (ch1 <= '>')
        {
          switch (ch1)
          {
            case '<':
            case '>':
            case '"':
            case '&':
              return (s.Length - num1);

            case '=':
              goto Label_007A;
          }
        }
        else if ((ch1 >= '\x00a0') && (ch1 < 'Ā'))
        {
          return (s.Length - num1);
        }
      Label_007A:
        chPtr2++;
        num1--;
      }
      return -1;
    }

    private class UrlDecoder
    {
      private int _bufferSize;
      private byte[] _byteBuffer;
      private char[] _charBuffer;
      private Encoding _encoding;
      private int _numBytes;
      private int _numChars;

      internal UrlDecoder(int bufferSize, Encoding encoding)
      {
        this._bufferSize = bufferSize;
        this._encoding = encoding;
        this._charBuffer = new char[bufferSize];
      }

      internal void AddByte(byte b)
      {
        if (this._byteBuffer == null)
        {
          this._byteBuffer = new byte[this._bufferSize];
        }
        this._byteBuffer[this._numBytes++] = b;
      }

      internal void AddChar(char ch)
      {
        if (this._numBytes > 0)
        {
          this.FlushBytes();
        }
        this._charBuffer[this._numChars++] = ch;
      }

      private void FlushBytes()
      {
        if (this._numBytes > 0)
        {
          this._numChars += this._encoding.GetChars(this._byteBuffer, 0, this._numBytes, this._charBuffer, this._numChars);
          this._numBytes = 0;
        }
      }

      internal string GetString()
      {
        if (this._numBytes > 0)
        {
          this.FlushBytes();
        }
        if (this._numChars > 0)
        {
          return new string(this._charBuffer, 0, this._numChars);
        }
        return string.Empty;
      }
    }

    internal class HtmlEntities
    {
      // Methods
      static HtmlEntities()
      {
        HtmlEntities._lookupLockObject = new object();
        HtmlEntities._entitiesList = new string[] { 
            "\"-quot", "&-amp", "<-lt", ">-gt", "\x00a0-nbsp", "\x00a1-iexcl", "\x00a2-cent", "\x00a3-pound", "\x00a4-curren", "\x00a5-yen", "\x00a6-brvbar", "\x00a7-sect", "\x00a8-uml", "\x00a9-copy", "\x00aa-ordf", "\x00ab-laquo", 
            "\x00ac-not", "\x00ad-shy", "\x00ae-reg", "\x00af-macr", "\x00b0-deg", "\x00b1-plusmn", "\x00b2-sup2", "\x00b3-sup3", "\x00b4-acute", "\x00b5-micro", "\x00b6-para", "\x00b7-middot", "\x00b8-cedil", "\x00b9-sup1", "\x00ba-ordm", "\x00bb-raquo", 
            "\x00bc-frac14", "\x00bd-frac12", "\x00be-frac34", "\x00bf-iquest", "\x00c0-Agrave", "\x00c1-Aacute", "\x00c2-Acirc", "\x00c3-Atilde", "\x00c4-Auml", "\x00c5-Aring", "\x00c6-AElig", "\x00c7-Ccedil", "\x00c8-Egrave", "\x00c9-Eacute", "\x00ca-Ecirc", "\x00cb-Euml", 
            "\x00cc-Igrave", "\x00cd-Iacute", "\x00ce-Icirc", "\x00cf-Iuml", "\x00d0-ETH", "\x00d1-Ntilde", "\x00d2-Ograve", "\x00d3-Oacute", "\x00d4-Ocirc", "\x00d5-Otilde", "\x00d6-Ouml", "\x00d7-times", "\x00d8-Oslash", "\x00d9-Ugrave", "\x00da-Uacute", "\x00db-Ucirc", 
            "\x00dc-Uuml", "\x00dd-Yacute", "\x00de-THORN", "\x00df-szlig", "\x00e0-agrave", "\x00e1-aacute", "\x00e2-acirc", "\x00e3-atilde", "\x00e4-auml", "\x00e5-aring", "\x00e6-aelig", "\x00e7-ccedil", "\x00e8-egrave", "\x00e9-eacute", "\x00ea-ecirc", "\x00eb-euml", 
            "\x00ec-igrave", "\x00ed-iacute", "\x00ee-icirc", "\x00ef-iuml", "\x00f0-eth", "\x00f1-ntilde", "\x00f2-ograve", "\x00f3-oacute", "\x00f4-ocirc", "\x00f5-otilde", "\x00f6-ouml", "\x00f7-divide", "\x00f8-oslash", "\x00f9-ugrave", "\x00fa-uacute", "\x00fb-ucirc", 
            "\x00fc-uuml", "\x00fd-yacute", "\x00fe-thorn", "\x00ff-yuml", "\u0152-OElig", "\u0153-oelig", "\u0160-Scaron", "\u0161-scaron", "\u0178-Yuml", "\u0192-fnof", "\u02c6-circ", "\u02dc-tilde", "\u0391-Alpha", "\u0392-Beta", "\u0393-Gamma", "\u0394-Delta", 
            "\u0395-Epsilon", "\u0396-Zeta", "\u0397-Eta", "\u0398-Theta", "\u0399-Iota", "\u039a-Kappa", "\u039b-Lambda", "\u039c-Mu", "\u039d-Nu", "\u039e-Xi", "\u039f-Omicron", "\u03a0-Pi", "\u03a1-Rho", "\u03a3-Sigma", "\u03a4-Tau", "\u03a5-Upsilon", 
            "\u03a6-Phi", "\u03a7-Chi", "\u03a8-Psi", "\u03a9-Omega", "\u03b1-alpha", "\u03b2-beta", "\u03b3-gamma", "\u03b4-delta", "\u03b5-epsilon", "\u03b6-zeta", "\u03b7-eta", "\u03b8-theta", "\u03b9-iota", "\u03ba-kappa", "\u03bb-lambda", "\u03bc-mu", 
            "\u03bd-nu", "\u03be-xi", "\u03bf-omicron", "\u03c0-pi", "\u03c1-rho", "\u03c2-sigmaf", "\u03c3-sigma", "\u03c4-tau", "\u03c5-upsilon", "\u03c6-phi", "\u03c7-chi", "\u03c8-psi", "\u03c9-omega", "\u03d1-thetasym", "\u03d2-upsih", "\u03d6-piv", 
            "\u2002-ensp", "\u2003-emsp", "\u2009-thinsp", "\u200c-zwnj", "\u200d-zwj", "\u200e-lrm", "\u200f-rlm", "\u2013-ndash", "\u2014-mdash", "\u2018-lsquo", "\u2019-rsquo", "\u201a-sbquo", "\u201c-ldquo", "\u201d-rdquo", "\u201e-bdquo", "\u2020-dagger", 
            "\u2021-Dagger", "\u2022-bull", "\u2026-hellip", "\u2030-permil", "\u2032-prime", "\u2033-Prime", "\u2039-lsaquo", "\u203a-rsaquo", "\u203e-oline", "\u2044-frasl", "\u20ac-euro", "\u2111-image", "\u2118-weierp", "\u211c-real", "\u2122-trade", "\u2135-alefsym", 
            "\u2190-larr", "\u2191-uarr", "\u2192-rarr", "\u2193-darr", "\u2194-harr", "\u21b5-crarr", "\u21d0-lArr", "\u21d1-uArr", "\u21d2-rArr", "\u21d3-dArr", "\u21d4-hArr", "\u2200-forall", "\u2202-part", "\u2203-exist", "\u2205-empty", "\u2207-nabla", 
            "\u2208-isin", "\u2209-notin", "\u220b-ni", "\u220f-prod", "\u2211-sum", "\u2212-minus", "\u2217-lowast", "\u221a-radic", "\u221d-prop", "\u221e-infin", "\u2220-ang", "\u2227-and", "\u2228-or", "\u2229-cap", "\u222a-cup", "\u222b-int", 
            "\u2234-there4", "\u223c-sim", "\u2245-cong", "\u2248-asymp", "\u2260-ne", "\u2261-equiv", "\u2264-le", "\u2265-ge", "\u2282-sub", "\u2283-sup", "\u2284-nsub", "\u2286-sube", "\u2287-supe", "\u2295-oplus", "\u2297-otimes", "\u22a5-perp", 
            "\u22c5-sdot", "\u2308-lceil", "\u2309-rceil", "\u230a-lfloor", "\u230b-rfloor", "\u2329-lang", "\u232a-rang", "\u25ca-loz", "\u2660-spades", "\u2663-clubs", "\u2665-hearts", "\u2666-diams",
			"'-apos"
       };
      }

      private HtmlEntities()
      {
      }

      internal static char Lookup(string entity)
      {
        if (HtmlEntities._entitiesLookupTable == null)
        {
          lock (HtmlEntities._lookupLockObject)
          {
            if (HtmlEntities._entitiesLookupTable == null)
            {
              System.Collections.Hashtable hashtable1 = new System.Collections.Hashtable();
              foreach (string text1 in HtmlEntities._entitiesList)
              {
                hashtable1[text1.Substring(2)] = text1[0];
              }
              HtmlEntities._entitiesLookupTable = hashtable1;
            }
          }
        }
        object obj1 = HtmlEntities._entitiesLookupTable[entity];
        if (obj1 != null)
        {
          return (char)obj1;
        }
        return '\0';
      }
 
      // Fields
      private static string[] _entitiesList;
      private static System.Collections.Hashtable _entitiesLookupTable;
      private static object _lookupLockObject;
    }

    #endregion
  }
}
