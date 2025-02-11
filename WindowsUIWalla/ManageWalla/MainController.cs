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
using System.Reflection;
using System.Deployment;

/*
    - Code refactor - Done Upload.
 
 */

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
        private GalleryStyleList galleryStyleList = null;
        private GalleryPresentationList galleryPresentationList = null;
        private bool createdCategory = false;
        private ServerHelper serverHelper = null;
        private static readonly ILog logger = LogManager.GetLogger(typeof(MainController));

        public MainController() 
        {
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

        async public void Dispose()
        {
            if (state == null)
                return;

            await Logout();

            SaveCacheToDisk();
        }

        private void SaveCacheToDisk()
        {
            if (state != null && state.account != null && state.account.ProfileName.Length > 0)
            {
                CacheHelper.SaveGlobalState(state, state.account.ProfileName);
                CacheHelper.SaveThumbCacheList(thumbCacheList, state.account.ProfileName);
                CacheHelper.SaveMainCopyCacheList(mainCopyCacheList, state.account.ProfileName);
                CacheHelper.SaveUploadImageStateList(uploadImageStateList, state.account.ProfileName);
            }
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

        public void SetUpCacheFiles(string profileName, UploadImageStateList uploadImageStateListParam, 
            GalleryPresentationList presentationListParam, GalleryStyleList styleListParam)
        {
            state = CacheHelper.GetGlobalState(profileName);
            thumbCacheList = CacheHelper.GetThumbCacheList(profileName);
            mainCopyCacheList = CacheHelper.GetMainCopyCacheList(profileName);

            CacheHelper.GetUploadImageStateList(uploadImageStateListParam, profileName);
            uploadImageStateList = uploadImageStateListParam;

            CacheHelper.GalleryPresentationPopulateFromState(state, presentationListParam);
            galleryPresentationList = presentationListParam;

            CacheHelper.GalleryStylePopulateFromState(state, styleListParam);
            galleryStyleList = styleListParam;

            //Clear out temp upload folder
            CreateClearTempFolder(profileName);

        }

        async public Task<bool> CheckOnline()
        {
            return await serverHelper.isOnline(Properties.Settings.Default.WebServerTest);
        }

        async public Task<Logon> GetLogonToken()
        {
            int major = 0;
            int minor = 0;
            GetVersion(ref major, ref minor);

            //TODO Get file size properly.

            System.OperatingSystem osInfo = System.Environment.OSVersion;
            //AssemblyName currentAssembly = Assembly.GetExecutingAssembly().GetName();
            //FileInfo file = new FileInfo(currentAssembly.EscapedCodeBase);

            /*Set app detail, including current platform.*/
            AppDetail appDetail = new AppDetail();
            appDetail.AppKey = Properties.Settings.Default.WallaAppKey;
            appDetail.AppCRC = 101;  // file.Length;
            appDetail.AppMajorVersion = major;
            appDetail.AppMinorVersion = minor;
            appDetail.PlatformOS = Properties.Settings.Default.OS;
            appDetail.PlatformType = "PC";
            appDetail.OSMajorVersion = osInfo.Version.Major;
            appDetail.OSMinorVersion = osInfo.Version.Minor;

            Logon logon = await serverHelper.GetLogonToken(appDetail);
            if (logon == null || logon.Key.Length != 32)
                return null;

            return logon;
        }

        private void GetVersion(ref int major, ref int minor)
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            major = version.Major;
            minor = version.Minor;
        }

        async public Task<string> Logon(string logonToken, string profileName, string email, string password)
        {
            Logon logon = new Logon();
            logon.Key = logonToken;

            //Logon logon = await serverHelper.GetLogonToken();
            //if (logon == null || logon.Key.Length != 32)
            //    return "Logon failed";

            //logon.ProfileName = profileName;
            if (profileName.Length > 0)
                logon.ProfileName = profileName;
            else
                logon.Email = email;

            logon.Password = password;

            if (await serverHelper.Logon(logon))
                return "OK";
            else
                return "Logon failed";
        }

        async public Task Logout()
        {
            await serverHelper.Logout();
            //Ignore response, we need to close it out regardless.

            SaveCacheToDisk();
            CacheHelper.ResetGlobalState(state);
        }

        public void LogoutFromOffline()
        {
            CacheHelper.ResetGlobalState(state);
        }

        async public Task AccountDetailsGet(CancellationToken cancelToken)
        {
            try
            {
                Account account = await serverHelper.AccountGet(cancelToken);
                state.account = account;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("AccountDetailsGet has been cancelled");
                throw cancelEx;
            }
        }

        async public Task<bool> SetUserApp(CancellationToken cancelToken)
        {
            long userAppId;
            UserApp userApp = null;
            UserApp newUserApp = null;

            if (state.userApp == null)
            {
                //TODO add find existing user app function.
                newUserApp = UserAppInitialiseObject(state.userApp);
                userAppId = await serverHelper.UserAppCreateUpdateAsync(newUserApp, cancelToken);

                //New user app created ok, retrieve the values now.
                userApp = await serverHelper.UserAppGet(userAppId);
            }
            else
            {
                userApp = await serverHelper.UserAppGet(state.userApp.id);
                if (userApp == null)
                {
                    //Problem with existing userapp.
                    //Create a new user app, use existing values if possible.
                    newUserApp = UserAppInitialiseObject(state.userApp);
                    userAppId = await serverHelper.UserAppCreateUpdateAsync(newUserApp, cancelToken);

                    //New user app created ok, retrieve the values now.
                    userApp = await serverHelper.UserAppGet(userAppId);
                }
            }

            if (userApp.Blocked)
                return false;

            state.userApp = userApp;
            return true;
            
        }

        private UserApp UserAppInitialiseObject(UserApp existing)
        {
            string uploadFolderDefault = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), state.account.ProfileName + " auto upload (fw)");
            string copyFolderDefault = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), state.account.ProfileName + " copies (fw)");

            UserApp newUserApp = new UserApp();
            newUserApp.MachineName = System.Environment.MachineName;

            if (existing != null)
            {
                newUserApp.AutoUpload = existing.AutoUpload;

                if (Directory.Exists(existing.AutoUploadFolder))
                    newUserApp.AutoUploadFolder = existing.AutoUploadFolder;
                else
                    newUserApp.AutoUploadFolder = uploadFolderDefault;

                if (Directory.Exists(existing.MainCopyFolder))
                    newUserApp.MainCopyFolder = existing.MainCopyFolder;
                else
                    newUserApp.MainCopyFolder = copyFolderDefault;

                newUserApp.MainCopyCacheSizeMB = existing.MainCopyCacheSizeMB;
                newUserApp.ThumbCacheSizeMB = existing.ThumbCacheSizeMB;
            }
            else
            {
                newUserApp.AutoUpload = true;
                newUserApp.AutoUploadFolder = uploadFolderDefault;
                newUserApp.MainCopyFolder = copyFolderDefault;
            }

            /*Before sending to the server, ensure these are valid.*/
            if (!Directory.Exists(newUserApp.AutoUploadFolder))
                Directory.CreateDirectory(newUserApp.AutoUploadFolder);

            if (!Directory.Exists(newUserApp.MainCopyFolder))
                Directory.CreateDirectory(newUserApp.MainCopyFolder);

            return newUserApp;
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
        }
        
        public string AccountNewUserUrl()
        {
            return serverHelper.GetWebUrl(false) + "newaccount?accountType=1";
        }

        public string AccountForgotPasswordUrl()
        {
            return serverHelper.GetWebUrl(false) + "forgotpassword";
        }

        async public Task<string> GetAccountSettingsUrlAsync(CancellationToken cancelToken)
        {
            try
            {
                string logonToken = await serverHelper.AccountGetPassThroughTokenAsync(cancelToken);
                return serverHelper.GetWebUrl(true) + "settings/account?logonToken=" + WebUtility.UrlEncode(logonToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryCreatePreviewAsync has been cancelled");
                throw cancelEx;
            }
        }

        private void CreateClearTempFolder(String profileName)
        {
            string tempFolderName = Path.Combine(System.Windows.Forms.Application.UserAppDataPath, profileName + "-temp");

            if (Directory.Exists(tempFolderName))
            {
                string[] filesToDelete = Directory.GetFiles(tempFolderName, "*.tmp");
                for (int i = 0; i < filesToDelete.Length; i++)
                {
                    File.Delete(filesToDelete[i]);
                }
            }
            else
            {
                Directory.CreateDirectory(tempFolderName);
            }
        }
        #endregion

        #region Upload Methods
        async public Task<List<string>> LoadImagesFromArray(String[] fileNames, UploadImageFileList meFots, bool loadThumb, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            List<string> errorResponses = new List<string>();
            try
            {
                for (int i = 0; i < fileNames.Length; i++)
                {
                    UploadImage newImage = new UploadImage();
                    string response = await newImage.Setup(fileNames[i], loadThumb);
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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.LoadImagesFromArray()", (int)duration.TotalMilliseconds, ""); }
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
            DateTime startTime = DateTime.Now;
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

                foreach (UploadImage currentImage in meFots)
                {
                    if (uploadState.MetaTagRefAll)
                        currentImage.Meta.Tags = uploadState.MetaTagRef;

                    if (uploadState.MetaTakenDateSetAll)
                    {
                        currentImage.Meta.TakenDate = uploadState.MetaTakenDate;
                        currentImage.Meta.TakenDateSet = true;
                    }

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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.UploadManualAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async public Task<List<string>> UploadProcessAsync(UploadImageFileList meFots, bool isAuto, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
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

                    await currentUpload.CompressFile(state.account.ProfileName);

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

                    await currentUpload.RemoveCompressedFile(); 

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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.UploadProcessAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async public Task UploadAutoAsync(UploadImageFileList meFots, UploadUIState uploadState, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.UploadAutoAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        private void AddMachineTag(UploadImage current)
        {
            ImageMetaTagRef newTagRef = new ImageMetaTagRef();
            newTagRef.id = state.userApp.TagId;

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

        async public Task<List<string>> LoadImagesFromFolder(DirectoryInfo imageDirectory, bool recursive, UploadImageFileList meFots, bool loadThumb, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            List<string> errorResponses = new List<string>();
            try
            {
                if (recursive)
                {
                    foreach (DirectoryInfo folder in imageDirectory.GetDirectories())
                    {
                        List<string> tempErrorResponses = await LoadImagesFromFolder(folder, recursive, meFots, loadThumb, cancelToken);
                        errorResponses.AddRange(tempErrorResponses);
                    }
                }

                foreach (FileInfo file in imageDirectory.GetFiles().OfType<FileInfo>())
                {
                    if (IsFormatOK(file.Extension.ToUpper().Substring(1)))
                    {
                        UploadImage newImage = new UploadImage();
                        string response = await newImage.Setup(file.FullName, loadThumb);
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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.LoadImagesFromFolder()", (int)duration.TotalMilliseconds, imageDirectory); }
            }
        }

        async public Task<long> CountImagesFromFolder(DirectoryInfo imageDirectory, bool recursive, long count, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                if (recursive)
                {
                    foreach (DirectoryInfo folder in imageDirectory.GetDirectories())
                    {
                        count = count + await CountImagesFromFolder(folder, recursive, count, cancelToken);
                    }
                }

                foreach (FileInfo file in imageDirectory.GetFiles().OfType<FileInfo>())
                {
                    if (IsFormatOK(file.Extension.ToUpper().Substring(1)))
                    {
                        count = count + 1;
                    }
                }

                return count;
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CountImagesFromFolder has been cancelled");
                throw cancelEx;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.CountImagesFromFolder()", (int)duration.TotalMilliseconds, imageDirectory); }
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
            DateTime startTime = DateTime.Now;            

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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.CheckImagesForAutoUploadAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        //TODO Sort out with date modified or a way or limiting the number of requests needed.
        async public Task RefreshUploadStatusListAsync(long[] orderIds, CancellationToken cancelToken, UploadImageStateList currentUploadStatusList)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    ImageIdList orderIdList = new ImageIdList();
                    orderIdList.ImageRef = orderIds;

                    UploadStatusList serverUploadStatusList = await serverHelper.UploadGetStatusListAsync(orderIdList, cancelToken);
                    if (serverUploadStatusList.ImageUploadRef != null)
                    {
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
                                newImage.imageId = serverImageState.imageId;
                                newImage.sizeBytes = 0;
                                newImage.fileName = "";
                                newImage.fullPath = "";

                                currentUploadStatusList.Add(newImage);  //Check this is OK.
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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.RefreshUploadStatusListAsync()", (int)duration.TotalMilliseconds, ""); }
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
            DateTime startTime = DateTime.Now;
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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.TagRefreshListAsync()", (int)duration.TotalMilliseconds, ""); }
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
            DateTime startTime = DateTime.Now;
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
                logger.Debug("TagGetImagesAsync has been cancelled");
                throw cancelEx;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.TagGetImagesAsync()", (int)duration.TotalMilliseconds, ""); }
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
                logger.Debug("TagGetMetaAsync has been cancelled");
                throw cancelEx;
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
        }

        async public Task<ImageList> CategoryGetImagesAsync(long categoryId, int cursor, string searchQueryString, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.CategoryGetImagesAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async public Task CategoryRefreshListAsync(CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
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
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("CategoryRefreshListAsync has been cancelled.");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                state.categoryLoadState = GlobalState.DataLoadState.Unavailable;
                throw (ex);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.CategoryRefreshListAsync()", (int)duration.TotalMilliseconds, ""); }
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
        }
        #endregion

        #region Gallery
        async public Task GalleryRefreshListAsync(CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.GalleryRefreshListAsync()", (int)duration.TotalMilliseconds, ""); }
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
                logger.Debug("GalleryGetMetaAsync has been cancelled");
                throw cancelEx;
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
        }

        async public Task<ImageList> GalleryGetImagesAsync(long id, string galleryName, int cursor, long sectionId, string searchQueryString, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
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
                logger.Debug("GalleryGetImagesAsync has been cancelled");
                throw cancelEx;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.GalleryGetImagesAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async public Task<string> GetGalleryLogonUrlAsync(string galleryName, CancellationToken cancelToken)
        {
            try
            {
                string logonToken = await serverHelper.GalleryGetLogonTokenAsync(galleryName, cancelToken);
                return serverHelper.GetWebUrl(true) + "gallery/" + galleryName + "?logonToken=" + WebUtility.UrlEncode(logonToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryCreatePreviewAsync has been cancelled");
                throw cancelEx;
            }
        }

        public string GetGalleryUrl(string galleryName, string complexUrl)
        {
            if (complexUrl.Length > 0)
                return serverHelper.GetWebUrl(true) + "gallery/" + galleryName + "?key=" + WebUtility.UrlEncode(complexUrl);
            else
                return serverHelper.GetWebUrl(true) + "gallery/" + galleryName;
        }

        async public Task<string> GalleryCreatePreviewAsync(Gallery gallery, CancellationToken cancelToken)
        {
            try
            {
                return await serverHelper.GalleryCreatePreviewAsync(gallery, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryCreatePreviewAsync has been cancelled");
                throw cancelEx;
            }
        }

        public string GetGalleryPreviewUrl(string galleryPreviewKey)
        {
            return serverHelper.GetWebUrl(true) + "gallery/stylepreview?key=" + WebUtility.UrlEncode(galleryPreviewKey);
        }

        async public Task GalleryOptionRefreshAsync(GalleryPresentationList presentationList, GalleryStyleList styleList, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    GalleryOption GalleryOption;
                    if (state.GalleryOption != null)
                    {
                        GalleryOption = await serverHelper.GalleryGetOptionsAsync(state.GalleryOption.lastChanged, cancelToken);
                        if (GalleryOption == null)
                        {
                            return;
                        }
                        else
                        {
                            state.GalleryOption = GalleryOption;
                        }
                    }
                    else
                    {
                        GalleryOption = await serverHelper.GalleryGetOptionsAsync(null, cancelToken);
                        if (GalleryOption == null)
                        {
                            throw new Exception("Unexpected problem loading the gallery options, none populated");
                        }
                        else
                        {
                            state.GalleryOption = GalleryOption;
                        }
                    }

                    //Rebuild Prentation Object.
                    presentationList.Clear();
                    foreach (GalleryOptionPresentationRef current in state.GalleryOption.Presentation)
                    {
                        GalleryPresentationItem newItem = new GalleryPresentationItem();
                        newItem.PresentationId = current.presentationId;
                        newItem.Name = current.name;
                        newItem.Desc = current.description;
                        newItem.MaxSections = current.maxSections;
                        presentationList.Add(newItem);
                    }

                    styleList.Clear();
                    foreach (GalleryOptionStyleRef current in state.GalleryOption.Style)
                    {
                        GalleryStyleItem newItem = new GalleryStyleItem();
                        newItem.StyleId = current.styleId;
                        newItem.Name = current.name;
                        newItem.Desc = current.description;
                        styleList.Add(newItem);
                    }

                    //Load images.
                    foreach (GalleryPresentationItem item in presentationList)
                    {
                        await item.LoadImageAsync(cancelToken,serverHelper);
                    }

                    foreach (GalleryStyleItem item in styleList)
                    {
                        await item.LoadImageAsync(cancelToken, serverHelper);
                    }

                    state.galleryPresentationList = presentationList;
                    state.galleryStyleList = styleList;
                }
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryOptionRefreshAsync has been cancelled.");
                throw (cancelEx);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.GalleryOptionRefreshAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async public Task GalleryGetSectionListAndMerge(Gallery gallery, GallerySectionList gallerySectionList, bool isReset, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                //Retrieve server sections list.
                Gallery serverGallery = await serverHelper.GalleryGetSections(gallery, cancelToken);

                if (isReset)
                    gallerySectionList.Clear();

                //Loop through existing list, remove entries not present server side.
                int existingCounter = gallerySectionList.Count-1;
                while (existingCounter >= 0)
                {
                    GallerySectionItem current = gallerySectionList[existingCounter];

                    GallerySectionRef found = serverGallery.Sections.FirstOrDefault<GallerySectionRef>(r => r.id == current.sectionId);
                    if (found == null)
                        gallerySectionList.Remove(current);

                    existingCounter--;
                }

                //Loop through server list and add any new entries.
                foreach (GallerySectionRef current in serverGallery.Sections)
                {
                    GallerySectionItem found = gallerySectionList.FirstOrDefault<GallerySectionItem>(r => r.sectionId == current.id);
                    if (found == null)
                    {
                        GallerySectionItem newSection = new GallerySectionItem();
                        newSection.sectionId = current.id;
                        newSection.name = current.name;
                        newSection.nameOveride = current.name;
                        newSection.desc = current.desc;
                        newSection.descOveride = current.desc;
                        newSection.sequence = 0;

                        gallerySectionList.Add(newSection);
                    }
                }
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("GalleryGetSectionListAndMerge has been cancelled");
                throw cancelEx;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainController.GalleryGetSectionListAndMerge()", (int)duration.TotalMilliseconds, ""); }
            }
        }
        #endregion

        #region  Images Processing
        async public Task DeleteImagesAsync(ImageIdList imageList, CancellationToken cancelToken)
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
        }
        #endregion
    }
}
