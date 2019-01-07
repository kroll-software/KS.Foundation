using System;
using System.Collections.Generic;
using System.Text;

namespace KS.Foundation.HtmlHelpers
{
    public enum HttpStates
    {
        idle = 0,
        sending = 1,
        fetching = 2
    }

    public class KSHttp
    {
        public delegate void StateChangedEventHandler(HttpStates State);
        private StateChangedEventHandler StateChangedEvent = null;
        public event StateChangedEventHandler StateChanged
        {
            add
            {
                StateChangedEvent = (StateChangedEventHandler)System.Delegate.Combine(StateChangedEvent, value);
            }
            remove
            {
                StateChangedEvent = (StateChangedEventHandler)System.Delegate.Remove(StateChangedEvent, value);
            }
        }


        private HttpStates m_State;

        private int m_TimeOut;
        private string m_URL;
        private string m_ProxyHost;
        private string m_ProxyPort;
        private string m_ProxyUser;
        private string m_ProxyPassword;

        private string m_UserAgent;
        private string m_Accept;        

        private string m_Content;
        private string m_PlainText;

        private long m_nBytesSent;
        private long m_nBytesReceived;        
        private bool m_AllowRedirect;        
        private bool bCancel;

        private int m_ReplyCode;
        private object m_LastModified;
        private long m_ContentLength;
        private string m_ContentType;

        public string Tag;


        #region " Public Properties "

        public HttpStates State
        {
            get
            {
                return m_State;
            }
        }

        public int ReplyCode
        {
            get
            {
                return m_ReplyCode;
            }
        }

        public object LastModified
        {
            get
            {
                return m_LastModified;
            }
        }


        public long ContentLength
        {
            get
            {
                return m_ContentLength;
            }
        }

        public string ContentType
        {
            get
            {
                return m_ContentType;
            }
        }        

        public int TimeOut
        {
            get 
            { 
                return m_TimeOut;
            }
            set 
            {
                m_TimeOut = value;
            }
        }

        public string UserAgent
        {
            get
            {
                return m_UserAgent;
            }
            set
            {
                m_UserAgent = value;
            }
        }

        public string Accept
        {
            get
            {
                return m_Accept;
            }
            set
            {
                m_Accept = value;
            }
        }

        public string Content
        {
            get
            {
                return m_Content;
            }
        }

        public string PlainText
        {
            get
            {
                return m_PlainText;
            }
        }        

        public string ProxyHost
        {
            get
            {
                return m_ProxyHost;
            }
            set
            {
                m_ProxyHost = value;
            }
        }

        public string ProxyPort
        {
            get
            {
                return m_ProxyPort;
            }
            set
            {
                m_ProxyPort = value;
            }
        }

        public string ProxyUser
        {
            get
            {
                return m_ProxyUser;
            }
            set
            {
                m_ProxyUser = value;
            }
        }

        public string ProxyPassword
        {
            get
            {
                return m_ProxyPassword;
            }
            set
            {
                m_ProxyPassword = value;
            }
        }

        public long BytesReceived
        {
            get
            {
                return m_nBytesReceived;
            }
        }

        public long BytesSent
        {
            get
            {
                return m_nBytesSent;
            }
        }

        

        public bool AllowRedirect
        {
            get
            {
                return m_AllowRedirect;
            }
            set
            {
                m_AllowRedirect = value;
            }
        }

        public string URL
        {
            get
            {
                return m_URL;
            }
        }

        #endregion

        // **************** KSHTTP ***********************

        public KSHttp()
        {                        
            Reset();
        }

        public void Reset()
        {            
            m_TimeOut = 10;
            m_URL = "";
            m_ProxyHost = "";
            m_ProxyPort = "";
            m_ProxyUser = "";
            m_ProxyPassword = "";

            m_UserAgent = "Mozilla/4.0";
            m_Accept = "text/*";            

            m_Content = "";
            m_PlainText = "";
            m_ReplyCode = 0;
            m_LastModified = null;
            m_ContentLength = 0;
            m_ContentType = "";


            m_nBytesSent = 0;
            m_nBytesReceived = 0;            
            m_AllowRedirect = true;
            m_State = HttpStates.idle;
            
            bCancel = false;
            
            FireStateChanged(m_State);
        }


