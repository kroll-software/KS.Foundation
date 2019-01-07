using System;
using System.Collections.Generic;
using System.Text;

namespace KS.Foundation.HtmlHelpers
{
    public enum DocumentTypes
    {        
        Unknown = 0,        
        Page = 1,        
        Image = 2,        
        Media = 3,        
        Binary = 4
    }

    public enum ProtocolTypes
    {
        unknown = 0,        
        HTTP = 1,        
        HTTPS = 2,        
        FTP = 3,        
        WAIS = 4,        
        NNTP = 5,        
        TELNET = 6,        
        MAIL = 7 
    }    
    
    public class KSURL
    {
        public Dictionary<string, string> TLDNames = null;
        public Dictionary<string, DocumentTypes> KnownExtensions = null;        
        
        private bool DC_flag = false;
        private bool DomainFlag = false;

        private DocumentTypes m_DocumentType;
        private ProtocolTypes m_ProtocolType;
        //private string m_URL;
        private string m_EMail;
        private string m_Domain;
        private string m_TLD;
        private string m_Path;
        private string m_Protocol;
        private string m_Server;
        private string m_Port;
        private string m_Document;
        private string m_Anchor;
        private string m_Params;
        private string m_Extension;

        public KSURL()
        {
            TLDNames = new Dictionary<string,string>();
            KnownExtensions = new Dictionary<string, DocumentTypes>();

            // this.FillCountries(); // automatisch bei Bedarf
            this.FillExtensions();
        }
        
        public KSURL(string URL)
        {
            TLDNames = new Dictionary<string, string>();
            KnownExtensions = new Dictionary<string, DocumentTypes>();

            // this.FillCountries(); // automatisch bei Bedarf
            this.FillExtensions();            

            this.URL = URL;
        }

        public string URL
        {
            get
            {                
                string S = m_Protocol + m_Server;

                if (m_Port != "")
                    S += ":" + m_Port;

                if (m_Path != "")
                    S += m_Path;

                if (m_Document != "")
                    S += m_Document;

                if (m_Anchor != "")
                    S += "#" + m_Anchor;

                if (m_Params != "")
                    S += "?" + m_Params;

                return S;
            }
            set
            {
                string S = value.Trim();	            

	            int i;
	            int k;
	            int n;	            

	            Reset();

	            if (S != "")
	            {
		            //Javascript
		            string SU = S.ToLower();		            
		            i = SU.IndexOf("javascript:");

		            if (i >= 0)
			            S = Strings.StrLeft(S, i);

		            // Params
		            i = S.IndexOf('?');
		            if (i >= 0)
		            {
			            m_Params = Strings.StrMid(S, i + 2);
			            S = Strings.StrLeft(S, i);
			            //m_Params.Replace("&amp;", "&");
		            }

		            // Protocol
		            m_ProtocolType = GetProtocol(S);            		
		            if (m_ProtocolType == ProtocolTypes.MAIL)
		            {
			            m_EMail = Strings.StrMid(S, m_Protocol.Length + 1);
			            i = m_EMail.IndexOf('?');
			            if (i >= 0)
			            {
				            m_EMail = Strings.StrLeft(m_EMail, i);
			            }
		            }
		            else
		            {
			            if (m_ProtocolType == ProtocolTypes.unknown)
			            {
				            if ((S.IndexOf("://") <= 0) && (Strings.StrLeft(S, 1) != "/"))
				            {
					            m_Protocol = "http://";
					            S = m_Protocol + S;
				            }
			            }
            			            			
			            // Anchor
			            i = S.IndexOf('#');
			            if (i >= 0)
			            {
				            m_Anchor = Strings.StrMid(S, i + 2);
				            S = Strings.StrLeft(S, i);
			            }

			            m_Server = GetServer(S);
			            m_Document = GetDocument(S, m_Protocol, m_Server);            						            

			            // Path
			            i = m_Protocol.Length + m_Server.Length;
			            k = m_Document.Length;

			            n = S.Length - k - i;

			            if (n >= 0 && n <= S.Length)
				            m_Path = Strings.StrMid(S, i + 1, n);
			            else
				            m_Path = "";
            			
			            m_Path = FixDecoding(m_Path);
			            m_Path = ConvertRelativePath(m_Path);

			            if (Strings.StrRight(m_Path, 1) != "/")
				            m_Path += "/";
            			
			            m_Document = FixDecoding(m_Document);

			            // Extension
			            i = m_Document.Length - 1;
			            while ((i >= 0) && (m_Document[i] != '.'))
				            i --;
            			
			            if (i >= 0)
				            m_Extension = Strings.StrMid(m_Document, i + 2);
			            else
				            m_Extension = "";
            			            			
			            // Port
			            i = m_Server.IndexOf(':');
			            if (i >= 1)
			            {
				            m_Port = Strings.StrMid(m_Server, i + 2);
				            m_Server = Strings.StrLeft(m_Server, i);
			            }

			            m_DocumentType = GetLinkType();
		            }
	            }

            }
        }

