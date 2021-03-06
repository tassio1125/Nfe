﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

using NFe.Components;

namespace NFe.ConvertTxt
{
    public class ConversaoTXT
    {
        #region --- public properties

        public List<txtTOxmlClassRetorno> cRetorno = null;
        public string cMensagemErro { get; private set; }

        #endregion

        #region -- private proprieties

        private NFe NFe = null;
        private string FSegmento;
        private int LinhaLida;
        private string Registro;
        private string layout;
        private string chave;
        /// <summary>
        /// conteudo do arquivo de cada nota
        /// </summary>
        private Dictionary<int, List<string>> xConteudoArquivo;

        #endregion

        public ConversaoTXT() 
        {
            this.xConteudoArquivo = new Dictionary<int, List<string>>();
            this.cRetorno = new List<txtTOxmlClassRetorno>();
            this.cMensagemErro = "";
            this.LinhaLida = 0;
        }

        /// <summary>
        /// CarregarArquivo
        /// </summary>
        private bool CarregarArquivo(string cArquivo)
        {
            if (File.Exists(cArquivo))
            {
                TextReader txt = new StreamReader(cArquivo, Encoding.Default, true);
                try
                {
                    int nNota = -1;
                    string cLinhaTXT = txt.ReadLine();
                    if (cLinhaTXT != null)
                    {
                        if (!cLinhaTXT.StartsWith("NOTAFISCAL") && !cLinhaTXT.StartsWith("NOTA FISCAL"))
                        {
                            this.cMensagemErro = " Conteúdo da primeira linha do arquivo deve ser 'NOTAFISCAL'";
                        }
                        cLinhaTXT = txt.ReadLine();
                        this.LinhaLida = 1;
                    }
                    while (cLinhaTXT != null)
                    {
                        ++LinhaLida;

                        if (cLinhaTXT.Trim().Length > 0)
                        {
                            if (cLinhaTXT.StartsWith("A|"))
                            {
                                ++nNota;
                                xConteudoArquivo.Add(nNota, new List<string>());
                            }
                            List<string> temp;
                            xConteudoArquivo.TryGetValue(nNota, out temp);
                            temp.Add("§" + cLinhaTXT.Trim() + (!cLinhaTXT.EndsWith("|") ? "|" : ""));
                        }
                        cLinhaTXT = txt.ReadLine();
                    }
                }
                catch (IOException ex)
                {
                    this.cMensagemErro += ex.Message;
                }
                catch (Exception ex)
                {
                    this.cMensagemErro += ex.Message;
                }
                finally
                {
                    txt.Close();
                }
            }
            else
                this.cMensagemErro = "Arquivo [" + cArquivo + "] não encontrado";

            return ((this.xConteudoArquivo.Count == 0 || !string.IsNullOrEmpty(this.cMensagemErro)) ? false : true);
        }

        /// <summary>
        /// Converter
        /// </summary>
        public bool Converter(string cArquivo, string cFolderDestino)//, string cFolderRetorno)
        {
            cRetorno.Clear();

            if (this.CarregarArquivo(cArquivo))
            {
                this.LinhaLida = 0;
                foreach (List<string> content in this.xConteudoArquivo.Values)
                {
                    NFe = null;
                    NFe = new NFe();
                    bool houveErro = false;

                    foreach (string xContent in content)
                    {
                        houveErro = false;
                        ++this.LinhaLida;
                        try
                        {
                            ///
                            /// processa o TXT
                            /// 
                            this.LerRegistro(xContent);
                        }
                        catch(Exception ex)
                        {
                            houveErro = true;
                            this.cMensagemErro += "Layout: " + this.layout.Replace("§", "") + Environment.NewLine;
                            this.cMensagemErro += "Linha lida: " + (this.LinhaLida+1).ToString()+ Environment.NewLine+
                                                    "Conteudo: " + xContent.Substring(1) + Environment.NewLine +
                                                    ex.Message + Environment.NewLine;
                        }
                    }
                    

                    if (!houveErro && this.cMensagemErro == "")
                    {
                        NFeW nfew = new NFeW();
                        try
                        {
                            nfew.cMensagemErro = this.cMensagemErro;
                            ///
                            /// gera o XML da nota
                            /// 
                            nfew.GerarXml(NFe, cFolderDestino);//cFolderRetorno);
                            if (nfew.cFileName != "")
                            {
                                ///
                                /// Adiciona o XML na lista de arquivos convertidos
                                /// 
                                this.cRetorno.Add(new txtTOxmlClassRetorno(nfew.cFileName, NFe.infNFe.ID, NFe.ide.nNF, NFe.ide.serie));
                            }
                        }
                        catch (Exception ex)
                        {
                            nfew.cMensagemErro += ex.Message;
                        }
                        this.cMensagemErro = nfew.cMensagemErro;
                    }

                    if (this.cMensagemErro != "")
                    {
                        ///
                        /// exclui os arquivos gerados
                        /// 
                        foreach (txtTOxmlClassRetorno txtClass in this.cRetorno)
                        {
                            string dArquivo = txtClass.XMLFileName;
                            if (File.Exists(dArquivo))
                            {
                                FileInfo fi = new FileInfo(dArquivo);
                                fi.Delete();
                            }
                        }
                    }
                }
                return string.IsNullOrEmpty(this.cMensagemErro);
            }
            return false;
        }

        /// <summary>
        /// getDateTime
        /// </summary>
        public DateTime getDateTime(TpcnTipoCampo Tipo, string value)
        {
            if (string.IsNullOrEmpty(value))
                return DateTime.MinValue;

            try
            {
                int _ano = Convert.ToInt16(value.Substring(0, 4));
                int _mes = Convert.ToInt16(value.Substring(5, 2));
                int _dia = Convert.ToInt16(value.Substring(8, 2));
                if (Tipo == TpcnTipoCampo.tcDatHor && value.Contains(":"))
                {
                    int _hora = Convert.ToInt16(value.Substring(11, 2));
                    int _min = Convert.ToInt16(value.Substring(14, 2));
                    int _seg = Convert.ToInt16(value.Substring(17, 2));
                    return new DateTime(_ano, _mes, _dia, _hora, _min, _seg);
                }
                return new DateTime(_ano, _mes, _dia);
            }
            catch
            {
                throw new Exception("Data inválida do conteudo [" + value + "]");
            }
        }

        /// <summary>
        /// getDateTime2
        /// </summary>
        public DateTime getDate2(TpcnTipoCampo Tipo, string value)
        {
            if (string.IsNullOrEmpty(value))
                return DateTime.MinValue;

            if (value.Contains("-"))
                return this.getDateTime(Tipo, value);

            try
            {
                int _ano = Convert.ToInt16(value.Substring(0, 4));
                int _mes = Convert.ToInt16(value.Substring(4, 2));
                int _dia = Convert.ToInt16(value.Substring(6, 2));
                return new DateTime(_ano, _mes, _dia);
            }
            catch
            {
                throw new Exception("Data inválida do conteudo [" + value + "]");
            }
        }

        /// <summary>
        /// getTime
        /// </summary>
        private DateTime getTime(string value)
        {
            if (string.IsNullOrEmpty(value))
                return DateTime.MinValue;

            try
            {
                int _hora = Convert.ToInt16(value.Substring(0, 2));
                int _min = Convert.ToInt16(value.Substring(3, 2));
                int _seg = Convert.ToInt16(value.Substring(6, 2));
                return new DateTime(1,1,1, _hora, _min, _seg);
            }
            catch
            {
                throw new Exception("Hora inválida do conteudo [" + value + "]");
            }
        }

        /// <summary>
        /// RetornarConteudoTag
        /// </summary>
        private string RetornarConteudoTag(string TAG)
        {
            ///
            /// "§B14|cUF|AAMM|CNPJ|Mod|serie|nNF"); //ok
            /// 
            /// se a tag a ser consulta é CNPJ, então é verificada no layout quantos pipes existem até ela.
            /// neste caso no comando abaixo será retornado "§B14|cUF|AAMM|" existindo 3 pipes para pegar
            /// o valor do retorno
            /// 
            if (!layout.StartsWith("§")) layout = "§" + layout;
            if (!layout.EndsWith("|")) layout += "|";
            string fValue = layout.Substring(0, layout.ToUpper().IndexOf("|" + TAG.ToUpper().Trim() + "|") + 1);
            if (fValue == "")
                throw new Exception("Segmento: " + this.FSegmento + " - Tag: " + TAG + " não encontrada");

            string[] pipes = fValue.Split(new char[] { '|' });
            int j = pipes.GetLength(0) - 2;
            if (j >= 0)
            {
                ///
                /// qual a posicao do conteudo do registro lido
                /// 
                string[] dados = this.Registro.Split(new char[] { '|' });
                try
                {
                    return dados[j + 1].TrimStart().TrimEnd();
                }
                catch
                {
                    return "";
                }
            }
            else
                return "";
        }

        /// <summary>
        /// SomenteNumeros
        /// </summary>
        private string SomenteNumeros(string entrada)
        {
            if (string.IsNullOrEmpty(entrada)) return "";

            StringBuilder saida = new StringBuilder(entrada.Length);
            foreach (char c in entrada)
            {
                if (char.IsDigit(c))
                {
                    saida.Append(c);
                }
            }
            return saida.ToString();
        }

        /// <summary>
        /// LerCampo
        /// </summary>

        private double LerDouble(TpcnTipoCampo Tipo, TpcnResources tag, ObOp optional, int maxLength)
        {
            return (double)this.LerCampo(Tipo, tag, optional, 0, maxLength);
        }

