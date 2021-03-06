﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Web.Services.Description;
using System.Xml;
using System.IO;
using System.Linq;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using Microsoft.CSharp;
using System.Reflection;
using System.Xml.Serialization;
using System.ComponentModel;

namespace NFe.Components
{
    public class WebServiceProxy
    {
        #region Propriedades
        /// <summary>
        /// Descrição do serviço (WSDL)
        /// </summary>
        private ServiceDescription serviceDescription { get; set; }
        /// <summary>
        /// Código assembly do serviço
        /// </summary>
        private Assembly serviceAssemby { get; set; }
        /// <summary>
        /// Certificado digital a ser utilizado no consumo dos serviços
        /// </summary>
        private X509Certificate2 oCertificado { get; set; }

        #region Proxy
        /// <summary>
        /// Utiliza servidor proxy?
        /// </summary>
        public bool UtilizaServidorProxy { get; set; }
        /// <summary>
        /// Endereço do servidor de proxy
        /// </summary>
        public string ProxyServidor { get; set; }
        /// <summary>
        /// Usuário para autenticação no servidor de proxy
        /// </summary>
        public string ProxyUsuario { get; set; }
        /// <summary>
        /// Senha do usuário para autenticação no servidor de proxy
        /// </summary>
        public string ProxySenha { get; set; }
        /// <summary>
        /// Porta de comunicação do servidor proxy
        /// </summary>
        public int ProxyPorta { get; set; }
        #endregion

        /// <summary>
        /// Arquivo WSDL
        /// </summary>
        public string ArquivoWSDL { get; set; }
        private PadroesNFSe PadraoNFSe { get; set; }
        private Servicos servico;
        private bool taHomologacao;

        private string _NomeClasseWS;
        public string NomeClasseWS
        {
            get
            {
                switch (PadraoNFSe)
                {
                    #region DUETO
                    case PadroesNFSe.DUETO:
                        switch (servico)
                        {
                            case Servicos.NFSeConsultarLoteRps:
                                return "basic_INFSEConsultas";
                            case Servicos.NFSeConsultar:
                                return "basic_INFSEConsultas";
                            case Servicos.NFSeConsultarPorRps:
                                return "basic_INFSEConsultas";
                            case Servicos.NFSeConsultarSituacaoLoteRps:
                                return "basic_INFSEConsultas";
                            case Servicos.NFSeCancelar:
                                return "basic_INFSEGeracao";
                            case Servicos.NFSeRecepcionarLoteRps:
                                return "basic_INFSEGeracao";
                            default:
                                return _NomeClasseWS;
                        }
                    #endregion

                    #region ISSONLINE4R (4R Sistemas)
                    case PadroesNFSe.ISSONLINE4R:
                        switch (servico)
                        {
                            case Servicos.NFSeConsultarPorRps:
                                return (taHomologacao ? "hConsultarNfsePorRps" : "ConsultarNfsePorRps");

                            case Servicos.NFSeCancelar:
                                return (taHomologacao ? "hCancelarNfse" : NFe.Components.Servicos.NFSeCancelar.ToString());

                            case Servicos.NFSeRecepcionarLoteRps:
                                return (taHomologacao ? "hRecepcionarLoteRpsSincrono" : "RecepcionarLoteRpsSincrono");

                            default:
                                return _NomeClasseWS;
                        }
                    #endregion

                    #region SIMPLISS
                    case PadroesNFSe.SIMPLISS:
                        return _NomeClasseWS = "NfseService";
                    #endregion

                    #region PRONIM
                    case PadroesNFSe.PRONIN:
                        switch (servico)
                        {
                            case Servicos.NFSeCancelar:
                                return "basic_INFSEGeracao";
                            case Servicos.NFSeRecepcionarLoteRps:
                                return "basic_INFSEGeracao";
                            default:
                                return "basic_INFSEConsultas";
                        }
                    #endregion

                    #region E-GOVERNE
                    case PadroesNFSe.EGOVERNE:
                        return "WSNFSeV1001";
                    #endregion

                    default:
                        return _NomeClasseWS;
                }
            }
            protected set { _NomeClasseWS = value; }
        }
        public string[] NomeMetodoWS { get; protected set; }

        /// <summary>
        /// Lista utilizada para armazenar os webservices
        /// </summary>
        public static List<webServices> webServicesList { get; private set; }

