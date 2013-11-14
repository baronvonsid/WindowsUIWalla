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
using System.Windows;
using System.Windows.Threading;
using log4net;
using log4net.Config;
using System.Configuration;

namespace ManageWalla
{
    public class MainController : IDisposable
    {
        #region ClassSetup
        private MainTwo currentMain;
        private GlobalState state = null;
        private ServerHelper serverHelper = null;
        private static readonly ILog logger = LogManager.GetLogger(typeof(MainController));

        public MainController() 
        {//MainWindow currentMainParam
            //Set a reference to the main window for two way comms.
            //currentMain = currentMainParam;
        }

        public MainController(MainTwo currentMainParam)
        {
            //Set a reference to the main window for two way comms.
            currentMain = currentMainParam;
        }

        public GlobalState GetState()
        {
            return state;
        }

        public ServerHelper GetServerHelper()
        {
            return serverHelper;
        }

        public void Dispose()
        {
            state.SaveState();
        }
        #endregion

        #region AppInitialise
        /// <summary>
        /// Check for user specific state, saved locally.  Initialise object.
        /// Create Server helper and check if online.
        /// If not online then set online flag and finish further initialisation.
        /// If online. If user credentials in state object are present attempt logon to Walla.
        /// When success - Pass Machine details to Walla and retrieve machine id.
        /// If logon fails or no user credentials.
        /// Show Settings form and display message about failure.
        /// </summary>
        /// <param name="currentMainParam"></param>
        /// <returns></returns>
        public string InitApplication()
        {
            try
            {
                //Setup Server helper.
                serverHelper = new ServerHelper(Properties.Settings.Default.WallaWSHostname, long.Parse(Properties.Settings.Default.WallaWSPort), Properties.Settings.Default.WallaWSPath, Properties.Settings.Default.WallaAppKey);

                //Initialise state.
                state = GlobalState.GetState();
                if (state.userName == null || state.password == null)
                {
                    state.connectionState = GlobalState.ConnectionState.NoAccount;
                    return "";
                }

                return Logon(state.userName, state.password);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        //Windows Versions.
        private int GetPlatformId()
        {
            System.OperatingSystem osInfo = System.Environment.OSVersion;

            switch (osInfo.Version.Major)
            {
                case 6:
                    //Windows 7
                    return 100;
                case 7:
                    //Windows 8;
                    return 110;
                default:
                    return -1;
            }
        }

        //Used if initially there wa no user credentials or if the logon failed.
        public string Logon(string userName, string password)
        {
            //Verify if online
            if (!serverHelper.isOnline())
            {
                state.connectionState = GlobalState.ConnectionState.Offline;
                return "";
            }

            //Perform login
            string logonResponse = serverHelper.Logon(userName, password);
            if (logonResponse == "OK")
            {
                state.connectionState = GlobalState.ConnectionState.LoggedOn;
                state.userName = userName;
                state.password = password;
                state.lastLoggedIn = DateTime.Now;
            }
            else
            {
                state.connectionState = GlobalState.ConnectionState.FailedLogin;
                return logonResponse;
            }

            //Set MachineId server side and client side.
            state.machineId = serverHelper.SetSessionMachineId(System.Environment.MachineName, GetPlatformId());
            return "OK";
        }
        #endregion

        #region Upload Methods
        async public Task LoadImagesFromArray(String[] fileNames, UploadImageFileList meFots)
        {
            for (int i = 0; i < fileNames.Length; i++)
            {
                UploadImage newImage = new UploadImage();
                await newImage.Setup(fileNames[i]);
                meFots.Add(newImage);
            }

        }

        public void CreateCategoryFromFolder(DirectoryInfo currentFolder, UploadImageFileList meFots, long parentCategoryId)
        {
            long categoryId = 0; //serverHelper.CreateCategory(currentFolder.Name, "", parentCategoryId);
            foreach (UploadImage currentImage in meFots.OfType<UploadImage>().Where(r => r.FolderPath == currentFolder.FullName))
            {
                currentImage.Meta.categoryId = categoryId;
            }

            foreach (DirectoryInfo subFolder in currentFolder.GetDirectories())
            {
                CreateCategoryFromFolder(subFolder, meFots, categoryId);
            }
        }

        async public Task<string> DoUploadAsync(UploadImageFileList meFots, UploadUIState uploadState, long categoryId)
        {
            if (uploadState.UploadToNewCategory)
            {
                Category category = new Category();
                category.parentId = categoryId;
                category.Name = uploadState.CategoryName;
                category.Desc = uploadState.CategoryDesc;
                categoryId = await serverHelper.CategoryCreateAsync(category);
                //rootCategoryId = serverHelper.CreateCategory(uploadState.CategoryName, uploadState.CategoryDesc, uploadState.CategoryId);
            }

            //TODO - Implement, taken out of GUI.
            if (uploadState.MapToSubFolders)
            {
                if (uploadState.UploadToNewCategory)
                {
                    //Assumption being that all sub categories will now need to be created.
                    DirectoryInfo rootFolder = new DirectoryInfo(uploadState.RootFolder);
                    CreateCategoryFromFolder(rootFolder, meFots, categoryId);
                }
                else
                {
                    //Check local category xml for matches or create new.
                    //TODO after categories have been developed.
                }
            }
            else
            {
                foreach (UploadImage currentImage in meFots)
                {
                    currentImage.Meta.categoryId = categoryId;
                }
            }

            //Check for each chkAll box set to true, then replace respective values.
            if (uploadState.MetaTagRefAll)
            {
                foreach (UploadImage currentImage in meFots)
                {
                    currentImage.Meta.Tags = uploadState.MetaTagRef;
                }
            }

            while (meFots.Where(r => r.State == UploadImage.UploadState.None).Count() > 0)
            {
                UploadImage currentUpload = meFots.Where(r => r.State == UploadImage.UploadState.None).First();

                string response = await serverHelper.UploadImageAsync(currentUpload);
                if (response == null)
                {
                    //meFots.RemoveAt(0);
                    //Dispatcher.Invoke(DispatcherPriority.Send,

                    //theLabel.Invoke(new Action(() => theLabel.Text = "hello world from worker thread!"));

                    currentMain.uploadFots.Remove(currentUpload);

                    /*
                    Action UpdateFotsAction = delegate()
                    {
                        currentMain.uploadFots.RemoveAt(0);
                    };
                    
                    Application.Current.MainWindow.Dispatcher.Invoke(DispatcherPriority.Send, UpdateFotsAction);
                    */
                }
                else
                {
                    currentUpload.State = UploadImage.UploadState.Error;
                    currentUpload.UploadError = response;
                }
            }
            return null;
        }

        async public Task ResetMeFotsMeta(UploadImageFileList metFots)
        {
            foreach (UploadImage currentImage in metFots)
            {
                await currentImage.ResetMeta();
            }
        }

        async public Task LoadImagesFromFolder(DirectoryInfo imageDirectory, bool recursive, UploadImageFileList meFots)
        {
            if (recursive)
            {
                foreach (DirectoryInfo folder in imageDirectory.GetDirectories())
                {
                    await LoadImagesFromFolder(folder, recursive, meFots);
                }
            }

            foreach (FileInfo file in imageDirectory.GetFiles().OfType<FileInfo>())
            {
                switch (file.Extension.ToUpper().Substring(1))
                {
                    case "JPG":
                    case "JPEG":
                    case "TIF":
                    case "TIFF":
                    case "PSD":
                    case "PNG":
                    case "BMP":
                    case "GIF":
                    case "CR2":
                    case "ARW":
                    case "NEF":
                        UploadImage newImage = new UploadImage();
                        await newImage.Setup(file.FullName);
                        meFots.Add(newImage);
                        break;
                    default:
                        break;
                }
            }

            /* old code
            public void LoadImage(object uri)
            {
                var decoder = new JpegBitmapDecoder(new Uri(uri.ToString()), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                decoder.Frames[0].Freeze();
                this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<ImageSource>(SetImage), decoder.Frames[0]);
            }

            public void SetImage(ImageSource source)
            {
                this.BackgroundImage.Source = source;
            } 
            */
        }

        async public Task<string> RefreshUploadStatusListAsync()
        {
            try
            {

                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    state.uploadStatusList = await serverHelper.UploadGetStatusListAsync();
                    state.uploadStatusListState = GlobalState.DataLoadState.Loaded;
                    return "OK";
                }
                else
                {
                    if (state.uploadStatusList != null)
                    {
                        state.uploadStatusListState = GlobalState.DataLoadState.LocalCache;
                        return "OK";
                    }
                    else
                    {
                        state.uploadStatusListState = GlobalState.DataLoadState.Unavailable;
                        return "No local upload status list is available to show.";
                    }
                }
            }
            catch (Exception ex)
            {
                state.uploadStatusListState = GlobalState.DataLoadState.Unavailable;
                return ex.Message;
            }
        }
        #endregion

        #region Tag Methods
        /// <summary>
        /// Depending on connection status and whether there is a local cached version, this method
        /// refreshes the cached object and sets the status of the load state used.
        /// </summary>
        /// <returns>OK or error</returns>
        async public Task<string> TagRefreshListAsync()
        {
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    TagList tagList;
                    if (state.tagList != null)
                    {
                        tagList = await serverHelper.TagGetListAsync(state.tagList.lastChanged);
                    }
                    else
                    {
                        tagList = await serverHelper.TagGetListAsync(null);
                    }
                    state.tagList = tagList;
                    state.tagLoadState = GlobalState.DataLoadState.Loaded;
                    return "OK";
                }
                else
                {
                    if (state.tagList != null)
                    {
                        state.tagLoadState = GlobalState.DataLoadState.LocalCache;
                        return "OK";
                    }
                    else
                    {
                        state.tagLoadState = GlobalState.DataLoadState.Unavailable;
                        return "No local tag list is available to show.";
                    }
                }
            }
            catch (Exception ex)
            {
                state.tagLoadState = GlobalState.DataLoadState.Unavailable;
                return ex.Message;
            }
        }

