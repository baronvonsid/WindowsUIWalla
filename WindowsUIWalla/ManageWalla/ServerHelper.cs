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

        async public Task<string> Logon(string emailParam, string passwordParam)
        {

            //Logon sends back userId.
            userId = 100001;

            if (http == null)
            {
                http = new HttpClient();
                http.BaseAddress = new Uri("http://" + hostName + ":" + port.ToString() + wsPath + userId.ToString() + "/");
            }

            //Do logon
            //TODO - Logon and send application key.

            //Log failed login as a warning.

            return "OK";
        }

        //public long GetUserId()
        //{
        //    return userId;
        //}


        async public Task<Account> AccountGet(CancellationToken cancelToken)
        {
            try
            {
                Account account = new Account();
                account.id = 100001;
                account.ProfileName = "ProfileName";
                account.Machines = new AccountMachine[1];
                account.Machines[0] = new AccountMachine();
                account.Machines[0].id = 500001;
                account.Machines[0].platformId = 200;
                account.Machines[0].name = System.Environment.MachineName;
                return account;

                /*
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlSerializer serialKiller = new XmlSerializer(typeof(Account));
                Account account = (Account)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                return account;
                */

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

        async public Task MachineMarkSession(long machineId, CancellationToken cancelToken)
        {
            try
            {
                return;

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "machine/" + machineId.ToString());
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("MarkMachineSession has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

        }

        async public Task<long> MachineRegisterNew(string machineName, int platformId, CancellationToken cancelToken)
        {
            try
            {
                return 0;

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "machine/" + platformId.ToString() + "/" + Uri.EscapeUriString(machineName));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request, cancelToken);
                response.EnsureSuccessStatusCode();

                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();
                long machineId = reader.ReadElementContentAsLong();

                return machineId;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GetByteArray has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return 0;
            }
        }


        #endregion

        #region Tag
        async public Task<TagList> TagGetListAsync(DateTime? lastModified)
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

                HttpResponseMessage response = await http.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(TagList));
                    TagList tagList = (TagList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                    return tagList;
                }
                else if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    response.EnsureSuccessStatusCode();
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<string> TagUpdateAsync(Tag newTag, string oldTagName)
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
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        async public Task<string> TagCreateAsync(Tag tag)
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
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        async public Task<Tag> TagGetMeta(string tagName)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "tag/" + Uri.EscapeUriString(tagName));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                XmlSerializer serialKiller = new XmlSerializer(typeof(Tag));
                Tag tag = (Tag)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                return tag;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<string> TagDeleteAsync(Tag tag)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "tag/" + Uri.EscapeUriString(tag.Name));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Tag>(tag, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        /*
        async public Task<ImageList> DeleteMeTagGetImageListAsync(string tagName, bool useDate, DateTime lastModified, int cursor, int size, string searchQueryString)
        {
            try
            {
                string requestUrl = "tag/" + tagName + "/" + cursor.ToString() + "/" + size.ToString() + "?" + searchQueryString ?? "";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                if (useDate)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified);
                }

                HttpResponseMessage response = await http.SendAsync(request);
                

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(ImageList));
                    ImageList tagImageList = (ImageList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                    return tagImageList;
                }
                else if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    response.EnsureSuccessStatusCode();
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
*/

        async public Task<string> TagAddRemoveImagesAsync(string tagName, ImageMoveList imagesToMove, bool add)
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
                HttpContent content = new ObjectContent<ImageMoveList>(imagesToMove, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }
        #endregion

        #region Gallery
        async public Task<GalleryList> GalleryGetListAsync(DateTime? lastModified)
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

                HttpResponseMessage response = await http.SendAsync(request);

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
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<string> GalleryUpdateAsync(Gallery gallery, string oldGalleryName)
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
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        async public Task<string> GalleryCreateAsync(Gallery gallery)
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
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        async public Task<Gallery> GalleryGetMeta(string galleryName)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "gallery/" + Uri.EscapeUriString(galleryName));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                XmlSerializer serialKiller = new XmlSerializer(typeof(Gallery));
                Gallery gallery = (Gallery)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                return gallery;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<string> GalleryDeleteAsync(Gallery gallery)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "gallery/" + Uri.EscapeUriString(gallery.Name));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Gallery>(gallery, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        /*
        async public Task<ImageList> DeleteMeGalleryGetImageListAsync(string galleryName, bool useDate, DateTime lastModified, int cursor, int size, string searchQueryString)
        {
            try
            {
                string requestUrl = "gallery/" + galleryName + "/" + cursor.ToString() + "/" + size.ToString() + "?" + searchQueryString ?? "";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                if (useDate)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified);
                }

                HttpResponseMessage response = await http.SendAsync(request);


                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(ImageList));
                    ImageList tagImageList = (ImageList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                    return tagImageList;
                }
                else if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    response.EnsureSuccessStatusCode();
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
        */
        #endregion

        #region Upload
        async public Task<string> UploadImageAsync(UploadImage image)
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
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(image.HttpFormat);

                //Upload file + image.Meta.Name
                
                //Upload file asynchronously and check response.
                HttpResponseMessage response = await http.SendAsync(requestImage);
                response.EnsureSuccessStatusCode();

                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();
                long imageId = reader.ReadElementContentAsLong();
                image.Meta.id = imageId;

                HttpRequestMessage requestMeta = new HttpRequestMessage(HttpMethod.Put, "image/" + imageId.ToString() + "/meta");
                requestMeta.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;

                //Upload info + image.Meta.Name

                HttpContent content = new ObjectContent<ImageMeta>(image.Meta, xmlFormatter);
                requestMeta.Content = content;
                HttpResponseMessage responseMeta = await http.SendAsync(requestMeta);
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
            catch (TaskCanceledException)
            {
                //rootPage.NotifyUser("Request canceled.", NotifyType.ErrorMessage);
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        async public Task<UploadStatusList> UploadGetStatusListAsync()
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "image/uploadstatus");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(UploadStatusList));
                    UploadStatusList uploadStatusList = (UploadStatusList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                    return uploadStatusList;
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
        #endregion

        #region Category
        async public Task<long> CategoryCreateAsync(Category category)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "category");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Category>(category, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                XmlReader reader = XmlReader.Create(response.Content.ReadAsStreamAsync().Result);
                reader.MoveToContent();
                long categoryId = reader.ReadElementContentAsLong();

                return categoryId;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<CategoryList> CategoryGetListAsync(DateTime? lastModified)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "categories");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                if (lastModified.HasValue)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified.Value);
                }

                HttpResponseMessage response = await http.SendAsync(request);

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
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<string> CategoryUpdateAsync(Category category)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "category/" + Uri.EscapeUriString(category.id.ToString()));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Category>(category, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        async public Task<Category> CategoryGetMeta(long categoryId)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "category/" + Uri.EscapeUriString(categoryId.ToString()));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                XmlSerializer serialKiller = new XmlSerializer(typeof(Category));
                Category category = (Category)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());

                return category;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<string> CategoryDeleteAsync(Category category)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "category/" + Uri.EscapeUriString(category.id.ToString()));
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<Category>(category, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        /*
        async public Task<ImageList> DeleteMeCategorGetImageListAsync(long categoryId, bool useDate, DateTime lastModified, int cursor, int size, string searchQueryString)
        {
            try
            {
                string requestUrl = "category/" + categoryId.ToString() + "/" + cursor.ToString() + "/" + size.ToString() + "?" + searchQueryString ?? "";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                if (useDate)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(lastModified);
                }

                HttpResponseMessage response = await http.SendAsync(request);

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
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
        */

        async public Task<string> CategoryMoveImagesAsync(long categoryId, ImageMoveList imagesToMove)
        {
            try
            {
                string url = "category/" + Uri.EscapeUriString(categoryId.ToString()) + "/images";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url);

                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageMoveList>(imagesToMove, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
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

        /*
        async public Task<BitmapImage> GetMainImage(long imageId, CancellationToken cancelToken)
        {
            try
            {
                string requestUrl = "image/" + imageId.ToString() + "/maincopy";
                //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                //request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                BitmapImage myBitmapImage = new BitmapImage();
                myBitmapImage.BeginInit();
                myBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                myBitmapImage.StreamSource = await http.GetStreamAsync(requestUrl);
                myBitmapImage.EndInit();
                //myBitmapImage.Freeze();

                cancelToken.ThrowIfCancellationRequested();

                return myBitmapImage;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GetMainImage has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }
        }

         */
 
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

        async public Task<string> DeleteImagesAsync(ImageList imageList)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "images");

                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
                xmlFormatter.UseXmlSerializer = true;
                HttpContent content = new ObjectContent<ImageList>(imageList, xmlFormatter);
                request.Content = content;
                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
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

        async public Task<string> ImageUpdateMetaAsync(ImageMeta imageMeta, CancellationToken cancelToken)
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

                return "OK";
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("ImageUpdateMetaAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }
        
        #endregion
    }
}