        private double LerDouble(TpcnTipoCampo Tipo, TpcnResources tag, ObOp optional, int minLength, int maxLength)
        {
            return (double)this.LerCampo(Tipo, tag, optional, minLength, maxLength);
        }
        private Int32 LerInt32(TpcnResources tag, ObOp optional, int minLength, int maxLength)
        {
            return (Int32)this.LerCampo(TpcnTipoCampo.tcInt, tag, optional, minLength, maxLength);
        }
        private string LerString(TpcnResources tag, ObOp optional, int minLength, int maxLength)
        {
            return (string)this.LerCampo(TpcnTipoCampo.tcStr, tag, optional, minLength, maxLength);
        }
        private object LerCampo(TpcnTipoCampo Tipo, TpcnResources tag, ObOp optional, int minLength, int maxLength)
        {
            int nDecimais = 0;
            string ConteudoTag = "";
            try
            {
                ConteudoTag = RetornarConteudoTag(tag.ToString());

                if (ConteudoTag != "")
                    if (ConteudoTag.StartsWith("§"))
                        ConteudoTag = "";

                int len = ConteudoTag.Length;
                if (len == 0 && optional == ObOp.Opcional)
                {
                }
                else
                {
                    switch (Tipo)
                    {
                        case TpcnTipoCampo.tcHor:
                            maxLength = minLength = 8; //hh:mm:ss
                            break;
                        case TpcnTipoCampo.tcDatYYYY_MM_DD:
                            maxLength = minLength = 10; //yyyy-MM-dd
                            break;
                        case TpcnTipoCampo.tcDatYYYYMMDD:
                            maxLength = minLength = 8; //yyyyMMdd
                            break;
                        case TpcnTipoCampo.tcDatHor:
                            maxLength = minLength = 19; //aaaa-mm-dd hh:mm:ss
                            break;
                        case TpcnTipoCampo.tcDec2:
                            nDecimais = 2;
                            break;
                        case TpcnTipoCampo.tcDec3:
                            nDecimais = 3;
                            break;
                        case TpcnTipoCampo.tcDec4:
                            nDecimais = 4;
                            break;
                        case TpcnTipoCampo.tcDec10:
                            nDecimais = 10;
                            break;
                    }

                    if (len == 0 && minLength > 0)
                    {
                        this.cMensagemErro += "Layout: " + this.layout.Replace("§", "") + Environment.NewLine;
                        this.cMensagemErro += string.Format("Segmento [{0}]: tag <{1}> deve ser informada.\r\n" +
                                                            "\tLinha: {2}: Conteudo do segmento: {3}",
                                                            this.FSegmento, tag.ToString(), this.LinhaLida, this.Registro.Substring(1)) + Environment.NewLine;
                    }
                    else
                    {
                        switch (Tipo)
                        {
                            case TpcnTipoCampo.tcDec2:
                            case TpcnTipoCampo.tcDec3:
                            case TpcnTipoCampo.tcDec4:
                            case TpcnTipoCampo.tcDec10:
                                //quando numerico do tipo double não consiste o tamanho minimo nem maximo
                                break;
                            default:
                                if ((len > maxLength || len < minLength) && (maxLength + minLength > 0))
                                {
                                    this.cMensagemErro += "Layout: " + this.layout.Replace("§", "") + Environment.NewLine;
                                    this.cMensagemErro += string.Format("Segmento [{0}]: tag <{1}> deve ter seu tamanho entre {2} e {3}. Conteudo: {4}" +
                                                            "\r\n\tLinha: {5}: Conteudo do segmento: {6}",
                                                            this.FSegmento, tag.ToString(), minLength, maxLength, ConteudoTag, this.LinhaLida, this.Registro.Substring(1)) + Environment.NewLine;
                                }
                                break;
                        }
                    }
                }

                if (optional == ObOp.Obrigatorio || (optional == ObOp.Opcional && len != 0))
                {
                    switch (Tipo)
                    {
                        case TpcnTipoCampo.tcDec2:
                        case TpcnTipoCampo.tcDec3:
                        case TpcnTipoCampo.tcDec4:
                        case TpcnTipoCampo.tcDec10:
                            {
                                int pos = ConteudoTag.IndexOf(".") + 1;
                                int ndec = ConteudoTag.Substring(pos).Length;
                                string xdec = ConteudoTag.Substring(pos);
                                //
                                // ajusta o numero de casas decimais
                                while (ndec > nDecimais)
                                {
                                    if (xdec.Substring(ndec - 1, 1) == "0")
                                        --ndec;
                                    else
                                        break;
                                }

                                if (ndec > nDecimais)
                                {
                                    this.cMensagemErro += "Layout: " + this.layout.Replace("§", "") + Environment.NewLine;
                                    this.cMensagemErro += string.Format("Segmento [{0}]: tag <{1}> número de casas decimais deve ser de {2} e existe(m) {3}" +
                                                                        "\r\n\tLinha: {4}: Conteudo do segmento: {5}",
                                                                        this.FSegmento, tag.ToString(), nDecimais, ndec, this.LinhaLida, this.Registro.Substring(1)) + Environment.NewLine;
                                }

                                #region -- atribui o numero de casas decimais que serão gravadas

                                if (tag == TpcnResources.vUnCom)
                                    switch (ndec)
                                    {
                                        case 2:
                                            NFe.det[NFe.det.Count - 1].Prod.vUnCom_Tipo = TpcnTipoCampo.tcDec2;
                                            break;
                                        case 3:
                                            NFe.det[NFe.det.Count - 1].Prod.vUnCom_Tipo = TpcnTipoCampo.tcDec3;
                                            break;
                                        case 4:
                                            NFe.det[NFe.det.Count - 1].Prod.vUnCom_Tipo = TpcnTipoCampo.tcDec4;
                                            break;
                                        case 10:
                                            NFe.det[NFe.det.Count - 1].Prod.vUnCom_Tipo = TpcnTipoCampo.tcDec10;
                                            break;
                                    }

                                if (tag == TpcnResources.vUnTrib)
                                    switch (ndec)
                                    {
                                        case 2:
                                            NFe.det[NFe.det.Count - 1].Prod.vUnTrib_Tipo = TpcnTipoCampo.tcDec2;
                                            break;
                                        case 3:
                                            NFe.det[NFe.det.Count - 1].Prod.vUnTrib_Tipo = TpcnTipoCampo.tcDec3;
                                            break;
                                        case 4:
                                            NFe.det[NFe.det.Count - 1].Prod.vUnTrib_Tipo = TpcnTipoCampo.tcDec4;
                                            break;
                                        case 10:
                                            NFe.det[NFe.det.Count - 1].Prod.vUnTrib_Tipo = TpcnTipoCampo.tcDec10;
                                            break;
                                    }

                                if (tag == TpcnResources.qTotMes)
                                    switch (ndec)
                                    {
                                        case 2:
                                            NFe.cana.qTotMes_Tipo = TpcnTipoCampo.tcDec2;
                                            break;
                                        case 3:
                                            NFe.cana.qTotMes_Tipo = TpcnTipoCampo.tcDec3;
                                            break;
                                        case 4:
                                            NFe.cana.qTotMes_Tipo = TpcnTipoCampo.tcDec4;
                                            break;
                                        case 10:
                                            NFe.cana.qTotMes_Tipo = TpcnTipoCampo.tcDec10;
                                            break;
                                    }

                                if (tag == TpcnResources.qTotAnt)
                                    switch (ndec)
                                    {
                                        case 2:
                                            NFe.cana.qTotAnt_Tipo = TpcnTipoCampo.tcDec2;
                                            break;
                                        case 3:
                                            NFe.cana.qTotAnt_Tipo = TpcnTipoCampo.tcDec3;
                                            break;
                                        case 4:
                                            NFe.cana.qTotAnt_Tipo = TpcnTipoCampo.tcDec4;
                                            break;
                                        case 10:
                                            NFe.cana.qTotAnt_Tipo = TpcnTipoCampo.tcDec10;
                                            break;
                                    }

                                if (tag == TpcnResources.qTotGer)
                                    switch (ndec)
                                    {
                                        case 2:
                                            NFe.cana.qTotGer_Tipo = TpcnTipoCampo.tcDec2;
                                            break;
                                        case 3:
                                            NFe.cana.qTotGer_Tipo = TpcnTipoCampo.tcDec3;
                                            break;
                                        case 4:
                                            NFe.cana.qTotGer_Tipo = TpcnTipoCampo.tcDec4;
                                            break;
                                        case 10:
                                            NFe.cana.qTotGer_Tipo = TpcnTipoCampo.tcDec10;
                                            break;
                                    }

                                if (tag == TpcnResources.qtde)// || tag == TpcnResources.qTotGer)
                                        switch (ndec)
                                        {
                                            case 2:
                                                NFe.cana.fordia[NFe.cana.fordia.Count - 1].qtde_Tipo = TpcnTipoCampo.tcDec2;
                                                break;
                                            case 3:
                                                NFe.cana.fordia[NFe.cana.fordia.Count - 1].qtde_Tipo = TpcnTipoCampo.tcDec3;
                                                break;
                                            case 4:
                                                NFe.cana.fordia[NFe.cana.fordia.Count - 1].qtde_Tipo = TpcnTipoCampo.tcDec4;
                                                break;
                                            case 10:
                                                NFe.cana.fordia[NFe.cana.fordia.Count - 1].qtde_Tipo = TpcnTipoCampo.tcDec10;
                                                break;
                                        }
                                

                                #endregion
                            }
                            break;
                    }
                }

                switch (Tipo)
                {
                    case TpcnTipoCampo.tcDatYYYYMMDD:
                        return this.getDate2(Tipo, ConteudoTag);

                    case TpcnTipoCampo.tcDatYYYY_MM_DD:
                    case TpcnTipoCampo.tcDatHor:
                        return this.getDateTime(Tipo, ConteudoTag);

                    case TpcnTipoCampo.tcHor:
                        return this.getTime(ConteudoTag);

                    case TpcnTipoCampo.tcDec2:
                    case TpcnTipoCampo.tcDec3:
                    case TpcnTipoCampo.tcDec4:
                    case TpcnTipoCampo.tcDec10:
                        return Convert.ToDouble("0" + ConteudoTag.Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));

                    case TpcnTipoCampo.tcInt:
                        return Convert.ToInt32("0" + SomenteNumeros(ConteudoTag));

                    default:
                        return ConteudoTag.Trim();
                        /*
                        if (ConteudoTag.StartsWith("<![CDATA["))
                            return ConteudoTag.Trim();
                        return ConteudoTag.Replace("&", "&amp;").
                                        Replace("<", "&lt;").
                                        Replace(">", "&gt;").
                                        Replace("\"", "&quot;").
                                        Replace("\r\n", "|").
                                        Replace("'", "&#39;").Trim();
                         */
                }
            }
            catch (Exception ex)
            {
                this.cMensagemErro += "Layout: " + this.layout.Replace("§", "") + Environment.NewLine;
                this.cMensagemErro += string.Format("Segmento [{0}]: tag <{1}> Conteudo: {2}\r\n" +
                                                    "\tLinha: {3}: Conteudo do segmento: {4}\r\n\tMensagem de erro: {5}",
                                                    this.FSegmento, tag.ToString(), ConteudoTag, this.LinhaLida, this.Registro.Substring(1),
                                                    ex.Message) + Environment.NewLine;
                switch (Tipo)
                {
                    case TpcnTipoCampo.tcHor:
                    case TpcnTipoCampo.tcDatYYYY_MM_DD:
                    case TpcnTipoCampo.tcDatYYYYMMDD:
                    case TpcnTipoCampo.tcDatHor:
                        return DateTime.MinValue;

                    case TpcnTipoCampo.tcDec2:
                    case TpcnTipoCampo.tcDec3:
                    case TpcnTipoCampo.tcDec4:
                    case TpcnTipoCampo.tcDec10:
                        return 0.0;

                    case TpcnTipoCampo.tcInt:
                        return 0;

                    default:
                        return "";
                }
            }
        }

        private int CasasDecimais75
        {
            get{
                return (double)NFe.infNFe.Versao >= 3.10 ? 7 : 5;
            }
        }
        private TpcnTipoCampo TipoCampo42
        {
            get{
                return (double)NFe.infNFe.Versao >= 3.10 ? TpcnTipoCampo.tcDec4 : TpcnTipoCampo.tcDec2;
            }
        }

        /// <summary>
        /// LerRegistro
        /// </summary>
        private void LerRegistro(string aRegistro)
        {
            int lenPipesRegistro = aRegistro.Split(new char[] { '|' }).Length - 1;
            int nProd = NFe.det.Count - 1;
            this.Registro = aRegistro;
            this.FSegmento = this.Registro.Substring(1, this.Registro.IndexOf("|") - 1);

            switch (this.FSegmento.ToUpper())
            {
                case "A":
                    layout = "§A|versao|Id|";
                    double v = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.versao, ObOp.Opcional, 6);
                    this.chave = this.LerString(TpcnResources.ID, ObOp.Opcional, 0, 47);
                    this.chave = this.SomenteNumeros(this.chave);
                    if (!string.IsNullOrEmpty(this.chave) && this.chave.Length != 44)
                    {
                        throw new Exception("Chave de acesso inválida no segmento A");
                    }

                    NFe.infNFe.Versao = (v>0 ? Convert.ToDecimal(v) : 2);
                    break;

                case "B":
                    layout = (NFe.infNFe.Versao >= 3 ?  "§B|cUF|cNF|NatOp|indPag|mod|serie|nNF|dhEmi|dhSaiEnt|tpNF|idDest|cMunFG|TpImp|TpEmis|cDV|TpAmb|FinNFe|indFinal|indPres|ProcEmi|VerProc|dhCont|xJust" :
                                                        "§B|cUF|cNF|NatOp|indPag|mod|serie|nNF|dEmi|dSaiEnt|hSaiEnt|tpNF|cMunFG|TpImp|TpEmis|cDV|TpAmb|FinNFe|ProcEmi|VerProc|dhCont|xJust");

                    ///
                    /// Grupo da TAG <ide>
                    /// 
                    #region -- <ide>

                    NFe.ide.cUF     = this.LerInt32(TpcnResources.cUF, ObOp.Obrigatorio, 2, 2);
                    NFe.ide.cNF     = this.LerInt32(TpcnResources.cNF, ObOp.Opcional, 8, 8);
                    NFe.ide.natOp   = this.LerString(TpcnResources.natOp, ObOp.Obrigatorio, 1, 60);
                    NFe.ide.indPag  = (TpcnIndicadorPagamento)this.LerInt32(TpcnResources.indPag, ObOp.Obrigatorio, 1, 1);
                    NFe.ide.mod     = (TpcnMod)this.LerInt32(TpcnResources.mod, ObOp.Obrigatorio, 2, 2);
                    NFe.ide.serie   = this.LerInt32(TpcnResources.serie, ObOp.Obrigatorio, 1, 3);
                    NFe.ide.nNF     = this.LerInt32(TpcnResources.nNF, ObOp.Obrigatorio, 1, 9);
                    if (NFe.infNFe.Versao >= 3)
                    {
                        NFe.ide.dhEmi    = this.LerString(TpcnResources.dhEmi,    ObOp.Obrigatorio, 19, 25);
                        NFe.ide.dhSaiEnt = this.LerString(TpcnResources.dhSaiEnt, ObOp.Opcional,     0, 25);
                        NFe.ide.idDest = (TpcnDestinoOperacao)this.LerInt32(TpcnResources.idDest, ObOp.Obrigatorio, 1, 1);
                    }
                    else
                    {
                        NFe.ide.dEmi    = (DateTime)this.LerCampo(TpcnTipoCampo.tcDatYYYY_MM_DD, TpcnResources.dEmi, ObOp.Obrigatorio, 10, 10);
                        NFe.ide.dSaiEnt = (DateTime)this.LerCampo(TpcnTipoCampo.tcDatYYYY_MM_DD, TpcnResources.dSaiEnt, ObOp.Opcional, 10, 10);
                        NFe.ide.hSaiEnt = (DateTime)this.LerCampo(TpcnTipoCampo.tcHor, TpcnResources.hSaiEnt, ObOp.Opcional, 8, 8);
                    }
                    NFe.ide.tpNF    = (TpcnTipoNFe)this.LerInt32(TpcnResources.tpNF, ObOp.Obrigatorio, 1, 1);
                    NFe.ide.cMunFG  = this.LerInt32(TpcnResources.cMunFG, ObOp.Obrigatorio, 7, 7);
                    NFe.ide.tpImp   = (TpcnTipoImpressao)this.LerInt32(TpcnResources.tpImp, ObOp.Obrigatorio, 1, 1);
                    NFe.ide.tpEmis  = (TipoEmissao)this.LerInt32(TpcnResources.tpEmis, ObOp.Obrigatorio, 1, 1);
                    NFe.ide.cDV     = this.LerInt32(TpcnResources.cDV, ObOp.Opcional, 1, 1);
                    NFe.ide.tpAmb   = (TipoAmbiente)this.LerInt32(TpcnResources.tpAmb, ObOp.Obrigatorio, 1, 1);
                    NFe.ide.finNFe  = (TpcnFinalidadeNFe)this.LerInt32(TpcnResources.finNFe, ObOp.Obrigatorio, 1, 1);
                    if (NFe.infNFe.Versao >= 3)
                    {
                        NFe.ide.indFinal = (TpcnConsumidorFinal)this.LerInt32(TpcnResources.indFinal, ObOp.Obrigatorio, 1, 1);
                        NFe.ide.indPres = (TpcnPresencaComprador)this.LerInt32(TpcnResources.indPres, ObOp.Obrigatorio, 1, 1);
                    }
                    NFe.ide.procEmi = (TpcnProcessoEmissao)this.LerInt32(TpcnResources.procEmi, ObOp.Obrigatorio, 1, 1);
                    NFe.ide.verProc = this.LerString(TpcnResources.verProc, ObOp.Obrigatorio, 1, 20);
                    NFe.ide.dhCont  = this.LerString(TpcnResources.dhCont, ObOp.Opcional, 0, 25);
                    NFe.ide.xJust   = this.LerString(TpcnResources.xJust, ObOp.Opcional, 15, 256);

                    if (!string.IsNullOrEmpty(this.chave))
                    {
                        if (NFe.ide.cNF == 0)
                            NFe.ide.cNF = Convert.ToInt32(this.chave.Substring(35, 8));

                        if (NFe.ide.cDV == 0)
                            NFe.ide.cDV = Convert.ToInt32(this.chave.Substring(this.chave.Length - 1, 1));
                    }
                    break;
                    #endregion

                case "B13":
                case "BA02":
                    layout = "§"+ this.FSegmento + "|refNFe"; //ok

                    ///
                    /// Grupo da TAG <ide><NFref><refNFe>
                    ///
                    #region <ide><NFref><refNFe>

                    NFe.ide.NFref.Add(new NFref(this.LerString(TpcnResources.refNFe, ObOp.Obrigatorio, 44, 44), null));

                    #endregion
                    break;

                case "B14":
                case "BA03":
                    layout = "§" + this.FSegmento + "|cUF|AAMM|CNPJ|mod|serie|nNF"; //ok

                    ///
                    /// Grupo da TAG <ide><NFref><RefNF>
                    ///
                    #region <ide><NFref><RefNF>
                    {
                        NFref item = new NFref();
                        item.refNF = new refNF();

                        item.refNF.cUF   = this.LerInt32(TpcnResources.cUF, ObOp.Obrigatorio, 2, 2);
                        item.refNF.AAMM  = this.LerString(TpcnResources.AAMM, ObOp.Obrigatorio, 4, 4);
                        item.refNF.CNPJ  = this.LerString(TpcnResources.CNPJ, ObOp.Obrigatorio, 14, 14);
                        item.refNF.mod   = this.LerString(TpcnResources.mod, ObOp.Obrigatorio, 2, 2);
                        item.refNF.serie = this.LerInt32(TpcnResources.serie, ObOp.Obrigatorio, 1, 3);
                        item.refNF.nNF   = this.LerInt32(TpcnResources.nNF, ObOp.Obrigatorio, 1, 9);

                        NFe.ide.NFref.Add(item);
                    }
                    #endregion
                    break;

                case "BA10":

                case "B20A":
                    layout = "§" + this.FSegmento + "|cUF|AAMM|IE|mod|serie|nNF" + (FSegmento.ToUpper().Equals("BA10") ? "|refCTe" : "");

                    #region B20a | BA10
                    {
                        NFref item = new NFref();
                        item.refNFP = new refNFP();
                        item.refNFP.cUF     = this.LerInt32(TpcnResources.cUF, ObOp.Obrigatorio, 2, 2);
                        item.refNFP.AAMM    = this.LerString(TpcnResources.AAMM, ObOp.Obrigatorio, 4, 4);
                        item.refNFP.IE      = this.LerString(TpcnResources.IE, ObOp.Obrigatorio, 1, 14);
                        item.refNFP.mod     = this.LerString(TpcnResources.mod, ObOp.Obrigatorio, 2, 2);
                        item.refNFP.serie   = this.LerInt32(TpcnResources.serie, ObOp.Obrigatorio, 1, 3);
                        item.refNFP.nNF     = this.LerInt32(TpcnResources.nNF, ObOp.Obrigatorio, 1, 9);
                        if (FSegmento.ToUpper().Equals("BA10"))
                        {
                            item.refCTe = this.LerString(TpcnResources.refCTe, ObOp.Obrigatorio, 44, 44);
                        }
                        NFe.ide.NFref.Add(item);
                    }
                    #endregion
                    break;

                case "B20D":
                case "BA13":
                    layout = "§" + this.FSegmento + "|CNPJ"; //ok

                    if (NFe.ide.NFref.Count == 0 || (NFe.ide.NFref.Count > 0 && NFe.ide.NFref[NFe.ide.NFref.Count-1].refNFP == null))
                        throw new Exception(FSegmento.ToUpper().Equals("B20D") ? "Segmento B20d sem segmento B20A" : "Segmento BA13 sem segmento BA10");
                    NFe.ide.NFref[NFe.ide.NFref.Count-1].refNFP.CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Obrigatorio, 14, 14);
                    break;

