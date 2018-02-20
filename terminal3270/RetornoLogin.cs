using System;
using System.Collections.Generic;
using System.Text;

namespace terminal3270
{
    public class RetornoLogin		
    {
        private string _LU;
        private string _URL;
        private string _Mensagem;
        private bool _Sucesso;

        public string LU { get { return _LU; } set { _LU = value; } }

        public string URL { get { return _URL; } set { _URL = value; } }

        public string Mensagem { get { return _Mensagem; } set { _Mensagem = value; } }

        public bool Sucesso { get { return _Sucesso; } set { _Sucesso = value; } }
    }
}