        public static bool IEProxyEnabled()
        {            
            try
            {                
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentConfig.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");
                return ((int)(key.GetValue("ProxyEnable", 0)) != 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string IEProxy()
        {
            try
            {                
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentConfig.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");
                return (string)(key.GetValue("ProxyServer", ""));
            }
            catch (Exception)
            {
                return "";
            }
        }

        private void SetProxy(ref System.Net.HttpWebRequest Request)
        {
            if (m_ProxyHost != "")
            {

                try
                {
                    System.Net.WebProxy P = new System.Net.WebProxy(m_ProxyHost + ":" + m_ProxyPort.SafeInt() + "/", true);

                    if (m_ProxyUser != "")
                    {
                        P.Credentials = new System.Net.NetworkCredential(m_ProxyUser, m_ProxyPassword);
                    }

                    Request.Proxy = P;

                }
                catch (Exception ex)
                {
					ex.LogError ();
                    // modGlobal.ErrMsgBox(ex.Message);
                    //Request.Proxy = System.Net.GlobalProxySelection.GetEmptyWebProxy();
                    Request.Proxy = null;
                }

            }
            else
            {
                Request.Proxy = Request.Proxy = null; ;
            }
        }

        public string GetPage(string url)
        {            
            return GetHtmlPage(url, false);
        }

        public string GetHead(string url)
        {
            return GetHtmlPage(url, true);
        }

        private string GetHtmlPage(string url, bool bHeadOnly)
        {
            m_URL = url;
            string result = "";

            System.Net.HttpWebRequest Request;            

            m_State = HttpStates.sending;
            FireStateChanged(m_State);

            try
            {
                Request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            }
            catch (Exception ex)
            {
                //modGlobal.ErrMsgBox(ex.Message);
                throw ex;
                //return "";
            }

            System.IO.StreamReader readStream = null;

            System.Text.Encoding Enc = System.Text.Encoding.GetEncoding(1252);

            SetProxy(ref Request);
            Request.KeepAlive = false;
            Request.ProtocolVersion = System.Net.HttpVersion.Version10;
            Request.Method = "GET";

            Request.AllowAutoRedirect = m_AllowRedirect;
            Request.MaximumAutomaticRedirections = 10;
            Request.Timeout = this.m_TimeOut * 1000;
            Request.UserAgent = this.m_UserAgent;

            try
            {
                System.Net.HttpWebResponse Response = (System.Net.HttpWebResponse)Request.GetResponse();                
                m_nBytesSent += Request.ContentLength;

                m_ReplyCode = (int)Response.StatusCode;
                m_LastModified = Response.LastModified;
                m_ContentLength = Response.ContentLength;
                m_ContentType = Response.ContentType;
                                
                if (bHeadOnly)
                    return "";

                System.IO.Stream responseStream = Response.GetResponseStream();
                readStream = new System.IO.StreamReader(responseStream, Enc);

                // ----- Read Results ...
                m_State = HttpStates.fetching;
                FireStateChanged(m_State);

                System.Text.StringBuilder SB = new System.Text.StringBuilder();

                char[] read = new char[257];
                // Reads 256 characters at a time.
                int count = readStream.Read(read, 0, 256);
                m_nBytesReceived += count;

                //Console.WriteLine("HTML..." + ControlChars.Lf + ControlChars.Cr)
                while (count > 0 && !bCancel)
                {
                    // Dumps the 256 characters to a string and displays the string to the console.
                    string str = new string(read, 0, count);

                    SB.Append(str);
                    //Console.Write(str)
                    count = readStream.Read(read, 0, 256);
                    m_nBytesReceived += count;
                }

                result = SB.ToString();
                // ----- /Read Results ...

            }
            catch (Exception ex)
            {
                //modGlobal.ErrMsgBox(ex.Message);
                throw ex;
                //result = "";
            }
            finally
            {
                try
                {
                    if (readStream != null)
                    {
                        readStream.Close();
                        readStream = null;
                    }
                }
                catch (Exception)
                {
                }

                m_State = HttpStates.idle;
                FireStateChanged(m_State);
            }

            return result;
        }        

        public string PostHtmlPage(string url, string rparams)
        {
            m_URL = url;
            string result = "";
            System.IO.StreamWriter Writer = null;
            System.IO.StreamReader readStream = null;

            m_State = HttpStates.sending;
            FireStateChanged(m_State);

            System.Net.HttpWebRequest Request;

            try
            {
                Request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            }
            catch (Exception ex)
            {
                throw ex;
                //modGlobal.ErrMsgBox(ex.Message);
                //return "";
            }

            System.Text.Encoding Enc = System.Text.Encoding.GetEncoding(1252);

            SetProxy(ref Request);
            Request.KeepAlive = false;
            Request.ProtocolVersion = System.Net.HttpVersion.Version10;
            Request.Method = "POST";
            Request.ContentType = "application/x-www-form-urlencoded";
            Request.ContentLength = rparams.Length;            

            Request.AllowAutoRedirect = m_AllowRedirect;
            Request.MaximumAutomaticRedirections = 10;
            Request.Timeout = this.m_TimeOut * 1000;
            Request.UserAgent = this.m_UserAgent;

            try
            {
                Writer = new System.IO.StreamWriter(Request.GetRequestStream(), Enc);
                Writer.Write(rparams);
                Writer.Flush();
            }
            catch (Exception ex)
            {
                throw ex;
                //modGlobal.ErrMsgBox(ex.Message);
                //return "";
            }
            finally
            {
                try
                {
                    if (Writer != null)
                    {
                        Writer.Close();
                        Writer = null;
                    }
                }
                catch (Exception)
                {
                }
            }

            try
            {
                System.Net.HttpWebResponse Response = (System.Net.HttpWebResponse)Request.GetResponse();
                m_nBytesSent += Request.ContentLength;

                m_ReplyCode = (int)Response.StatusCode;
                m_LastModified = Response.LastModified;
                m_ContentLength = Response.ContentLength;
                m_ContentType = Response.ContentType;

                System.IO.Stream responseStream = Response.GetResponseStream();
                readStream = new System.IO.StreamReader(responseStream, Enc);

                // ----- Read Results ...
                m_State = HttpStates.fetching;
                FireStateChanged(m_State);

                System.Text.StringBuilder SB = new System.Text.StringBuilder();

                char[] read = new char[257];
                // Reads 256 characters at a time.
                int count = readStream.Read(read, 0, 256);
                m_nBytesReceived += count;

                //Console.WriteLine("HTML..." + ControlChars.Lf + ControlChars.Cr)
                while (count > 0 && !bCancel)
                {
                    // Dumps the 256 characters to a string and displays the string to the console.
                    string str = new string(read, 0, count);

                    SB.Append(str);
                    //Console.Write(str)
                    count = readStream.Read(read, 0, 256);
                    m_nBytesReceived += count;
                }

                result = SB.ToString();
                // ----- /Read Results ...

            }
            catch (Exception ex)
            {
                throw ex;
                //modGlobal.ErrMsgBox(ex.Message);
                //result = "";
            }
            finally
            {
                try
                {
                    if (readStream != null)
                    {
                        readStream.Close();
                        readStream = null;
                    }
                }
                catch (Exception)
                {
                }

                m_State = HttpStates.idle;
                FireStateChanged(m_State);
            }

            return result;
        }

        private void FireStateChanged(HttpStates State)
        {
            if (StateChangedEvent != null)
                StateChangedEvent(State);
        }

        public void Cancel()
        {
            bCancel = true;
        }
    }
}