        private DocumentTypes GetLinkType()
        {
            string E = m_Extension.ToLower();

            if ((m_Document == "/") || (m_Document == ""))
            {
                return DocumentTypes.Page;
            }
            else
            {
                if (KnownExtensions.ContainsKey(E))
                    return KnownExtensions[E];
                else
                    return DocumentTypes.Unknown;
            }
        }

        public string ToServer()
        {
            string strResult = "";

            if (m_Protocol != "mailto:")
            {
                strResult = m_Protocol + m_Server;
                if (m_Port != "")
                    strResult += ":" + m_Port;
            }

            return strResult;
        }

        public string Server
        {
            get
            {                
                return m_Server;
            }
        }

        public string Domain
        {
            get
            {
                if (!DomainFlag)
                    Server2Domain();

                return m_Domain;
            }
        }

        public string Email
        {
            get
            {
                return m_EMail;
            }
        }

        public string TLD
        {
            get
            {
                if (!DomainFlag)
                    Server2Domain();

                return m_TLD;
            }
        }

        public string Country
        {
            get
            {
                if (!DomainFlag)
                    Server2Domain();

                string S = TLD2Domain(m_TLD);
                return S;
            }
        }        

        public bool IsIP(string S)
        {
            int dots = 0;
            bool cFlag = false;

            for (int i = 0; i < S.Length; i++)
            {
                if (!(S[i] >= '0' && S[i] <= '9'))
                {
                    cFlag = true;
                    break;
                }

                if (S[i] == '.')
                    dots++;
            }

            return !cFlag && (dots == 3);
        }

        public string Resource
        {
            get
            {
                string strResult = m_Path;

                if (strResult == "")
                    strResult = "/";

                strResult += m_Document;
                Strings.Replace(strResult, " ", "%20");
                return strResult;
            }
        }

        public string Path
        {
            get
            {
                string strResult = m_Path;

                if (strResult == "")
                    strResult = "/";

                return strResult;
            }
            set
            {
                m_Path = value;
            }
        }


        public string ToURL(bool WithAnchors, bool WithParams)
        {
            string strResult = "";

            if (m_Protocol != "mailto:")
            {
                strResult = m_Protocol + m_Server;
                if (m_Port != "")
                    strResult += ":" + m_Port;

                strResult += m_Path + m_Document;

                if (WithAnchors && (m_Anchor != ""))
                    strResult += "#" + m_Anchor;

                if (WithParams && m_Params != "")
                    strResult += "?" + m_Params;
            }

            return strResult;
        }

        public DocumentTypes DocumentType
        {
            get
            {
                return m_DocumentType;
            }
        }

        public ProtocolTypes ProtocolType
        {
            get
            {
                return m_ProtocolType;
            }
        }

        public void Reset() 
        {
	        m_Protocol = "";
	        m_ProtocolType = 0;
	        m_Server = "";
	        m_Port = "";
	        m_Path = "";
	        m_Document = "";
	        m_DocumentType = 0;
	        m_Anchor = "";
	        m_Params = "";
	        m_Extension = "";
	        m_EMail = "";
	        m_Domain = "";
	        m_TLD = "";
        	
	        DomainFlag = false;
        }

