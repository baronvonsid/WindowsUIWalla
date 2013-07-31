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

namespace ManageWalla
{
    public class ServerHelper
    {
        private const String baseUri = "http://192.168.0.4:8080/WallaWS/v1/user/simo1n/";
        //private const String baseUri = "http://localhost:8081/WallaWS/v1/user/simo1n/";
        private HttpClient http = null;
        private GlobalState state = null;
        private static readonly ILog logger = LogManager.GetLogger(typeof(ServerHelper));

        public ServerHelper(GlobalState value)
        {
            state = value;
            http = new HttpClient();
            http.BaseAddress = new Uri(baseUri);
        }

        async public Task<TagList> GetTagsAvailableAsync()
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "tags");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                if (state.tagList != null)
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(state.tagList.LastChanged);
                }

                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(TagList));
                    TagList tagList = (TagList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                    return tagList;
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        public string UpdateTag(Tag newTag, string oldTagName)
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
                HttpResponseMessage response = http.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();

                return "";
            }
            catch (Exception ex)
            {
                //TODO Log failure.
                return "Tag could not be updated, there was an error on the server:" + ex.Message;
            }
        }

        public string SaveNewTag(Tag tag)
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
                HttpResponseMessage response = http.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();

                return "";
            }
            catch (Exception ex)
            {
                //TODO Log failure.
                return ex.Message;
            }
        }

        public Tag GetTagMeta(TagListTagRef tagRef)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "tag/" + Uri.EscapeUriString(tagRef.name));
            request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

            HttpResponseMessage response = http.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            XmlSerializer serialKiller = new XmlSerializer(typeof(Tag));
            Tag tag = (Tag)serialKiller.Deserialize(response.Content.ReadAsStreamAsync().Result);

            return tag;
        }

        public string DeleteTag(Tag tag)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "tag/" + Uri.EscapeUriString(tag.Name));
            request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

            XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter();
            xmlFormatter.UseXmlSerializer = true;
            HttpContent content = new ObjectContent<Tag>(tag, xmlFormatter);
            request.Content = content;
            HttpResponseMessage response = http.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            state.tagList = null;

            return "";
        }

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

                System.Threading.Thread.Sleep(1000);

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

        async public Task<string> GetUploadStatusListAsync()
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "image/uploadstatus");
                request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));

                HttpResponseMessage response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();

                /*
                XmlSerializer serialKiller = new XmlSerializer(typeof(UploadStatusList));
                UploadStatusList uploadStatusList = (UploadStatusList)serialKiller.Deserialize(await response.Content.ReadAsStreamAsync());
                return uploadStatusList;
                 */ 
            }
            catch (HttpRequestException httpEx)
            {
                logger.Error(httpEx);
                throw httpEx;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        public long CreateCategory(string categoryName, string categoryDesc, long parentCategoryId)
        {
            //TODO
            return 10;
        }
    }
}