        /// <summary>
        /// Finds a local cached version of the image list.  If there is one present and no search qeury is specified,
        /// then the server version is requested if a newer one is available, this is then added to the cache.
        /// If the application is in offline mode then just return the cached version.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="tagName"></param>
        /// <param name="cursor"></param>
        /// <param name="searchQueryString"></param>
        /// <returns></returns>
        async public Task<ImageList> TagGetImagesAsync(long id, string tagName, int cursor, string searchQueryString)
        {
            try
            {
                //Find a locally cached version,  ignore query string. 
                ImageList localTagList = state.tagImageList.Where(r => (r.id == id && r.imageCursor == cursor)).FirstOrDefault();
                if (state.connectionState != GlobalState.ConnectionState.LoggedOn)
                {
                    return localTagList;
                }

                if (localTagList != null && searchQueryString == null)
                {
                    //With Local version, check with server is a new version is required.
                    DateTime lastModified = localTagList.LastChanged;
                    ImageList tagImageList = await serverHelper.GetImageListAsync("tag", tagName, lastModified, cursor, state.imageFetchSize, searchQueryString);
                    if (tagImageList != null)
                    {
                        state.tagImageList.Add(tagImageList);
                        return tagImageList;
                    }
                    else
                    {
                        return localTagList;
                    }
                }
                else
                {
                    //Add the image list to the state if no search is specified.
                    ImageList tagImageList = await serverHelper.GetImageListAsync("tag", tagName, null, cursor, state.imageFetchSize, searchQueryString);
                    if (tagImageList != null)
                    {
                        if (searchQueryString == null)
                            state.tagImageList.Add(tagImageList);

                        return tagImageList;
                    }
                    else
                    {
                        return localTagList;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                currentMain.DisplayMessage("There was a problem retrieving the images associated with the Tag: " + tagName + ".  Error: " + ex.Message, MainTwo.MessageSeverity.Error, false);
                return null;
            }
        }

        async public Task<Tag> TagGetMetaAsync(TagListTagRef tagRef)
        {
            try
            {
                return await serverHelper.TagGetMeta(tagRef.name);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                currentMain.DisplayMessage("There was a problem retrieving the tag meta data: " + tagRef.name + ".  Error: " + ex.Message, MainTwo.MessageSeverity.Error, false);
                return null;
            }
            
        }

        async public Task<string> TagUpdateAsync(Tag newTag, string oldTagName)
        {
            string response = await serverHelper.TagUpdateAsync(newTag, oldTagName);
            if (response != "OK")
                response = "Tag could not be updated, there was an error on the server:" + response;

            return response;
        }

        async public Task<string> TagCreateAsync(Tag tag)
        {
            string response = await serverHelper.TagCreateAsync(tag);
            if (response != "OK")
                response = "Tag could not be created, there was an error on the server:" + response;

            return response;
        }

        async public Task<string> TagDeleteAsync(Tag tag)
        {
            string response = await serverHelper.TagDeleteAsync(tag);
            if (response != "OK")
                response = "Tag could not be deleted, there was an error on the server:" + response;

            return response;
        }

        async public Task<string> TagAddRemoveImagesAsync(string tagName, ImageMoveList moveList, bool add)
        {
            string response = await serverHelper.TagAddRemoveImagesAsync(tagName, moveList, add);
            if (response != "OK")
                response = "Images could not be add\removed from the Tag, there was an error on the server:" + response;

            return response;
        }
        #endregion

        #region Category
        async public Task<Category> CategoryGetMetaAsync(CategoryListCategoryRef categoryRef)
        {
            try
            {
                return await serverHelper.CategoryGetMeta(categoryRef.id);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                currentMain.DisplayMessage("There was a problem retrieving the Category meta data: " + categoryRef.id.ToString() + ".  Error: " + ex.Message, MainTwo.MessageSeverity.Error, false);
                return null;
            }

        }

        async public Task<string> CategoryUpdateAsync(Category existingCategory)
        {
            string response = await serverHelper.CategoryUpdateAsync(existingCategory);
            if (response != "OK")
                response = "Category could not be updated, there was an error on the server:" + response;

            return response;
        }

        async public Task<string> CategoryCreateAsync(Category category)
        {
            try
            {
                long categoryId = await serverHelper.CategoryCreateAsync(category);
                return "OK";
            }
            catch (Exception ex)
            {
                return "Category could not be created, there was an error on the server: " + ex.Message;
            }
        }

        async public Task<string> CategoryDeleteAsync(Category category)
        {
            string response = await serverHelper.CategoryDeleteAsync(category);
            if (response != "OK")
                response = "Category could not be deleted, there was an error on the server:" + response;

            return response;
        }

        async public Task<ImageList> CategoryGetImagesAsync(long categoryId, int cursor, string searchQueryString)
        {
            try
            {
                //Find a locally cached version,  ignore query string. 
                ImageList localCategoryList = state.categoryImageList.Where(r => (r.id == categoryId && r.imageCursor == cursor)).FirstOrDefault();
                if (state.connectionState != GlobalState.ConnectionState.LoggedOn)
                {
                    return localCategoryList;
                }

                if (localCategoryList != null && searchQueryString == null)
                {
                    //With Local version, check with server is a new version is required.
                    ImageList categoryImageList = await serverHelper.GetImageListAsync("category", categoryId.ToString(), localCategoryList.LastChanged, cursor, state.imageFetchSize, searchQueryString);
                    if (categoryImageList != null)
                    {
                        state.categoryImageList.Add(categoryImageList);
                        return categoryImageList;
                    }
                    else
                    {
                        return localCategoryList;
                    }
                }
                else
                {
                    //Add the image list to the state if no search is specified.
                    ImageList categoryImageList = await serverHelper.GetImageListAsync("category", categoryId.ToString(), null, cursor, state.imageFetchSize, searchQueryString);
                    if (categoryImageList != null)
                    {
                        if (searchQueryString == null)
                            state.categoryImageList.Add(categoryImageList);

                        return categoryImageList;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                currentMain.DisplayMessage("There was a problem retrieving the images associated with the Category: " + categoryId.ToString() + ".  Error: " + ex.Message, MainTwo.MessageSeverity.Error, false);
                return null;
            }
        }

        async public Task<string> CategoryRefreshListAsync()
        {
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    CategoryList categoryList;
                    if (state.categoryList != null)
                    {
                        categoryList = await serverHelper.CategoryGetListAsync(state.categoryList.lastChanged);
                    }
                    else
                    {
                        categoryList = await serverHelper.CategoryGetListAsync(null);
                    }
                    state.categoryList = categoryList;
                    state.categoryLoadState = GlobalState.DataLoadState.Loaded;
                    return "OK";
                }
                else
                {
                    if (state.tagList != null)
                    {
                        state.categoryLoadState = GlobalState.DataLoadState.LocalCache;
                        return "OK";
                    }
                    else
                    {
                        state.categoryLoadState = GlobalState.DataLoadState.Unavailable;
                        return "No local Category list is available.";
                    }
                }
            }
            catch (Exception ex)
            {
                state.categoryLoadState = GlobalState.DataLoadState.Unavailable;
                return ex.Message;
            }
        }

        async public Task<string> CategoryMoveImagesAsync(long categoryId, ImageMoveList moveList)
        {
            string response = await serverHelper.CategoryMoveImagesAsync(categoryId, moveList);
            if (response != "OK")
                response = "Images could not be moved category, there was an error on the server:" + response;

            return response;
        }
        #endregion

        #region Gallery
        async public Task<string> GalleryRefreshListAsync()
        {
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    GalleryList galleryList;
                    if (state.galleryList != null)
                    {
                        galleryList = await serverHelper.GalleryGetListAsync(state.galleryList.lastChanged);
                    }
                    else
                    {
                        galleryList = await serverHelper.GalleryGetListAsync(null);
                    }
                    state.galleryList = galleryList;
                    state.galleryLoadState = GlobalState.DataLoadState.Loaded;
                    return "OK";
                }
                else
                {
                    if (state.galleryList != null)
                    {
                        state.galleryLoadState = GlobalState.DataLoadState.LocalCache;
                        return "OK";
                    }
                    else
                    {
                        state.galleryLoadState = GlobalState.DataLoadState.Unavailable;
                        return "No local gallery list is available to show.";
                    }
                }
            }
            catch (Exception ex)
            {
                state.galleryLoadState = GlobalState.DataLoadState.Unavailable;
                return ex.Message;
            }
        }