        #endregion

        #region Construtores
        public WebServiceProxy(int cUF, string arquivoWSDL, X509Certificate2 Certificado, PadroesNFSe padraoNFSe, bool taHomologacao, Servicos servico)
        {
            this.ArquivoWSDL = arquivoWSDL;
            this.PadraoNFSe = padraoNFSe;
            this.servico = servico;
            this.taHomologacao = taHomologacao;

            //Definir o certificado digital que será utilizado na conexão com os serviços
            this.oCertificado = Certificado;

            //Confirmar a solicitação SSL automaticamente
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CertificateValidation);

            //Problema identificado com a Prefeitura de Porto Alegre - RS  Renan - 09/02/2015
            //Esta propriedade "Expect100Continue" por default é definida como "true" ou seja, o cliente esperará obter uma resposta 100-Continue do servidor para indicar que o cliente deve 
            //enviar os dados a ser lançadas. Esse mecanismo permite que os clientes evitem enviar grandes quantidades de dados através da rede quando o servidor, com base em cabeçalhos de solicitação, 
            //pretende descartar a solicitação.
            //Já esta propriedade marcada como "false", quando a solicitação inicial é enviada para o servidor, inclui os dados. Se, após ler os cabeçalhos de solicitação,
            //o servidor requer autenticação e deve enviar uma resposta 401, o cliente deve enviar novamente os dados com os cabeçalhos apropriadas de autenticação.
            ServicePointManager.Expect100Continue = false;

            //Obter a descrição do serviço (WSDL)    
            this.DescricaoServico(cUF, taHomologacao, arquivoWSDL);

            this.NomeClasseWS = null;
            this.NomeMetodoWS = null;
            if (this.serviceDescription.Services != null)
            {
                this.NomeClasseWS = ((System.Web.Services.Description.Service)this.serviceDescription.Services[0]).Name.Replace(" ", "");
            }

            if (this.serviceDescription.Bindings != null)
            {
                foreach (var xx in this.serviceDescription.Bindings)
                {
                    if (((System.Web.Services.Description.Binding)xx).Operations != null)
                    {
                        if (((System.Web.Services.Description.Binding)xx).Operations.Count > 0)
                        {
                            this.NomeMetodoWS = new string[((System.Web.Services.Description.Binding)xx).Operations.Count];
                            for (int n = 0; n < ((System.Web.Services.Description.Binding)xx).Operations.Count; ++n)
                            {
                                this.NomeMetodoWS[n] = ((System.Web.Services.Description.Binding)xx).Operations[n].Name;
                            }
                        }
                    }
                }
            }
            //if (Certificado != null)
            //Gerar e compilar a classe
            this.GerarClasse();
        }

        public WebServiceProxy(X509Certificate2 Certificado)
        {
            this.oCertificado = Certificado;
        }

        #endregion

        #region Métodos públicos

        #region ReturnArray()
        /// <summary>
        /// Método que verifica se o tipo de retornjo de uma operação/método é array ou não
        /// </summary>
        /// <param name="Instance">Instancia do objeto</param>
        /// <param name="methodName">Nome do método</param>
        /// <returns>true se o tipo de retorno do método passado por parâmetro for um array</returns>
        /*public bool ReturnArray(object Instance, string methodName)
        {
            Type tipoInstance = Instance.GetType();

            return tipoInstance.GetMethod(methodName).ReturnType.IsSubclassOf(typeof(Array));
        }*/
        #endregion

        #region Invoke()
        /// <summary>
        /// Invocar o método da classe
        /// </summary>
        /// <param name="Instance">Instância do objeto</param>
        /// <param name="methodName">Nome do método</param>
        /// <param name="parameters">Objeto com o conteúdo dos parâmetros do método</param>
        /// <returns>Objeto - Um objeto somente, podendo ser primário ou complexo</returns>
        private object Invoke(object Instance, string methodName, object[] parameters)
        {
            try
            {
                //Relacionar o certificado digital que será utilizado no serviço que será consumido do webservice
                this.RelacCertificado(Instance);
                
                Type tipoInstance = Instance.GetType();

                return tipoInstance.GetMethod(methodName).Invoke(Instance, parameters);
            }
            catch (Exception ex)
            {
                string msgErro = "Erro ao invocar o método '" + methodName + "'.\r\nWSDL: " + this.ArquivoWSDL + "\r\n" + ex.Message;

                if (ex.InnerException != null)
                {
                    msgErro += "\r\n " + ex.InnerException.Message;

                    if (ex.InnerException.InnerException != null)
                    {
                        msgErro += "\r\n " + ex.InnerException.InnerException.Message;

                        if (ex.InnerException.InnerException.InnerException != null)
                        {
                            msgErro += "\r\n " + ex.InnerException.InnerException.InnerException.Message;

                            if (ex.InnerException.InnerException.InnerException.InnerException != null)
                            {
                                msgErro += "\r\n " + ex.InnerException.InnerException.InnerException.InnerException.Message;
                            }
                        }
                    }
                }

                throw new Exception(msgErro);
            }
        }
        #endregion

