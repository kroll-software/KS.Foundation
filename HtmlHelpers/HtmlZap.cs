using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;

namespace KS.Foundation.HtmlHelpers
{    
    public class HtmlTag
    {
        public string html = "";
        public string Name = "";
        
        public Dictionary<string, Point> Attributes = new Dictionary<string, Point>();
        public void AddAttribute(string name, int index, int length)
        {            
            name = name.Replace("\r", "");
            name = name.Replace("\n", "");
            name = name.Replace(" ", "");

            if (!Attributes.ContainsKey(name))
                Attributes.Add(name, new Point(index, length));            
        }
        
        public string AttributeValue(string name)
        {
            if (!Attributes.ContainsKey(name))
                return "";
            else
            {
                Point p = Attributes[name];
                return SubString(p.X, p.Y);
            }
        }        

        /// <summary>
        /// attributename must be in lower case
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public void ReplaceAttributeValue(string attributename, string NewValue)
        {
            if (!Attributes.ContainsKey(attributename))
                return;
            
            Point p = Attributes[attributename];
            html = SubString(0, p.X) + NewValue + SubString(p.X + p.Y, html.Length);
            p.Y = NewValue.Length;            
        }        

        public void Reset()
        {
            html = "";
            Name = "";
            Attributes.Clear();
        }

        public override string ToString()
        {            
            return html;
        }

		private string StripQuotes(string s)
		{
			if (String.IsNullOrEmpty (s))
				return String.Empty;

			if (s.Length < 2)
				return s;

			if (s [0] == '"' || s [0] == '\'')
				s = Strings.StrMid (s, 2);

			if (s.Length == 0)
				return string.Empty;

			int i = s.Length - 1;
			if (s [i] == '"' || s [i] == '\'')
				s = Strings.StrLeft (s, i);
			return s;
		}

        private string SubString(int startindex, int length)
        {
            if (startindex >= 0 && length > 0)
            {
                if (startindex + length > html.Length)
                    length = html.Length - startindex;

                if (length > 0)
					return StripQuotes(html.Substring(startindex, length));
                else
                    return "";
            }
            else
                return "";
        }
    }

    public class HtmlZap
    {
        private string strHTML = "";
        private int Length = 0;
                
        private int iLastPos = 0;
        private int iPos = 0;        
        
        private bool bIsTag = false;        
        
        private HtmlTag m_Tag = null;

        public void Reset()
        {
            strHTML = "";
            Length = 0;

            iLastPos = 0;
            iPos = 0;            

            bIsTag = false;            

            if (m_Tag != null)
                m_Tag.Reset();
        }

        public bool EOF
        {
            get
            {
                return (iPos < 0 || iPos >= strHTML.Length - 1) && iLastPos == iPos;
            }
        }

        public bool IsTag
        {
            get
            {
                return bIsTag;
            }
        }

        public HtmlTag Tag
        {
            get
            {
                return m_Tag;
            }
        }        

        public int Position
        {
            get
            {
                return iPos;
            }
        }

        public int LastPosition
        {
            get
            {
                return iLastPos;
            }
        }

        public string CurrentSliceText
        {
            get
            {
                if (iPos < 0)
                    return SubString(iLastPos, strHTML.Length);
                else
                {
                    if (iPos >= Length - 1)
                        return SubString(iLastPos, Length);
                    else
                        return SubString(iLastPos, iPos - 1);
                }
            }
        }

        public string PlainText
        {
            get
            {
                return HttpUtility.HtmlDecode(CurrentSliceText);
            }
        }

        private HashSet<string> m_TagFilter = null;       
        public void SetTagFilter(string[] value)
        {
            if (value == null)
                m_TagFilter = null;
            else
            {
                if (m_TagFilter == null)
                    m_TagFilter = new HashSet<string>();
                else
                    m_TagFilter.Clear();

                foreach (string s in value)
                {
                    if (!m_TagFilter.Contains(s.ToLowerInvariant()))
                        m_TagFilter.Add(s.ToLowerInvariant());
                }
            }
        }

        public HtmlZap()
        {            
            m_Tag = new HtmlTag();
        }

        public void LoadHtml(string HTML)
        {
            Reset();

            strHTML = HTML;
            Length = strHTML.Length;
            
            NextSlice();
        }

        public void NextSlice()
        {            
            m_Tag.Reset();

            bIsTag = false;

            if (iPos < 0 || iPos > Length - 2)
            {
                iLastPos = iPos;
                return;
            }            
            
            char c = strHTML[iPos];
            char cNext = strHTML[iPos + 1];

            if (c == '<')
            {
                if (cNext == '!')
                {
                    if (iPos + 3 < Length && strHTML[iPos + 2] == '-' && strHTML[iPos + 3] == '-')
                    {
                        // Comment
                        iLastPos = iPos;
                        iPos = strHTML.IndexOf("-->", iPos + 1);
                        if (iPos >= 0)
                        {
                            iPos += 2;
                            bIsTag = true;
                            m_Tag.Name = "!--";
                            m_Tag.html = SubString(iLastPos, iPos);                            
                        }
                        return;
                    }                    
                }

                //if (Char.IsLetter(cNext) || cNext == '/' || cNext == '!')
                if (Char.IsLetter(cNext) || cNext == '/' || cNext == '!' || cNext == '?')
                {
                    iLastPos = iPos;
                    iPos = strHTML.IndexOf('>', iPos + 1);
                    if (iPos > 0)
                    {
                        bIsTag = true;
                        m_Tag.html = SubString(iLastPos, iPos);
                        m_Tag.Name = GetTagNameFromHtml(m_Tag.html);

                        if (m_TagFilter != null && !m_TagFilter.Contains(m_Tag.Name))
                            return;
                        
                        GetParameters();
                    }
                }
                else
                {
                    // not a tag
                    bIsTag = false;
                    iPos = strHTML.IndexOf('<', iPos + 1);
                }
            }
            else if (c == '>')
            {
                bIsTag = false;

                if (iPos > 0)
                    iLastPos = iPos + 1;

                iPos = strHTML.IndexOf('<', iPos + 1);
            }
            else
            {
                bIsTag = false;
                //we are on start
                bIsTag = false;
                iPos = strHTML.IndexOf('<', iPos + 1);
            }            
        }
        