        private ProtocolTypes GetProtocol(string S)
        {            
	        string Z = Strings.StrLeft(S, 7).ToLower();	        	        
	        
	        if (Z == "http://")
	        {
                m_Protocol = Strings.StrLeft(S, 7);
                return ProtocolTypes.HTTP;
	        }
	        else if (Z == "https:/")
	        {
                m_Protocol = Strings.StrLeft(S, 8);
                return ProtocolTypes.HTTPS;
	        }
            else if (Z == "mailto:")
            {
                m_Protocol = Strings.StrLeft(S, 7);
                return ProtocolTypes.MAIL;
            }
	        else if (Strings.StrLeft(Z, 6) == "ftp://")
	        {
		        m_Protocol = Strings.StrLeft(S, 6);
                return ProtocolTypes.FTP;
	        }
	        else if (Z == "wais://")
	        {
		        m_Protocol = Strings.StrLeft(S, 7);
                return ProtocolTypes.WAIS;
	        }
	        else if (Z == "nntp://")
	        {
		        m_Protocol = Strings.StrLeft(S, 7);
                return ProtocolTypes.NNTP;
	        }
	        else if (Z == "telnet:")
	        {
		        m_Protocol = Strings.StrLeft(S, 9);
                return ProtocolTypes.TELNET;
	        }	        
	        else
	        {
		        int i = Z.IndexOf("://");
		        if (i >= 0 && i <= 10)
		        {
			        m_Protocol = Strings.StrLeft(S, i + 3);
		        }
		        else
			        m_Protocol = "";

                return ProtocolTypes.unknown;
	        }            
        }

        public string GetServer(string S)
        {
            string Z = S;

	        if (Z == "")
		        return "";

            int i = 0;

            if (m_Protocol.Length + 1 <= Z.Length)
                i = Z.IndexOf('/', m_Protocol.Length + 1);
        	
	        if (i >= 0)
		        Z = Strings.StrLeft(Z, i);

	        i = m_Protocol.Length;
	        if (i <= Z.Length)
		        Z = Strings.StrMid(Z, i + 1);
	        else
		        Z = "";
        	
	        Z = FixDecoding(Z);
	        return Z;
        }

        private void Server2Domain()
        {
            string Z = m_Server.ToLower();
	        string D;

	        if (Z == "")
		        return;        		        

	        int i;

	        i = Strings.InStrRev(Z, ".");
        	
	        if (i >= 0)
		        m_TLD = Strings.StrMid(Z, i + 2);
	        else
		        return;
        	
	        char c;

	        if (m_TLD != "")
	        {
		        c = m_TLD[0];
		        if (c >= '0' && c <= '9')
		        {
			        m_TLD = "";
			        m_Domain = m_Server;
			        return;
		        }
	        }
        	
	        D = Strings.StrLeft(Z, 4);
	        if (D == "www.")
		        m_Domain = Strings.StrMid(Z, 5);
	        else
		        m_Domain = m_Server;

	        DomainFlag = true;
        }

        private string TLD2Domain(string TLD)
        {
            if (!DC_flag)
                FillCountries();

            if (TLD == "")
            {
                return "";
            }
            else
            {
                if (TLDNames.ContainsKey(TLD))
                    return TLDNames[TLD];
                else
                    return "";
            }
        }

        private string GetDocument(string S, string Protocol, string Server)
        {
            int i;
	        int k;
	        int p;

            string strURL = S;
	        if (strURL == "")
		        return "";

	        k = Protocol.Length + Server.Length;	                    
	        p = strURL.Length;

	        i = p - 1;
	        while ((i > k) && (strURL[i] != '/'))
		        i--;
        	
	        strURL = Strings.StrMid(strURL, i + 2, p - i);
	        
	        /**
	        if (strURL == "")
		        strURL = "/";
	        **/

            if (strURL.IndexOf('.') < 0)
                strURL = "";

	        return strURL;
        }