        #region InvokeXML()
        /// <summary>
        /// Invocar o método da classe
        /// </summary>
        /// <param name="Instance">Instância do objeto</param>
        /// <param name="methodName">Nome do método</param>
        /// <param name="parameters">Objeto com o conteúdo dos parâmetros do método</param>
        /// <returns>Um objeto do tipo XML</returns>
        public XmlNode InvokeXML(object Instance, string methodName, object[] parameters)
        {
            //Invocar método do serviço
            return (XmlNode)this.Invoke(Instance, methodName, parameters);
        }
        #endregion

        #region InvokeXML()
        /// <summary>
        /// Invocar o método da classe
        /// </summary>
        /// <param name="Instance">Instância do objeto</param>
        /// <param name="methodName">Nome do método</param>
        /// <param name="parameters">Objeto com o conteúdo dos parâmetros do método</param>
        /// <returns>Um objeto do tipo string</returns>
        public string InvokeStr(object Instance, string methodName, object[] parameters)
        {
            //Invocar método do serviço
            return (string)this.Invoke(Instance, methodName, parameters);
        }
        #endregion

        #region SetProp()
        /// <summary>
        /// Alterar valor das propriedades da classe
        /// </summary>
        /// <param name="Instance">Instância do objeto</param>
        /// <param name="propertyName">Nome da propriedade</param>
        /// <param name="novoValor">Novo valor para ser gravado na propriedade</param>
        /// <remarks>
        /// Autor: Wandrey Mundin Ferreira
        /// Data: 09/02/2010
        /// </remarks>
        public void SetProp(object instance, string propertyName, object novoValor)
        {
            Type tipoInstance = instance.GetType();
            PropertyInfo property = tipoInstance.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            property.SetValue(instance, novoValor, null);
        }
        #endregion

        #region GetProp()
        /// <summary>
        /// Alterar valor das propriedades da classe
        /// </summary>
        /// <param name="instance">Instância do objeto</param>
        /// <param name="propertyName">Nome da propriedade</param>
        /// <remarks>
        /// Autor: Wandrey Mundin Ferreira
        /// Data: 09/02/2010
        /// </remarks>
        public object GetProp(object instance, string propertyName)
        {
            Type tipoInstance = instance.GetType();
            PropertyInfo property = tipoInstance.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            return property.GetValue(instance, null);
        }
        #endregion

        #region CertificateValidation
        /// <summary>
        /// Responsável por retornar uma confirmação verdadeira para a proriedade ServerCertificateValidationCallback 
        /// da classe ServicePointManager para confirmar a solicitação SSL automaticamente.
        /// </summary>
        /// <returns>Retornará sempre true</returns>
        public bool CertificateValidation(object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErros)
        {
            return true;
        }
        #endregion

        #region CriarObjeto()
        /// <summary>
        /// Criar objeto das classes do serviço
        /// </summary>
        /// <param name="NomeClasse">Nome da classe que é para ser instanciado um novo objeto</param>
        /// <returns>Retorna o objeto</returns>
        public object CriarObjeto(string NomeClasse)
        {
            if (string.IsNullOrEmpty(NomeClasse) || this.serviceAssemby.GetType(NomeClasse) == null)
                throw new Exception("Nome da classe '" + NomeClasse + "' no webservice não pode ser processada\r\nWSDL: " + this.ArquivoWSDL);

            return Activator.CreateInstance(this.serviceAssemby.GetType(NomeClasse));
        }
        #endregion

        #endregion

        #region Métodos privados