        public string SubString(int StartPos, int EndPos)
        {
            EndPos = Math.Min(EndPos, strHTML.Length - 1);
            if (StartPos >= 0 && EndPos >= StartPos)
                return strHTML.Substring(StartPos, EndPos - StartPos + 1);
            else
                return "";            
        }

        public string GetTagNameFromHtml(string html)
        {
            // get tag name
            if (html == null || html.Length < 3)
                return "";

            if (html[0] != '<')
                return "";

            int i = html.IndexOf(' ', 1);
            if (i > 1 && i < html.Length)                     
                return html.Substring(1, i - 1).ToLowerInvariant();            
            else                     
                return html.Substring(1, html.Length - 2).ToLowerInvariant();            
        }

        private void GetParameters()
        {
            if (m_Tag.html == "")
                return;            

            // Transform the Attributes

            string S = m_Tag.html;            

            char ch;
            char chQuote = '\0';            

            int iStart = 1;
            int iMax = S.Length;
            int i = iStart;

            // get tag name
            i = S.IndexOf(' ', iStart);

            if (String.IsNullOrEmpty(m_Tag.Name))
            {
                if (i >= 0 && i < iMax)
                {
                    if (i - iStart > 0)
                        m_Tag.Name = S.Substring(iStart, i - iStart).ToLowerInvariant();
                }
                else
                {
                    if (iMax - iStart - 1 > 0)
                        m_Tag.Name = S.Substring(iStart, iMax - iStart - 1).ToLowerInvariant();

                    return;
                }
            }

            if (i < 2)
                return;

            //if (m_Tag.Name == "img")
            //{
            //    int iTest = 0;
            //}

            do
            {
                while (i < iMax && (S[i] == ' ' || S[i] == '\r' || S[i] == '\n'))
                    i++;

                if (i < iMax)
                {
                    iStart = i;
                    i = S.IndexOf('=', iStart);
                    if (i < 0)
                        break;

                    // name
                    string strName = S.Substring(iStart, i - iStart);                    

                    // value
                    iStart = i;     // iStart is on '='                    
                    chQuote = '\0';

                    do
                    {
                        ch = S[i];
                        if ((ch == '\"') || (ch == '\'') || (ch == '´'))
                        {
                            if (chQuote == '\0')
                                chQuote = ch;
                            else if (chQuote == ch)
                                chQuote = '\0';
                        }
                        else if ((ch == ' ' || ch == '>') && chQuote == '\0')
                        {
                            string strValue = S.Substring(iStart + 1, i - iStart - 1);
                            m_Tag.AddAttribute(strName.ToLowerInvariant(), iStart + 1, strValue.Length);
                            break;
                        }                        

                        i++;
                    } while (i < iMax);
                }
                
            } while (i >= 0 && i < iMax);
        }        

        public static string Html2PlainText(string html)
        {
            StringBuilder SB = new StringBuilder();

            HtmlHelpers.HtmlZap ZAP = new HtmlHelpers.HtmlZap();
            ZAP.SetTagFilter(new string[]{"p", "div", "br", "head", "style", "script", "?xml"});

            ZAP.LoadHtml(html);

            while (!ZAP.EOF)
            {
                if (ZAP.IsTag)
                {
                    switch (ZAP.Tag.Name)
                    {
                        case "p":
                        case "div":
                        case "br":
                            SB.Append("\r\n");
                            break;

                        case "head":
                            while (!ZAP.EOF && ZAP.Tag.Name != "/head")
                                ZAP.NextSlice();
                            break;

                        case "style":                            
                            while (!ZAP.EOF && ZAP.Tag.Name != "/style")
                                ZAP.NextSlice();
                            break;

                        case "script":
                            while (!ZAP.EOF && ZAP.Tag.Name != "/script")                            
                                ZAP.NextSlice();
                            break;

                        case "?xml":                            
                            ZAP.NextSlice();
                            break;
                    }
                }
                else
                {
                    SB.Append(ZAP.PlainText);
                }

                ZAP.NextSlice();
            }

            SB.Append(ZAP.PlainText);
            return SB.ToString();
        }

        public static string BeautifyPlainText(string PlainText)
        {
            if (PlainText == null)
                return "";

            string[] A = Strings.SplitLines(PlainText);                        
            System.Text.StringBuilder SB = new System.Text.StringBuilder(PlainText.Length + 1024);

            bool bFirstLine = true;
            bool bFlag = false;
            foreach (string tempLoopVar_S in A)
            {
                string S = tempLoopVar_S.Trim();

                if (S.Length == 0)
                {
                    if (!(bFlag || bFirstLine))
                    {
                        SB.Append("\r\n");
                    }
                    bFlag = true;
                }
                else
                {
                    bFlag = false;
                    SB.Append(S);
                    SB.Append("\r\n");
                }

                bFirstLine = false;
            }

            return SB.ToString();
        }
    }
}
