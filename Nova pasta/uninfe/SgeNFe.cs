using System;
using System.Management;
using Microsoft.Win32;


namespace uninfe
{
    class ClassSge
    {
        private RegistryKey RegistroDoWindows;
        public bool bErro;
        public string sErro;

        public bool VerificaRegistro()
        {
            bErro = false;
            sErro = "";
            bool bOK = true;
            try
            {
                RegistroDoWindows = Registry.CurrentUser.OpenSubKey("SgeNFe", true);
                if (RegistroDoWindows == null) bOK = false;
            }
            catch (Exception e)
            {
                bErro = true;
                sErro = e.Message;
            }
            return bOK;
        }


        public bool SalvaRegistro(string Registro, string Chave)
        {
            bool bRetorno = true;
            try
            {
                RegistroDoWindows = Registry.CurrentUser.OpenSubKey("SgeNFe", true);
                if (RegistroDoWindows == null) RegistroDoWindows = Registry.CurrentUser.CreateSubKey("SgeNFe");
                RegistroDoWindows.SetValue("Registro", Registro);
                RegistroDoWindows.SetValue("Chave", Chave);
            }
            catch (Exception e)
            {
                bRetorno = false;
                bErro = true;
                sErro = e.Message;
            }
            return bRetorno;
        }

        public bool RegistroOK()
        {
            bool bRetorno = false;
            RegistroDoWindows = Registry.CurrentUser.OpenSubKey("SgeNFe", true);
            string Registro = RegistroDoWindows.GetValue("Registro").ToString();
            string ChaveReg = RegistroDoWindows.GetValue("Chave").ToString();
            /*string ChaveCal = GerarChave(Registro);*/
            string hdReg = GetParam(Registro, '-', 2);
            string hdLocal = GetNSerieHD();
            bRetorno = ChaveReg.Trim() != "";
            /* bRetorno = ChaveReg == ChaveCal; */
            if (bRetorno == true) bRetorno = hdReg == hdLocal;
            return bRetorno;
        }

        public string GetNSerieHD()
        {
            string sHD = "";
            ManagementObject disk = new ManagementObject("win32_logicaldisk.deviceid=\"c:\"");
            disk.Get();
            sHD = disk["VolumeSerialNumber"].ToString();
            return sHD;
        }

        public string SoNumero(String sTexto)
        {
            string sResult = "";
            string Numeros = "0123456789";
            sTexto = sTexto.Trim();
            for (int i = 0; i <= (sTexto.Length - 1); i++)
            {
                if (Numeros.IndexOf(sTexto[i])!=-1)
                {
                    sResult = sResult + sTexto[i];
                }
            }
            return sResult;
        }

        public char GetChar(char chSouce, String Dicionario, int intShift)
        {
          char cRet = chSouce;
          int i = 0;
          int j = Dicionario.Length-1;
          int i2;
          while (i <= j)
          {
              if (Dicionario[i] == chSouce)
              {
                  i2 = (i + 1 + intShift) % (j+1);
                  if ((i2 != (i + 1)))
                  {
                      if ((i2 - 1) < 0) cRet = '\0';
                      else cRet = Dicionario[i2 - 1];
                  }
                  else cRet = Dicionario[j - i2];
                  break;
              }
              else i++;
          }
          return cRet;
        }

        public string GerarChave(String Registro)
        {
            string sDicionario = "0123456789";
            string sRetorno = "";
            DateTime Data = DateTime.Now;
            string sData = Data.ToString("ddMMyyyy");
            int intShist = int.Parse(sData);
            int iLenReg = Registro.Length-1;
            int j=0;
            char c;
            if (iLenReg!=0)
            {
                for (int i = 0; i <= iLenReg; i++)
                {
                    c = GetChar(Registro[i], sDicionario, intShist);
                    if (c != '\0') sRetorno = sRetorno + c;
                    j++;
                    if (j > (sData.Length-1)) j = 0;
                }

            }
            return SoNumero(sRetorno.Trim());
        }

        public string GetParam(String sString, char cDelimitador, int index)
        {
            string[] valores = sString.Split(new char[] { cDelimitador });
            return valores[index-1];
        }

        public string RetornaUm(string Texto)
        {
            int j = Texto.Length-1;
            int i2;
            string s = "";
            for (int i = 0; i <= j; i++)          
                s = s + (Convert.ToInt32(Texto[i]));
            while (s.Length > 1)
            {
                j = s.Length-1;
                i2 = 0;
                for (int i = 0; i <= j; i++) 
                {
                    i2 = i2 + int.Parse(s[i].ToString());
                }
                s = Convert.ToString(i2);
            }
            i2 = int.Parse(s);
            i2 = 10 - i2;
            s = Convert.ToString(i2);
            return s;
        }

        public string GeraDigitoVerificadorRegistro(string sRegistro)
        {
            string sSys = GetParam(sRegistro, '-', 1);
            string sHD = GetParam(sRegistro, '-', 2);
            string sOutro = GetParam(sRegistro, '-', 3);
            return RetornaUm(sSys)+RetornaUm(sHD)+RetornaUm(sOutro);
        }

    }
}