        #region DescricaoServico()
        /// <summary>
        /// Obter a descrição completa do serviço, ou seja, o WSDL do webservice de um arquivo local
        /// </summary>
        /// <param name="arquivoWSDL">Local e nome do arquivo WDDL</param>
        /// <param name="Certificado">Certificado digital</param>
        private void DescricaoServico(int cUF, bool taHomologacao, string arquivoWSDL)
        {
            //Forçar utilizar o protocolo SSL 3.0 que está de acordo com o manual de integração do SEFAZ
            //Wandrey 31/03/2010
            switch (cUF)
            {
                case 52: //Goiás
                    if (taHomologacao)
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                    else
                        goto default;
                    break;
                case 13: // Amazonas
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                    break;
                case 21: // Maranhão
                     ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                     break;
                case 22: // Piaui
                     ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                     break;
                case 15: // Para
                     ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                     break;
                default:
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                    break;
            }

            //Definir a descrição completa do servido (WSDL)
            this.serviceDescription = ServiceDescription.Read(arquivoWSDL);
        }
        #endregion

        #region GerarClasse()
        /// <summary>
        /// Gerar o source code do serviço
        /// </summary>
        private void GerarClasse()
        {
            #region Gerar o código da classe
            StringWriter writer = new StringWriter(CultureInfo.CurrentCulture);
            CSharpCodeProvider provider = new CSharpCodeProvider();
            provider.GenerateCodeFromNamespace(GerarGrafo(), writer, null);
            #endregion

            string codigoClasse = writer.ToString();

            #region Compilar o código da classe
            CompilerResults results = provider.CompileAssemblyFromSource(ParametroCompilacao(), codigoClasse);
            serviceAssemby = results.CompiledAssembly;
            #endregion
        }
        #endregion

        #region ParametroCompilacao
        /// <summary>
        /// Montar os parâmetros para a compilação da classe
        /// </summary>
        /// <returns>Retorna os parâmetros</returns>
        private CompilerParameters ParametroCompilacao()
        {
            CompilerParameters parameters = new CompilerParameters(new string[] { "System.dll", "System.Xml.dll", "System.Web.Services.dll", "System.Data.dll" });
            parameters.GenerateExecutable = false;
            parameters.GenerateInMemory = true;
            parameters.TreatWarningsAsErrors = false;
            parameters.WarningLevel = 4;

            return parameters;
        }
        #endregion

        #region GerarGrafo()
        /// <summary>
        /// Gerar a estrutura e o grafo da classe
        /// </summary>
        private CodeNamespace GerarGrafo()
        {
            #region Gerar a estrutura da classe do serviço
            //Gerar a estrutura da classe do serviço
            ServiceDescriptionImporter importer = new ServiceDescriptionImporter();
            importer.AddServiceDescription(this.serviceDescription, string.Empty, string.Empty);

            //Definir o nome do protocolo a ser utilizado
            //Não posso definir, tenho que deixar por conta do WSDL definir, ou pode dar erro em alguns estados
            //importer.ProtocolName = "Soap12";
            if (PadraoNFSe == PadroesNFSe.THEMA)
                importer.ProtocolName = "Soap";

            //Tipos deste serviço devem ser gerados como propriedades e não como simples campos
            importer.CodeGenerationOptions = CodeGenerationOptions.GenerateProperties;
            #endregion

            #region Se a NFSe for padrão DUETO/WEBISS/SALVADOR_BA/PRONIN preciso importar os schemas do WSDL
            switch (PadraoNFSe)
            {
                case PadroesNFSe.SMARAPD:
                case PadroesNFSe.DUETO:
                case PadroesNFSe.WEBISS:
                case PadroesNFSe.SALVADOR_BA:
                case PadroesNFSe.GIF:
                case PadroesNFSe.PRONIN:
                    //Tive que utilizar a WebClient para que a OpenRead funcionasse, não foi possível fazer funcionar com a SecureWebClient. Tem que analisar melhor. Wandrey e Renan 10/09/2013
                    WebClient client = new WebClient();
                    Stream stream = client.OpenRead(ArquivoWSDL);

                    //Esta sim tem que ser com a SecureWebClient pq tem que ter o certificado. Wandrey 10/09/2013
                    SecureWebClient client2 = new SecureWebClient(oCertificado);

                    // Add any imported files
                    foreach (System.Xml.Schema.XmlSchema wsdlSchema in serviceDescription.Types.Schemas)
                    {
                        foreach (System.Xml.Schema.XmlSchemaObject externalSchema in wsdlSchema.Includes)
                        {
                            if (externalSchema is System.Xml.Schema.XmlSchemaImport)
                            {
                                Uri baseUri = new Uri(ArquivoWSDL);
                                Uri schemaUri = new Uri(baseUri, ((System.Xml.Schema.XmlSchemaExternal)externalSchema).SchemaLocation);
                                stream = client2.OpenRead(schemaUri);
                                System.Xml.Schema.XmlSchema schema = System.Xml.Schema.XmlSchema.Read(stream, null);
                                importer.Schemas.Add(schema);
                            }
                        }
                    }
                    break;
            }
            #endregion

            #region Gerar o o grafo da classe para depois gerar o código
            CodeNamespace @namespace = new CodeNamespace();
            CodeCompileUnit unit = new CodeCompileUnit();
            unit.Namespaces.Add(@namespace);
            ServiceDescriptionImportWarnings warmings = importer.Import(@namespace, unit);
            #endregion

            return @namespace;
        }
        #endregion