        async public Task<Gallery> GalleryGetMetaAsync(GalleryListGalleryRef galleryRef)
        {
            try
            {
                return await serverHelper.GalleryGetMeta(galleryRef.name);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                currentMain.DisplayMessage("There was a problem retrieving the tag meta data: " + galleryRef.name + ".  Error: " + ex.Message, MainTwo.MessageSeverity.Error, false);
                return null;
            }

        }

        async public Task<string> GalleryUpdateAsync(Gallery gallery, string oldGalleryName)
        {
            string response = await serverHelper.GalleryUpdateAsync(gallery, oldGalleryName);
            if (response != "OK")
                response = "Gallery could not be updated, there was an error on the server:" + response;

            return response;
        }

        async public Task<string> GalleryCreateAsync(Gallery gallery)
        {
            string response = await serverHelper.GalleryCreateAsync(gallery);
            if (response != "OK")
                response = "Gallery could not be created, there was an error on the server:" + response;

            return response;
        }

        async public Task<string> GalleryDeleteAsync(Gallery gallery)
        {
            string response = await serverHelper.GalleryDeleteAsync(gallery);
            if (response != "OK")
                response = "Gallery could not be deleted, there was an error on the server:" + response;

            return response;
        }

        async public Task<ImageList> GalleryGetImagesAsync(long id, string galleryName, int cursor, string searchQueryString)
        {
            try
            {
                //Find a locally cached version,  ignore query string. 
                ImageList localGalleryList = state.galleryImageList.Where(r => (r.id == id && r.imageCursor == cursor)).FirstOrDefault();
                if (state.connectionState != GlobalState.ConnectionState.LoggedOn)
                {
                    return localGalleryList;
                }

                if (localGalleryList != null && searchQueryString == null)
                {
                    //With Local version, check with server is a new version is required.
                    DateTime lastModified = localGalleryList.LastChanged;
                    ImageList galleryImageList = await serverHelper.GetImageListAsync("gallery", galleryName, lastModified, cursor, state.imageFetchSize, searchQueryString);
                    if (galleryImageList != null)
                    {
                        state.tagImageList.Add(galleryImageList);
                        return galleryImageList;
                    }
                    else
                    {
                        return localGalleryList;
                    }
                }
                else
                {
                    //Add the image list to the state if no search is specified.
                    ImageList galleryImageList = await serverHelper.GetImageListAsync("gallery", galleryName, null, cursor, state.imageFetchSize, searchQueryString);
                    if (galleryImageList != null)
                    {
                        if (searchQueryString == null)
                            state.tagImageList.Add(galleryImageList);

                        return galleryImageList;
                    }
                    else
                    {
                        return localGalleryList;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                currentMain.DisplayMessage("There was a problem retrieving the images associated with the Gallery: " + galleryName + ".  Error: " + ex.Message, MainTwo.MessageSeverity.Error, false);
                return null;
            }
        }
        #endregion
    }
}
