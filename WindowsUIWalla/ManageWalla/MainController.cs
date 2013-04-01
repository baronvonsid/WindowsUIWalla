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


namespace ManageWalla
{
    public class MainController
    {

        private const String baseUri = "http://localhost:8081/WallaWS/v1/user/simo1n/";
        private HttpClient http = null;
        private MainWindow currentMain;

        public MainController(MainWindow currentMainParam)
        {
            currentMain = currentMainParam;
            http = new HttpClient();
            http.BaseAddress = new Uri(baseUri);
            


        }


        /// <summary>
        /// For each entity - Category, Tag, View List, Account Settings
        /// Check local cache for entries and check Walla Hub for updates
        /// Then refresh local data caches.
        /// </summary>
        public void RetrieveGeneralUserConfig()
        {


            GetCategoryTree();
        }

        public void PopulateImagePane()
        {
            //Assume a list of images were passed in.
            //Retreive from local cache or the web server if not present.

            DirectoryInfo imageDirectory = new DirectoryInfo(@"C:\Users\scansick\Pictures");

            foreach (FileInfo file in imageDirectory.GetFiles())
            {

                System.Windows.Media.Imaging.JpegBitmapDecoder newJpeg = new System.Windows.Media.Imaging.JpegBitmapDecoder(file.OpenRead(), System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.None);

                System.Windows.Media.Imaging.BitmapImage image = new System.Windows.Media.Imaging.BitmapImage();
                image.StreamSource = file.OpenRead();

                

                System.Windows.UIElement element = new System.Windows.UIElement();
                
                //System.Windows.Media.Imaging.BitmapImage image = new System.Windows.Media.Imaging.BitmapImage();
                //image.
                //currentMain.wrapImages.Children.Add(image);


            }

        }

        public TagList GetTagsAvailable()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "tags");
            request.Headers.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
            //request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");
   

            HttpResponseMessage response = http.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            XmlSerializer serialKiller = new XmlSerializer(typeof(TagList));
            TagList tagList = (TagList)serialKiller.Deserialize(response.Content.ReadAsStreamAsync().Result);

            return tagList;
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
                return "The new tag could not be saved, there was an error on the server:" + ex.Message;
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

        private void GetCategoryTree()
        {
            currentMain.RefreshCategoryTreeView();
        }

        /*
        public static T ReadAsDataContract<T>(HttpContent content)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            return (T)serializer.ReadObject(content.ReadAsStreamAsync().Result);
        }
        */
    }
}