        public string ConvertRelativePath(string S)
        {
            int i;
	        int k;
	        string Z = S;

	        if (Z == "")
		        return "";

	        do
	        {
		        i = Z.IndexOf("/../");

		        if (i == 0)
		        {			        
			        Z = Strings.DeleteStr(Z, 2, 3);
		        }
		        else if (i > 0)
		        {
			        k = i - 1;

			        while ((k >= 0) && (Z[k] != '/'))
				        k --;
        			
			        if (k >= 0)
				        Z = Strings.DeleteStr(Z, k + 1, i + 3 - k);
			        else
				        return Z;
		        }
	        }	
	        while (i >= 0);

	        Strings.Replace(Z, "/./", "/");        	
	        return Z;
        }

        private string FixDecoding(string S)
        {       
            string Z = S;
	        if (Z.IndexOf('%') >= 0)
	        {
		        Z = HttpUtility.UrlDecode(Z);
		        Z = Z.Trim();		        
	        }

	        return Z;
        }
        
        private void FillCountries()
        {
            TLDNames.Add("ac", "Ascension");
            TLDNames.Add("ad", "Andorra");
            TLDNames.Add("ae", "United Arab Emirates");
            TLDNames.Add("af", "Afghanistan");
            TLDNames.Add("ag", "Antigua and Barbuda");
            TLDNames.Add("ai", "Anguilla");
            TLDNames.Add("al", "Albania");
            TLDNames.Add("am", "Armenia");
            TLDNames.Add("ao", "Angola");
            TLDNames.Add("aq", "Antartica");
            TLDNames.Add("ar", "Argentina");
            TLDNames.Add("as", "American Samoa");
            TLDNames.Add("at", "Austria");
            TLDNames.Add("au", "Australia");
            TLDNames.Add("aw", "Aruba");
            TLDNames.Add("az", "Azerbaijan");
            TLDNames.Add("ba", "Bosnia and Herzegovina");
            TLDNames.Add("bb", "Barbados");
            TLDNames.Add("bd", "Bangladesh");
            TLDNames.Add("be", "Belgium");
            TLDNames.Add("bf", "Burkina Faso");
            TLDNames.Add("bg", "Bulgaria");
            TLDNames.Add("bh", "Bahrain");
            TLDNames.Add("bi", "Burundi");
            TLDNames.Add("bj", "Benin");
            TLDNames.Add("bm", "Bermuda");
            TLDNames.Add("bn", "Brunei Darussalam");

            TLDNames.Add("bo", "Bolivia");
            TLDNames.Add("br", "Brazil");
            TLDNames.Add("bs", "Bahamas");
            TLDNames.Add("bt", "Bhutan");
            TLDNames.Add("bv", "Bouvet Island");
            TLDNames.Add("bw", "Botswana");
            TLDNames.Add("by", "Belarus");
            TLDNames.Add("bz", "Belize");

            TLDNames.Add("ca", "Canada");
            TLDNames.Add("cc", "Cocos (Keeling) Islands");
            TLDNames.Add("cd", "Congo, Democratic Republic of the");
            TLDNames.Add("cf", "Central African Republic");
            TLDNames.Add("cg", "Congo, Republic of");
            TLDNames.Add("ch", "Switzerland");
            TLDNames.Add("ci", "Cote d'Ivoire");
            TLDNames.Add("ck", "Cook Islands");
            TLDNames.Add("cl", "Chile");
            TLDNames.Add("cm", "Cameroon");
            TLDNames.Add("cn", "China");
            TLDNames.Add("co", "Colombia");
            TLDNames.Add("cr", "Costa Rica");
            TLDNames.Add("cu", "Cuba");
            TLDNames.Add("cv", "Cap Verde");
            TLDNames.Add("cx", "Christmas Island");
            TLDNames.Add("cy", "Cyprus");
            TLDNames.Add("cz", "Czech Republic");

            TLDNames.Add("de", "Germany");
            TLDNames.Add("dj", "Djibouti");
            TLDNames.Add("dk", "Denmark");
            TLDNames.Add("dm", "Dominica");
            TLDNames.Add("do", "Dominican Republic");
            TLDNames.Add("dz", "Algeria");

            TLDNames.Add("ec", "Ecuador");
            TLDNames.Add("ee", "Estonia");
            TLDNames.Add("eg", "Egypt");
            TLDNames.Add("eh", "Western Sahara");
            TLDNames.Add("er", "Eritrea");
            TLDNames.Add("es", "Spain");
            TLDNames.Add("et", "Ethiopia");

            TLDNames.Add("fi", "Finland");
            TLDNames.Add("fj", "Fiji");
            TLDNames.Add("fk", "Falkland Islands (Malvina)");
            TLDNames.Add("fm", "Micronesia, Federal State of");
            TLDNames.Add("fo", "Faroe Islands");
            TLDNames.Add("fr", "France");

            TLDNames.Add("ga", "Gabon");
            TLDNames.Add("gd", "Grenada");
            TLDNames.Add("ge", "Georgia");
            TLDNames.Add("gf", "French Guiana");
            TLDNames.Add("gg", "Guernsey");
            TLDNames.Add("gh", "Ghana");
            TLDNames.Add("gi", "Gibraltar");
            TLDNames.Add("gl", "Greenland");
            TLDNames.Add("gm", "Gambia");
            TLDNames.Add("gn", "Guinea");
            TLDNames.Add("gp", "Guadeloupe");
            TLDNames.Add("gq", "Equatorial Guinea");
            TLDNames.Add("gr", "Greece");
            TLDNames.Add("gs", "South Georgia and the South Sandwich Islands");
            TLDNames.Add("gt", "Guatemala");
            TLDNames.Add("gu", "Guam");
            TLDNames.Add("gw", "Guinea-Bissau");
            TLDNames.Add("gy", "Guyana");

            TLDNames.Add("hk", "Hong Kong");
            TLDNames.Add("hm", "Heard and McDonald Islands");
            TLDNames.Add("hn", "Honduras");
            TLDNames.Add("hr", "Croatia/Hrvatska");
            TLDNames.Add("ht", "Haiti");
            TLDNames.Add("hu", "Hungary");

            TLDNames.Add("id", "Indonesia");
            TLDNames.Add("ie", "Ireland");
            TLDNames.Add("il", "Israel");
            TLDNames.Add("im", "Isle of Man");
            TLDNames.Add("in", "India");
            TLDNames.Add("io", "British Indian Ocean Territory");
            TLDNames.Add("iq", "Iraq");
            TLDNames.Add("ir", "Iran (Islamic Republic of)");
            TLDNames.Add("is", "Iceland");
            TLDNames.Add("it", "Italy");

            TLDNames.Add("je", "Jersey");
            TLDNames.Add("jm", "Jamaica");
            TLDNames.Add("jo", "Jordan");
            TLDNames.Add("jp", "Japan");

            TLDNames.Add("ke", "Kenya");
            TLDNames.Add("kg", "Kyrgyzstan");
            TLDNames.Add("kh", "Cambodia");
            TLDNames.Add("ki", "Kiribati");
            TLDNames.Add("km", "Comoros");
            TLDNames.Add("kn", "Saint Kitts and Nevis");
            TLDNames.Add("kp", "Korea, Democratic People's Republic");
            TLDNames.Add("kr", "Korea, Republic of");
            TLDNames.Add("kw", "Kuwait");
            TLDNames.Add("ky", "Cayman Islands");
            TLDNames.Add("kz", "Kazakhstan");

            TLDNames.Add("la", "Lao People's Democratic Republic");
            TLDNames.Add("lb", "Lebanon");
            TLDNames.Add("lc", "Saint Lucia");
            TLDNames.Add("li", "Liechtenstein");
            TLDNames.Add("lk", "Sri Lanka");
            TLDNames.Add("lr", "Liberia");
            TLDNames.Add("ls", "Lesotho");
            TLDNames.Add("lt", "Lithuania");
            TLDNames.Add("lu", "Luxembourg");
            TLDNames.Add("lv", "Latvia");
            TLDNames.Add("ly", "Libyan Arab Jamahiriya");

            TLDNames.Add("ma", "Morocco");
            TLDNames.Add("mc", "Monaco");
            TLDNames.Add("md", "Moldova, Republic of");
            TLDNames.Add("mg", "Madagascar");
            TLDNames.Add("mh", "Marshall Islands");
            TLDNames.Add("mk", "Macedonia, Former Yugoslav Republic");
            TLDNames.Add("ml", "Mali");
            TLDNames.Add("mm", "Myanmar");
            TLDNames.Add("mn", "Mongolia");
            TLDNames.Add("mo", "Macau");
            TLDNames.Add("mp", "Northern Mariana Islands");
            TLDNames.Add("mq", "Martinique");
            TLDNames.Add("mr", "Mauritania");
            TLDNames.Add("ms", "Montserrat");
            TLDNames.Add("mt", "Malta");
            TLDNames.Add("mu", "Mauritius");
            TLDNames.Add("mv", "Maldives");
            TLDNames.Add("mw", "Malawi");
            TLDNames.Add("mx", "Mexico");
            TLDNames.Add("my", "Malaysia");
            TLDNames.Add("mz", "Mozambique");

            TLDNames.Add("na", "Namibia");
            TLDNames.Add("nc", "New Caledonia");
            TLDNames.Add("ne", "Niger");
            TLDNames.Add("nf", "Norfolk Island");
            TLDNames.Add("ng", "Nigeria");
            TLDNames.Add("ni", "Nicaragua");
            TLDNames.Add("nl", "Netherlands");
            TLDNames.Add("no", "Norway");
            TLDNames.Add("np", "Nepal");
            TLDNames.Add("nr", "Nauru");
            TLDNames.Add("nu", "Niue");
            TLDNames.Add("nz", "New Zealand");

            TLDNames.Add("om", "Oman");

            TLDNames.Add("pa", "Panama");
            TLDNames.Add("pe", "Peru");
            TLDNames.Add("pf", "French Polynesia");
            TLDNames.Add("pg", "Papua New Guinea");
            TLDNames.Add("ph", "Philippines");
            TLDNames.Add("pk", "Pakistan");
            TLDNames.Add("pl", "Poland");
            TLDNames.Add("pm", "St. Pierre and Miquelon");
            TLDNames.Add("pn", "Pitcairn Island");
            TLDNames.Add("pr", "Puerto Rico");
            TLDNames.Add("ps", "Palestinian Territories");
            TLDNames.Add("pt", "Portugal");
            TLDNames.Add("pw", "Palau");
            TLDNames.Add("py", "Paraguay");

            TLDNames.Add("qa", "Qatar");

            TLDNames.Add("re", "Reunion Island");
            TLDNames.Add("ro", "Romania");
            TLDNames.Add("ru", "Russian Federation");
            TLDNames.Add("rw", "Rwanda");

            TLDNames.Add("sa", "Saudi Arabia");
            TLDNames.Add("sb", "Solomon Islands");
            TLDNames.Add("sc", "Seychelles");
            TLDNames.Add("sd", "Sudan");
            TLDNames.Add("se", "Sweden");
            TLDNames.Add("sg", "Singapore");
            TLDNames.Add("sh", "St. Helena");
            TLDNames.Add("si", "Slovenia");
            TLDNames.Add("sj", "Svalbard and Jan Mayen Islands");
            TLDNames.Add("sk", "Slovak Republic");
            TLDNames.Add("sl", "Sierra Leone");
            TLDNames.Add("sm", "San Marino");
            TLDNames.Add("sn", "Senegal");
            TLDNames.Add("so", "Somalia");
            TLDNames.Add("sr", "Suriname");
            TLDNames.Add("st", "Sao Tome and Principe");
            TLDNames.Add("sv", "El Salvador");
            TLDNames.Add("sy", "Syrian Arab Republic");
            TLDNames.Add("sz", "Swaziland");

            TLDNames.Add("tc", "Turks and Caicos Islands");
            TLDNames.Add("td", "Chad");
            TLDNames.Add("tf", "French Southern Territories");
            TLDNames.Add("tg", "Togo");
            TLDNames.Add("th", "Thailand");
            TLDNames.Add("tj", "Tajikistan");
            TLDNames.Add("tk", "Tokelau");
            TLDNames.Add("tm", "Turkmenistan");
            TLDNames.Add("tn", "Tunisia");
            TLDNames.Add("to", "Tonga");
            TLDNames.Add("tp", "East Timor");
            TLDNames.Add("tr", "Turkey");
            TLDNames.Add("tt", "Trinidad and Tobago");
            TLDNames.Add("tv", "Tuvalu");
            TLDNames.Add("tw", "Taiwan");
            TLDNames.Add("tz", "Tanzania");

            TLDNames.Add("ua", "Ukraine");
            TLDNames.Add("ug", "Uganda");
            TLDNames.Add("uk", "United Kingdom");
            TLDNames.Add("um", "US Minor Outlying Islands");
            TLDNames.Add("us", "United States");
            TLDNames.Add("uy", "Uruguay");
            TLDNames.Add("uz", "Uzbekistan");

            TLDNames.Add("va", "Holy See (City Vatican State)");
            TLDNames.Add("vc", "Saint Vincent and the Grenadines");
            TLDNames.Add("ve", "Venezuela");
            TLDNames.Add("vg", "Virgin Islands (British)");
            TLDNames.Add("vi", "Virgin Islands (USA)");
            TLDNames.Add("vn", "Vietnam");
            TLDNames.Add("vu", "Vanuatu");
            TLDNames.Add("wf", "Wallis and Futuna Islands");
            TLDNames.Add("ws", "Western Samoa");

            TLDNames.Add("ye", "Yemen");
            TLDNames.Add("yt", "Mayotte");
            TLDNames.Add("yu", "Yugoslavia");

            TLDNames.Add("za", "South Africa");
            TLDNames.Add("zm", "Zambia");
            TLDNames.Add("zw", "Zimbabwe");

            TLDNames.Add("biz", "Business");
            TLDNames.Add("com", "US-Commercial");
            TLDNames.Add("info", "Information");
            TLDNames.Add("museum", "Museum");
            TLDNames.Add("name", "Individual Person");
            TLDNames.Add("net", "Net");
            TLDNames.Add("org", "US-Organization");
            TLDNames.Add("gov", "US-Government");
            TLDNames.Add("edu", "US-Education");
            TLDNames.Add("mil", "US-Military");
            TLDNames.Add("int", "International Organizations");

            TLDNames.Add("eu", "Europe");

            DC_flag = true;
        }

