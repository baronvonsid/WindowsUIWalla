﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Net;
using System.Net.Mime;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using log4net;
using System.Threading;

namespace ManageWalla
{
    public class ServerHelper
    {
        #region Object setup and session management
        private HttpClient http = null;
        private static readonly ILog logger = LogManager.GetLogger(typeof(ServerHelper));
        private string hostName;
        private long port;
        private string wsPath;
        private string appKey;
        //private long userId;
        private string userName;
        private string webPath;
        private string password;
        private CookieContainer cookie;
        private HttpClientHandler handler;
        private DateTime lastReInitAttempt = DateTime.Now.AddMinutes(-1);
        private AppDetail appDetailCached = null;

        public ServerHelper(string hostNameParam, int portParam, string wsPathParam, string appKeyParam, string webPathParam)
        {
            hostName = hostNameParam;
            port = portParam;
            wsPath = wsPathParam;
            appKey = appKeyParam;
            webPath = webPathParam;
            cookie = new CookieContainer();
            handler = new HttpClientHandler();
            handler.CookieContainer = cookie;
        }

        async public Task<bool> isOnline(string webServerTest)
        {
            try
            {
                IPAddress[] addresslist = await Dns.GetHostAddressesAsync(webServerTest);
                if (addresslist[0].ToString().Length > 6)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        async public Task<Logon> GetLogonToken(AppDetail appDetail)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            Logon logon = null;
            try
            {
                http = new HttpClient(handler);
                http.BaseAddress = new Uri("http://" + hostName + ":" + port.ToString() + wsPath);
                
                url = "logon/token";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<AppDetail>(appDetail, xmlFormatter);
                request.Content = content;

                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                XmlSerializer serialKiller = new XmlSerializer(typeof(Logon));
                logon = (Logon)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                appDetailCached = appDetail;
                return logon;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.Logon()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<bool> Logon(Logon logon)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "logon";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Logon>(logon, xmlFormatter);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                request.Content = content;
                
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                handler = new HttpClientHandler();
                handler.CookieContainer = cookie;
                http = new HttpClient(handler);
                
                http.BaseAddress = new Uri("http://" + hostName + ":" + port.ToString() + wsPath + logon.ProfileName + "/");
                http.Timeout = new TimeSpan(0, 5, 0);

                this.userName = logon.ProfileName;
                this.password = logon.Password;
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.Logon()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<bool> Logout()
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                http = new HttpClient(handler);
                http.BaseAddress = new Uri("http://" + hostName + ":" + port.ToString() + wsPath);

                url = "logout";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.Logon()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<Account> AccountGet(CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await AccountGet(cancelToken);
                }

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(Account));
                Account account = (Account)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return account;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("AccountGet has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.AccountGet()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        public string GetWebUrl(bool withName)
        {
            string url = "http://" + hostName + ":" + port.ToString() + webPath;

            if (withName)
                url = url + userName + "/";

            return url;
        }

        /*
        async public Task<bool> VerifyAppAndPlatform(ClientApp clientApp, bool verify)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                HttpClient initialHttp = null; ;
                if (verify)
                {
                    initialHttp = new HttpClient();
                    initialHttp.BaseAddress = new Uri("http://" + hostName + ":" + port.ToString() + wsPath);
                }
                else
                {
                    initialHttp = http;
                }

                HttpRequestMessage request = new HttpRequestMessage((verify) ? HttpMethod.Post: HttpMethod.Put, "clientapp");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ClientApp>(clientApp, xmlFormatter);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                request.Content = content;

                HttpResponseMessage response = await initialHttp.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.VerifyApp()", (int)duration.TotalMilliseconds, ""); }
            }
        }
        */

        async public Task<UserApp> UserAppGet(long userAppId)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "userapp/" + userAppId.ToString();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(UserApp));
                UserApp userApp = (UserApp)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                
                return userApp;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.UserAppGet()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<long> UserAppCreateUpdateAsync(UserApp userApp, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "userapp";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<UserApp>(userApp, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();
                return reader.ReadElementContentAsLong();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("UserAppCreateUpdateAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.UserAppCreateUpdateAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        private HttpClient GetHttpProxyWithoutUser(HttpClientHandler handler)
        {
            HttpClient proxy = new HttpClient(handler);
            proxy.BaseAddress = new Uri("http://" + hostName + ":" + port.ToString() + wsPath);
            return proxy;
        }

        async public Task<string> AccountGetPassThroughTokenAsync(CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "logontoken/";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await AccountGetPassThroughTokenAsync(cancelToken);
                }

                response.EnsureSuccessStatusCode();
                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();
                return reader.ReadElementContentAsString();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) { logger.Debug("AccountGetPassThroughTokenAsync has been cancelled."); }
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.AccountGetPassThroughTokenAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async private Task<bool> SessionExpiredReInit()
        {
            //Don't allow continuous re-tries.
            if (lastReInitAttempt > DateTime.Now.AddMinutes(-1))
                return false;

            lastReInitAttempt = DateTime.Now;

            cookie = new CookieContainer();
            handler = new HttpClientHandler();
            handler.CookieContainer = cookie;

            Logon logon = await GetLogonToken(appDetailCached);
            if (logon == null || logon.Key.Length != 32)
                return false;

            logon.ProfileName = userName;
            logon.Password = password;

            return await Logon(logon);
        }
        #endregion

        #region Tag
        async public Task<TagList> TagGetListAsync(DateTime? lastModified, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "tags";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                if (lastModified.HasValue)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified.Value);
                }

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await TagGetListAsync(lastModified, cancelToken);
                }
                else if (response.StatusCode == HttpStatusCode.NotModified)
                    return null;

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(TagList));
                TagList tagList = (TagList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return tagList;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("TagGetListAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.TagGetListAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task TagUpdateAsync(Tag newTag, string oldTagName, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "tag/" + Uri.EscapeUriString(oldTagName);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Tag>(newTag, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await TagUpdateAsync(newTag, oldTagName, cancelToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("TagUpdateAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.TagUpdateAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task TagCreateAsync(Tag tag, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "tag/" + Uri.EscapeUriString(tag.Name);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Tag>(tag, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await TagCreateAsync(tag, cancelToken);
                }

                response.EnsureSuccessStatusCode();

            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("TagCreateAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.TagCreateAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<Tag> TagGetMeta(string tagName, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "tag/" + Uri.EscapeUriString(tagName);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await TagGetMeta(tagName, cancelToken);
                }
                    
                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(Tag));
                Tag tag = (Tag)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return tag;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("TagGetMeta has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.TagGetMeta()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task TagDeleteAsync(Tag tag, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "tag/" + Uri.EscapeUriString(tag.Name);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Tag>(tag, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await TagDeleteAsync(tag, cancelToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("TagDeleteAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.TagDeleteAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task TagAddRemoveImagesAsync(string tagName, ImageIdList imagesToMove, bool add, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                HttpRequestMessage request = null;
                url =  "tag/" + Uri.EscapeUriString(tagName) + "/images";
                if (add)
                {
                    request = new HttpRequestMessage(HttpMethod.Put, url);
                }
                else
                {
                    request = new HttpRequestMessage(HttpMethod.Delete, url);
                }
                
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageIdList>(imagesToMove, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await TagAddRemoveImagesAsync(tagName, imagesToMove, add, cancelToken);
                }

                response.EnsureSuccessStatusCode();

            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("TagAddRemoveImagesAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.TagAddRemoveImagesAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }
        #endregion

        #region Gallery
        async public Task<GalleryList> GalleryGetListAsync(DateTime? lastModified, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "galleries";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                if (lastModified.HasValue)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified.Value);
                }

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await GalleryGetListAsync(lastModified, cancelToken);
                }
                else if (response.StatusCode == HttpStatusCode.NotModified)
                    return null;

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(GalleryList));
                GalleryList galleryList = (GalleryList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return galleryList;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("GalleryGetListAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GalleryGetListAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task GalleryUpdateAsync(Gallery gallery, string oldGalleryName, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "gallery/" + Uri.EscapeUriString(oldGalleryName);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Gallery>(gallery, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await GalleryUpdateAsync(gallery, oldGalleryName, cancelToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("GalleryUpdateAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GalleryUpdateAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task GalleryCreateAsync(Gallery gallery, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "gallery/" + Uri.EscapeUriString(gallery.Name);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Gallery>(gallery, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await GalleryCreateAsync(gallery, cancelToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("GalleryCreateAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GalleryCreateAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<Gallery> GalleryGetMeta(string galleryName, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "gallery/" + Uri.EscapeUriString(galleryName);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await GalleryGetMeta(galleryName, cancelToken);
                }

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(Gallery));
                Gallery gallery = (Gallery)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return gallery;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("GalleryGetMeta has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GalleryGetMeta()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task GalleryDeleteAsync(Gallery gallery, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "gallery/" + Uri.EscapeUriString(gallery.Name);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Gallery>(gallery, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await GalleryDeleteAsync(gallery, cancelToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) {logger.Debug("GalleryDeleteAsync has been cancelled.");}
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GalleryDeleteAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<GalleryOption> GalleryGetOptionsAsync(DateTime? lastModified, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "gallery/galleryoption";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                if (lastModified.HasValue)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified.Value);
                }

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await GalleryGetOptionsAsync(lastModified, cancelToken);
                }
                else if (response.StatusCode == HttpStatusCode.NotModified)
                    return null;

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(GalleryOption));
                GalleryOption GalleryOption = (GalleryOption)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return GalleryOption;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("GalleryGetOptionsAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GalleryGetOptionsAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<Gallery> GalleryGetSections(Gallery gallery, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "gallery/gallerysections";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Gallery>(gallery, xmlFormatter);
                request.Content = content;

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await GalleryGetSections(gallery, cancelToken);
                }

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(Gallery));
                Gallery newGallery = (Gallery)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return newGallery;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("GalleryGetSections has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GalleryGetSections()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<string> GalleryCreatePreviewAsync(Gallery gallery, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "gallerypreview";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Gallery>(gallery, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await GalleryCreatePreviewAsync(gallery, cancelToken);
                }

                response.EnsureSuccessStatusCode();
                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();
                return reader.ReadElementContentAsString();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) { logger.Debug("GalleryCreatePreviewAsync has been cancelled."); }
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GalleryCreatePreviewAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<string> GalleryGetLogonTokenAsync(String galleryName, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "gallery/" + Uri.EscapeUriString(galleryName) + "/gallerylogon";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await GalleryGetLogonTokenAsync(galleryName, cancelToken);
                }

                response.EnsureSuccessStatusCode();
                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();
                return reader.ReadElementContentAsString();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) { logger.Debug("GalleryGetLogonTokenAsync has been cancelled."); }
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GalleryGetLogonTokenAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }
        #endregion

        #region Upload
        async public Task<string> UploadImageAsync(UploadImage image, UploadImageState newUploadEntry, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            HttpResponseMessage response = null;
            //FileStream fileStream = null;
            try
            {
                //Initial Request setup
                url = "image";
                HttpRequestMessage requestImage = new HttpRequestMessage(HttpMethod.Post, url);
                requestImage.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                requestImage.Headers.ExpectContinue = true;

                //Associate file to upload.
                using (FileStream fileStream = new FileStream(image.CompressedName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    StreamContent streamContent = new StreamContent(fileStream);
                    requestImage.Content = streamContent;
                    streamContent.Headers.ContentLength = fileStream.Length;
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    //Upload file asynchronously and check response.
                    response = await http.SendAsync(requestImage, cancelToken);
                    fileStream.Close();
                }

                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await UploadImageAsync(image, newUploadEntry, cancelToken);
                    else
                        response.EnsureSuccessStatusCode();
                }
                else
                {
                    response.EnsureSuccessStatusCode();

                    XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                    reader.MoveToContent();
                    long imageId = reader.ReadElementContentAsLong();
                    image.Meta.id = imageId;

                    newUploadEntry.lastUpdated = DateTime.Now;
                    newUploadEntry.imageId = imageId;
                    newUploadEntry.status = UploadImage.ImageStatus.FileReceived;
                    image.Meta.Status = 1;

                    url = "image/" + imageId.ToString() + "/meta";
                    HttpRequestMessage requestMeta = new HttpRequestMessage(HttpMethod.Put, url);
                    requestMeta.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                    XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                    xmlFormatter.UseXmlSerializer = true;

                    //Upload info + image.Meta.Name

                    HttpContent content = new ObjectContent<ImageMeta>(image.Meta, xmlFormatter);
                    requestMeta.Content = content;
                    HttpResponseMessage responseMeta = await http.SendAsync(requestMeta, cancelToken);
                    cancelToken.ThrowIfCancellationRequested();
                    responseMeta.EnsureSuccessStatusCode();
                }
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                logger.Error(httpEx);
                return httpEx.Message;
            }
            catch (OperationCanceledException ex)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    if (logger.IsDebugEnabled) { logger.Debug("UploadImageAsync has been cancelled."); }
                    throw ex;
                }
                else
                {
                    logger.Error(ex);
                    return "Upload cancelled, connection too slow. 5 minute timeout breached.";
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.UploadImageAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<UploadStatusList> UploadGetStatusListAsync(ImageIdList orderIdList, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "image/uploadstatus";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageIdList>(orderIdList, xmlFormatter);
                request.Content = content;

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await UploadGetStatusListAsync(orderIdList, cancelToken);
                }

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(UploadStatusList));
                UploadStatusList uploadStatusList = (UploadStatusList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return uploadStatusList;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("UploadGetStatusListAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.UploadGetStatusListAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }
        #endregion

        #region Category
        async public Task<long> CategoryCreateAsync(Category category, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "category";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Category>(category, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await CategoryCreateAsync(category, cancelToken);
                }

                response.EnsureSuccessStatusCode();
                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();
                return reader.ReadElementContentAsLong();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("CategoryCreateAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.CategoryCreateAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<CategoryList> CategoryGetListAsync(DateTime? lastModified, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "categories";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                if (lastModified.HasValue)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified.Value);
                }

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await CategoryGetListAsync(lastModified, cancelToken);
                }
                else if (response.StatusCode == HttpStatusCode.NotModified)
                    return null;

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(CategoryList));
                CategoryList categoryList = (CategoryList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return categoryList;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("CategoryGetListAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.CategoryGetListAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task CategoryUpdateAsync(Category category, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "category/" + Uri.EscapeUriString(category.id.ToString());
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Category>(category, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await CategoryUpdateAsync(category, cancelToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("CategoryUpdateAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.CategoryUpdateAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<Category> CategoryGetMeta(long categoryId, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "category/" + Uri.EscapeUriString(categoryId.ToString());
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await CategoryGetMeta(categoryId, cancelToken);
                }

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(Category));
                Category category = (Category)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return category;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("CategoryGetMeta has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.CategoryGetMeta()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task CategoryDeleteAsync(Category category, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "category/" + Uri.EscapeUriString(category.id.ToString());
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Category>(category, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await CategoryDeleteAsync(category, cancelToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("CategoryDeleteAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.CategoryDeleteAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<string> CategoryMoveImagesAsync(long categoryId, ImageIdList imagesToMove, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            String url = "";
            try
            {
                url = "category/" + Uri.EscapeUriString(categoryId.ToString()) + "/images";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url);

                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageIdList>(imagesToMove, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await CategoryMoveImagesAsync(categoryId, imagesToMove, cancelToken);
                }

                response.EnsureSuccessStatusCode();
                return "OK";
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("CategoryMoveImagesAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.CategoryMoveImagesAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }
        #endregion

        #region Images
        async public Task<Byte[]> GetByteArray(string requestUrl, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                MemoryStream memory;
                var stream = await http.GetStreamAsync(requestUrl);
                using (memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                }

                cancelToken.ThrowIfCancellationRequested();
                return memory.ToArray();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("GetByteArray has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GetByteArray()", (int)duration.TotalMilliseconds, requestUrl); }
            }
        }
 
        async public Task<ImageList> GetImageListAsync(string type, string id, DateTime? lastModified, int cursor, int size, string searchQueryString, long sectionId, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                /* GET /{userName}/{type}/{identity}/{imageCursor}/{size} */

                url = type + "/" + id + "/" + cursor.ToString() + "/" + size.ToString(); // +"?" + searchQueryString ?? "";

                if (sectionId > 0)
                    url = url + "?sectionId=" + sectionId.ToString();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                if (lastModified.HasValue)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified.Value);
                }

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await GetImageListAsync(type, id, lastModified, cursor, size, searchQueryString, sectionId, cancelToken);
                }
                else if (response.StatusCode == HttpStatusCode.NotModified)
                    return null;

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(ImageList));
                ImageList categoryImageList = (ImageList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return categoryImageList;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("GetImageListAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.GetImageListAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task DeleteImagesAsync(ImageIdList imageList, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "images";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "images");

                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageIdList>(imageList, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await DeleteImagesAsync(imageList, cancelToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("DeleteImagesAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.DeleteImagesAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task<ImageMeta> ImageGetMeta(long imageId, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "image/" + imageId.ToString() + "/meta";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        return await ImageGetMeta(imageId, cancelToken);
                }

                response.EnsureSuccessStatusCode();
                XmlSerializer serialKiller = new XmlSerializer(typeof(ImageMeta));
                ImageMeta imageMeta = (ImageMeta)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return imageMeta;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) { logger.Debug("ImageGetMeta has been cancelled."); }
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.ImageGetMeta()", (int)duration.TotalMilliseconds, url); }
            }
        }

        async public Task ImageUpdateMetaAsync(ImageMeta imageMeta, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            try
            {
                url = "image/" + imageMeta.id.ToString() + "/meta";
                HttpRequestMessage requestMeta = new HttpRequestMessage(HttpMethod.Put, url);
                requestMeta.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageMeta>(imageMeta, xmlFormatter);
                requestMeta.Content = content;
                HttpResponseMessage response = await http.SendAsync(requestMeta, HttpCompletionOption.ResponseContentRead, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await SessionExpiredReInit())
                        await ImageUpdateMetaAsync(imageMeta, cancelToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) {logger.Debug("ImageUpdateMetaAsync has been cancelled.");}
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "ServerHelper.ImageUpdateMetaAsync()", (int)duration.TotalMilliseconds, url); }
            }
        }
        
        #endregion
    }
}
