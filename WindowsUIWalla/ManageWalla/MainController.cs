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
using System.Windows;
using System.Windows.Threading;
using log4net;
using log4net.Config;
using System.Configuration;
using System.Threading;

namespace ManageWalla
{
    public class MainController : IDisposable
    {
        #region ClassSetup
        private MainTwo currentMain;
        private GlobalState state = null;
        private List<ThumbCache> thumbCacheList = null;
        private List<MainCopyCache> mainCopyCacheList = null;

        //private ThumbState thumbState = null;
        private ServerHelper serverHelper = null;
        private static readonly ILog logger = LogManager.GetLogger(typeof(MainController));
        private CancellationTokenSource cancelTokenSourceToDel = new CancellationTokenSource(); //TODO delete this.

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

        public List<ThumbCache> GetThumbCacheList()
        {
            return thumbCacheList;
        }

        public List<MainCopyCache> GetMainCopyCacheList()
        {
            return mainCopyCacheList;
        }

        public void Dispose()
        {
            CacheHelper.SaveGlobalState(state);
            CacheHelper.SaveThumbCacheList(thumbCacheList);
            CacheHelper.SaveMainCopyCacheList(mainCopyCacheList);
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
        public void InitApplication()
        {
            try
            {

                //Setup Server helper.
                serverHelper = new ServerHelper(Properties.Settings.Default.WallaWSHostname, Properties.Settings.Default.WallaWSPort,
                    Properties.Settings.Default.WallaWSPath, Properties.Settings.Default.WallaAppKey, Properties.Settings.Default.WallaWebPath);

                //Initialise state.
                state = CacheHelper.GetGlobalState();
                thumbCacheList = CacheHelper.GetThumbCacheList();
                mainCopyCacheList = CacheHelper.GetMainCopyCacheList();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
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
                    return 200;
                case 7:
                    //Windows 8;
                    return 300;
                default:
                    return -1;
            }
        }

        async public Task<string> Logon(string email, string password)
        {
            //Verify if online
            if (!await serverHelper.isOnline(Properties.Settings.Default.WebServerTest))
            {
                state.connectionState = GlobalState.ConnectionState.Offline;
                return "";
            }

            string logonResponse = await serverHelper.Logon(email, password);
            if (logonResponse == "OK")
            {
                state.connectionState = GlobalState.ConnectionState.LoggedOn;
            }
            else
            {
                state.connectionState = GlobalState.ConnectionState.FailedLogin;
                return logonResponse;
            }

            return "OK";
        }

        async public Task AccountDetailsGet(CancellationToken cancelToken)
        {
            try
            {
                Account account = await serverHelper.AccountGet(cancelToken);
                state.account = account;

            }
            catch (OperationCanceledException)
            {
                //Suppress exception
                logger.Debug("AccountDetailsGet has been cancelled");
            }
        }

        async public Task MachineSetIdentity(CancellationToken cancelToken)
        {
            //Get current platformId and machine name.
            int platformId = GetPlatformId();
            string machineName = System.Environment.MachineName;
            bool found = false;

            foreach (AccountMachine current in state.account.Machines)
            {
                if (platformId == current.platformId && machineName == current.name)
                {
                    state.machineId = current.id;
                    found = true;
                }
            }
            try
            {
                if (found)
                {
                    await serverHelper.MachineMarkSession(state.machineId, cancelToken);
                }
                else
                {
                    long machineId = await serverHelper.MachineRegisterNew(machineName, platformId, cancelToken);
                    state.machineId = machineId;
                }
            }
            catch (OperationCanceledException)
            {
                //Suppress exception
                logger.Debug("AccountDetailsGet has been cancelled");
            }
        }
        #endregion

        #region Upload Methods
        async public Task LoadImagesFromArray(String[] fileNames, UploadImageFileList meFots, CancellationToken cancelToken)
        {
            try
            {
                for (int i = 0; i < fileNames.Length; i++)
                {
                    UploadImage newImage = new UploadImage();
                    await newImage.Setup(fileNames[i]);
                    meFots.Add(newImage);

                    cancelToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("LoadImagesFromArray has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async private void CreateCategoryFromFolder(DirectoryInfo currentFolder, UploadImageFileList meFots, long parentCategoryId, CancellationToken cancelToken)
        {
            string folderName = currentFolder.Name;
            if (folderName.Length > 30)
                folderName = currentFolder.Name.Substring(0, 30);

            CategoryListCategoryRef existingCategory = state.categoryList.CategoryRef.Where
                (r => r.parentId == parentCategoryId && r.name.ToUpper() == folderName.ToUpper()).FirstOrDefault<CategoryListCategoryRef>();

            long categoryId;


            if (existingCategory == null)
            {
                Category category = new Category();
                category.parentId = parentCategoryId;
                category.Name = folderName;
                category.Desc = "";
                categoryId = await serverHelper.CategoryCreateAsync(category, cancelToken);
            }
            else
            {
                categoryId = existingCategory.id;
            }

            foreach (UploadImage currentImage in meFots.OfType<UploadImage>().Where(r => r.FolderPath == currentFolder.FullName))
            {
                currentImage.Meta.categoryId = categoryId;
            }

            foreach (DirectoryInfo subFolder in currentFolder.GetDirectories())
            {
                CreateCategoryFromFolder(subFolder, meFots, categoryId, cancelToken);
            }
        }

        async public Task DoUploadAsync(UploadImageFileList meFots, UploadUIState uploadState, long categoryId, CancellationToken cancelToken)
        {
            try
            {
                if (uploadState.UploadToNewCategory)
                {
                    Category category = new Category();
                    category.parentId = categoryId;
                    category.Name = uploadState.CategoryName;
                    category.Desc = uploadState.CategoryDesc;
                    categoryId = await serverHelper.CategoryCreateAsync(category, cancelToken);
                }

                if (uploadState.MapToSubFolders)
                {
                    DirectoryInfo rootFolder = new DirectoryInfo(uploadState.RootFolder);
                    CreateCategoryFromFolder(rootFolder, meFots, categoryId, cancelToken);
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
                    cancelToken.ThrowIfCancellationRequested();

                    UploadImage currentUpload = meFots.Where(r => r.State == UploadImage.UploadState.None).First();

                    AddMachineTag(currentUpload);
                    currentUpload.Meta.MachineId = state.machineId;

                    string response = await serverHelper.UploadImageAsync(currentUpload, cancelToken);
                    if (response == null)
                    {
                        currentMain.uploadFots.Remove(currentUpload);
                    }
                    else
                    {
                        currentUpload.State = UploadImage.UploadState.Error;
                        currentUpload.UploadError = response;
                    }
                }
            }
            catch (OperationCanceledException cancelEx)
            {
                //Suppress exception and just return null.
                logger.Debug("DoUploadAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        private void AddMachineTag(UploadImage current)
        {
            ImageMetaTagRef newTagRef = new ImageMetaTagRef();
            newTagRef.id = state.account.Machines.Single(r => r.id == state.machineId).tagId;

            ImageMetaTagRef[] newTagArray;

            if (current.Meta.Tags == null)
            {
                newTagArray = new ImageMetaTagRef[1];
                newTagArray[0] = newTagRef;
            }
            else
            {
                newTagArray = new ImageMetaTagRef[current.Meta.Tags.Length + 1];
                current.Meta.Tags.CopyTo(newTagArray, 0);
                newTagArray[newTagArray.Length - 1] = newTagRef;
            }
            current.Meta.Tags = newTagArray;
        }

        async public Task ResetMeFotsMeta(UploadImageFileList metFots)
        {
            foreach (UploadImage currentImage in metFots)
            {
                await currentImage.ResetMeta();
            }
        }

        async public Task LoadImagesFromFolder(DirectoryInfo imageDirectory, bool recursive, UploadImageFileList meFots, CancellationToken cancelToken)
        {
            try
            {
                if (recursive)
                {
                    foreach (DirectoryInfo folder in imageDirectory.GetDirectories())
                    {
                        await LoadImagesFromFolder(folder, recursive, meFots, cancelToken);
                    }
                }

                foreach (FileInfo file in imageDirectory.GetFiles().OfType<FileInfo>())
                {
                    if (IsFormatOK(file.Extension.ToUpper().Substring(1)))
                    {
                        UploadImage newImage = new UploadImage();
                        await newImage.Setup(file.FullName);
                        meFots.Add(newImage);
                        cancelToken.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("LoadImagesFromFolder has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        private bool IsFormatOK(string fileExtension)
        {
            string[] formatsOK = new string[11] { "JPG", "JPEG", "TIF", "TIFF", "PSD", "PNG", "BMP", "GIF", "CR2", "ARW", "NEF" };
            return formatsOK.Any(r => r == fileExtension);
        }

        //TODO Sort out with date modified and to use local version.
        async public Task RefreshUploadStatusListAsync(CancellationToken cancelToken)
        {
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    state.uploadStatusList = await serverHelper.UploadGetStatusListAsync(cancelToken);
                    state.uploadStatusListState = GlobalState.DataLoadState.Loaded;
                }
                else
                {
                    if (state.uploadStatusList != null)
                    {
                        state.uploadStatusListState = GlobalState.DataLoadState.LocalCache;
                    }
                    else
                    {
                        state.uploadStatusListState = GlobalState.DataLoadState.Unavailable;
                    }
                }
            }
            catch (OperationCanceledException cancelEx)
            {
                //Suppress exception and just return null.
                logger.Debug("RefreshUploadStatusListAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                state.uploadStatusListState = GlobalState.DataLoadState.Unavailable;
                logger.Error(ex);
                throw ex;
            }

        }
        #endregion

        #region Tag Methods
        /// <summary>
        /// Depending on connection status and whether there is a local cached version, this method
        /// refreshes the cached object and sets the status of the load state used.
        /// </summary>
        /// <returns>OK or error</returns>
        async public Task TagRefreshListAsync(CancellationToken cancelToken)
        {
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    TagList tagList;
                    if (state.tagList != null)
                    {
                        tagList = await serverHelper.TagGetListAsync(state.tagList.lastChanged, cancelToken);
                        if (tagList != null)
                        {
                            state.tagList = tagList;
                        }
                        state.tagLoadState = GlobalState.DataLoadState.Loaded;
                    }
                    else
                    {
                        tagList = await serverHelper.TagGetListAsync(null, cancelToken);
                        if (tagList != null)
                        {
                            state.tagList = tagList;
                            state.tagLoadState = GlobalState.DataLoadState.Loaded;
                        }
                        else
                        {
                            state.tagLoadState = GlobalState.DataLoadState.Unavailable;
                        }
                    }
                }
                else
                {
                    if (state.tagList != null)
                    {
                        state.tagLoadState = GlobalState.DataLoadState.LocalCache;
                    }
                    else
                    {
                        state.tagLoadState = GlobalState.DataLoadState.Unavailable;
                    }
                }
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagRefreshListAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                state.tagLoadState = GlobalState.DataLoadState.Unavailable;
                throw (ex);
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
        async public Task<ImageList> TagGetImagesAsync(long id, string tagName, int cursor, string searchQueryString, CancellationToken cancelToken)
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
                    ImageList tagImageList = await serverHelper.GetImageListAsync("tag", tagName, lastModified, cursor, state.imageFetchSize, searchQueryString, -1, cancelToken);
                    if (tagImageList != null)
                    {
                        state.tagImageList.Remove(localTagList);
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
                    ImageList tagImageList = await serverHelper.GetImageListAsync("tag", tagName, null, cursor, state.imageFetchSize, searchQueryString, -1, cancelToken);
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
            catch (OperationCanceledException cancelEx)
            {
                //Suppress exception and just return null.
                logger.Debug("TagGetImagesAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                currentMain.ShowMessage(MainTwo.MessageType.Error,"There was a problem retrieving the images associated with the Tag: " + tagName + ".  Error: " + ex.Message);
                return null;
            }
        }

        async public Task<Tag> TagGetMetaAsync(TagListTagRef tagRef, CancellationToken cancelToken)
        {
            try
            {
                return await serverHelper.TagGetMeta(tagRef.name, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                //Suppress exception and just return null.
                logger.Debug("TagGetMetaAsync has been cancelled");
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
                await serverHelper.TagUpdateAsync(newTag, oldTagName, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagUpdateAsync has been cancelled");
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
                await serverHelper.TagCreateAsync(tag, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagCreateAsync has been cancelled");
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
                await serverHelper.TagDeleteAsync(tag, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagDeleteAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task TagAddRemoveImagesAsync(string tagName, ImageMoveList moveList, bool add, CancellationToken cancelToken)
        {
            try
            {
                await serverHelper.TagAddRemoveImagesAsync(tagName, moveList, add, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagAddRemoveImagesAsync has been cancelled");
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
        async public Task<Category> CategoryGetMetaAsync(CategoryListCategoryRef categoryRef, CancellationToken cancelToken)
        {
            try
            {
                return await serverHelper.CategoryGetMeta(categoryRef.id, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryGetMetaAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task CategoryUpdateAsync(Category existingCategory, CancellationToken cancelToken)
        {
            try
            {
                await serverHelper.CategoryUpdateAsync(existingCategory, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryUpdateAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task CategoryCreateAsync(Category category, CancellationToken cancelToken)
        {
            try
            {
                await serverHelper.CategoryCreateAsync(category, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryCreateAsync has been cancelled");
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
                await serverHelper.CategoryDeleteAsync(category, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("TagDeleteAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<ImageList> CategoryGetImagesAsync(long categoryId, int cursor, string searchQueryString, CancellationToken cancelToken)
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
                    ImageList categoryImageList = await serverHelper.GetImageListAsync("category", categoryId.ToString(), localCategoryList.LastChanged, cursor, state.imageFetchSize, searchQueryString, -1, cancelToken);
                    if (categoryImageList != null)
                    {
                        state.categoryImageList.Remove(localCategoryList);
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
                    ImageList categoryImageList = await serverHelper.GetImageListAsync("category", categoryId.ToString(), null, cursor, state.imageFetchSize, searchQueryString, -1, cancelToken);
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
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryGetImagesAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                currentMain.ShowMessage(MainTwo.MessageType.Error, "There was a problem retrieving the images associated with the Category: " + categoryId.ToString() + ".  Error: " + ex.Message);
                return null;
            }
        }

        async public Task CategoryRefreshListAsync(CancellationToken cancelToken)
        {
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    CategoryList categoryList;
                    if (state.categoryList != null)
                    {
                        categoryList = await serverHelper.CategoryGetListAsync(state.categoryList.lastChanged, cancelToken);
                        if (categoryList != null)
                        {
                            state.categoryList = categoryList;
                        }
                        state.categoryLoadState = GlobalState.DataLoadState.Loaded;
                    }
                    else
                    {
                        categoryList = await serverHelper.CategoryGetListAsync(null, cancelToken);
                        if (categoryList != null)
                        {
                            state.categoryList = categoryList;
                            state.categoryLoadState = GlobalState.DataLoadState.Loaded;
                        }
                        else
                        {
                            state.categoryLoadState = GlobalState.DataLoadState.Unavailable;
                        }
                    }
                }
                else
                {
                    if (state.categoryList != null)
                    {
                        state.categoryLoadState = GlobalState.DataLoadState.LocalCache;
                    }
                    else
                    {
                        state.categoryLoadState = GlobalState.DataLoadState.Unavailable;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.Debug("CategoryRefreshListAsync has been cancelled.");
            }
            catch (Exception ex)
            {
                state.categoryLoadState = GlobalState.DataLoadState.Unavailable;
                throw (ex);
            }
        }

        async public Task CategoryMoveImagesAsync(long categoryId, ImageMoveList moveList, CancellationToken cancelToken)
        {
            try
            {
                await serverHelper.CategoryMoveImagesAsync(categoryId, moveList, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryMoveImagesAsync has been cancelled");
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
        async public Task GalleryRefreshListAsync(CancellationToken cancelToken)
        {
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    GalleryList galleryList;
                    if (state.galleryList != null)
                    {
                        galleryList = await serverHelper.GalleryGetListAsync(state.galleryList.lastChanged, cancelToken);
                        if (galleryList != null)
                        {
                            state.galleryList = galleryList;
                        }
                        state.galleryLoadState = GlobalState.DataLoadState.Loaded;
                    }
                    else
                    {
                        galleryList = await serverHelper.GalleryGetListAsync(null, cancelToken);
                        if (galleryList != null)
                        {
                            state.galleryList = galleryList;
                            state.galleryLoadState = GlobalState.DataLoadState.Loaded;
                        }
                        else
                        {
                            state.galleryLoadState = GlobalState.DataLoadState.Unavailable;
                        }
                    }
                }
                else
                {
                    if (state.galleryList != null)
                    {
                        state.galleryLoadState = GlobalState.DataLoadState.LocalCache;
                    }
                    else
                    {
                        state.galleryLoadState = GlobalState.DataLoadState.Unavailable;
                    }
                }
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryRefreshListAsync has been cancelled.");
                throw (cancelEx);
            }
            catch (Exception ex)
            {
                state.galleryLoadState = GlobalState.DataLoadState.Unavailable;
                throw (ex);
            }
        }

        async public Task<Gallery> GalleryGetMetaAsync(GalleryListGalleryRef galleryRef, CancellationToken cancelToken)
        {
            try
            {
                return await serverHelper.GalleryGetMeta(galleryRef.name, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                //Suppress exception and just return null.
                logger.Debug("GalleryGetMetaAsync has been cancelled");
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
                await serverHelper.GalleryUpdateAsync(gallery, oldGalleryName, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryUpdateAsync has been cancelled");
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
                await serverHelper.GalleryCreateAsync(gallery, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryCreateAsync has been cancelled");
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
                await serverHelper.GalleryDeleteAsync(gallery, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryDeleteAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<ImageList> GalleryGetImagesAsync(long id, string galleryName, int cursor, long sectionId, string searchQueryString, CancellationToken cancelToken)
        {
            try
            {
                //Find a locally cached version,  ignore query string. 
                ImageList localGalleryList = state.galleryImageList.Where(r => (r.id == id && r.imageCursor == cursor  && r.sectionId == sectionId)).FirstOrDefault();
                if (state.connectionState != GlobalState.ConnectionState.LoggedOn)
                {
                    return localGalleryList;
                }

                if (localGalleryList != null && searchQueryString == null)
                {
                    //With Local version, check with server is a new version is required.
                    DateTime lastModified = localGalleryList.LastChanged;
                    ImageList galleryImageList = await serverHelper.GetImageListAsync("gallery", galleryName, lastModified, cursor, state.imageFetchSize, searchQueryString, sectionId, cancelToken);
                    if (galleryImageList != null)
                    {
                        state.galleryImageList.Remove(localGalleryList);
                        state.galleryImageList.Add(galleryImageList);
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
                    ImageList galleryImageList = await serverHelper.GetImageListAsync("gallery", galleryName, null, cursor, state.imageFetchSize, searchQueryString, sectionId, cancelToken);
                    if (galleryImageList != null)
                    {
                        if (searchQueryString == null)
                            state.galleryImageList.Add(galleryImageList);

                        return galleryImageList;
                    }
                    else
                    {
                        return localGalleryList;
                    }
                }
            }
            catch (OperationCanceledException cancelEx)
            {
                //Suppress exception and just return null.
                logger.Debug("GalleryGetImagesAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                currentMain.ShowMessage(MainTwo.MessageType.Error, "There was a problem retrieving the images associated with the Gallery: " + galleryName + ".  Error: " + ex.Message);
                return null;
            }
        }

        public String GetGalleryUrl(string galleryName, string urlComplex)
        {
            if (urlComplex != null && urlComplex.Length > 0)
            {
                return serverHelper.GetWebUrl() + "gallery/" + galleryName + "?key=" + urlComplex;
            }
            else
            {
                return serverHelper.GetWebUrl() + "gallery/" + galleryName;
            }
        }
        #endregion

        #region  Images Processing
        async public Task DeleteImagesAsync(ImageList imageList, CancellationToken cancelToken)
        {
            try
            {
                await serverHelper.DeleteImagesAsync(imageList, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("DeleteImagesAsync has been cancelled");
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
