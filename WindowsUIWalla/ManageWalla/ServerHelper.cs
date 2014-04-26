using System;
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
        private long userId;
        private string webPath;
        private string sessionKey;

        public ServerHelper(string hostNameParam, int portParam, string wsPathParam, string appKeyParam, string webPathParam)
        {
            hostName = hostNameParam;
            port = portParam;
            wsPath = wsPathParam;
            appKey = appKeyParam;
            webPath = webPathParam;
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

        async public Task<bool> Logon(string userName, string passwordParam)
        {
            try
            {
                HttpClient initialHttp = new HttpClient();
                
                initialHttp.BaseAddress = new Uri("http://" + hostName + ":" + port.ToString() + wsPath);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,"logon?userName=" + userName);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await initialHttp.SendAsync(request);
                response.EnsureSuccessStatusCode();

                http = new HttpClient();
                http.BaseAddress = new Uri("http://" + hostName + ":" + port.ToString() + wsPath + userName + "/");

                //TODO get session returned to Walla for use in all subsequent requests.

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        async public Task<Account> AccountGet(CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlSerializer serialKiller = new XmlSerializer(typeof(Account));
                Account account = (Account)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                return account;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("AccountGet has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        public string GetWebUrl()
        {
            return "http://" + hostName + ":" + port.ToString() + webPath + userId.ToString() + "/";
        }

        async public Task<bool> VerifyApp(string validation)
        {
            try
            {
                HttpClient initialHttp = new HttpClient();
                initialHttp.BaseAddress = new Uri("http://" + hostName + ":" + port.ToString() + wsPath);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "appcheck?wsKey=" + validation);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await initialHttp.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        async public Task<bool> VerifyPlatform(string os, string machineType, int majorVersion, int minorVersion)
        {
            try
            {

                HttpClient initialHttp = new HttpClient();
                initialHttp.BaseAddress = new Uri("http://" + hostName + ":" + port.ToString() + wsPath);

                // POST /platform?OS={OS}&machine={machine}&major={major}&minor={minor}
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,
                    "platform?OS=" + os + "&machineType=" + machineType +
                    "&major=" + majorVersion.ToString() + "&minor=" + minorVersion.ToString());
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await initialHttp.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        async public Task<UserApp> UserAppGet(long userAppId)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "userapp/" + userAppId.ToString());
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request);
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
        }

        async public Task<long> UserAppCreateUpdateAsync(UserApp userApp, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "userapp");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<UserApp>(userApp, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();
                return reader.ReadElementContentAsLong();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("UserAppCreateUpdateAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
        #endregion

        #region Tag
        async public Task<TagList> TagGetListAsync(DateTime? lastModified, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "tags");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                if (lastModified.HasValue)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified.Value);
                }

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(TagList));
                    TagList tagList = (TagList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                    cancelToken.ThrowIfCancellationRequested();

                    return tagList;
                }
                else if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    response.EnsureSuccessStatusCode();
                }
                return null;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagGetListAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task TagUpdateAsync(Tag newTag, string oldTagName, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "tag/" + Uri.EscapeUriString(oldTagName));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Tag>(newTag, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagUpdateAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task TagCreateAsync(Tag tag, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "tag/" + Uri.EscapeUriString(tag.Name));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Tag>(tag, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagCreateAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<Tag> TagGetMeta(string tagName, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "tag/" + Uri.EscapeUriString(tagName));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlSerializer serialKiller = new XmlSerializer(typeof(Tag));
                Tag tag = (Tag)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                cancelToken.ThrowIfCancellationRequested();

                return tag;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagGetMeta has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task TagDeleteAsync(Tag tag, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "tag/" + Uri.EscapeUriString(tag.Name));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Tag>(tag, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagDeleteAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task TagAddRemoveImagesAsync(string tagName, ImageIdList imagesToMove, bool add, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = null;
                string url =  "tag/" + Uri.EscapeUriString(tagName) + "/images";
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
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagAddRemoveImagesAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
        #endregion

        #region Gallery
        async public Task<GalleryList> GalleryGetListAsync(DateTime? lastModified, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "galleries");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                if (lastModified.HasValue)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified.Value);
                }

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(GalleryList));
                    GalleryList galleryList = (GalleryList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                    return galleryList;
                }
                else if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    response.EnsureSuccessStatusCode();
                }
                return null;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryGetListAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task GalleryUpdateAsync(Gallery gallery, string oldGalleryName, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "gallery/" + Uri.EscapeUriString(oldGalleryName));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Gallery>(gallery, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryUpdateAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task GalleryCreateAsync(Gallery gallery, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "gallery/" + Uri.EscapeUriString(gallery.Name));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Gallery>(gallery, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryCreateAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<Gallery> GalleryGetMeta(string galleryName, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "gallery/" + Uri.EscapeUriString(galleryName));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlSerializer serialKiller = new XmlSerializer(typeof(Gallery));
                Gallery gallery = (Gallery)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                return gallery;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryGetMeta has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task GalleryDeleteAsync(Gallery gallery, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "gallery/" + Uri.EscapeUriString(gallery.Name));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Gallery>(gallery, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException)
            {
                logger.Debug("GalleryDeleteAsync has been cancelled.");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
        #endregion

        #region Upload
        async public Task<string> UploadImageAsync(UploadImage image, UploadImageState newUploadEntry, CancellationToken cancelToken)
        {
            try
            {
                //Preparing + image.Meta.Name

                //Initial Request setup
                HttpRequestMessage requestImage = new HttpRequestMessage(HttpMethod.Post, "image");
                requestImage.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                //Associate file to upload.
                FileStream fileStream = new FileStream(image.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                
                StreamContent streamContent = new StreamContent(fileStream);
                requestImage.Content = streamContent;
                streamContent.Headers.ContentLength = fileStream.Length;
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                //Upload file asynchronously and check response.
                HttpResponseMessage response = await http.SendAsync(requestImage, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();
                long imageId = reader.ReadElementContentAsLong();
                image.Meta.id = imageId;

                newUploadEntry.lastUpdated = DateTime.Now;
                newUploadEntry.imageId = imageId;
                newUploadEntry.status = UploadImage.ImageStatus.FileReceived;
                image.Meta.Status = 1;

                HttpRequestMessage requestMeta = new HttpRequestMessage(HttpMethod.Put, "image/" + imageId.ToString() + "/meta");
                requestMeta.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;

                //Upload info + image.Meta.Name

                HttpContent content = new ObjectContent<ImageMeta>(image.Meta, xmlFormatter);
                requestMeta.Content = content;
                HttpResponseMessage responseMeta = await http.SendAsync(requestMeta, cancelToken);
                responseMeta.EnsureSuccessStatusCode();



                //System.Threading.Thread.Sleep(1000);

                //Uploaded + image.Meta.Name
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                logger.Error(httpEx);
                return httpEx.Message;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("UploadImageAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        async public Task<UploadStatusList> UploadGetStatusListAsync(ImageIdList orderIdList, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "image/uploadstatus");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageIdList>(orderIdList, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(UploadStatusList));
                    UploadStatusList uploadStatusList = (UploadStatusList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                    return uploadStatusList;
                }
                return null;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("UploadGetStatusListAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
        #endregion

        #region Category
        async public Task<long> CategoryCreateAsync(Category category, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "category");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Category>(category, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();

                return reader.ReadElementContentAsLong();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryCreateAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<CategoryList> CategoryGetListAsync(DateTime? lastModified, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "categories");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                if (lastModified.HasValue)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified.Value);
                }

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(CategoryList));
                    CategoryList categoryList = (CategoryList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                    return categoryList;
                }
                else if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    response.EnsureSuccessStatusCode();
                }

                return null;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryGetListAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task CategoryUpdateAsync(Category category, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "category/" + Uri.EscapeUriString(category.id.ToString()));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Category>(category, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryUpdateAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<Category> CategoryGetMeta(long categoryId, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "category/" + Uri.EscapeUriString(categoryId.ToString()));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlSerializer serialKiller = new XmlSerializer(typeof(Category));
                Category category = (Category)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                return category;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryGetMeta has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task CategoryDeleteAsync(Category category, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "category/" + Uri.EscapeUriString(category.id.ToString()));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Category>(category, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryDeleteAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<string> CategoryMoveImagesAsync(long categoryId, ImageIdList imagesToMove, CancellationToken cancelToken)
        {
            try
            {
                string url = "category/" + Uri.EscapeUriString(categoryId.ToString()) + "/images";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url);

                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageIdList>(imagesToMove, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryMoveImagesAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
        #endregion

        #region Images
        async public Task<Byte[]> GetByteArray(string requestUrl, CancellationToken cancelToken)
        {
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
                logger.Debug("GetByteArray has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }
        }
 
        async public Task<ImageList> GetImageListAsync(string type, string id, DateTime? lastModified, int cursor, int size, string searchQueryString, long sectionId, CancellationToken cancelToken)
        {
            try
            {
                /* GET /{userName}/{type}/{identity}/{imageCursor}/{size} */

                string requestUrl = type + "/" + id + "/" + cursor.ToString() + "/" + size.ToString(); // +"?" + searchQueryString ?? "";

                if (sectionId > 0)
                    requestUrl = requestUrl + "?sectionId=" + sectionId.ToString();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                if (lastModified.HasValue)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified.Value);
                }

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(ImageList));
                    ImageList categoryImageList = (ImageList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                    return categoryImageList;
                }
                else if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    response.EnsureSuccessStatusCode();
                }
                return null;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GetImageListAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task DeleteImagesAsync(ImageList imageList, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "images");

                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageList>(imageList, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("DeleteImagesAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<ImageMeta> ImageGetMeta(long imageId, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "image/" + imageId.ToString() + "/meta");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlSerializer serialKiller = new XmlSerializer(typeof(ImageMeta));
                ImageMeta imageMeta = (ImageMeta)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                return imageMeta;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("ImageGetMeta has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task ImageUpdateMetaAsync(ImageMeta imageMeta, CancellationToken cancelToken)
        {
            try
            {
                HttpRequestMessage requestMeta = new HttpRequestMessage(HttpMethod.Put, "image/" + imageMeta.id.ToString() + "/meta");
                requestMeta.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageMeta>(imageMeta, xmlFormatter);
                requestMeta.Content = content;
                HttpResponseMessage response = await http.SendAsync(requestMeta, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("ImageUpdateMetaAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
        
        #endregion
    }
}