                case "B20E":
                case "BA14":
                    layout = "§" + this.FSegmento + "|CPF"; //ok

                    if (NFe.ide.NFref.Count == 0 || (NFe.ide.NFref.Count > 0 && NFe.ide.NFref[NFe.ide.NFref.Count - 1].refNFP == null))
                        throw new Exception(FSegmento.ToUpper().Equals("B20E") ? "Segmento B20e sem segmento B20A" : "Segmento BA14 sem segmento BA10");
                    NFe.ide.NFref[NFe.ide.NFref.Count - 1].refNFP.CPF = this.LerString(TpcnResources.CPF, ObOp.Obrigatorio, 11, 11);
                    break;

                case "B20I":
                    layout = "§B20i|refCTe"; //ok

                    NFe.ide.NFref.Add(new NFref(null, LerString(TpcnResources.refCTe, ObOp.Obrigatorio, 44, 44)));
                    break;

                case "B20J":
                case "BA20":
                    layout = "§"+ this.FSegmento + "|mod|nECF|nCOO"; //ok
                    {
                        NFref item = new NFref();
                        item.refECF = new refECF();
                        item.refECF.mod  = this.LerString(TpcnResources.mod, ObOp.Obrigatorio, 2, 2);
                        item.refECF.nECF = this.LerInt32(TpcnResources.nECF, ObOp.Obrigatorio, 1, 3);
                        item.refECF.nCOO = this.LerInt32(TpcnResources.nCOO, ObOp.Obrigatorio, 1, 6);
                        NFe.ide.NFref.Add(item);
                    }
                    break;

                case "C":
                    layout = "§C|xNome|xFant|IE|IEST|IM|CNAE|CRT"; //okz

                    ///
                    /// Grupo da TAG <emit>
                    ///
                    #region <emit>

                    NFe.emit.xNome  = this.LerString(TpcnResources.xNome, ObOp.Obrigatorio, 2, 60);
                    NFe.emit.xFant  = this.LerString(TpcnResources.xFant, ObOp.Opcional, 1, 60);
                    NFe.emit.IE     = this.LerString(TpcnResources.IE, ObOp.Opcional, 0, 14);
                    NFe.emit.IEST   = this.LerString(TpcnResources.IEST, ObOp.Opcional, 2, 14);
                    NFe.emit.IM     = this.LerString(TpcnResources.IM, ObOp.Opcional, 1, 15);
                    NFe.emit.CNAE   = this.LerString(TpcnResources.CNAE, ObOp.Opcional, 7, 7);
                    NFe.emit.CRT = (TpcnCRT)this.LerInt32(TpcnResources.CRT, ObOp.Obrigatorio, 1, 1);

                    #endregion
                    break;

                case "C02": 
                    layout = "§C02|CNPJ"; //ok
                    

                    NFe.emit.CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Obrigatorio, 14, 14);
                    break;

                case "C02A":    
                    layout = "§C02A|CPF"; //ok
                    NFe.emit.CPF = this.LerString(TpcnResources.CPF, ObOp.Obrigatorio, 11, 11);
                    break;

                case "C05":
                    layout = "§C05|xLgr|nro|xCpl|xBairro|cMun|xMun|UF|CEP|cPais|xPais|fone"; //ok

                    ///
                    /// Grupo da TAG <emit><EnderEmit>
                    /// 
                    #region <emit><EnderEmit>

                    NFe.emit.enderEmit.xLgr     = this.LerString(TpcnResources.xLgr, ObOp.Obrigatorio, 2, 60);
                    NFe.emit.enderEmit.nro      = this.LerString(TpcnResources.nro, ObOp.Obrigatorio, 1, 60);
                    NFe.emit.enderEmit.xCpl     = this.LerString(TpcnResources.xCpl, ObOp.Opcional, 1, 60);
                    NFe.emit.enderEmit.xBairro  = this.LerString(TpcnResources.xBairro, ObOp.Obrigatorio, 2, 60);
                    NFe.emit.enderEmit.cMun     = this.LerInt32(TpcnResources.cMun, ObOp.Obrigatorio, 7, 7);
                    NFe.emit.enderEmit.xMun     = this.LerString(TpcnResources.xMun, ObOp.Obrigatorio, 2, 60);
                    NFe.emit.enderEmit.UF       = this.LerString(TpcnResources.UF, ObOp.Obrigatorio, 2, 2);
                    NFe.emit.enderEmit.CEP      = this.LerInt32(TpcnResources.CEP, ObOp.Opcional, 0, 8);
                    NFe.emit.enderEmit.cPais    = this.LerInt32(TpcnResources.cPais, ObOp.Obrigatorio, 4, 4);
                    NFe.emit.enderEmit.xPais    = this.LerString(TpcnResources.xPais, ObOp.Opcional, 1, 60);
                    NFe.emit.enderEmit.fone     = this.LerString(TpcnResources.fone, ObOp.Opcional, 6, 14);

                    #endregion
                    break;

                case "D":
                    layout = "§D|CNPJ|xOrgao|matr|xAgente|fone|UF|nDAR|dEmi|vDAR|repEmi|dPag"; //ok

                    ///
                    /// Grupo da TAG <avulsa>
                    /// 
                    #region <avulsa>

                    NFe.avulsa.CNPJ     = this.LerString(TpcnResources.CNPJ, ObOp.Obrigatorio, 14, 14);
                    NFe.avulsa.xOrgao   = this.LerString(TpcnResources.xOrgao, ObOp.Obrigatorio, 1, 60);
                    NFe.avulsa.matr     = this.LerString(TpcnResources.matr, ObOp.Obrigatorio, 1, 60);
                    NFe.avulsa.xAgente  = this.LerString(TpcnResources.xAgente, ObOp.Obrigatorio, 1, 60);
                    NFe.avulsa.fone     = this.LerString(TpcnResources.fone, ObOp.Obrigatorio, 6, 14);
                    NFe.avulsa.UF       = this.LerString(TpcnResources.UF, ObOp.Obrigatorio, 2, 2);
                    NFe.avulsa.nDAR     = this.LerString(TpcnResources.nDAR, ObOp.Obrigatorio, 1, 60);
                    NFe.avulsa.dEmi     = (DateTime)this.LerCampo(TpcnTipoCampo.tcDatYYYY_MM_DD, TpcnResources.dEmi, ObOp.Obrigatorio, 10, 10);
                    NFe.avulsa.vDAR     = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDAR, ObOp.Obrigatorio, 15);
                    NFe.avulsa.repEmi   = this.LerString(TpcnResources.repEmi, ObOp.Obrigatorio, 1, 60);
                    NFe.avulsa.dPag     = (DateTime)this.LerCampo(TpcnTipoCampo.tcDatYYYY_MM_DD, TpcnResources.dPag, ObOp.Opcional, 10, 10);

                    #endregion
                    break;

                case "E":

                    layout = (NFe.infNFe.Versao >= 3 ? "§E|xNome|indIEDest|IE|ISUF|IM|email" : "§E|xNome|IE|ISUF|email");

                    ///
                    /// Grupo da TAG <dest>
                    /// 
                    #region <dest>

                    NFe.dest.xNome = this.LerString(TpcnResources.xNome, (NFe.infNFe.Versao >= 3 && NFe.ide.mod != TpcnMod.modNFe ? ObOp.Opcional : ObOp.Obrigatorio), 2, 60);
                    if (NFe.infNFe.Versao >= 3) 
                        NFe.dest.indIEDest = (TpcnindIEDest)this.LerInt32(TpcnResources.indIEDest, ObOp.Opcional, 0, 1);
                    NFe.dest.IE    = this.LerString(TpcnResources.IE, ObOp.Opcional, 0, 14);
                    NFe.dest.ISUF  = this.LerString(TpcnResources.ISUF, ObOp.Opcional, 8, 9);
                    if (NFe.infNFe.Versao >= 3) 
                        NFe.dest.IM = this.LerString(TpcnResources.IM, ObOp.Opcional, 1, 15);
                    NFe.dest.email = this.LerString(TpcnResources.email, ObOp.Opcional, 1, 60);

                    #endregion
                    break;

                case "E02":
                    layout = "§E02|CNPJ"; //ok
                    NFe.dest.CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Opcional, 14, 14);
                    break;

                case "E03": 
                    //if (NFe.infNFe.Versao >= 3)
                      //  layout = "§E03|CPF|idEstrangeiro"; //só UniNFe = segmento "E03A"
                    //else
                        layout = "§E03|CPF"; //ok
                    if (NFe.ide.mod == TpcnMod.modNFCe && NFe.infNFe.Versao >= 3) //nfc-e
                        NFe.dest.CPF = this.LerString(TpcnResources.CPF, ObOp.Opcional, 11, 11);
                    else
                        NFe.dest.CPF = this.LerString(TpcnResources.CPF, ObOp.Obrigatorio, 11, 11);

