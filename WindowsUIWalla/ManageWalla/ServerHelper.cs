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

namespace ManageWalla
{
    public class ServerHelper
    {
        
        private const String baseUri = "http://localhost:8081/WallaWS/v1/user/simo1n/";
        private HttpClient http = null;
        private GlobalState state = null;

        public ServerHelper(GlobalState value)
        {
            state = value;
            http = new HttpClient();
            http.BaseAddress = new Uri(baseUri);
        }

        public TagList GetTagsAvailable()
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

                HttpResponseMessage response = http.SendAsync(request).Result;
                if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    return state.tagList;
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlSerializer serialKiller = new XmlSerializer(typeof(TagList));
                    TagList tagList = (TagList)serialKiller.Deserialize(response.Content.ReadAsStreamAsync().Result);
                    state.tagList = tagList;
                    return tagList;
                }

                throw new Exception("/Tags web service returned an error code: " + response.StatusCode.ToString());
            }
            catch (Exception ex)
            {
                //TODO Log failure.
                Console.WriteLine(ex.Message);
                return null;
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

        public void UploadImage(ImageMeta image, string fullPath)
        {
            //Assert file to server, get Id


            //Send Image Meta to server with new Id.


        }

        public long CreateCategory(string categoryName, string categoryDesc, long parentCategoryId)
        {
            //TODO
            return 10;
        }
    }
}