        #region RelacCertificado
        /// <summary>
        /// Relacionar o certificado digital com o serviço que será consumido do webservice
        /// </summary>
        /// <param name="instance">Objeto do serviço que será consumido</param>
        private void RelacCertificado(object instance)
        {
            if (this.oCertificado != null)
            {
                Type tipoInstance = instance.GetType();
                object oClientCertificates = tipoInstance.InvokeMember("ClientCertificates", System.Reflection.BindingFlags.GetProperty, null, instance, new Object[] { });
                Type tipoClientCertificates;
                tipoClientCertificates = oClientCertificates.GetType();
                tipoClientCertificates.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, oClientCertificates, new Object[] { this.oCertificado });
            }
        }
        #endregion

        #endregion

        #region Objeto da BETHA Sistemas para acessar os WebServices da NFSe
        public IBetha Betha;
        #endregion

        #region CarregaWebServicesList()
        /// <summary>
        /// Carrega a lista de webservices definidos no arquivo WebService.XML
        /// </summary>
        public static bool CarregaWebServicesList()
        {
            bool atualizaWSDL = false;
            if (webServicesList == null)
            {
                webServicesList = new List<webServices>();
                Propriedade.Municipios = null;
                Propriedade.Municipios = new List<Municipio>();

                XmlDocument doc = new XmlDocument();
                /// danasa 1-2012
                if (File.Exists(Propriedade.NomeArqXMLMunicipios))
                {
                    doc.Load(Propriedade.NomeArqXMLMunicipios);
                    XmlNodeList estadoList = doc.GetElementsByTagName(NFe.Components.NFeStrConstants.Registro);
                    foreach (XmlNode registroNode in estadoList)
                    {
                        XmlElement registroElemento = (XmlElement)registroNode;
                        if (registroElemento.Attributes.Count > 0)
                        {
                            int IDmunicipio = Convert.ToInt32("0" + Functions.OnlyNumbers(registroElemento.Attributes[TpcnResources.ID.ToString()].Value));
                            string Nome = registroElemento.Attributes[NFeStrConstants.Nome].Value;
                            string Padrao = registroElemento.Attributes[NFeStrConstants.Padrao].Value;
                            string UF = Functions.CodigoParaUF(Convert.ToInt32(IDmunicipio.ToString().Substring(0, 2)));

                            ///
                            /// danasa 9-2013
                            /// verifica se o 'novo' padrao existe, nao existindo retorna para atualizar os wsdl's dele
                            string dirSchemas = Path.Combine(Propriedade.PastaExecutavel, "NFse\\schemas\\NFSe\\" + Padrao);
                            if (!Directory.Exists(dirSchemas))
                            {
                                atualizaWSDL = true;
                            }
                            PadroesNFSe pdr = WebServiceNFSe.GetPadraoFromString(Padrao);


                            webServices wsItem = new webServices(IDmunicipio, Nome, UF);

                            PreencheURLw(wsItem.LocalHomologacao,
                                         NFe.Components.NFeStrConstants.LocalHomologacao,
                                         WebServiceNFSe.WebServicesHomologacao(ref pdr, IDmunicipio),
                                         "",
                                         "NFse\\");
                            PreencheURLw(wsItem.LocalProducao,
                                         NFe.Components.NFeStrConstants.LocalProducao,
                                         WebServiceNFSe.WebServicesProducao(ref pdr, IDmunicipio),
                                         "",
                                         "NFse\\");

                            webServicesList.Add(wsItem);
                            ///
                            /// adiciona na lista que será usada na manutencao
                            /// 
                            Propriedade.Municipios.Add(new Municipio(IDmunicipio, UF, Nome, pdr));
                        }
                    }
                }
                /// danasa 1-2012

                bool salvaXmlLocal = false;
                LoadArqXMLWebService(Propriedade.NomeArqXMLWebService_NFe, "NFe\\");
                salvaXmlLocal = LoadArqXMLWebService(Propriedade.NomeArqXMLWebService_NFSe, "NFse\\");

                if (salvaXmlLocal)
                {
                    WebServiceNFSe.SalvarXMLMunicipios(null, null, 0, null, false);
                }
            }
            return atualizaWSDL;
        }

        private static bool LoadArqXMLWebService(string filenameWS, string subfolder)
        {
            bool salvaXmlLocal = false;

            if (File.Exists(filenameWS))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filenameWS);
                XmlNodeList estadoList = doc.GetElementsByTagName(NFeStrConstants.Estado);
                foreach (XmlNode estadoNode in estadoList)
                {
                    XmlElement estadoElemento = (XmlElement)estadoNode;
                    if (estadoElemento.Attributes.Count > 0)
                    {
                        if (estadoElemento.Attributes[TpcnResources.UF.ToString()].Value != "XX")
                        {
                            int ID = Convert.ToInt32("0" + Functions.OnlyNumbers(estadoElemento.Attributes[TpcnResources.ID.ToString()].Value));
                            if (ID == 0)
                                continue;
                            string Nome = estadoElemento.Attributes[NFeStrConstants.Nome].Value;
                            string UF = estadoElemento.Attributes[TpcnResources.UF.ToString()].Value;

                            /// danasa 1-2012
                            ///
                            /// verifica se o ID já está na lista
                            /// isto previne que no xml de configuracao tenha duplicidade e evita derrubar o programa
                            ///
                            var ci = (from i in webServicesList where i.ID == ID select i).FirstOrDefault();
                            if (ci == null)
                            {
                                webServices wsItem = new webServices(ID, Nome, UF);
                                XmlNodeList urlList;

                                urlList = estadoElemento.GetElementsByTagName(NFe.Components.NFeStrConstants.LocalHomologacao);
                                if (urlList.Count > 0)
                                    PreencheURLw(wsItem.LocalHomologacao,
                                                 NFe.Components.NFeStrConstants.LocalHomologacao,
                                                 urlList.Item(0).OuterXml,
                                                 UF,
                                                 subfolder);

                                urlList = estadoElemento.GetElementsByTagName(NFe.Components.NFeStrConstants.LocalProducao);
                                if (urlList.Count > 0)
                                    PreencheURLw(wsItem.LocalProducao,
                                                 NFe.Components.NFeStrConstants.LocalProducao,
                                                 urlList.Item(0).OuterXml,
                                                 UF,
                                                 subfolder);

                                webServicesList.Add(wsItem);
                            }
                            // danasa 1-2012
                            if (estadoElemento.HasAttribute(NFeStrConstants.Padrao) || subfolder.Equals("NFse\\"))
                            {
                                try
                                {
                                    string padrao = estadoElemento.Attributes[NFeStrConstants.Padrao].Value;
                                    if (!padrao.Equals(PadroesNFSe.NaoIdentificado.ToString(), StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        var cc = (from i in Propriedade.Municipios
                                                  where i.CodigoMunicipio == ID
                                                  select i).FirstOrDefault();
                                        if (cc == null)
                                        {
                                            Propriedade.Municipios.Add(new Municipio(ID, UF, Nome, WebServiceNFSe.GetPadraoFromString(padrao)));
                                            salvaXmlLocal = true;
                                        }
                                        else
                                        {
                                            if (!cc.PadraoStr.Equals(padrao) || !cc.UF.Equals(UF) || !cc.Nome.Equals(Nome, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                cc.Padrao = WebServiceNFSe.GetPadraoFromString(padrao);
                                                cc.Nome = Nome;
                                                cc.UF = UF;
                                                salvaXmlLocal = true;
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                            // danasa 1-2012
                        }
                    }
                }
            }
            return salvaXmlLocal;
        }
        #endregion

        #region reloadWebServicesList()
        /// <summary>
        /// Recarrega a lista de webservices
        /// usado pelo projeto da NFes quando da manutencao
        /// </summary>
        public static bool reloadWebServicesList()
        {
            webServicesList = null;
            return CarregaWebServicesList();
        }
        #endregion

        #region PreencheURLw
        private static void PreencheURLw(URLws wsItem, string tagName, string urls, string uf, string subfolder)
        {
            if (urls == "")
                return;

            string AppPath = Propriedade.PastaExecutavel + "\\" + subfolder;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(urls);
            XmlNodeList urlList = doc.ChildNodes;
            if (urlList == null)
                return;

            for (int i = 0; i < urlList.Count; ++i)
            {
                for (int j = 0; j < urlList[i].ChildNodes.Count; ++j)
                {
                    string appPath = "";
                    System.Reflection.PropertyInfo ClassProperty = wsItem.GetType().GetProperty(urlList[i].ChildNodes[j].Name);
                    if (ClassProperty != null)
                    {
                        appPath = AppPath + urlList[i].ChildNodes[j].InnerText;

                        if (!string.IsNullOrEmpty(urlList[i].ChildNodes[j].InnerText))
                        {
                            if (urlList[i].ChildNodes[j].InnerText.ToLower().EndsWith("asmx?wsdl"))
                            {
                                appPath = urlList[i].ChildNodes[j].InnerText;
                            }
                            else
                            {
                                if (!File.Exists(appPath))
                                {
                                    appPath = "";
                                }
                            }
                        }
                        else
                            appPath = "";

                        ClassProperty.SetValue(wsItem, appPath, null);
                    }

                    if (appPath == "" && !string.IsNullOrEmpty(urlList[i].ChildNodes[j].InnerText))
                    {
                        string msg = "";
                        Console.WriteLine(msg = "wsItem <" + urlList[i].ChildNodes[j].InnerText + "> nao encontrada na classe URLws em <" + urlList[i].ChildNodes[j].Name + ">");

                        NFe.Components.Functions.WriteLog(msg, false, true, "");
                    }
                }
            }
        }
        #endregion
    }

    public class webServices
    {
        public int ID { get; private set; }
        public string Nome { get; private set; }
        public string UF { get; private set; }
        public URLws LocalHomologacao { get; private set; }
        public URLws LocalProducao { get; private set; }

        public webServices(int id, string nome, string uf)
        {
            LocalHomologacao = new URLws();
            LocalProducao = new URLws();
            ID = id;
            Nome = nome;
            UF = uf;
        }
    }

    [ToolboxItem(false)]
    class SecureWebClient : WebClient
    {
        private readonly X509Certificate2 Certificado;

        public SecureWebClient(X509Certificate2 certificado)
        {
            Certificado = certificado;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.ClientCertificates.Add(Certificado);
            return request;
        }
    }

    public class URLws
    {
        public URLws()
        {
            ///
            /// NFS-e
            CancelarNfse =
            ConsultarLoteRps =
            ConsultarNfse =
            ConsultarNfsePorRps =
            ConsultarSituacaoLoteRps =
            ConsultarURLNfse =
            ConsultarNFSePNG =
            InutilizarNFSe =
            RecepcionarLoteRps =
                ///
                /// NF-e
            NFeRecepcaoEvento =
            NFeConsulta =
            NFeConsultaCadastro =
            NFeConsultaNFeDest =
            NFeDownload =
            NFeInutilizacao =
            NFeManifDest =
            NFeRecepcao =
            NFeRetRecepcao =
            NFeStatusServico =
            //NFeRegistroDeSaida =
            //NFeRegistroDeSaidaCancelamento =
            NFeAutorizacao =
            NFeRetAutorizacao =
                ///
                /// MDF-e
            MDFeRecepcao =
            MDFeRetRecepcao =
            MDFeConsulta =
            MDFeStatusServico =
            MDFeRecepcaoEvento =
            MDFeNaoEncerrado =
                ///
                /// DF-e
            DFeRecepcao =
                ///
                /// CT-e
            CTeRecepcao =
            CTeRetRecepcao =
            CTeInutilizacao =
            CTeConsulta =
            CTeStatusServico =
            CTeRecepcaoEvento =
            CTeConsultaCadastro = string.Empty;
        }

        #region Propriedades referente as tags do webservice.xml
        // ******** ATENÇÃO *******
        // os nomes das propriedades tem que ser iguais as tags no WebService.xml
        // ******** ATENÇÃO *******

        #region NFe
        public string NFeRecepcao { get; set; }
        public string NFeRetRecepcao { get; set; }
        public string NFeInutilizacao { get; set; }
        public string NFeConsulta { get; set; }
        public string NFeStatusServico { get; set; }
        public string NFeConsultaCadastro { get; set; }
        public string NFeAutorizacao { get; set; }
        public string NFeRetAutorizacao { get; set; }
        /// <summary>
        /// Recepção de eventos da NFe
        /// </summary>
        public string NFeRecepcaoEvento { get; set; }
        public string NFeDownload { get; set; }
        public string NFeConsultaNFeDest { get; set; }
        public string NFeManifDest { get; set; }
        //public string NFeRegistroDeSaida { get; set; }
        //public string NFeRegistroDeSaidaCancelamento { get; set; }
        #endregion

        #region NFS-e
        /// <summary>
        /// Enviar Lote RPS NFS-e 
        /// </summary>
        public string RecepcionarLoteRps { get; set; }
        /// <summary>
        /// Consultar Situação do lote RPS NFS-e
        /// </summary>
        public string ConsultarSituacaoLoteRps { get; set; }
        /// <summary>
        /// Consultar NFS-e por RPS
        /// </summary>
        public string ConsultarNfsePorRps { get; set; }
        /// <summary>
        /// Consultar NFS-e por NFS-e
        /// </summary>
        public string ConsultarNfse { get; set; }
        /// <summary>
        /// Consultar lote RPS
        /// </summary>
        public string ConsultarLoteRps { get; set; }
        /// <summary>
        /// Cancelar NFS-e
        /// </summary>
        public string CancelarNfse { get; set; }
        /// <summary>
        /// Consulta URL de Visualização da NFSe
        /// </summary>
        public string ConsultarURLNfse { get; set; }
        /// <summary>
        /// Consulta a imagem em PNG da Nota
        /// </summary>
        public string ConsultarNFSePNG { get; set; }
        /// <summary>
        /// Inutilização da NFSe
        /// </summary>
        public string InutilizarNFSe { get; set; }
        #endregion

        #region MDF-e
        /// <summary>
        /// Recepção do MDFe
        /// </summary>
        public string MDFeRecepcao { get; set; }
        /// <summary>
        /// Consulta Recibo do lote de MDFe enviado
        /// </summary>
        public string MDFeRetRecepcao { get; set; }
        /// <summary>
        /// Consulta situação do MDFe
        /// </summary>
        public string MDFeConsulta { get; set; }
        /// <summary>
        /// Consulta status do serviço de MDFe
        /// </summary>
        public string MDFeStatusServico { get; set; }
        /// <summary>
        /// Recepcao dos eventos do MDF-e
        /// </summary>
        public string MDFeRecepcaoEvento { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MDFeNaoEncerrado { get; set; }
        #endregion

        #region CTe
        /// <summary>
        /// Recepção do CTe
        /// </summary>
        public string CTeRecepcao { get; set; }
        /// <summary>
        /// Consulta recibo do lote de CTe enviado
        /// </summary>
        public string CTeRetRecepcao { get; set; }
        /// <summary>
        /// Inutilização numeração do CTe
        /// </summary>
        public string CTeInutilizacao { get; set; }
        /// <summary>
        /// Consulta Situação do CTe
        /// </summary>
        public string CTeConsulta { get; set; }
        /// <summary>
        /// Consulta Status Serviço do CTe
        /// </summary>
        public string CTeStatusServico { get; set; }
        /// <summary>
        /// Consulta cadastro do contribuinte do CTe
        /// </summary>
        public string CTeConsultaCadastro { get; set; }
        /// <summary>
        /// Recepção de eventos do CTe
        /// </summary>
        public string CTeRecepcaoEvento { get; set; }
        #endregion

        #region DF-e
        public string DFeRecepcao { get; set; }
        #endregion

        #endregion
    }
}