//                    if (NFe.infNFe.Versao >= 3)
//                    {
//                        NFe.dest.idEstrangeiro = this.LerString(TpcnResources.idEstrangeiro, ObOp.Opcional, 5, 20);
//                        if (string.IsNullOrEmpty(NFe.dest.idEstrangeiro) && string.IsNullOrEmpty(NFe.dest.CPF) && lenPipesRegistro == 4)
//                            NFe.dest.idEstrangeiro = "NAO GERAR TAG";
//                    }
                    break;

                case "E03A":
                    layout = "§E03a|idEstrangeiro"; //ok
                    NFe.dest.idEstrangeiro = this.LerString(TpcnResources.idEstrangeiro, ObOp.Opcional, 5, 20);
                    if (string.IsNullOrEmpty(NFe.dest.idEstrangeiro) && string.IsNullOrEmpty(NFe.dest.CPF))
                        NFe.dest.idEstrangeiro = "NAO GERAR TAG";
                    break;

                case "E05":
                    layout = "§E05|xLgr|nro|xCpl|xBairro|cMun|xMun|UF|CEP|cPais|xPais|fone"; //ok
                    ///
                    /// Grupo da TAG <dest><EnderDest>
                    /// 
                    #region <dest><EnderDest>
                    NFe.dest.enderDest.xLgr     = this.LerString(TpcnResources.xLgr, ObOp.Obrigatorio, 2, 60);
                    NFe.dest.enderDest.nro      = this.LerString(TpcnResources.nro, ObOp.Obrigatorio, 1, 60);
                    NFe.dest.enderDest.xCpl     = this.LerString(TpcnResources.xCpl, ObOp.Opcional, 1, 60);
                    NFe.dest.enderDest.xBairro  = this.LerString(TpcnResources.xBairro, ObOp.Obrigatorio, 1, 60);
                    NFe.dest.enderDest.cMun     = this.LerInt32(TpcnResources.cMun, ObOp.Obrigatorio, 7, 7);
                    NFe.dest.enderDest.xMun     = this.LerString(TpcnResources.xMun, ObOp.Obrigatorio, 2, 60);
                    NFe.dest.enderDest.UF       = this.LerString(TpcnResources.UF, ObOp.Obrigatorio, 2, 2);
                    NFe.dest.enderDest.CEP      = this.LerInt32(TpcnResources.CEP, ObOp.Opcional, 0, 8);
                    NFe.dest.enderDest.cPais    = this.LerInt32(TpcnResources.cPais, ObOp.Obrigatorio, 2, 4);
                    NFe.dest.enderDest.xPais    = this.LerString(TpcnResources.xPais, ObOp.Opcional, 2, 60);
                    NFe.dest.enderDest.fone     = this.LerString(TpcnResources.fone, ObOp.Opcional, 6, 14);
                    #endregion                    
                    break;

                    /*
                     * SUBSTITUIDO PELO SEGMENTO "E"
                     * 

                case "E16A": //só UniNFe = segmento "E"
                    layout = "§E16a|indIEDest"; //ok
                    if ((double)NFe.infNFe.Versao >= 3.10)
                        NFe.dest.indIEDest = (TpcnindIEDest)this.LerInt32(TpcnResources.indIEDest, ObOp.Obrigatorio, 1, 1);
                    break;

                case "E17": //só UniNFe = segmento "E"
                    layout = "§E17|IE"; //ok
                    NFe.dest.IE = this.LerString(TpcnResources.IE, ObOp.Opcional, 2, 14);
                    break;

                case "E18A": //só UniNFe = segmento "E"
                    layout = "§E18A|IM"; //ok
                    NFe.dest.IM = this.LerString(TpcnResources.IM, ObOp.Opcional, 1, 15);
                    break;
                     * 
                     */

                case "F":
                    layout = "§F|xLgr|nro|xCpl|xBairro|cMun|xMun|UF"; //ok
                    ///
                    /// Grupo da TAG <retirada> 
                    /// 
                    #region <retirada> 
                    NFe.retirada.xLgr   = this.LerString(TpcnResources.xLgr, ObOp.Obrigatorio, 2, 60);
                    NFe.retirada.nro    = this.LerString(TpcnResources.nro, ObOp.Obrigatorio, 1, 60);
                    NFe.retirada.xCpl   = this.LerString(TpcnResources.xCpl, ObOp.Opcional, 1, 60);
                    NFe.retirada.xBairro = this.LerString(TpcnResources.xBairro, ObOp.Obrigatorio, 1, 60);
                    NFe.retirada.cMun   = this.LerInt32(TpcnResources.cMun, ObOp.Obrigatorio, 7, 7);
                    NFe.retirada.xMun   = this.LerString(TpcnResources.xMun, ObOp.Obrigatorio, 2, 60);
                    NFe.retirada.UF     = this.LerString(TpcnResources.UF, ObOp.Obrigatorio, 2, 2);
                    #endregion                    
                    break;

                case "F02": 
                    layout = "§F02|CNPJ"; //ok
                    NFe.retirada.CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Obrigatorio, 14, 14);
                    break;

                case "F02A":    
                    layout = "§F02a|CPF"; //ok
                    NFe.retirada.CPF = this.LerString(TpcnResources.CPF, ObOp.Obrigatorio, 11, 11);
                    break;

                case "G":   
                    layout = "§G|xLgr|nro|xCpl|xBairro|cMun|xMun|UF"; //ok
                    ///
                    /// Grupo da TAG <entrega>
                    /// 
                    #region <entrega>
                    NFe.entrega.xLgr    = this.LerString(TpcnResources.xLgr, ObOp.Obrigatorio, 2, 60);
                    NFe.entrega.nro     = this.LerString(TpcnResources.nro, ObOp.Obrigatorio, 1, 60);
                    NFe.entrega.xCpl    = this.LerString(TpcnResources.xCpl, ObOp.Opcional, 1, 60);
                    NFe.entrega.xBairro = this.LerString(TpcnResources.xBairro, ObOp.Obrigatorio, 1, 60);
                    NFe.entrega.cMun    = this.LerInt32(TpcnResources.cMun, ObOp.Obrigatorio, 7, 7);
                    NFe.entrega.xMun    = this.LerString(TpcnResources.xMun, ObOp.Obrigatorio, 2, 60);
                    NFe.entrega.UF      = this.LerString(TpcnResources.UF, ObOp.Obrigatorio, 2, 2);
                
                    #endregion                    
                    break;

                case "G02":
                    layout = "§" + this.FSegmento + "|CNPJ"; //ok
                    NFe.entrega.CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Obrigatorio, 14, 14);
                    break;

                case "G02A":
                    layout = "§" + this.FSegmento + "|CPF"; //ok
                    NFe.entrega.CPF = this.LerString(TpcnResources.CPF, ObOp.Obrigatorio, 11, 11);
                    break;

                case "G51":
                case "GA02":
                    layout = "§" + this.FSegmento + "|CNPJ"; //ok
                    NFe.autXML.Add(new autXML { CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Obrigatorio, 14, 14) });
                    break;

                case "G52":
                case "GA03":
                    layout = "§" + this.FSegmento + "|CPF"; //ok
                    NFe.autXML.Add(new autXML { CPF = this.LerString(TpcnResources.CPF, ObOp.Obrigatorio, 11, 11) });
                    break;

                ///
                /// Grupo da TAG <det>
                /// 
                case "H":   
                    layout = "§H|nItem|infAdProd"; //ok

                    NFe.det.Add(new Det());
                    nProd = NFe.det.Count - 1;
                    NFe.det[nProd].Prod.nItem = this.LerInt32(TpcnResources.NItem, ObOp.Obrigatorio, 1, 3);
                    NFe.det[nProd].infAdProd = this.LerString(TpcnResources.infAdProd, ObOp.Opcional, 0, 500);
                    break;

                case "I":
                        layout = "§I|cProd|cEAN|XProd|NCM|EXTIPI|CFOP|UCom|QCom|VUnCom|VProd|CEANTrib|UTrib|QTrib|VUnTrib|VFrete|VSeg|VDesc|vOutro|indTot|xPed|nItemPed|nFCI"; //ok
                    if (NFe.infNFe.Versao >= 3 && lenPipesRegistro > 23)
                        layout = "§I|cProd|cEAN|XProd|NCM|NVE|EXTIPI|CFOP|UCom|QCom|VUnCom|VProd|CEANTrib|UTrib|QTrib|VUnTrib|VFrete|VSeg|VDesc|vOutro|indTot|xPed|nItemPed|nFCI"; //ok
                    ///
                    /// Grupo da TAG <det><prod>
                    /// 
                    #region <det><prod>

                    NFe.det[nProd].Prod.cProd = this.LerString(TpcnResources.cProd, ObOp.Obrigatorio, 1, 60);
                    NFe.det[nProd].Prod.cEAN = this.LerString(TpcnResources.cEAN, ObOp.Obrigatorio, 0, 14);
                    NFe.det[nProd].Prod.xProd   = this.LerString(TpcnResources.xProd, ObOp.Obrigatorio, 1, 120);
                    NFe.det[nProd].Prod.NCM     = this.LerString(TpcnResources.NCM, ObOp.Obrigatorio, 2, 8);
                    if (NFe.infNFe.Versao >= 3 && lenPipesRegistro > 23)
                    {
                        NFe.det[nProd].Prod.NVE = this.LerString(TpcnResources.NVE, ObOp.Opcional, 0, 6);
                    }
                    NFe.det[nProd].Prod.EXTIPI  = this.LerString(TpcnResources.EXTIPI, ObOp.Opcional, 2, 3);
                    NFe.det[nProd].Prod.CFOP    = this.LerString(TpcnResources.CFOP, ObOp.Obrigatorio, 4, 4);
                    NFe.det[nProd].Prod.uCom    = this.LerString(TpcnResources.uCom, ObOp.Obrigatorio, 1, 6);
                    NFe.det[nProd].Prod.qCom    = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qCom, ObOp.Obrigatorio, 11);
                    NFe.det[nProd].Prod.vUnCom  = this.LerDouble(TpcnTipoCampo.tcDec10, TpcnResources.vUnCom, ObOp.Opcional, 21);
                    NFe.det[nProd].Prod.vProd   = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vProd, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Prod.cEANTrib = this.LerString(TpcnResources.cEANTrib, ObOp.Obrigatorio, 0, 14);
                    NFe.det[nProd].Prod.uTrib   = this.LerString(TpcnResources.uTrib, ObOp.Obrigatorio, 1, 6);
                    NFe.det[nProd].Prod.qTrib   = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qTrib, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Prod.vUnTrib = this.LerDouble(TpcnTipoCampo.tcDec10, TpcnResources.vUnTrib, ObOp.Obrigatorio, 21);
                    NFe.det[nProd].Prod.vFrete  = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vFrete, ObOp.Opcional, 15);
                    NFe.det[nProd].Prod.vSeg    = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vSeg, ObOp.Opcional, 15);
                    NFe.det[nProd].Prod.vDesc   = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDesc, ObOp.Opcional, 15);
                    NFe.det[nProd].Prod.vOutro  = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vOutro, ObOp.Opcional, 15);
                    NFe.det[nProd].Prod.indTot = (TpcnIndicadorTotal)this.LerInt32(TpcnResources.indTot, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Prod.xPed    = this.LerString(TpcnResources.xPed, ObOp.Opcional, 1, 15);
                    NFe.det[nProd].Prod.nItemPed = this.LerInt32(TpcnResources.nItemPed, ObOp.Opcional, 0, 6);
                    NFe.det[nProd].Prod.nFCI    = this.LerString(TpcnResources.nFCI, ObOp.Opcional, 0, 255);
                    NFe.det[nProd].Imposto.ISSQN.cSitTrib = string.Empty;

                    
                    #endregion                    
                    break;

                case "I05A":
                    layout = "§" + this.FSegmento + "|NVE";
                    NFe.det[nProd].Prod.NVE = this.LerString(TpcnResources.NVE, ObOp.Opcional, 0, 6);
                    break;

                case "I18":
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§I18|nDI|dDI|xLocDesemb|UFDesemb|dDesemb|tpViaTransp|vAFRMM|tpIntermedio|CNPJ|UFTerceiro|cExportador" :
                                "§I18|nDI|dDI|xLocDesemb|UFDesemb|dDesemb|cExportador"); //ok

                    ///
                    /// Grupo da TAG <det><prod><DI>
                    /// 
                    #region <det><prod><DI>

                    DI diItem = new DI();

                    diItem.nDI          = this.LerString(TpcnResources.nDI, ObOp.Obrigatorio, 1, 12);
                    diItem.dDI          = (DateTime)this.LerCampo(TpcnTipoCampo.tcDatYYYY_MM_DD, TpcnResources.dDI, ObOp.Obrigatorio, 10, 10);
                    diItem.xLocDesemb   = this.LerString(TpcnResources.xLocDesemb, ObOp.Obrigatorio, 1, 60);
                    diItem.UFDesemb     = this.LerString(TpcnResources.UFDesemb, ObOp.Obrigatorio, 2, 2);
                    diItem.dDesemb      = (DateTime)this.LerCampo(TpcnTipoCampo.tcDatYYYY_MM_DD, TpcnResources.dDesemb, ObOp.Obrigatorio, 10, 10);
                    if (NFe.infNFe.Versao >= 3)
                    {
                        diItem.tpViaTransp = (TpcnTipoViaTransp)this.LerInt32(TpcnResources.tpViaTransp, ObOp.Opcional, 1, 2);
                        diItem.vAFRMM = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vAFRMM, ObOp.Opcional, 15);
                        diItem.tpIntermedio = (TpcnTipoIntermedio)this.LerInt32(TpcnResources.tpIntermedio, ObOp.Opcional, 1, 1);
                        diItem.CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Opcional, 14, 14);
                        diItem.UFTerceiro = this.LerString(TpcnResources.UFTerceiro, ObOp.Opcional, 2, 2);
                    }
                    diItem.cExportador = this.LerString(TpcnResources.cExportador, ObOp.Obrigatorio, 1, 60);

                    NFe.det[nProd].Prod.DI.Add(diItem);
                    #endregion
                    break;

                    /*
                     * 
                     * 

                case "I23A":    //só UniNFe = segmento "I18"
                    layout = "§I23a|tpViaTransp|vAFRMM|tpIntermedio|CNPJ|UFTerceiro"; //ok
                    var dii = NFe.det[nProd].Prod.DI[NFe.det[nProd].Prod.DI.Count - 1];
                    if (dii != null)
                    {
                        dii.tpViaTransp = (TpcnTipoViaTransp)this.LerInt32(TpcnResources.tpViaTransp, ObOp.Opcional, 1, 2);
                        dii.vAFRMM = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vAFRMM, ObOp.Opcional, 15);
                        dii.tpIntermedio = (TpcnTipoIntermedio)this.LerInt32(TpcnResources.tpIntermedio, ObOp.Opcional, 1, 1);
                        dii.CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Opcional, 14, 14);
                        dii.UFTerceiro = this.LerString(TpcnResources.UFTerceiro, ObOp.Opcional, 2, 2);
                    }
                    break;
                     * 
                     * 
                     */

                case "I25":
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§I25|NAdicao|NSeqAdic|CFabricante|VDescDI|nDraw" :
                                "§I25|NAdicao|NSeqAdic|CFabricante|VDescDI"); //ok
                    ///
                    /// Grupo da TAG <det><prod><DI><adi> 
                    /// 
                    #region <det><prod><DI><adi>

                    Adi adiItem = new Adi();

                    adiItem.nAdicao     = this.LerInt32(TpcnResources.nAdicao, ObOp.Obrigatorio, 1, 3);
                    adiItem.nSeqAdi     = this.LerInt32(TpcnResources.nSeqAdic, ObOp.Obrigatorio, 1, 3);
                    adiItem.cFabricante = this.LerString(TpcnResources.cFabricante, ObOp.Obrigatorio, 1, 60);
                    adiItem.vDescDI     = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDescDI, ObOp.Opcional, 15);
                    if (NFe.infNFe.Versao >= 3)
                        adiItem.nDraw = this.LerString(TpcnResources.nDraw, ObOp.Opcional, 0, 11);

                    NFe.det[nProd].Prod.DI[NFe.det[nProd].Prod.DI.Count-1].adi.Add(adiItem);
                    #endregion
                    break;

                    /*
                     * 

                case "I29A":    //só UniNFe = segmento "I25"
                    layout = "§I29A|nDraw"; //ok
                    #region <det><prod><DI><adi>
                    {
                        int l = NFe.det[nProd].Prod.DI[NFe.det[nProd].Prod.DI.Count - 1].adi.Count - 1;
                        var item = NFe.det[nProd].Prod.DI[NFe.det[nProd].Prod.DI.Count - 1].adi[l];

                        item.nDraw = this.LerString(TpcnResources.nDraw, ObOp.Opcional, 0, 11);
                    }
                    #endregion
                    break;
                     * 
                     * 
                     */

                case "I50":
                    layout = "§I50|nDraw";
                    #region <det><prod><detExport>
                    NFe.det[nProd].Prod.detExport.Add(new detExport { nDraw = this.LerString(TpcnResources.nDraw, ObOp.Opcional, 0, 11) });
                    #endregion
                    break;

                case "I52":
                    layout = "§I52|nRE|chNFe|qExport"; //ok
                    #region <det><prod><detExport><exportInd>
                    NFe.det[nProd].Prod.detExport[NFe.det[nProd].Prod.detExport.Count - 1].exportInd.nRE = this.LerString(TpcnResources.nRE, ObOp.Obrigatorio, 12, 12);
                    NFe.det[nProd].Prod.detExport[NFe.det[nProd].Prod.detExport.Count - 1].exportInd.chNFe = this.LerString(TpcnResources.chNFe, ObOp.Obrigatorio, 44, 44);
                    NFe.det[nProd].Prod.detExport[NFe.det[nProd].Prod.detExport.Count - 1].exportInd.qExport = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qExport, ObOp.Obrigatorio, 1, 15);
                    #endregion
                    break;

                case "J":   
                case "JA":
                    layout = "§" + this.FSegmento + "|tpOp|Chassi|CCor|XCor|Pot|cilin|pesoL|pesoB|NSerie|TpComb|NMotor|CMT|Dist|anoMod|anoFab|tpPint|tpVeic|espVeic|VIN|condVeic|cMod|cCorDENATRAN|lota|tpRest"; //ok
                    ///
                    /// Grupo da TAG <det><prod><veicProd>
                    /// 
                    #region <det><prod><veicProd>

                    NFe.det[nProd].Prod.veicProd.tpOp   = this.LerString(TpcnResources.tpOp, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Prod.veicProd.chassi = this.LerString(TpcnResources.chassi, ObOp.Obrigatorio, 17, 17);
                    NFe.det[nProd].Prod.veicProd.cCor   = this.LerString(TpcnResources.cCor, ObOp.Obrigatorio, 1, 4);
                    NFe.det[nProd].Prod.veicProd.xCor   = this.LerString(TpcnResources.xCor, ObOp.Obrigatorio, 1, 40);
                    NFe.det[nProd].Prod.veicProd.pot    = this.LerString(TpcnResources.pot, ObOp.Obrigatorio, 1, 4);
                    NFe.det[nProd].Prod.veicProd.cilin  = this.LerString(TpcnResources.cilin, ObOp.Obrigatorio, 1, 4);
                    NFe.det[nProd].Prod.veicProd.pesoL  = this.LerString(TpcnResources.pesoL, ObOp.Obrigatorio, 1, 9);
                    NFe.det[nProd].Prod.veicProd.pesoB  = this.LerString(TpcnResources.pesoB, ObOp.Obrigatorio, 1, 9);
                    NFe.det[nProd].Prod.veicProd.nSerie = this.LerString(TpcnResources.nSerie, ObOp.Obrigatorio, 1, 9);
                    NFe.det[nProd].Prod.veicProd.tpComb = this.LerString(TpcnResources.tpComb, ObOp.Obrigatorio, 1, 2);
                    NFe.det[nProd].Prod.veicProd.nMotor = this.LerString(TpcnResources.nMotor, ObOp.Obrigatorio, 1, 21);
                    NFe.det[nProd].Prod.veicProd.CMT    = this.LerString(TpcnResources.CMT, ObOp.Obrigatorio, 1, 9);
                    NFe.det[nProd].Prod.veicProd.dist   = this.LerString(TpcnResources.dist, ObOp.Obrigatorio, 1, 4);
                    NFe.det[nProd].Prod.veicProd.anoMod = this.LerInt32(TpcnResources.anoMod, ObOp.Obrigatorio, 4, 4);
                    NFe.det[nProd].Prod.veicProd.anoFab = this.LerInt32(TpcnResources.anoFab, ObOp.Obrigatorio, 4, 4);
                    NFe.det[nProd].Prod.veicProd.tpPint = this.LerString(TpcnResources.tpPint, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Prod.veicProd.tpVeic = this.LerInt32(TpcnResources.tpVeic, ObOp.Obrigatorio, 1, 2);
                    NFe.det[nProd].Prod.veicProd.espVeic = this.LerInt32(TpcnResources.espVeic, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Prod.veicProd.VIN    = this.LerString(TpcnResources.VIN, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Prod.veicProd.condVeic = this.LerString(TpcnResources.condVeic, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Prod.veicProd.cMod   = this.LerString(TpcnResources.cMod, ObOp.Obrigatorio, 1, 6);
                    NFe.det[nProd].Prod.veicProd.cCorDENATRAN = this.LerInt32(TpcnResources.cCorDENATRAN, ObOp.Obrigatorio, 1, 2);
                    NFe.det[nProd].Prod.veicProd.lota   = this.LerInt32(TpcnResources.lota, ObOp.Obrigatorio, 1, 3);
                    NFe.det[nProd].Prod.veicProd.tpRest = this.LerInt32(TpcnResources.tpRest, ObOp.Obrigatorio, 1, 1);
                    

                    #endregion
                    break;

                case "K":
                    layout = "§K|nLote|qLote|dFab|dVal|vPMC"; //ok

                    ///
                    /// Grupo da TAG <det><prod><med>
                    /// 
                    #region <det><prod><med>
                    Med medItem = new Med();

                    medItem.nLote =          LerString(TpcnResources.nLote, ObOp.Obrigatorio, 1, 20);
                    medItem.qLote =          LerDouble(TpcnTipoCampo.tcDec3, TpcnResources.qLote, ObOp.Obrigatorio, 11);
                    medItem.dFab = (DateTime)this.LerCampo(TpcnTipoCampo.tcDatYYYY_MM_DD,  TpcnResources.dFab, ObOp.Obrigatorio, 10, 10);
                    medItem.dVal = (DateTime)this.LerCampo(TpcnTipoCampo.tcDatYYYY_MM_DD,  TpcnResources.dVal, ObOp.Obrigatorio, 10, 10);
                    medItem.vPMC =           LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPMC, ObOp.Obrigatorio, 15);

                    NFe.det[nProd].Prod.med.Add(medItem);
                    #endregion
                    break;

                case "L":
                    layout = "§L|tpArma|nSerie|nCano|descr"; //ok
                    ///
                    /// Grupo da TAG <det><prod><arma>
                    /// 
                    #region <det><prod><arma>
                    Arma armaItem = new Arma();

                    armaItem.tpArma = (TpcnTipoArma)this.LerInt32(TpcnResources.tpArma, ObOp.Obrigatorio, 1, 1);
                    armaItem.nSerie =               LerInt32(TpcnResources.nSerie, ObOp.Obrigatorio, 1, (double)NFe.infNFe.Versao>=3.10 ? 15 : 9);
                    armaItem.nCano  =               LerInt32(TpcnResources.nCano, ObOp.Obrigatorio, 1, (double)NFe.infNFe.Versao >= 3.10 ? 15 : 9);
                    armaItem.descr  =               LerString(TpcnResources.descr, ObOp.Obrigatorio, 1, 256);

                    NFe.det[nProd].Prod.arma.Add(armaItem);
                    #endregion
                    break;

                case "LA":
                case "L01":
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§" + this.FSegmento + "|CProdANP|pMixGN|CODIF|QTemp|UFCons" :
                                "§" + this.FSegmento + "|CProdANP|CODIF|QTemp|UFCons"); //ok
                    ///
                    /// Grupo da TAG <det><prod><comb>
                    /// 
                    #region <det><prod><comb>
                    NFe.det[nProd].Prod.comb.cProdANP = this.LerInt32(TpcnResources.cProdANP, ObOp.Obrigatorio, 9, 9);
                    if (NFe.infNFe.Versao >= 3)
                        NFe.det[nProd].Prod.comb.pMixGN = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.pMixGN, ObOp.Opcional, 6);
                    NFe.det[nProd].Prod.comb.CODIF  = this.LerString(TpcnResources.CODIF, ObOp.Opcional, 0, 21);
                    NFe.det[nProd].Prod.comb.qTemp  = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qTemp, ObOp.Opcional, 16);
                    NFe.det[nProd].Prod.comb.UFCons = this.LerString(TpcnResources.UFCons, ObOp.Obrigatorio, 2, 2);
                    #endregion
                    break;

                    /*
                     * 

                case "L102A":   //só UniNFe = segmento "L01"
                    layout = "§L102a|pMixGN"; //ok
                    NFe.det[nProd].Prod.comb.pMixGN = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.pMixGN, ObOp.Opcional, 6);
                    break;
                     * 
                     */

                case "LA07":
                case "L105":
                    layout = "§" + this.FSegmento + "|qBCProd|vAliqProd|vCIDE"; //ok
                    ///
                    /// Grupo da TAG <det><prod><comb><CIDE>
                    /// 
                    #region <det><prod><comb><CIDE>
                    NFe.det[nProd].Prod.comb.CIDE.qBCprod = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qBCProd, ObOp.Obrigatorio, 16);
                    NFe.det[nProd].Prod.comb.CIDE.vAliqProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.vAliqProd, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Prod.comb.CIDE.vCIDE = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vCIDE, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "LB":
                case "L109":
                    layout = "§" + this.FSegmento + "|nRECOPI"; //ok
                    NFe.det[nProd].Prod.nRECOPI = this.LerString(TpcnResources.nRECOPI, ObOp.Opcional, 20, 20);
                    break;

                case "M":
                    layout = "§M|vTotTrib|"; //ok   
                        NFe.det[nProd].Imposto.vTotTrib = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vTotTrib, ObOp.Opcional, 15);
                    break;

                    /*
                     * 

                case "M02":
                    layout = "§M02|vTotTrib"; //ok   
                    NFe.det[nProd].Imposto.vTotTrib = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vTotTrib, ObOp.Opcional, 15);
                    break;
                     * 
                     */

                ///
                /// Grupo da TAG <det><imposto><ICMS>
                /// 
                case "N02": 
                    layout = "§N02|Orig|CST|ModBC|VBC|PICMS|VICMS"; //ok   
                    #region ICMS00

                    NFe.det[nProd].Imposto.ICMS.orig  = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST   = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.ICMS.modBC = (TpcnDeterminacaoBaseIcms)this.LerInt32(TpcnResources.modBC, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.vBC   = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMS = this.LerDouble(this.TipoCampo42, TpcnResources.pICMS, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Obrigatorio, 15);

                    #endregion
                    break;

                case "N03": 
                    layout = "§N03|Orig|CST|ModBC|VBC|PICMS|VICMS|ModBCST|PMVAST|PRedBCST|VBCST|PICMSST|VICMSST"; //ok
                    #region ICMS10

                    NFe.det[nProd].Imposto.ICMS.ICMSPart10 = 0;
                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.ICMS.modBC = (TpcnDeterminacaoBaseIcms)this.LerInt32(TpcnResources.modBC, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMS = this.LerDouble(this.TipoCampo42, TpcnResources.pICMS, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.modBCST = (TpcnDeterminacaoBaseIcmsST)this.LerInt32(TpcnResources.modBCST, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pMVAST = this.LerDouble(this.TipoCampo42, TpcnResources.pMVAST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.pRedBCST = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBCST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBCST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMSST = this.LerDouble(this.TipoCampo42, TpcnResources.pICMSST, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMSST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSST, ObOp.Obrigatorio, 15);

                    #endregion
                    break;

                case "N04":
                    layout = "§N04|orig|CST|modBC|pRedBC|vBC|pICMS|vICMS|vICMSDeson|motDesICMS|";  //no manual
                    #region ICMS20

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.ICMS.modBC = (TpcnDeterminacaoBaseIcms)this.LerInt32(TpcnResources.modBC, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pRedBC = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBC, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMS = this.LerDouble(this.TipoCampo42, TpcnResources.pICMS, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.vICMSDeson = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSDeson, ObOp.Opcional, 15);
                    NFe.det[nProd].Imposto.ICMS.motDesICMS = this.LerInt32(TpcnResources.motDesICMS, ObOp.Opcional, 1, 1);

                    #endregion
                    break;

                case "N05":
                    layout = "§N05|orig|CST|modBCST|pMVAST|pRedBCST|vBCST|pICMSST|vICMSST|vICMSDeson|motDesICMS"; //no manual

                    #region ICMS30

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.ICMS.modBCST = (TpcnDeterminacaoBaseIcmsST)this.LerInt32(TpcnResources.modBCST, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pMVAST = this.LerDouble(this.TipoCampo42, TpcnResources.pMVAST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.pRedBCST = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBCST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBCST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMSST = this.LerDouble(this.TipoCampo42, TpcnResources.pICMSST, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMSST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.vICMSDeson = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSDeson, ObOp.Opcional, 15);
                    NFe.det[nProd].Imposto.ICMS.motDesICMS = this.LerInt32(TpcnResources.motDesICMS, ObOp.Opcional, 1, 1);

                    #endregion
                    break;

                case "N06":
                    //layout = "§N06|orig|CST|vICMSDeson|motDesICMS";   <<<<<<<<< layout errado da Sefaz
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§N06|orig|CST|vICMS|vICMSDeson|motDesICMS" :
                                "§N06|orig|CST|vICMS|motDesICMS");

                    #region ICMS40, ICMS41 ICMS50

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.ICMS.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Opcional, 15);
                    if (NFe.infNFe.Versao >= 3)
                        NFe.det[nProd].Imposto.ICMS.vICMSDeson = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSDeson, ObOp.Opcional, 15);
                    NFe.det[nProd].Imposto.ICMS.motDesICMS = this.LerInt32(TpcnResources.motDesICMS, ObOp.Opcional, 1, 1);

                    #endregion
                    break;

                case "N07":
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§N07|orig|CST|modBC|pRedBC|vBC|pICMS|vICMSOp|pDif|vICMSDif|vICMS" :
                                "§N07|orig|CST|modBC|pRedBC|vBC|pICMS|vICMS");

                    #region ICMS51

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.ICMS.modBC = (TpcnDeterminacaoBaseIcms)this.LerInt32(TpcnResources.modBC, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pRedBC = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBC, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Opcional, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMS = this.LerDouble(this.TipoCampo42, TpcnResources.pICMS, ObOp.Opcional, this.CasasDecimais75);
                    if (NFe.infNFe.Versao < 3)
                    {
                    NFe.det[nProd].Imposto.ICMS.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Opcional, 15);
                    }
                    else
                    {
                    NFe.det[nProd].Imposto.ICMS.vICMSOp = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSOp, ObOp.Opcional, 15);
                    NFe.det[nProd].Imposto.ICMS.pDif = this.LerDouble(this.TipoCampo42, TpcnResources.pDif, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMSDif = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSDif, ObOp.Opcional, 15);
                        NFe.det[nProd].Imposto.ICMS.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Opcional, 15);

                        if (lenPipesRegistro == 10) //a quem ainda nao alterou para a tag <vICMS>
                        {

                        ///
                        /// como nao tinha a tag vICMS definida no layout do TXT, vamos calcular o valor do ICMS
                        ///
                        if (NFe.det[nProd].Imposto.ICMS.vICMS == 0)
                        {
                            NFe.det[nProd].Imposto.ICMS.vICMS = NFe.det[nProd].Imposto.ICMS.vICMSOp - NFe.det[nProd].Imposto.ICMS.vICMSDif;

                            if (NFe.det[nProd].Imposto.ICMS.vICMS < 0) NFe.det[nProd].Imposto.ICMS.vICMS = 0;
                        }
                    }
                    }

                    #endregion
                    break;

                case "N08": 
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§N08|Orig|CST|vBCSTRet|vICMSSTRet" :
                                "§N08|Orig|CST|vBCST|vICMSST");

                    #region ICMS60

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    if (NFe.infNFe.Versao >= 3)
                    {
                    NFe.det[nProd].Imposto.ICMS.vBCSTRet = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCSTRet, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.vICMSSTRet = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSSTRet, ObOp.Obrigatorio, 15);
                    }
                    else
                    {
                        NFe.det[nProd].Imposto.ICMS.vBCSTRet = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCST, ObOp.Obrigatorio, 15);
                        NFe.det[nProd].Imposto.ICMS.vICMSSTRet = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSST, ObOp.Obrigatorio, 15);
                    }
                    

                    #endregion
                    break;

                case "N09":
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§N09|orig|CST|modBC|pRedBC|vBC|pICMS|vICMS|modBCST|pMVAST|pRedBCST|vBCST|pICMSST|vICMSST|vICMSDeson|motDesICMS" :
                                "§N09|Orig|CST|ModBC|PRedBC|VBC|PICMS|VICMS|ModBCST|PMVAST|PRedBCST|VBCST|PICMSST|VICMSST");

                    #region ICMS70

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.ICMS.modBC = (TpcnDeterminacaoBaseIcms)this.LerInt32(TpcnResources.modBC, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pRedBC = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBC, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMS = this.LerDouble(this.TipoCampo42, TpcnResources.pICMS, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.modBCST = (TpcnDeterminacaoBaseIcmsST)this.LerInt32(TpcnResources.modBCST, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pMVAST = this.LerDouble(this.TipoCampo42, TpcnResources.pMVAST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.pRedBCST = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBCST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBCST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMSST = this.LerDouble(this.TipoCampo42, TpcnResources.pICMSST, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMSST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSST, ObOp.Obrigatorio, 15);
                    if (NFe.infNFe.Versao >= 3)
                    {
                    NFe.det[nProd].Imposto.ICMS.vICMSDeson = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSDeson, ObOp.Opcional, 15);
                    NFe.det[nProd].Imposto.ICMS.motDesICMS = this.LerInt32(TpcnResources.motDesICMS, ObOp.Opcional, 1, 1);
                    }

                    #endregion
                    break;

                case "N10":
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§N10|orig|CST|modBC|vBC|pRedBC|pICMS|vICMS|modBCST|pMVAST|pRedBCST|vBCST|pICMSST|vICMSST|vICMSDeson|motDesICMS" :
                                "§N10|orig|CST|modBC|pRedBC|vBC|pICMS|vICMS|modBCST|pMVAST|pRedBCST|vBCST|pICMSST|vICMSST");

                    #region ICMS90

                    NFe.det[nProd].Imposto.ICMS.ICMSPart90 = 0;
                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.ICMS.modBC = (TpcnDeterminacaoBaseIcms)this.LerInt32(TpcnResources.modBC, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pRedBC = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBC, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMS = this.LerDouble(this.TipoCampo42, TpcnResources.pICMS, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.modBCST = (TpcnDeterminacaoBaseIcmsST)this.LerInt32(TpcnResources.modBCST, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pMVAST = this.LerDouble(this.TipoCampo42, TpcnResources.pMVAST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.pRedBCST = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBCST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBCST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMSST = this.LerDouble(this.TipoCampo42, TpcnResources.pICMSST, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMSST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSST, ObOp.Obrigatorio, 15);
                    if (NFe.infNFe.Versao >= 3)
                    {
                    NFe.det[nProd].Imposto.ICMS.vICMSDeson = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSDeson, ObOp.Opcional, 15);
                    NFe.det[nProd].Imposto.ICMS.motDesICMS = this.LerInt32(TpcnResources.motDesICMS, ObOp.Opcional, 1, 1);
                    }

                    #endregion                    
                    break;

                case "N10A":
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§N10a|orig|CST|modBC|vBC|pRedBC|pICMS|vICMS|modBCST|pMVAST|pRedBCST|vBCST|pICMSST|vICMSST|pBCOp|UFST" :
                                "§N10a|Orig|CST|ModBC|PRedBC|VBC|PICMS|VICMS|ModBCST|PMVAST|PRedBCST|VBCST|PICMSST|VICMSST|pBCOp|UFST");

                    #region ICMSPart-10, ICMSPart-90

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.ICMS.modBC = (TpcnDeterminacaoBaseIcms)this.LerInt32(TpcnResources.modBC, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pRedBC = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBC, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMS = this.LerDouble(this.TipoCampo42, TpcnResources.pICMS, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.modBCST = (TpcnDeterminacaoBaseIcmsST)this.LerInt32(TpcnResources.modBCST, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pMVAST = this.LerDouble(this.TipoCampo42, TpcnResources.pMVAST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.pRedBCST = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBCST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBCST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMSST = this.LerDouble(this.TipoCampo42, TpcnResources.pICMSST, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMSST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pBCOp = this.LerDouble(this.TipoCampo42, TpcnResources.pBCOp, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.UFST = this.LerString(TpcnResources.UFST, ObOp.Obrigatorio, 2, 2);
                    if (NFe.det[nProd].Imposto.ICMS.CST == "10")
                        NFe.det[nProd].Imposto.ICMS.ICMSPart10 = 1;
                    else
                        NFe.det[nProd].Imposto.ICMS.ICMSPart90 = 1;
                    

                    #endregion
                    break;

                case "N10B":    
                    layout = "§N10b|Orig|CST|vBCSTRet|vICMSSTRet|vBCSTDest|vICMSSTDest";

                    #region ICMS-ST

                    NFe.det[nProd].Imposto.ICMS.ICMSst = 1;
                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CST         = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.ICMS.vBCSTRet    = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCSTRet, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.vICMSSTRet  = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSSTRet, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.vBCSTDest   = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCSTDest, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.vICMSSTDest = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSSTDest, ObOp.Obrigatorio, 15);
                    

                    #endregion
                    break;

                case "N10C":    
                    layout = "§N10c|Orig|CSOSN|pCredSN|vCredICMSSN";

                    #region ICMSSN101

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CSOSN = this.LerInt32(TpcnResources.CSOSN, ObOp.Obrigatorio, 3, 3);
                    NFe.det[nProd].Imposto.ICMS.pCredSN = this.LerDouble(this.TipoCampo42, TpcnResources.pCredSN, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vCredICMSSN = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vCredICMSSN, ObOp.Obrigatorio, 15);

                    #endregion
                    break;

                case "N10D":    
                    layout = "§N10d|Orig|CSOSN";

                    #region ICMSSN102

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CSOSN = this.LerInt32(TpcnResources.CSOSN, ObOp.Obrigatorio, 3, 3);

                    #endregion
                    break;

                case "N10E":    
                    layout = "§N10e|Orig|CSOSN|modBCST|pMVAST|pRedBCST|vBCST|pICMSST|vICMSST|pCredSN|vCredICMSSN";

                    #region ICMSSN201

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CSOSN = this.LerInt32(TpcnResources.CSOSN, ObOp.Obrigatorio, 3, 3);
                    NFe.det[nProd].Imposto.ICMS.modBCST = (TpcnDeterminacaoBaseIcmsST)this.LerInt32(TpcnResources.modBCST, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pMVAST = this.LerDouble(this.TipoCampo42, TpcnResources.pMVAST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.pRedBCST = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBCST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBCST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMSST = this.LerDouble(this.TipoCampo42, TpcnResources.pICMSST, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMSST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pCredSN = this.LerDouble(this.TipoCampo42, TpcnResources.pCredSN, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vCredICMSSN = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vCredICMSSN, ObOp.Obrigatorio, 15);

                    #endregion
                    break;

                case "N10F":    
                    layout = "§N10f|Orig|CSOSN|modBCST|pMVAST|pRedBCST|vBCST|pICMSST|vICMSST";

                    #region ICMSSN202

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CSOSN = this.LerInt32(TpcnResources.CSOSN, ObOp.Obrigatorio, 3, 3);
                    NFe.det[nProd].Imposto.ICMS.modBCST = (TpcnDeterminacaoBaseIcmsST)this.LerInt32(TpcnResources.modBCST, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pMVAST = this.LerDouble(this.TipoCampo42, TpcnResources.pMVAST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.pRedBCST = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBCST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBCST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMSST = this.LerDouble(this.TipoCampo42, TpcnResources.pICMSST, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMSST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSST, ObOp.Obrigatorio, 15);

                    #endregion
                    break;

                case "N10G":
                    layout = (NFe.infNFe.Versao >= 3 ? "§N10g|orig|CSOSN|vBCSTRet|vICMSSTRet" :
                                                       "§N10g|Orig|CSOSN|modBCST|vBCSTRet|vICMSSTRet");

                    #region ICMSSN500

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CSOSN = this.LerInt32(TpcnResources.CSOSN, ObOp.Obrigatorio, 3, 3);
                    if (NFe.infNFe.Versao < 3)
                        NFe.det[nProd].Imposto.ICMS.modBCST = (TpcnDeterminacaoBaseIcmsST)this.LerInt32(TpcnResources.modBCST, ObOp.Opcional, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.vBCSTRet = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCSTRet, ObOp.Opcional, 15);
                    NFe.det[nProd].Imposto.ICMS.vICMSSTRet = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSSTRet, ObOp.Opcional, 15);

                    #endregion
                    break;

                    
                case "N10H":    
                    layout = "§N10h|Orig|CSOSN|modBC|vBC|pRedBC|pICMS|vICMS|modBCST|pMVAST|pRedBCST|vBCST|pICMSST|vICMSST|pCredSN|vCredICMSSN";
                    

                    #region ICMSSN900

                    NFe.det[nProd].Imposto.ICMS.orig = (TpcnOrigemMercadoria)this.LerInt32(TpcnResources.orig, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.CSOSN = this.LerInt32(TpcnResources.CSOSN, ObOp.Obrigatorio, 3, 3);
                    NFe.det[nProd].Imposto.ICMS.modBC = (TpcnDeterminacaoBaseIcms)this.LerInt32(TpcnResources.modBC, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pRedBC = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBC, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.pICMS = this.LerDouble(this.TipoCampo42, TpcnResources.pICMS, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.modBCST = (TpcnDeterminacaoBaseIcmsST)this.LerInt32(TpcnResources.modBCST, ObOp.Obrigatorio, 1, 1);
                    NFe.det[nProd].Imposto.ICMS.pMVAST = this.LerDouble(this.TipoCampo42, TpcnResources.pMVAST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.pRedBCST = this.LerDouble(this.TipoCampo42, TpcnResources.pRedBCST, ObOp.Opcional, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vBCST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pICMSST = this.LerDouble(this.TipoCampo42, TpcnResources.pICMSST, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vICMSST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSST, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ICMS.pCredSN = this.LerDouble(this.TipoCampo42, TpcnResources.pCredSN, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ICMS.vCredICMSSN = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vCredICMSSN, ObOp.Obrigatorio, 15);

                    #endregion
                    break;

                case "O":   
                    layout = "§O|clEnq|CNPJProd|cSelo|qSelo|cEnq"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><IPI>
                    /// 
                    #region <det><imposto><IPI>
                    NFe.det[nProd].Imposto.IPI.clEnq    = this.LerString(TpcnResources.clEnq, ObOp.Opcional, 5, 5);
                    NFe.det[nProd].Imposto.IPI.CNPJProd = this.LerString(TpcnResources.CNPJProd, ObOp.Opcional, 14, 14);
                    NFe.det[nProd].Imposto.IPI.cSelo    = this.LerString(TpcnResources.cSelo, ObOp.Opcional, 1, 60);
                    NFe.det[nProd].Imposto.IPI.qSelo    = this.LerInt32(TpcnResources.qSelo, ObOp.Opcional, 1, 12);
                    NFe.det[nProd].Imposto.IPI.cEnq = this.LerString(TpcnResources.cEnq, ObOp.Obrigatorio, 3, 3);

                
                    #endregion                    
                    break;

                case "O07": 
                    layout = "§O07|CST|vIPI"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><IPITrib>
                    /// 
                    #region <det><imposto><IPITrib>
                    NFe.det[nProd].Imposto.IPI.CST  = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.IPI.vIPI = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vIPI, ObOp.Opcional, 15);

                
                    #endregion                    
                    break;

                case "O08": 
                    layout = "§O08|CST"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><IPINT>
                    /// 
                    NFe.det[nProd].Imposto.IPI.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    break;

                case "O10": 
                    layout = "§O10|vBC|pIPI"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><IPI>
                    /// 
                    #region <det><imposto><IPI>
                    NFe.det[nProd].Imposto.IPI.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.IPI.pIPI = this.LerDouble(NFe.infNFe.Versao > 3 ? TpcnTipoCampo.tcDec4 : TpcnTipoCampo.tcDec2, TpcnResources.pIPI, ObOp.Obrigatorio, NFe.infNFe.Versao > 3 ? 7 : 5);
                    #endregion
                    break;

                case "O11": 
                    layout = (NFe.infNFe.Versao >= 3 ? "§O11|qUnid|vUnid|vIPI" : "§O11|qUnid|vUnid"); //ok
                    ///
                    /// Grupo da TAG <det><imposto><IPI>
                    /// 
                    #region <det><imposto><IPI>
                    NFe.det[nProd].Imposto.IPI.qUnid = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qUnid, ObOp.Obrigatorio, 16);
                    NFe.det[nProd].Imposto.IPI.vUnid = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.vUnid, ObOp.Obrigatorio, 15);
                    if (NFe.infNFe.Versao >= 3)
                        NFe.det[nProd].Imposto.IPI.vIPI = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.vIPI, ObOp.Opcional, 15);
                    #endregion
                    break;

                case "P":   
                    layout = "§P|vBC|vDespAdu|vII|vIOF"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><II>
                    /// 
                    #region <det><imposto><IPI>
                    NFe.det[nProd].Imposto.II.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.II.vDespAdu = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDespAdu, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.II.vII = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vII, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.II.vIOF = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vIOF, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "Q02": 
                    layout = "§Q02|CST|VBC|PPIS|VPIS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><pis><pisaliq>
                    /// 
                    #region <det><imposto><pis><pisaliq>
                    NFe.det[nProd].Imposto.PIS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.PIS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.PIS.pPIS = this.LerDouble(NFe.infNFe.Versao > 3 ? TpcnTipoCampo.tcDec4 : TpcnTipoCampo.tcDec2, TpcnResources.pPIS, ObOp.Obrigatorio, NFe.infNFe.Versao > 3 ? 7 : 5);
                    NFe.det[nProd].Imposto.PIS.vPIS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPIS, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "Q03": 
                    layout = "§Q03|CST|QBCProd|VAliqProd|VPIS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><pis><pisqtde>
                    /// 
                    #region <det><imposto><pis><pisqtde>
                    NFe.det[nProd].Imposto.PIS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.PIS.qBCProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qBCProd, ObOp.Obrigatorio, 16);
                    NFe.det[nProd].Imposto.PIS.vAliqProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.vAliqProd, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.PIS.vPIS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPIS, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "Q04": 
                    layout = "§Q04|CST"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><pis><pisNT>
                    /// 
                    #region <det><imposto><pis><pisNT>
                    NFe.det[nProd].Imposto.PIS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
	                #endregion      
                    break;

                case "Q05": 
                    layout = "§Q05|CST|vPIS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><pis><pisOutr>
                    /// 
                    #region <det><imposto><pis><pisOutr>
                    NFe.det[nProd].Imposto.PIS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.PIS.vPIS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPIS, NFe.infNFe.Versao >= 3 ? ObOp.Opcional : ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "Q07": 
                    layout = (NFe.infNFe.Versao >= 3 ? "§Q07|vBC|pPIS|vPIS" : "§Q07|vBC|pPIS"); //ok
                    ///
                    /// Grupo da TAG <det><imposto><pis><pisqtde>
                    /// 
                    #region <det><imposto><pis><pisqtde>
                    if (NFe.infNFe.Versao >= 3)
                    {
                        NFe.det[nProd].Imposto.PIS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                        NFe.det[nProd].Imposto.PIS.pPIS = this.LerDouble(NFe.infNFe.Versao > 3 ? TpcnTipoCampo.tcDec4 : TpcnTipoCampo.tcDec2, TpcnResources.pPIS, ObOp.Obrigatorio, NFe.infNFe.Versao > 3 ? 7 : 5);
                        NFe.det[nProd].Imposto.PIS.vPIS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPIS, ObOp.Obrigatorio, 15);
                    }
                    else
                    {
                    NFe.det[nProd].Imposto.PIS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.PIS.pPIS = this.LerDouble(NFe.infNFe.Versao > 3 ? TpcnTipoCampo.tcDec4 : TpcnTipoCampo.tcDec2, TpcnResources.pPIS, ObOp.Obrigatorio, NFe.infNFe.Versao > 3 ? 7 : 5);
                    }
                    #endregion
                    break;

                case "Q10": 
                    layout = "§Q10|qBCProd|vAliqProd"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><pis><pisqtde>
                    /// 
                    #region <det><imposto><pis><pisqtde>
                    NFe.det[nProd].Imposto.PIS.qBCProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qBCProd, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.PIS.vAliqProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.vAliqProd, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "R":   
                    layout = "§R|vPIS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><pisST>
                    /// 
                    #region <det><imposto><pisST>
                    NFe.det[nProd].Imposto.PISST.vPIS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPIS, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "R02": 
                    layout = "§R02|vBC|pPIS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><pisST>
                    /// 
                    #region <det><imposto><pisST>
                    NFe.det[nProd].Imposto.PISST.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.PISST.pPis = this.LerDouble(this.TipoCampo42, TpcnResources.pPIS, ObOp.Obrigatorio, this.CasasDecimais75);
                    #endregion
                    break;

                case "R04": 
                    layout = "§R04|qBCProd|vAliqProd" + (NFe.infNFe.Versao >= 3 ? "|vPIS" : "");
                    ///
                    /// Grupo da TAG <det><imposto><pisST>
                    /// 
                    #region <det><imposto><pisST>
                    NFe.det[nProd].Imposto.PISST.qBCProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qBCProd, ObOp.Obrigatorio, 16);
                    NFe.det[nProd].Imposto.PISST.vAliqProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.vAliqProd, ObOp.Obrigatorio, 15);
                    if (NFe.infNFe.Versao>=3)
                        NFe.det[nProd].Imposto.PISST.vPIS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPIS, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "S02": 
                    layout = "§S02|CST|vBC|pCOFINS|vCOFINS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><COFINS>
                    /// 
                    #region <det><imposto><COFINS>
                    NFe.det[nProd].Imposto.COFINS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.COFINS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.COFINS.pCOFINS = this.LerDouble(this.TipoCampo42, TpcnResources.pCOFINS, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.COFINS.vCOFINS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vCOFINS, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "S03":
                    layout = "§S03|CST|QBCProd|VAliqProd|VCOFINS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><COFINSQtde>
                    /// 
                    #region <det><imposto><COFINSQtde>
                    NFe.det[nProd].Imposto.COFINS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.COFINS.qBCProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qBCProd, ObOp.Obrigatorio, 16);
                    NFe.det[nProd].Imposto.COFINS.vAliqProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.vAliqProd, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.COFINS.vCOFINS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vCOFINS, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "S04":
                    layout = "§S04|CST"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><COFINSNT>
                    /// 
                    #region <det><imposto><COFINSNT>
                    NFe.det[nProd].Imposto.COFINS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    #endregion
                    break;

                case "S05":
                    layout = "§S05|CST|VCOFINS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><COFINSOutr>
                    /// 
                    #region <det><imposto><COFINSOutr>
                    NFe.det[nProd].Imposto.COFINS.CST = this.LerString(TpcnResources.CST, ObOp.Obrigatorio, 2, 2);
                    NFe.det[nProd].Imposto.COFINS.vCOFINS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vCOFINS, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "S07":
                    layout = "§S07|VBC|PCOFINS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><COFINSOutr>
                    /// 
                    #region <det><imposto><COFINSOutr>
                    NFe.det[nProd].Imposto.COFINS.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.COFINS.pCOFINS = this.LerDouble(this.TipoCampo42, TpcnResources.pCOFINS, ObOp.Obrigatorio, this.CasasDecimais75);
                    #endregion
                    break;

                case "S09":
                    layout = "§S09|QBCProd|VAliqProd"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><COFINSST>
                    /// 
                    #region <det><imposto><COFINSST>
                    NFe.det[nProd].Imposto.COFINS.qBCProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qBCProd, ObOp.Obrigatorio, 16);
                    NFe.det[nProd].Imposto.COFINS.vAliqProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.vAliqProd, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "T":
                    layout = "§T|VCOFINS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><COFINSST>
                    /// 
                    #region <det><imposto><COFINSST>
                    NFe.det[nProd].Imposto.COFINSST.vCOFINS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vCOFINS, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "T02":
                    layout = "§T02|VBC|PCOFINS"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><COFINSST>
                    /// 
                    #region <det><imposto><COFINSST>
                    NFe.det[nProd].Imposto.COFINSST.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.COFINSST.pCOFINS = this.LerDouble(this.TipoCampo42, TpcnResources.pCOFINS, ObOp.Obrigatorio, this.CasasDecimais75);
                    #endregion
                    break;

                case "T04":
                    layout = "§T04|QBCProd|VAliqProd"; //ok
                    ///
                    /// Grupo da TAG <det><imposto><COFINSST>
                    /// 
                    #region <det><imposto><COFINSST>
                    NFe.det[nProd].Imposto.COFINSST.qBCProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.qBCProd, ObOp.Obrigatorio, 16);
                    NFe.det[nProd].Imposto.COFINSST.vAliqProd = this.LerDouble(TpcnTipoCampo.tcDec4, TpcnResources.vAliqProd, ObOp.Obrigatorio, 15);
                    #endregion
                    break;

                case "U":
                    layout = (NFe.infNFe.Versao >= 3 ? 
                                "§U|VBC|VAliq|VISSQN|CMunFG|CListServ|vDeducao|vOutro|vDescIncond|vDescCond|vISSRet|indISS|cServico|cMun|cPais|nProcesso|indIncentivo|" :
                                "§U|VBC|VAliq|VISSQN|CMunFG|CListServ|cSitTrib"); //ok
                    ///
                    /// Grupo da TAG <det><imposto><ISSQN>
                    /// 
                    #region <det><imposto><ISSQN>
                    NFe.det[nProd].Imposto.ISSQN.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ISSQN.vAliq = this.LerDouble(this.TipoCampo42, TpcnResources.vAliq, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.det[nProd].Imposto.ISSQN.vISSQN = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vISSQN, ObOp.Obrigatorio, 15);
                    NFe.det[nProd].Imposto.ISSQN.cMunFG = this.LerInt32(TpcnResources.cMunFG, ObOp.Obrigatorio, 7, 7);
                    if (NFe.infNFe.Versao >= 3)
                    {
                        NFe.det[nProd].Imposto.ISSQN.cListServ = this.LerString(TpcnResources.cListServ, ObOp.Obrigatorio, 5, 5);
                        NFe.det[nProd].Imposto.ISSQN.vDeducao = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDeducao, ObOp.Opcional, 15);
                        NFe.det[nProd].Imposto.ISSQN.vOutro = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vOutro, ObOp.Opcional, 15);
                        NFe.det[nProd].Imposto.ISSQN.vDescIncond = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDescIncond, ObOp.Opcional, 15);
                        NFe.det[nProd].Imposto.ISSQN.vDescCond = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDescCond, ObOp.Opcional, 15);
                        NFe.det[nProd].Imposto.ISSQN.vISSRet = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vISSRet, ObOp.Opcional, 15);
                        NFe.det[nProd].Imposto.ISSQN.indISS = (TpcnindISS)this.LerInt32(TpcnResources.indISS, ObOp.Obrigatorio, 1, 1);
                        NFe.det[nProd].Imposto.ISSQN.cServico = this.LerString(TpcnResources.cServico, ObOp.Opcional, 1, 20);
                        NFe.det[nProd].Imposto.ISSQN.cMun = this.LerInt32(TpcnResources.cMun, ObOp.Opcional, 7, 7);
                        NFe.det[nProd].Imposto.ISSQN.cPais = this.LerInt32(TpcnResources.cPais, ObOp.Opcional, 4, 4);
                        NFe.det[nProd].Imposto.ISSQN.nProcesso = this.LerString(TpcnResources.nProcesso, ObOp.Opcional, 1, 30);
                        NFe.det[nProd].Imposto.ISSQN.indIncentivo = this.LerInt32(TpcnResources.indIncentivo, ObOp.Opcional, 1, 1) == 1;
                    }
                    else
                    {
                        NFe.det[nProd].Imposto.ISSQN.cListServ = this.LerString(TpcnResources.cListServ, ObOp.Obrigatorio, 3, 4);
                        NFe.det[nProd].Imposto.ISSQN.cSitTrib  = this.LerString(TpcnResources.cSitTrib, ObOp.Obrigatorio, 1, 1);
                    }
                    #endregion
                    break;

                case "UA":
                    layout = "§UA|pDevol|vIPIDevol";
                    NFe.det[nProd].impostoDevol.pDevol = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.pDevol, ObOp.Opcional, 5);
                    NFe.det[nProd].impostoDevol.vIPIDevol = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vIPIDevol, ObOp.Opcional, 5);
                    break;

                    /*
                     * 

                case "U51": //So Uninfe -> Segmento UA
                    layout = "§U51|pDevol"; //ok
                    NFe.det[nProd].impostoDevol.pDevol = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.pDevol, ObOp.Opcional, 5);
                    break;

                case "U61": //So Uninfe -> Segmento UA
                    layout = "§U61|vIPIDevol"; //ok
                    NFe.det[nProd].impostoDevol.vIPIDevol = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vIPIDevol, ObOp.Opcional, 5);
                    break;
                     * 
                     * */

                case "W02":
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§W02|vBC|vICMS|vICMSDeson|vBCST|vST|vProd|vFrete|vSeg|vDesc|vII|vIPI|vPIS|vCOFINS|vOutro|vNF|vTotTrib" :
                                "§W02|VBC|VICMS|VBCST|VST|VProd|VFrete|VSeg|VDesc|VII|VIPI|VPIS|VCOFINS|VOutro|VNF|vTotTrib|vICMSDeson"); //ok
                    ///
                    /// Grupo da TAG <total><ICMSTot>
                    /// 
                    #region <total><ICMSTot>
                    NFe.Total.ICMSTot.vBC = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vICMS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMS, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vBCST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCST, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vST = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vST, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vProd = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vProd, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vFrete = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vFrete, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vSeg = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vSeg, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vDesc = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDesc, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vII = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vII, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vIPI = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vIPI, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vPIS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPIS, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vCOFINS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vCOFINS, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vOutro = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vOutro, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vNF = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vNF, ObOp.Obrigatorio, 15);
                    NFe.Total.ICMSTot.vTotTrib = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vTotTrib, ObOp.Opcional, 15);
                    NFe.Total.ICMSTot.vICMSDeson = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSDeson, ObOp.Opcional, 15);
                    #endregion
                    break;

                case "W17":
                    layout = (NFe.infNFe.Versao >= 3 ?
                                "§W17|VServ|VBC|VISS|VPIS|VCOFINS|dCompet|vDeducao|vOutro|vDescIncond|vDescCond|vISSRet|cRegTrib" :
                                "§W17|VServ|VBC|VISS|VPIS|VCOFINS");
                    ///
                    /// Grupo da TAG <total><ISSQNtot>
                    /// 
                    #region <total><ISSQNtot>
                    NFe.Total.ISSQNtot.vServ    = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vServ, ObOp.Opcional, 15);
                    NFe.Total.ISSQNtot.vBC      = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBC, ObOp.Opcional, 15);
                    NFe.Total.ISSQNtot.vISS     = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vISS, ObOp.Opcional, 15);
                    NFe.Total.ISSQNtot.vPIS     = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPIS, ObOp.Opcional, 15);
                    NFe.Total.ISSQNtot.vCOFINS  = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vCOFINS, ObOp.Opcional, 15);

                    if ((double)NFe.infNFe.Versao >= 3.10)
                    {
                        NFe.Total.ISSQNtot.dCompet = (DateTime)this.LerCampo(TpcnTipoCampo.tcDatYYYY_MM_DD, TpcnResources.dCompet, ObOp.Opcional, 10, 10);
                        NFe.Total.ISSQNtot.vDeducao = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDeducao, ObOp.Opcional, 15);
                        NFe.Total.ISSQNtot.vOutro = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vOutro, ObOp.Opcional, 15);
                        NFe.Total.ISSQNtot.vDescIncond = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDescIncond, ObOp.Opcional, 15);
                        NFe.Total.ISSQNtot.vDescCond = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDescCond, ObOp.Opcional, 15);
                        NFe.Total.ISSQNtot.vISSRet = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vISSRet, ObOp.Opcional, 15);
                        NFe.Total.ISSQNtot.cRegTrib = (TpcnRegimeTributario)this.LerInt32(TpcnResources.cRegTrib, ObOp.Opcional, 1, 1);
                    }
                    #endregion
                    break;

                case "W23":
                    layout = "§W23|VRetPIS|VRetCOFINS|VRetCSLL|VBCIRRF|VIRRF|VBCRetPrev|VRetPrev"; //ok
                    ///
                    /// Grupo da TAG <total><retTrib>
                    /// 
                    #region <total><retTrib>
                    NFe.Total.retTrib.vRetPIS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vRetPIS, ObOp.Opcional, 15);
                    NFe.Total.retTrib.vRetCOFINS = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vRetCOFINS, ObOp.Opcional, 15);
                    NFe.Total.retTrib.vRetCSLL  = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vRetCSLL, ObOp.Opcional, 15);
                    NFe.Total.retTrib.vBCIRRF   = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCIRRF, ObOp.Opcional, 15);
                    NFe.Total.retTrib.vIRRF     = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vIRRF, ObOp.Opcional, 15);
                    NFe.Total.retTrib.vBCRetPrev = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCRetPrev, ObOp.Opcional, 15);
                    NFe.Total.retTrib.vRetPrev = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vRetPrev, ObOp.Opcional, 15);
                    #endregion
                    break;

                case "X":
                    layout = "§X|modFrete"; //ok
                    ///
                    /// Grupo da TAG <transp>
                    /// 
                    NFe.Transp.modFrete = (TpcnModalidadeFrete)this.LerInt32(TpcnResources.modFrete, ObOp.Obrigatorio, 1, 1);
                    break;

                case "X03":
                    layout = "§X03|xNome|IE|xEnder|UF|xMun"; //ok
                    ///
                    /// Grupo da TAG <transp><TRansportadora>
                    /// 
                    #region <transp><TRansportadora>
                    NFe.Transp.Transporta.xNome = this.LerString(TpcnResources.xNome, ObOp.Opcional, 1, 60);
                    NFe.Transp.Transporta.IE    = this.LerString(TpcnResources.IE, ObOp.Opcional, 0, 14);
                    NFe.Transp.Transporta.xEnder = this.LerString(TpcnResources.xEnder, ObOp.Opcional, 1, 60);
                    NFe.Transp.Transporta.xMun  = this.LerString(TpcnResources.xMun, ObOp.Opcional, 1, 60);
                    NFe.Transp.Transporta.UF    = this.LerString(TpcnResources.UF, ObOp.Opcional, 2, 2);
                    #endregion
                    break;

                case "X04":
                    layout = "§X04|CNPJ"; //ok

                    NFe.Transp.Transporta.CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Opcional, 14, 14);
                    break;

                case "X05":
                    layout = "§X05|CPF"; //ok

                    NFe.Transp.Transporta.CPF = this.LerString(TpcnResources.CPF, ObOp.Opcional, 11, 11);
                    break;

                case "X11":
                    layout = "§X11|VServ|VBCRet|PICMSRet|VICMSRet|CFOP|CMunFG"; //ok
                    ///
                    /// Grupo da TAG <transp><retTransp>
                    /// 
                    #region <transp><retTransp>
                    NFe.Transp.retTransp.vServ = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vServ, ObOp.Obrigatorio, 15);
                    NFe.Transp.retTransp.vBCRet = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vBCRet, ObOp.Obrigatorio, 15);
                    NFe.Transp.retTransp.pICMSRet = this.LerDouble(this.TipoCampo42, TpcnResources.pICMSRet, ObOp.Obrigatorio, this.CasasDecimais75);
                    NFe.Transp.retTransp.vICMSRet = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vICMSRet, ObOp.Obrigatorio, 15);
                    NFe.Transp.retTransp.CFOP   = this.LerString(TpcnResources.CFOP, ObOp.Obrigatorio, 4, 4);
                    NFe.Transp.retTransp.cMunFG = this.LerInt32(TpcnResources.cMunFG, ObOp.Obrigatorio, 7, 7);
                    #endregion
                    break;

                case "X18":
                    layout = "§X18|Placa|UF|RNTC"; //ok
                    ///
                    /// Grupo da TAG <transp><veicTransp>
                    /// 
                    #region <transp><veicTransp>
                    NFe.Transp.veicTransp.placa = this.LerString(TpcnResources.placa, ObOp.Obrigatorio, 1, 8);
                    NFe.Transp.veicTransp.UF    = this.LerString(TpcnResources.UF, ObOp.Obrigatorio, 2, 2);
                    NFe.Transp.veicTransp.RNTC  = this.LerString(TpcnResources.RNTC, ObOp.Opcional, 1, 20);
                    #endregion
                    break;

                case "X22":
                    layout = "§X22|Placa|UF|RNTC" + (NFe.infNFe.Versao >= 3 ? "|vagao|balsa" : "");
                    ///
                    /// Grupo da TAG <transp><reboque>
                    /// 
                    #region <transp><reboque>
                    NFe.Transp.Reboque.Add(new Reboque());
                    NFe.Transp.Reboque[NFe.Transp.Reboque.Count - 1].placa = this.LerString(TpcnResources.placa, ObOp.Obrigatorio, 1, 8);
                    NFe.Transp.Reboque[NFe.Transp.Reboque.Count - 1].UF = this.LerString(TpcnResources.UF, ObOp.Obrigatorio, 2, 2);
                    NFe.Transp.Reboque[NFe.Transp.Reboque.Count - 1].RNTC = this.LerString(TpcnResources.RNTC, ObOp.Opcional, 1, 20);
                    if (NFe.infNFe.Versao >= 3)
                    {
                        NFe.Transp.vagao = this.LerString(TpcnResources.vagao, ObOp.Opcional, 1, 20);
                        NFe.Transp.balsa = this.LerString(TpcnResources.balsa, ObOp.Opcional, 1, 20);
                    }
                    #endregion
                    break;

                    /*
                     * 

                case "X25a":
                    layout = "§X25a|vagao"; //ok
                    NFe.Transp.vagao = this.LerString(TpcnResources.vagao, ObOp.Opcional, 1, 20);
                    break;

                case "X25b":
                    layout = "§X25b|balsa"; //ok
                    NFe.Transp.balsa = this.LerString(TpcnResources.balsa, ObOp.Opcional, 1, 20);
                    break;
                     * 
                     */

                case "X26":
                    layout = "§X26|QVol|Esp|Marca|NVol|PesoL|PesoB"; //ok
                    ///
                    /// Grupo da TAG <transp><vol>
                    /// 
                    #region <transp><vol>
                    NFe.Transp.Vol.Add(new Vol());
                    NFe.Transp.Vol[NFe.Transp.Vol.Count - 1].qVol = this.LerInt32(TpcnResources.qVol, ObOp.Obrigatorio, 1, 15);
                    NFe.Transp.Vol[NFe.Transp.Vol.Count - 1].esp = this.LerString(TpcnResources.esp, ObOp.Opcional, 1, 60);
                    NFe.Transp.Vol[NFe.Transp.Vol.Count - 1].marca = this.LerString(TpcnResources.marca, ObOp.Opcional, 1, 60);
                    NFe.Transp.Vol[NFe.Transp.Vol.Count - 1].nVol = this.LerString(TpcnResources.nVol, ObOp.Opcional, 1, 60);
                    NFe.Transp.Vol[NFe.Transp.Vol.Count - 1].pesoL = this.LerDouble(TpcnTipoCampo.tcDec3, TpcnResources.pesoL, ObOp.Opcional, 15);
                    NFe.Transp.Vol[NFe.Transp.Vol.Count - 1].pesoB = this.LerDouble(TpcnTipoCampo.tcDec3, TpcnResources.pesoB, ObOp.Opcional, 15);
                    #endregion
                    break;

                case "X33":
                    layout = "§X33|NLacre"; //ok
                    ///
                    /// Grupo da TAG <transp><vol><lacres>
                    /// 
                    #region <transp><vol><lacres>
                    Lacres lacreItem = new Lacres();
                    lacreItem.nLacre = this.LerString(TpcnResources.nLacre, ObOp.Obrigatorio, 1, 60);

                    NFe.Transp.Vol[NFe.Transp.Vol.Count - 1].Lacres.Add(lacreItem);
                    #endregion
                    break;

                case "Y02":
                    layout = "§Y02|NFat|VOrig|VDesc|VLiq"; //ok
                    ///
                    /// Grupo da TAG <cobr>
                    /// 
                    #region <cobr>
                    NFe.Cobr.Fat.nFat = this.LerString(TpcnResources.nFat, ObOp.Opcional, 1, 60);
                    NFe.Cobr.Fat.vOrig = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vOrig, ObOp.Opcional, 15);
                    NFe.Cobr.Fat.vDesc = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDesc, ObOp.Opcional, 15);
                    NFe.Cobr.Fat.vLiq = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vLiq, ObOp.Opcional, 15);
                    #endregion
                    break;

                case "Y07":
                    layout = "§Y07|NDup|DVenc|VDup"; //ok
                    ///
                    /// Grupo da TAG <cobr><dup>
                    /// 
                    #region <cobr><dup>
                    NFe.Cobr.Dup.Add(new Dup());
                    NFe.Cobr.Dup[NFe.Cobr.Dup.Count - 1].nDup = this.LerString(TpcnResources.nDup, ObOp.Opcional, 1, 60);
                    NFe.Cobr.Dup[NFe.Cobr.Dup.Count - 1].dVenc = (DateTime)this.LerCampo(TpcnTipoCampo.tcDatYYYY_MM_DD, TpcnResources.dVenc, ObOp.Opcional, 10, 10);
                    NFe.Cobr.Dup[NFe.Cobr.Dup.Count - 1].vDup = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDup, ObOp.Opcional, 15);
                    #endregion
                    break;

                ///
                /// NFC-e
                /// 
                case "YA":
                    layout = "§YA|tPag|vPag|CNPJ|tBand|cAut";
                    #region YA
                    NFe.pag.Add(new pag());
                    NFe.pag[NFe.pag.Count - 1].tPag = (TpcnFormaPagamento)this.LerInt32(TpcnResources.tPag, ObOp.Obrigatorio, 2, 2);
                    NFe.pag[NFe.pag.Count - 1].vPag = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPag, ObOp.Obrigatorio, 15);
                    NFe.pag[NFe.pag.Count - 1].CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Opcional, 14, 14);
                    NFe.pag[NFe.pag.Count - 1].tBand = (TpcnBandeiraCartao)this.LerInt32(TpcnResources.tBand, ObOp.Opcional, 2, 2);
                    NFe.pag[NFe.pag.Count - 1].cAut = this.LerString(TpcnResources.cAut, ObOp.Opcional, 1, 20);
                    #endregion
                    break;

                    /*
                     * 

                case "YA02":
                    layout = "§YA02|tPag";
                    NFe.pag.Add(new pag());
                    NFe.pag[NFe.pag.Count - 1].tPag = (TpcnFormaPagamento)this.LerInt32(TpcnResources.tPag, ObOp.Obrigatorio, 2, 2);
                    break;
                case "YA03":
                    layout = "§YA03|vPag";
                    NFe.pag[NFe.pag.Count - 1].vPag = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vPag, ObOp.Obrigatorio, 15);
                    break;
                case "YA05":
                    layout = "§YA05|CNPJ";
                    NFe.pag[NFe.pag.Count - 1].CNPJ = this.LerString(TpcnResources.CNPJ, ObOp.Opcional, 14, 14);
                    break;
                case "YA06":
                    layout = "§YA06|tBand";
                    NFe.pag[NFe.pag.Count - 1].tBand = (TpcnBandeiraCartao)this.LerInt32(TpcnResources.tBand, ObOp.Opcional, 2, 2);
                    break;
                case "YA07":
                    layout = "§YA07|cAut";
                    NFe.pag[NFe.pag.Count - 1].cAut = this.LerString(TpcnResources.cAut, ObOp.Opcional, 1, 20);
                    break;
                     * 
                     */

                case "Z":
                    layout = "§Z|InfAdFisco|InfCpl"; //ok
                    ///
                    /// Grupo da TAG <InfAdic>
                    /// 
                    #region <InfAdic>
                    NFe.InfAdic.infAdFisco = this.LerString(TpcnResources.infAdFisco, ObOp.Opcional, 1, 2000);
                    NFe.InfAdic.infCpl = this.LerString(TpcnResources.infCpl, ObOp.Opcional, 1, 5000);
                    #endregion
                    break;

                case "Z04":
                    layout = "§Z04|XCampo|XTexto"; //ok
                    ///
                    /// Grupo da TAG <infAdic><obsCont>
                    /// 
                    #region <infAdic><obsCont>
                    NFe.InfAdic.obsCont.Add(new obsCont());
                    NFe.InfAdic.obsCont[NFe.InfAdic.obsCont.Count - 1].xCampo = this.LerString(TpcnResources.xCampo, ObOp.Obrigatorio, 1, 20);
                    NFe.InfAdic.obsCont[NFe.InfAdic.obsCont.Count - 1].xTexto = this.LerString(TpcnResources.xTexto, ObOp.Obrigatorio, 1, 60);
                    #endregion
                    break;

                case "Z07":
                    layout = "§Z07|XCampo|XTexto"; //ok - ?
                    ///
                    /// Grupo da TAG <infAdic><obsFisco>
                    /// 
                    #region <infAdic><obsFisco>
                    NFe.InfAdic.obsFisco.Add(new obsFisco());
                    NFe.InfAdic.obsFisco[NFe.InfAdic.obsFisco.Count - 1].xCampo = this.LerString(TpcnResources.xCampo, ObOp.Obrigatorio, 1, 20);
                    NFe.InfAdic.obsFisco[NFe.InfAdic.obsFisco.Count - 1].xTexto = this.LerString(TpcnResources.xTexto, ObOp.Obrigatorio, 1, 60);
                    #endregion
                    break;

                case "Z10":
                    layout = "§Z10|NProc|IndProc"; //ok
                    ///
                    /// Grupo da TAG <infAdic><procRef>
                    /// 
                    #region <infAdic><procRef>
                    NFe.InfAdic.procRef.Add(new procRef());
                    NFe.InfAdic.procRef[NFe.InfAdic.procRef.Count - 1].nProc = this.LerString(TpcnResources.nProc, ObOp.Obrigatorio, 1, 60);
                    NFe.InfAdic.procRef[NFe.InfAdic.procRef.Count - 1].indProc = this.LerString(TpcnResources.indProc, ObOp.Obrigatorio, 1, 1);
                    #endregion
                    break;

                case "ZA":
                case "ZA01":    //Só UniNFe
                    if (NFe.infNFe.Versao >= 3)
                    {
                        layout = "§"+this.FSegmento+"|UFSaidaPais|xLocExporta|xLocDespacho"; //ok
                        ///
                        /// Grupo da TAG <exporta>
                        /// 
                        NFe.exporta.UFSaidaPais = this.LerString(TpcnResources.UFSaidaPais, ObOp.Obrigatorio, 2, 2);
                        NFe.exporta.xLocExporta = this.LerString(TpcnResources.xLocExporta, ObOp.Obrigatorio, 1, 60);
                        NFe.exporta.xLocDespacho = this.LerString(TpcnResources.xLocDespacho, ObOp.Obrigatorio, 1, 60);
                    }
                    else
                    {
                        layout = "§ZA|UFEmbarq|XLocEmbarq"; //ok
                        ///
                        /// Grupo da TAG <exporta>
                        /// 
                        NFe.exporta.UFEmbarq = this.LerString(TpcnResources.UFEmbarq, ObOp.Obrigatorio, 2, 2);
                        NFe.exporta.xLocEmbarq = this.LerString(TpcnResources.xLocEmbarq, ObOp.Obrigatorio, 1, 60);
                    }
                    break;

                case "ZB":
                    layout = "§ZB|XNEmp|XPed|XCont"; //ok
                    ///
                    /// Grupo da TAG <compra>
                    /// 
                    NFe.compra.xNEmp = this.LerString(TpcnResources.xNEmp, ObOp.Opcional, 1, 17);
                    NFe.compra.xPed = this.LerString(TpcnResources.xPed, ObOp.Opcional, 1, 60);
                    NFe.compra.xCont = this.LerString(TpcnResources.xCont, ObOp.Opcional, 1, 60);
                    break;

                case "ZC":

                case "ZC01":
                    layout = "§"+this.FSegmento + "|safra|ref|qTotMes|qTotAnt|qTotGer|vFor|vTotDed|vLiqFor";
                    NFe.cana.safra = this.LerString(TpcnResources.safra, ObOp.Obrigatorio, 4, 9);
                    NFe.cana.Ref = this.LerString(TpcnResources.Ref, ObOp.Obrigatorio, 7, 7);
                    NFe.cana.qTotMes = this.LerDouble(TpcnTipoCampo.tcDec10, TpcnResources.qTotMes, ObOp.Obrigatorio, 11);
                    NFe.cana.qTotAnt = this.LerDouble(TpcnTipoCampo.tcDec10, TpcnResources.qTotAnt, ObOp.Obrigatorio, 11);
                    NFe.cana.qTotGer = this.LerDouble(TpcnTipoCampo.tcDec10, TpcnResources.qTotGer, ObOp.Obrigatorio, 11);
                    NFe.cana.vFor = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vFor, ObOp.Obrigatorio, 15);
                    NFe.cana.vTotDed = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vTotDed, ObOp.Obrigatorio, 15);
                    NFe.cana.vLiqFor = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vLiqFor, ObOp.Obrigatorio, 15);
                    break;

                case "ZC04":
                    layout = "§ZC04|dia|qtde";
                    NFe.cana.fordia.Add(new fordia());
                    NFe.cana.fordia[NFe.cana.fordia.Count - 1].dia = this.LerInt32(TpcnResources.dia, ObOp.Obrigatorio, 1, 2);
                    NFe.cana.fordia[NFe.cana.fordia.Count - 1].qtde = this.LerDouble(TpcnTipoCampo.tcDec10, TpcnResources.qtde, ObOp.Obrigatorio, 11);
                    break;

                case "ZC10":
                    layout = "§ZC10|xDed|vDed";
                    NFe.cana.deduc.Add(new deduc());
                    NFe.cana.deduc[NFe.cana.deduc.Count - 1].xDed = this.LerString(TpcnResources.xDed, ObOp.Obrigatorio, 1, 60);
                    NFe.cana.deduc[NFe.cana.deduc.Count - 1].vDed = this.LerDouble(TpcnTipoCampo.tcDec2, TpcnResources.vDed, ObOp.Obrigatorio, 15);
                    break;
            }
        }
    }

    public class txtTOxmlClassRetorno
    {
        public Int32 NotaFiscal { get; set; }
        public Int32 Serie { get; set; }
        public string XMLFileName { get; set; }
        public string ChaveNFe { get; set; }

        public txtTOxmlClassRetorno(string xmlFileName, string chaveNFe, Int32 notaFiscal, Int32 serie)
        {
            this.XMLFileName = xmlFileName;
            this.ChaveNFe = chaveNFe;
            this.NotaFiscal = notaFiscal;
            this.Serie = serie;
        }
    }
}