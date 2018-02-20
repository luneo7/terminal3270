using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace terminal3270
{
    public static class Validadores
    {
        public static bool ValidaCPF(string strCPF)
        {
            try
            {
                int Soma;
                int Resto;
                Soma = 0;
                strCPF = new Regex(@"[^\d]").Replace(strCPF, "").Trim();
                if (strCPF.Length != 11)
                    return false;
                if (strCPF == "00000000000")
                    return false;

                for (int i = 1; i <= 9; i++)
                    Soma = Soma + Convert.ToInt32(strCPF.Substring(i - 1, 1)) * (11 - i);
                Resto = (Soma * 10) % 11;
                if ((Resto == 10) || (Resto == 11))
                    Resto = 0;
                if (Resto != Convert.ToInt32(strCPF.Substring(9, 1)))
                    return false;
                Soma = 0;
                for (int i = 1; i <= 10; i++)
                    Soma = Soma + Convert.ToInt32(strCPF.Substring(i - 1, 1)) * (12 - i);
                Resto = (Soma * 10) % 11;
                if ((Resto == 10) || (Resto == 11))
                    Resto = 0;
                if (Resto != Convert.ToInt32(strCPF.Substring(10, 1)))
                    return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ValidaCNPJ(string strCNPJ)
        {
            try
            {
                strCNPJ = new Regex(@"[^\d]").Replace(strCNPJ, "").Trim();
                if (strCNPJ.Length != 14)
                    return false;
                int tamanho = strCNPJ.Length - 2;
                string numeros = strCNPJ.Substring(0, tamanho);
                var digitos = strCNPJ.Substring(tamanho);
                int soma = 0;
                int pos = tamanho - 7;
                for (int i = tamanho; i >= 1; i--)
                {
                    soma += Convert.ToInt32(numeros[tamanho - i].ToString()) * pos--;
                    if (pos < 2)
                        pos = 9;
                }
                int resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
                if (resultado != Convert.ToInt32(digitos[0].ToString()))
                    return false;

                tamanho = tamanho + 1;
                numeros = strCNPJ.Substring(0, tamanho);
                soma = 0;
                pos = tamanho - 7;
                for (int i = tamanho; i >= 1; i--)
                {
                    soma += Convert.ToInt32(numeros[tamanho - i].ToString()) * pos--;
                    if (pos < 2)
                        pos = 9;
                }
                resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
                if (resultado != Convert.ToInt32(digitos[1].ToString()))
                    return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
