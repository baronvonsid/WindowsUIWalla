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
        private UploadImageStateList uploadImageStateList = null;
        private bool createdCategory = false;

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

        //public UploadImageStateList UpdateFromStateUploadImageStateList()
       // {
         //   return uploadImageStateList;
        //}

        public void Dispose()
        {
            if (state == null)
                return;

            CacheHelper.SaveGlobalState(state, state.account.ProfileName);
            CacheHelper.SaveThumbCacheList(thumbCacheList, state.account.ProfileName);
            CacheHelper.SaveMainCopyCacheList(mainCopyCacheList, state.account.ProfileName);
            CacheHelper.SaveUploadImageStateList(uploadImageStateList, state.account.ProfileName);
        }
        #endregion

        #region AppInitialise
        public void SetupServerHelper()
        {
            try
            {
                serverHelper = new ServerHelper(Properties.Settings.Default.WallaWSHostname, Properties.Settings.Default.WallaWSPort,
                    Properties.Settings.Default.WallaWSPath, Properties.Settings.Default.WallaAppKey, Properties.Settings.Default.WallaWebPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        public bool CacheFilesPresent(string profileName)
        {
            return CacheHelper.CacheFilesPresent(profileName);
        }

        public void SetUpCacheFiles(string profileName, UploadImageStateList uploadImageStateListParam)
        {
            state = CacheHelper.GetGlobalState(profileName);
            thumbCacheList = CacheHelper.GetThumbCacheList(profileName);
            mainCopyCacheList = CacheHelper.GetMainCopyCacheList(profileName);
            CacheHelper.GetUploadImageStateList(uploadImageStateListParam, profileName);
            uploadImageStateList = uploadImageStateListParam;
        }

        async public Task<bool> SetPlatform()
        {
            System.OperatingSystem osInfo = System.Environment.OSVersion;

            return await serverHelper.VerifyPlatform(Properties.Settings.Default.OS, "PC", osInfo.Version.Major, osInfo.Version.Minor);
        }

        async public Task<bool> CheckOnline()
        {
            return await serverHelper.isOnline(Properties.Settings.Default.WebServerTest);
        }

        async public Task<bool> VerifyApp()
        {
            return await serverHelper.VerifyApp(Properties.Settings.Default.WallaAppKey);
        }

        async public Task<string> Logon(string userName, string password)
        {
            if (await serverHelper.Logon(userName, password))
            {
                return "OK";
            }
            else
            {
                return "Logon failed";
            }
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


        async public Task SetUserApp(CancellationToken cancelToken)
        {
            long userAppId;

            if (state.userApp == null)
            {
                //TODO add find existing user app function.

                string machineName = System.Environment.MachineName;

                string uploadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FotoWalla Auto Upload");
                string copyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FotoWalla Copies");

                UserApp newUserApp = new UserApp();
                newUserApp.MachineName = machineName;
                newUserApp.AutoUpload = true;
                newUserApp.AutoUploadFolder = uploadFolder;
                newUserApp.MainCopyFolder = copyFolder;

                userAppId = await serverHelper.UserAppCreateUpdateAsync(newUserApp, cancelToken);

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                if (!Directory.Exists(copyFolder))
                    Directory.CreateDirectory(copyFolder);
            }
            else
            {
                userAppId = state.userApp.id;
            }

            UserApp userApp = await serverHelper.UserAppGet(userAppId);
            if (userApp != null)
            {
                state.userApp = userApp;
            }
            else
            {
                throw new Exception("Valid settings for this application could not be established on the server.");
            }

        }

        async public Task UserAppUpdateAsync(UserApp userApp, CancellationToken cancelToken)
        {
            try
            {
                await serverHelper.UserAppCreateUpdateAsync(userApp, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("UserAppUpdateAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }
        #endregion

        #region Upload Methods
        async public Task<List<string>> LoadImagesFromArray(String[] fileNames, UploadImageFileList meFots, CancellationToken cancelToken)
        {
            List<string> errorResponses = new List<string>();
            try
            {
                for (int i = 0; i < fileNames.Length; i++)
                {
                    UploadImage newImage = new UploadImage();
                    string response = await newImage.Setup(fileNames[i], true);
                    if (response == "OK")
                    {
                        meFots.Add(newImage);
                    }
                    else
                    {
                        errorResponses.Add(response);
                    }

                    cancelToken.ThrowIfCancellationRequested();
                }
                return errorResponses;
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
                createdCategory = true;
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

        async public Task<List<string>> UploadManualAsync(UploadImageFileList meFots, UploadUIState uploadState, CancellationToken cancelToken)
        {
            try
            {
                createdCategory = false;

                if (uploadState.UploadToNewCategory)
                {
                    Category category = new Category();
                    category.parentId = uploadState.RootCategoryId;
                    category.Name = uploadState.CategoryName;
                    category.Desc = uploadState.CategoryDesc;
                    uploadState.RootCategoryId = await serverHelper.CategoryCreateAsync(category, cancelToken);
                    createdCategory = true;

                    //Reset these values in case of a resume scenario.
                    uploadState.UploadToNewCategory = false;
                    uploadState.RootCategoryName = uploadState.CategoryName;
                    uploadState.CategoryName = "";
                    uploadState.CategoryDesc = "";
                }

                if (uploadState.MapToSubFolders)
                {
                    DirectoryInfo rootFolder = new DirectoryInfo(uploadState.RootFolder);
                    CreateCategoryFromFolder(rootFolder, meFots, uploadState.RootCategoryId, cancelToken);
                }
                else
                {
                    foreach (UploadImage currentImage in meFots)
                    {
                        currentImage.Meta.categoryId = uploadState.RootCategoryId;
                    }
                }

                if (createdCategory)
                    await currentMain.RefreshAndDisplayCategoryList(true);

                //Check for each chkAll box set to true, then replace respective values.
                if (uploadState.MetaTagRefAll)
                {
                    foreach (UploadImage currentImage in meFots)
                    {
                        currentImage.Meta.Tags = uploadState.MetaTagRef;
                    }
                }

                if (uploadState.MetaTakenDateSetAll)
                {
                    foreach (UploadImage currentImage in meFots)
                    {
                        currentImage.Meta.TakenDate = uploadState.MetaTakenDate;
                        currentImage.Meta.TakenDateSet = true;
                    }
                }

                foreach (UploadImage currentImage in meFots)
                {
                    if (uploadState.MetaTagRefAll)
                    {
                        currentImage.Meta.Tags = uploadState.MetaTagRef;
                    }

                    //currentImage.Meta.TakenDate = currentImage.Meta.TakenDateFile;

                    AddMachineTag(currentImage);
                    currentImage.Meta.UserAppId = state.userApp.id;
                }

                return await UploadProcessAsync(meFots, false, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                //Suppress exception and just return null.
                logger.Debug("UploadManualAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task<List<string>> UploadProcessAsync(UploadImageFileList meFots, bool isAuto, CancellationToken cancelToken)
        {
            try
            {
                List<string> responseErrors = new List<string>();
                while (meFots.Count() > 0)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    //UploadImage currentUpload = meFots.Where(r => r.State == UploadImage.UploadState.None).First();
                    UploadImage currentUpload = meFots[0];
                    
                    //Get or Create new Upload entry in cache.
                    UploadImageState newUploadEntry = CacheHelper.GetOrCreateCacheItem(uploadImageStateList, 
                        currentUpload.Meta.OriginalFileName, currentUpload.FilePath, currentUpload.Meta.Name, 
                        currentUpload.Meta.Size, isAuto, state.userApp.id, state.userApp.MachineName);

                    string response = await serverHelper.UploadImageAsync(currentUpload, newUploadEntry, cancelToken);
                    if (response == null)
                    {
                        newUploadEntry.lastUpdated = DateTime.Now;
                        newUploadEntry.status = UploadImage.ImageStatus.AwaitingProcessed;
                    }
                    else
                    {
                        newUploadEntry.error = true;
                        newUploadEntry.lastUpdated = DateTime.Now;
                        newUploadEntry.errorMessage = response;
                        responseErrors.Add(currentUpload.Meta.OriginalFileName + " error: " + response);
                    }
                    currentMain.RefreshUploadStatusListBinding();
                    
                    meFots.Remove(currentUpload);
                }
                return responseErrors;
            }
            catch (OperationCanceledException cancelEx)
            {
                //Suppress exception and just return null.
                logger.Debug("UploadProcessAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        async public Task UploadAutoAsync(UploadImageFileList meFots, UploadUIState uploadState, CancellationToken cancelToken)
        {
            try
            {
                foreach (UploadImage currentImage in meFots)
                {
                    AddMachineTag(currentImage);
                    currentImage.Meta.UserAppId = state.userApp.id;
                    currentImage.Meta.categoryId = uploadState.AutoCategoryId;
                }

                await UploadProcessAsync(meFots, true, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("UploadAutoAsync has been cancelled");
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
            //TODO Change to use tagid from userapp object.
            newTagRef.id = state.userApp.TagId;   //account.Machines.Single(r => r.id == state.userAppId).tagId;

            if (current.Meta.Tags == null)
            {
                current.Meta.Tags = new ImageMetaTagRef[1] { newTagRef };
            }
            else
            {
                if (!current.Meta.Tags.Any<ImageMetaTagRef>(r => r.id == newTagRef.id))
                {
                    ImageMetaTagRef[] newTagArray = new ImageMetaTagRef[current.Meta.Tags.Length + 1];
                    current.Meta.Tags.CopyTo(newTagArray, 0);
                    newTagArray[newTagArray.Length - 1] = newTagRef;

                    current.Meta.Tags = newTagArray;
                }
            }
        }

        async public Task ResetMeFotsMeta(UploadImageFileList metFots)
        {
            foreach (UploadImage currentImage in metFots)
            {
                await currentImage.ResetMeta();
            }
        }

        async public Task<List<string>> LoadImagesFromFolder(DirectoryInfo imageDirectory, bool recursive, UploadImageFileList meFots, CancellationToken cancelToken)
        {
            List<string> errorResponses = new List<string>();
            try
            {
                if (recursive)
                {
                    foreach (DirectoryInfo folder in imageDirectory.GetDirectories())
                    {
                        List<string> tempErrorResponses = await LoadImagesFromFolder(folder, recursive, meFots, cancelToken);
                        errorResponses.AddRange(tempErrorResponses);
                    }
                }

                foreach (FileInfo file in imageDirectory.GetFiles().OfType<FileInfo>())
                {
                    if (IsFormatOK(file.Extension.ToUpper().Substring(1)))
                    {
                        UploadImage newImage = new UploadImage();
                        string response = await newImage.Setup(file.FullName, true);
                        if (response == "OK")
                        {
                            meFots.Add(newImage);
                        }
                        else
                        {
                            errorResponses.Add(response);
                        }
                        cancelToken.ThrowIfCancellationRequested();
                    }
                }

                return errorResponses;
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

        async public Task<List<string>> CheckImagesForAutoUploadAsync(DirectoryInfo imageDirectory, UploadImageFileList meFots, 
            CancellationToken cancelToken)
        {
            List<string> errorResponses = new List<string>();
            try
            {
                foreach (DirectoryInfo folder in imageDirectory.GetDirectories())
                {
                    List<string> tempErrorResponses = await CheckImagesForAutoUploadAsync(folder, meFots, cancelToken);
                    errorResponses.AddRange(tempErrorResponses);
                }

                foreach (FileInfo file in imageDirectory.GetFiles().OfType<FileInfo>())
                {
                    if (IsFormatOK(file.Extension.ToUpper().Substring(1)))
                    {
                        //Check for file already uploaded.

                        if (!uploadImageStateList.Any<UploadImageState>(r => (r.fileName == file.Name && r.sizeBytes == file.Length)))
                        {
                            UploadImage newImage = new UploadImage();
                            string response = await newImage.Setup(file.FullName, true);
                            if (response == "OK")
                            {
                                meFots.Add(newImage);
                            }
                            else
                            {
                                errorResponses.Add(response);
                            }
                        }
                        cancelToken.ThrowIfCancellationRequested();
                    }
                }

                return errorResponses;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CheckImagesForAutoUpload has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        //TODO Sort out with date modified and to use local version.
        async public Task RefreshUploadStatusListAsync(long[] orderIds, CancellationToken cancelToken, UploadImageStateList currentUploadStatusList)
        {
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    ImageIdList orderIdList = new ImageIdList();
                    orderIdList.ImageRef = orderIds;

                    //for (int i = 0; i < orderIds.Length; i++)
                    //{  
                    //}

                    UploadStatusList serverUploadStatusList = await serverHelper.UploadGetStatusListAsync(orderIdList, cancelToken);
                    foreach (UploadStatusListImageUploadRef serverImageState in serverUploadStatusList.ImageUploadRef)
                    {
                        UploadImageState current = currentUploadStatusList.FirstOrDefault<UploadImageState>(r => r.imageId == serverImageState.imageId);
                        if (current == null)
                        {
                            //Add to collection.
                            UploadImageState newImage = new UploadImageState();
                            newImage.error = serverImageState.error;
                            newImage.errorMessage = serverImageState.errorMessage;
                            newImage.lastUpdated = serverImageState.lastUpdated;
                            newImage.status = (UploadImage.ImageStatus)serverImageState.status;
                            newImage.name = serverImageState.name;
                        }
                        else
                        {
                            //Update current entry.
                            if (serverImageState.error)
                            {
                                current.error = true;
                                current.errorMessage = serverImageState.errorMessage;
                            }
                            else
                            {
                                current.error = false;
                                current.errorMessage = "";
                            }
                            current.lastUpdated = serverImageState.lastUpdated;
                            current.status = (UploadImage.ImageStatus)serverImageState.status;
                        }
                    }

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
                    ImageList tagImageList = await serverHelper.GetImageListAsync("tag", tagName, lastModified, cursor, state.userApp.FetchSize, searchQueryString, -1, cancelToken);
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
                    ImageList tagImageList = await serverHelper.GetImageListAsync("tag", tagName, null, cursor, state.userApp.FetchSize, searchQueryString, -1, cancelToken);
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

        async public Task TagAddRemoveImagesAsync(string tagName, ImageIdList moveList, bool add, CancellationToken cancelToken)
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
                    ImageList categoryImageList = await serverHelper.GetImageListAsync("category", categoryId.ToString(), localCategoryList.LastChanged, cursor, state.userApp.FetchSize, searchQueryString, -1, cancelToken);
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
                    ImageList categoryImageList = await serverHelper.GetImageListAsync("category", categoryId.ToString(), null, cursor, state.userApp.FetchSize, searchQueryString, -1, cancelToken);
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

        async public Task CategoryMoveImagesAsync(long categoryId, ImageIdList moveList, CancellationToken cancelToken)
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
                    ImageList galleryImageList = await serverHelper.GetImageListAsync("gallery", galleryName, lastModified, cursor, state.userApp.FetchSize, searchQueryString, sectionId, cancelToken);
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
                    ImageList galleryImageList = await serverHelper.GetImageListAsync("gallery", galleryName, null, cursor, state.userApp.FetchSize, searchQueryString, sectionId, cancelToken);
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

        public string GetGalleryUrl(string galleryName, string urlComplex)
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

        public string GetGalleryPreviewUrl(Gallery preview)
        {
            string queryString = "";
            //TODO build gallery query string logic.

            return serverHelper.GetWebUrl() + "galleryPreview?" + queryString;
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
