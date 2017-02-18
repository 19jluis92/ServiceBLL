﻿using System;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Beneficiarios.DTO;
using System.Configuration;


        /// <summary>
        /// Clase padre para los metodos de las BLL 
        /// </summary>
        public class ServiceBLL
        {

            protected HttpWebRequest request { get; set; }

            protected string _baseUrl { get; set; }

            protected NetworkCredential _credentials { get; set; }


            public ServiceBLL()
            {
                _baseUrl = "url_Sharepoint";

                var appSettings = ConfigurationManager.AppSettings;

                _credentials = new NetworkCredential(appSettings["User"], appSettings["Password"], appSettings["Domain"]);
            }

            protected ServiceBLL(HttpWebRequest context)
            {
                this.request = context;
                var appSettings = ConfigurationManager.AppSettings;

                _credentials = new NetworkCredential(appSettings["User"], appSettings["Password"], appSettings["Domain"]);
            }

            /// <summary>
            /// Login de usuarios
            /// </summary>
            /// <param name="user">nombre de usuario</param>
            /// <param name="pass">contraseña</param>
            /// <returns>UserDTO</returns>
            internal UserDTO LogIn(string user, string pass)
            {

                UserDTO userDB = new UserDTO();
                var request = this.GetList("", "getbytitle('Contactos')", string.Format("Items?$filter= Correo_x0020_Electr_x00f3_nico eq '{0}'", user));


                try
                {
                    using (var response = request.GetResponse())
                    {
                        using (var stream = response.GetResponseStream())
                        {

                            var reader = new StreamReader(stream);
                            var x = reader.ReadToEnd();
                            dynamic stuff = JObject.Parse(x);
                            var result3 = JsonConvert.SerializeObject(stuff.d.results[0]);
                            userDB = JsonConvert.DeserializeObject<UserDTO>(result3);

                        }
                    }
                }
                catch (Exception e)
                {
                    return null;
                }

                string[] _saltString = userDB.Password.Split('|');
                byte[] _salt = GetBytes(CreateSalt());
                int i = 0;
                foreach (string auxSalt in _saltString[1].Split('-'))
                {
                    _salt[i++] = Byte.Parse(auxSalt, NumberStyles.HexNumber);
                }
                var _passHash = BitConverter.ToString(Hash(pass, _salt));

                if (userDB.Email.ToLower().Equals(user.ToLower()) && (_saltString[0].SequenceEqual(_passHash)))
                    return userDB;
                return null;

            }


            /// <summary>
            /// Devuelve listas
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>HttpWebRequest</returns>
            internal HttpWebRequest GetList(string url, string function = "", string query = "", string method = "GET", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose", string webApi = "_api/web/lists")
            {

                request = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + webApi + ((function != "" ? '/' + function : function)) + (query != "" ? '/' + query : ""));
                request.Method = method;
                request.Credentials = _credentials;
                request.ContentType = contenType;
                request.Accept = accept;
                return request;
            }



            /// <summary>
            /// 
            /// </summary>
            /// <param name="url"></param>
            /// <param name="method"></param>
            /// <param name="contenType"></param>
            /// <param name="accept"></param>
            /// <param name="webApi"></param>
            /// <returns></returns>
            internal HttpWebRequest GetListFilesAttachedOnList(string url, string method = "GET", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose", string webApi = "_api/web/lists")
            {

                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                request.Credentials = _credentials;
                request.ContentType = contenType;
                request.Accept = accept;
                return request;
            }



            /// <summary>
            /// 
            /// </summary>
            /// <param name="url"></param>
            /// <param name="method"></param>
            /// <param name="contenType"></param>
            /// <param name="accept"></param>
            /// <param name="webApi"></param>
            /// <returns></returns>
            internal HttpWebRequest GetListFileOnList(string url, string function = "", string query = "", string method = "GET", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose", string webApi = "_api/web/lists")
            {
                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;


                HttpWebResponse endpointResponse = (HttpWebResponse)endpointRequest.GetResponse();
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }
                request = (HttpWebRequest)WebRequest.Create(_baseUrl + webApi + ((function != "" ? '/' + function : function)) + (query != "" ? '/' + query : ""));
                request.Method = method;
                request.Credentials = _credentials;
                request.ContentType = "application/json;odata=verbose"; ;
                request.Accept = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; ;
                request.Headers.Add("Authorization", "'Bearer ' + " + FormDigestValue);
                return request;
            }

            /// <summary>
            /// Guarda informacion
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="_bytes">contenido del objeto</param>
            /// <param name="_jsonLength">tamaño del paquete</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>HttpWebRequest</returns>
            internal HttpWebRequest Save(string url, byte[] _bytes, int _jsonLength, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose", string webApi = "_api/web/lists")
            {



                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;

                HttpWebResponse endpointResponse = (HttpWebResponse)endpointRequest.GetResponse();
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }


                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + webApi + ((function != "" ? '/' + function : function)) + (query != "" ? '/' + query : ""));
                requestCreateNew.Method = "POST";
                requestCreateNew.Accept = "application/json;odata=verbose";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;
                requestCreateNew.Headers.Add("X-RequestDigest", FormDigestValue);
                requestCreateNew.ContentLength = _jsonLength;
                Stream newStream = requestCreateNew.GetRequestStream();
                newStream.Write(_bytes, 0, _bytes.Length);
                newStream.Close();
                requestCreateNew.Timeout = 32000;



                return requestCreateNew;
            }
            /// <summary>
            /// Actualiza
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="_bytes">contenido del objeto</param>
            /// <param name="_jsonLength">tamaño del paquete</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>HttpWebRequest</returns>
            internal HttpWebRequest Update(string url, byte[] _bytes, int _jsonLength, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose", string webApi = "_api/web/lists")
            {



                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;

                HttpWebResponse endpointResponse = (HttpWebResponse)endpointRequest.GetResponse();
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }

                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + webApi + ((function != "" ? '/' + function : function)) + (query != "" ? '/' + query : ""));
                requestCreateNew.Method = "POST";
                requestCreateNew.Accept = "application/json;odata=verbose";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;
                requestCreateNew.Headers.Add("X-RequestDigest", FormDigestValue);
                requestCreateNew.Headers.Add("X-HTTP-Method", "MERGE");
                requestCreateNew.Headers.Add("IF-MATCH", "*");
                requestCreateNew.ContentLength = _jsonLength;
                Stream newStream = requestCreateNew.GetRequestStream();
                newStream.Write(_bytes, 0, _bytes.Length);
                newStream.Close();
                requestCreateNew.Timeout = 32000;



                return requestCreateNew;
            }

            /// <summary>
            /// Elimina 
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>HttpWebRequest</returns>
            internal HttpWebRequest Delete(string url, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose", string webApi = "_api/web/lists")
            {



                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;

                HttpWebResponse endpointResponse = (HttpWebResponse)endpointRequest.GetResponse();
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }

                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + webApi + ((function != "" ? '/' + function : function)) + (query != "" ? '/' + query : ""));
                requestCreateNew.Method = "POST";
                requestCreateNew.Accept = "application/json;odata=verbose";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;
                requestCreateNew.Headers.Add("X-RequestDigest", FormDigestValue);
                requestCreateNew.Headers.Add("X-HTTP-Method", "DELETE");
                requestCreateNew.Headers.Add("IF-MATCH", "*");




                return requestCreateNew;
            }


            /// <summary>
            /// Guarda archivo
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="_bytes">contenido del objeto</param>
            /// <param name="length">tamaño del paquete</param>
            /// <param name="solicitudId">Id de la solicitud</param>
            /// <param name="tipoDocumento">tipo de documento</param>
            /// <param name="nombre">nombre</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>string</returns>
            internal string SaveFile(string url, byte[] _bytes, int length, int solicitudId, string tipoDocumento, string nombre, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose", string webApi = "_api/web")
            {

                //request para digest value
                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }


                //request para subir archivo
                string url_Values = "";
                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + webApi + ((function != "" ? '/' + function : function)) + (query != "" ? '/' + query : ""));
                requestCreateNew.Method = "POST";
                requestCreateNew.Accept = "application/json;odata=verbose";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;

                requestCreateNew.Headers.Add("Authorization", "'Bearer ' + " + FormDigestValue);
                requestCreateNew.Headers.Add("X-RequestDigest", FormDigestValue);
                requestCreateNew.ContentLength = length;
                Stream newStream = requestCreateNew.GetRequestStream();

                newStream.Write(_bytes, 0, _bytes.Length);
                newStream.Close();
                newStream.Dispose();

                try
                {
                    WebResponse webResponse = requestCreateNew.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    dynamic stuff = JObject.Parse(response); ;
                    url_Values = stuff.d.ListItemAllFields.__deferred.uri;

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }

                //request para obtener ID de file

                HttpWebRequest requestValuesCheck = (HttpWebRequest)WebRequest.Create(url_Values);
                requestValuesCheck.Method = "GET";
                requestValuesCheck.Accept = "application/json;odata=verbose";
                requestValuesCheck.ContentType = "application/json;odata=verbose";
                requestValuesCheck.Credentials = cred;



                string ItemId = "";

                try
                {
                    WebResponse webResponse = requestValuesCheck.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    dynamic stuff = JObject.Parse(response);
                    ItemId = stuff.d.ID;


                    responseReader.Close();
                }
                catch (Exception e)
                {

                }




                return ItemId;
            }

            /// <summary>
            /// elimina archivo
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>HttpWebRequest</returns>
            internal HttpWebRequest DeleteFile(string url, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose")
            {


                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;


                HttpWebResponse endpointResponse = (HttpWebResponse)endpointRequest.GetResponse();
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }

                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/web" + ((function != "" ? '/' + function : function)) + (query != "" ? query : ""));
                requestCreateNew.Method = "POST";
                requestCreateNew.Accept = "application/json;odata=verbose";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;
                requestCreateNew.Headers.Add("X-RequestDigest", FormDigestValue);
                requestCreateNew.Headers.Add("X-HTTP-Method", "DELETE");
                requestCreateNew.Headers.Add("IF-MATCH", "*");
                requestCreateNew.ContentLength = 0;

                return requestCreateNew;
            }

            /// <summary>
            /// elimina un objeto
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <returns>HttpWebRequest</returns>
            internal HttpWebRequest DeleteItem(string url, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose")
            {
                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;


                HttpWebResponse endpointResponse = (HttpWebResponse)endpointRequest.GetResponse();
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }

                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/web/lists" + ((function != "" ? '/' + function : function)) + (query != "" ? '/' + query : ""));
                requestCreateNew.Method = "POST";
                requestCreateNew.Accept = "application/json;odata=verbose";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;
                requestCreateNew.Headers.Add("X-RequestDigest", FormDigestValue);
                requestCreateNew.Headers.Add("X-HTTP-Method", "DELETE");
                requestCreateNew.Headers.Add("IF-MATCH", "*");
                requestCreateNew.ContentLength = 0;

                return requestCreateNew;


            }


            /// <summary>
            /// Check in a los archivos
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="nombre">nombre del archivo</param>
            /// <param name="solicitudId">Id de la solicitud</param>
            /// <param name="ItemId">Id del archivo</param>
            /// <param name="tipoDocumento">tipo de documento</param>
            /// <param name="bodyFields">campos obligatorios</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>HttpWebRequest</returns>
            public bool CheckInFile(string url, string nombre, int solicitudId, string listName, string ItemId, string tipoDocumento, byte[] bodyFields, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose", string webApi = "_api/web")
            {
                Encoding encoding = Encoding.Default;
                //request para digest value
                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;

                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                    throw e;
                }


                //Request para setear campos obligatorios de archivos
                //HttpWebRequest requestSetValues = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + webApi + "/Lists/getByTitle('Documentos de Proyecto')/Items(" + ItemId + ")");
                HttpWebRequest requestSetValues = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + webApi + "/Lists/getByTitle('" + listName + "')/Items(" + ItemId + ")");
                requestSetValues.Method = "POST";
                requestSetValues.Accept = "application/json;odata=verbose";
                requestSetValues.ContentType = "application/json;odata=verbose";
                requestSetValues.Credentials = cred;
                requestSetValues.Headers.Add("X-RequestDigest", FormDigestValue);
                requestSetValues.Headers.Add("X-HTTP-Method", "MERGE");
                requestSetValues.Headers.Add("IF-MATCH", "*");


                requestSetValues.ContentLength = bodyFields.Length;
                Stream newStreamValues = requestSetValues.GetRequestStream();
                newStreamValues.Write(bodyFields, 0, bodyFields.Length);
                newStreamValues.Close();
                newStreamValues.Dispose();

                try
                {
                    WebResponse webResponse = requestSetValues.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();

                }
                catch (Exception e)
                {
                    throw e;
                }



                //REquest para Check in file

                HttpWebRequest requestCheckIn = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + webApi + "/Lists" + string.Format("/getByTitle('" + listName + "')/Items('{0}')/File/CheckIn(comment='Comment',checkintype=0)", ItemId));
                requestCheckIn.Method = "POST";
                requestCheckIn.Accept = "application/json;odata=verbose";
                requestCheckIn.ContentType = "application/json;odata=verbose";
                requestCheckIn.Credentials = cred;
                requestCheckIn.Headers.Add("X-RequestDigest", FormDigestValue);
                requestCheckIn.ContentLength = 0;
                requestCheckIn.Headers.Add("IF-MATCH", "*");
                try
                {

                    WebResponse webResponse = requestCheckIn.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                    return false;

                }

                return true;
            }

            /// <summary>
            /// CRea el salt para encriptar el password
            /// </summary>
            /// <param name="size">tamaño</param>
            /// <returns>string</returns>
            public static string CreateSalt(int size = 12)
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    var buff = new byte[size];
                    rng.GetBytes(buff);
                    return Convert.ToBase64String(buff);
                }
            }
            /// <summary>
            /// DEvuelve bytes de una cadena
            /// </summary>
            /// <param name="str">palabra parsear</param>
            /// <returns>byte[]</returns>
            public static byte[] GetBytes(string str)
            {
                byte[] bytes = new byte[str.Length * sizeof(char)];
                System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
                return bytes;
            }
            /// <summary>
            /// REaliza el hash
            /// </summary>
            /// <param name="value">cadena</param>
            /// <param name="salt">salt</param>
            /// <returns>byte[]</returns>
            public static byte[] Hash(string value, byte[] salt)
            {
                return Hash(Encoding.UTF8.GetBytes(value), salt);
            }
            /// <summary>
            /// REaliza el hash
            /// </summary>
            /// <param name="value">cadena</param>
            /// <param name="salt">salt</param>
            /// <returns>byte[]</returns>
            public static byte[] Hash(byte[] value, byte[] salt)
            {
                byte[] saltedValue = value.Concat(salt).ToArray();


                return new SHA256Managed().ComputeHash(saltedValue);
            }


            /// <summary>
            /// devuelve lista de elementos
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="id">Id de ITem</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>HttpWebRequest</returns>
            internal HttpWebRequest GetAllFilesById(string url, int id, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose")
            {


                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;


                HttpWebResponse endpointResponse = (HttpWebResponse)endpointRequest.GetResponse();
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }

                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/web/lists" + ((function != "" ? '/' + function : function)) + (query != "" ? "/" + query : ""));
                requestCreateNew.Method = "GET";
                requestCreateNew.Accept = "application/json;odata=verbose";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;


                return requestCreateNew;
            }

            /// <summary>
            /// devuelve lista de elementos
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="id">Id de ITem</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>HttpWebRequest</returns>
            internal HttpWebRequest GetAllFilesEvaluador(string url, int id, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose")
            {


                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;


                HttpWebResponse endpointResponse = (HttpWebResponse)endpointRequest.GetResponse();
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }

                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/web" + ((function != "" ? '/' + function : function)) + (query != "" ? "/" + query : ""));
                requestCreateNew.Method = "GET";
                requestCreateNew.Accept = "application/json;odata=verbose";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;


                return requestCreateNew;
            }

            /// <summary>
            /// devuelve el archivo a descargar
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>HttpWebRequest</returns>
            internal HttpWebRequest DownloadFileByName(string url, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose")
            {


                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;


                HttpWebResponse endpointResponse = (HttpWebResponse)endpointRequest.GetResponse();
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }

                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/web" + ((function != "" ? '/' + function : function)) + (query != "" ? "/" + query : ""));
                requestCreateNew.Method = "GET";
                requestCreateNew.Accept = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;
                requestCreateNew.Headers.Add("Authorization", "'Bearer ' + " + FormDigestValue);

                return requestCreateNew;
            }



            /// <summary>
            /// Guarda archivo
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="_bytes">contenido del objeto</param>
            /// <param name="length">tamaño del paquete</param>
            /// <param name="solicitudId">Id de la solicitud</param>
            /// <param name="tipoDocumento">tipo de documento</param>
            /// <param name="nombre">nombre</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            /// <returns>string</returns>
            internal string SaveFileAttachedOnList(string url, byte[] _bytes, int length, int solicitudId, string tipoDocumento, string nombre, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose", string webApi = "_api/web")
            {

                //request para digest value
                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }

                string ItemId = "";
                //request para subir archivo
                string url_Values = "";
                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + webApi + ((function != "" ? '/' + function : function)) + (query != "" ? '/' + query : ""));
                requestCreateNew.Method = "POST";
                requestCreateNew.Accept = "application/json;odata=verbose";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;

                requestCreateNew.Headers.Add("Authorization", "'Bearer ' + " + FormDigestValue);
                requestCreateNew.Headers.Add("X-RequestDigest", FormDigestValue);
                requestCreateNew.ContentLength = length;
                Stream newStream = requestCreateNew.GetRequestStream();

                newStream.Write(_bytes, 0, _bytes.Length);
                newStream.Close();
                newStream.Dispose();

                try
                {
                    WebResponse webResponse = requestCreateNew.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    dynamic stuff = JObject.Parse(response); ;
                    ItemId = url_Values = stuff.d.__metadata.uri;

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }









                return ItemId;
            }

            /// <summary>
            /// Guarda archivo
            /// </summary>
            /// <param name="url">string complemento para la url</param>
            /// <param name="_bytes">contenido del objeto</param>
            /// <param name="length">tamaño del paquete</param>
            /// <param name="solicitudId">Id de la solicitud</param>
            /// <param name="tipoDocumento">tipo de documento</param>
            /// <param name="nombre">nombre</param>
            /// <param name="function">funcion REST</param>
            /// <param name="query">query Rest</param>
            /// <param name="method">Tipo de Peticion</param>
            /// <param name="contenType"> tipo dontenido</param>
            /// <param name="accept">lo que acepta</param>
            /// <param name="webApi">cadena web api</param>
            internal void DeleteFileAttachedOnList(string url, string solicitudId, string nombre, string function = "", string query = "", string method = "POST", string contenType = "application/json;odata=verbose", string accept = "application/json;odata=verbose", string webApi = "_api/web")
            {

                //request para digest value
                HttpWebRequest endpointRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + "_api/contextinfo");

                endpointRequest.Method = "POST";
                string FormDigestValue = string.Empty;
                endpointRequest.Accept = "application/json;odata=verbose";
                NetworkCredential cred = _credentials;
                endpointRequest.Credentials = cred;
                endpointRequest.ContentLength = 0;
                try
                {
                    WebResponse webResponse = endpointRequest.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    var t = response.Substring(response.IndexOf("FormDigestValue") + 18);
                    FormDigestValue = t.Substring(0, t.IndexOf("\""));

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }


                //request para subir archivo

                HttpWebRequest requestCreateNew = (HttpWebRequest)WebRequest.Create(_baseUrl + (url == "" ? "" : url + "/") + webApi + ((function != "" ? '/' + function : function)) + (query != "" ? '/' + query : ""));
                requestCreateNew.Method = "POST";
                requestCreateNew.Accept = "application/json;odata=verbose";
                requestCreateNew.ContentType = "application/json;odata=verbose";
                requestCreateNew.Credentials = cred;
                requestCreateNew.Headers.Add("X-HTTP-Method", "DELETE");
                requestCreateNew.Headers.Add("Authorization", "'Bearer ' + " + FormDigestValue);
                requestCreateNew.Headers.Add("X-RequestDigest", FormDigestValue);
                requestCreateNew.Headers.Add("IF-MATCH", "*");
                requestCreateNew.ContentLength = 0;




                try
                {
                    WebResponse webResponse = requestCreateNew.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                    //dynamic stuff = JObject.Parse(response); ;
                    //ItemId = url_Values = stuff.d.__metadata.uri;

                    responseReader.Close();
                }
                catch (Exception e)
                {

                }










            }
        }
    }
