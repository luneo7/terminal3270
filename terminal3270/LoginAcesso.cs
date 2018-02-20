using System;
using System.Net;
using System.Text;
using System.IO;
using DavyKager;
using System.ComponentModel;
using System.Timers;
using log4net;

namespace terminal3270
{
    public class LoginAcesso
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LoginAcesso));

        private CookieContainer _cookies = new CookieContainer();
        string strCPF;
        string strSenha;
        string urlRefresh;
        private string _UserAgent;
        private string[] UserAgents = new string[12] { "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.101 Safari/537.36",
			"Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:25.0) Gecko/20100101 Firefox/25.0", 
			"Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)", 
			"Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0;)",
			"Mozilla/5.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0;WOW64)",
			"Mozilla/5.0 (Windows NT 6.0; WOW64; rv:24.0) Gecko/20100101 Firefox/24.0",
			"Mozilla/5.0 (Windows NT 6.2; rv:22.0) Gecko/20130405 Firefox/22.0",
			"Mozilla/5.0 (Windows NT 6.2; rv:22.0) Gecko/20130405 Firefox/23.0",
			"Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1667.0 Safari/537.36",
			"Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.16 Safari/537.36",
			"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1623.0 Safari/537.36",
			"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/29.0.1547.62 Safari/537.36" };

        private Random rnd = new Random();

        public string Usuario { get { return strCPF; }  }
        public string Senha { get { return strSenha; } }

        private readonly BackgroundWorker worker = new BackgroundWorker();
        private readonly Timer timer = new Timer(300000);

        public LoginAcesso()
        {
            worker.DoWork += worker_DoWork;
            timer.Elapsed += timer_Elapsed;
        }
        public RetornoLogin RealizarLogin(string strCPF, string strSenha)
        {
            RetornoLogin retLogin = new RetornoLogin();

            try
            {
                this.strCPF = strCPF;
                this.strSenha = strSenha;
                int r = rnd.Next(UserAgents.Length);
                _UserAgent = UserAgents[r];

                if (!Validadores.ValidaCPF(strCPF))
                {
                    retLogin.Sucesso = false;
                    retLogin.Mensagem = "CPF Inválido!";
                    return retLogin;
                }


                Encoding.UTF8.GetBytes("txtNumCpf=" + strCPF + "&txtSenha=" + strSenha + "&btnCancelar=Cancelar&btnAvancar=Avan%E7ar");

                byte[] byteArray = Encoding.UTF8.GetBytes("txtNumCpf=" + strCPF + "&txtSenha=" + strSenha);

                HttpWebRequest requisicao = (HttpWebRequest)HttpWebRequest.Create("https://acesso.serpro.gov.br/HOD10/jsp/logonJava.jsp");

                requisicao.UserAgent = _UserAgent;
                requisicao.CookieContainer = _cookies;
                requisicao.ContentType = "application/x-www-form-urlencoded";
                requisicao.Method = "POST";
                requisicao.ContentLength = byteArray.Length;
                requisicao.Referer = "https://acesso.serpro.gov.br/HOD10/jsp/logonID.jsp";
                requisicao.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                requisicao.AllowAutoRedirect = true;
                Stream streamDados = requisicao.GetRequestStream();
                streamDados.Write(byteArray, 0, byteArray.Length);
                streamDados.Close();

                String retornoHtml = "";
                using (HttpWebResponse resposta = (HttpWebResponse)requisicao.GetResponse())
                {
                    string charset = resposta.CharacterSet;
                    if (charset == "")
                    {
                        charset = "iso-8859-1";
                    }
                    Encoding responseEncoding = Encoding.GetEncoding(charset);
                    using (streamDados = resposta.GetResponseStream())
                    {
                        using (StreamReader leitorStream = new StreamReader(streamDados, responseEncoding))
                        {
                            retornoHtml = leitorStream.ReadToEnd();
                            int inicio = retornoHtml.IndexOf("top.location.href='");
                            if (inicio > 0)
                            {
                                inicio += 19;
                                int fim = retornoHtml.IndexOf("';", inicio);
                                retLogin.URL = retornoHtml.Substring(inicio, fim - inicio);
                                urlRefresh = retLogin.URL;
                                retLogin.LU = retLogin.URL.Substring(retLogin.URL.IndexOf("?luid=") + 6);
                            }
                            else
                            {
                                if (retornoHtml.Contains("Senha") && retornoHtml.Contains("confere"))
                                {
                                    retLogin.Mensagem = "Usuário/Senha inválido";
                                    retLogin.Sucesso = false;
                                }
                            }
                        }
                    }
                }

                if (!String.IsNullOrEmpty(retLogin.URL))
                {
                    requisicao = (HttpWebRequest)HttpWebRequest.Create(retLogin.URL);

                    requisicao.UserAgent = _UserAgent;
                    requisicao.CookieContainer = _cookies;
                    requisicao.ContentType = "application/x-www-form-urlencoded";
                    requisicao.Method = "GET";
                    requisicao.Referer = "https://acesso.serpro.gov.br/HOD10/jsp/logonID.jsp";
                    requisicao.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                    requisicao.AllowAutoRedirect = true;

                    using (HttpWebResponse resposta = (HttpWebResponse)requisicao.GetResponse())
                    {
                        string charset = resposta.CharacterSet;
                        if (charset == "")
                        {
                            charset = "iso-8859-1";
                        }
                        Encoding responseEncoding = Encoding.GetEncoding(charset);
                        using (streamDados = resposta.GetResponseStream())
                        {
                            using (StreamReader leitorStream = new StreamReader(streamDados, responseEncoding))
                            {
                                retornoHtml = leitorStream.ReadToEnd();
                                retLogin.Sucesso = true;                                
                                if (timer != null && timer.Enabled)
                                {
                                    timer.Stop();
                                }
                                timer.Start();
                            }
                        }
                    }
                }
                else
                {
                    retLogin.Sucesso = false;
                }
            }
            catch (Exception ex)
            {
                log.Error("Erro ao tentar realizar login (loginAcesso.RealizarLogin)", ex);
                retLogin.Sucesso = false;
                retLogin.Mensagem = ex.ToString();
            }

            return retLogin;
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!worker.IsBusy)
                worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                HttpWebRequest requisicao = (HttpWebRequest)HttpWebRequest.Create(urlRefresh);

                requisicao.UserAgent = _UserAgent;
                requisicao.CookieContainer = _cookies;
                requisicao.ContentType = "application/x-www-form-urlencoded";
                requisicao.Method = "GET";
                requisicao.Referer = urlRefresh;
                requisicao.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                requisicao.AllowAutoRedirect = true;

                using (HttpWebResponse resposta = (HttpWebResponse)requisicao.GetResponse())
                {
                    string charset = resposta.CharacterSet;
                    if (charset == "")
                    {
                        charset = "iso-8859-1";
                    }
                    Encoding responseEncoding = Encoding.GetEncoding(charset);
                    using (Stream streamDados = resposta.GetResponseStream())
                    {
                        using (StreamReader leitorStream = new StreamReader(streamDados, responseEncoding))
                        {
                            string retornoHtml = leitorStream.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Erro ao tentar manter sessão de login ativa (loginAcesso.workder_DoWork)", ex);
                timer.Stop();
            }
        }

    }
}