        private void FillExtensions()
        {
            string m_tPages = "htm|html|asp|aspx|shtm|shtml|htms|php|php3|php4|phtml|cgi|pl|cfm|cfml|jsp|nsf|htx|hta|ihtml|ghtml|sht|mspx";
            string m_tImages = "gif|jpg|jpeg|jpe|bmp|png|lwf|tif|tiff";
            string m_tMedia = "wav|au|pdf|txt|mid|midi|mp3|xm|mod|s3m";
            string m_tBinary = "zip|exe|arj|rar|gz|tar|tgz|z|bin|class|js|java|cab|sit|hqx|uu|uue";

            string[] s;

            s = Strings.Split(m_tPages, "|");
            foreach (string ss in s)
                KnownExtensions.Add(ss, DocumentTypes.Page);

            s = Strings.Split(m_tImages, "|");
            foreach (string ss in s)
                KnownExtensions.Add(ss, DocumentTypes.Image);            

            s = Strings.Split(m_tMedia, "|");
            foreach (string ss in s)
                KnownExtensions.Add(ss, DocumentTypes.Media);

            s = Strings.Split(m_tBinary, "|");
            foreach (string ss in s)
                KnownExtensions.Add(ss, DocumentTypes.Binary);                                     
        }


        public static string FixLink(string S, ref KSURL PageURL)
        {
            string strURL = S;

            string R = "";
            string S2;

            if (strURL == "")
                return R;

            KSURL pURL = new KSURL();
            pURL.URL = S;

            if (pURL.ProtocolType == ProtocolTypes.unknown)
            {
                if (Strings.StrLeft(strURL, 1) == "/")
                    S2 = PageURL.ToServer() + strURL;
                else
                    S2 = PageURL.ToServer() + PageURL.Path + strURL;
            }
            else
                S2 = S;

            pURL.URL = S2;
            R = pURL.ToURL(true, true);

            return R;
        }
    }
}
