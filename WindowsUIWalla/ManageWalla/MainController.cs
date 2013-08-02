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


namespace ManageWalla
{
    public class MainController : IDisposable
    {
        private MainWindow currentMain;
        private GlobalState state = null;
        private ServerHelper serverHelper = null;
        private string userName = ConfigurationManager.AppSettings["UserName"];
        private static readonly ILog logger = LogManager.GetLogger(typeof(MainController));

        public MainController(MainWindow currentMainParam)
        {
            currentMain = currentMainParam;

            state = GlobalState.GetState(userName);

            state.categoryLoadState = GlobalState.DataLoadState.No;
            state.tagLoadState = GlobalState.DataLoadState.No;
            state.viewLoadState = GlobalState.DataLoadState.No;
            state.uploadStatusListState = GlobalState.DataLoadState.No;

            serverHelper = new ServerHelper(state);
        }

        public GlobalState GetState()
        {
            return state;
        }

        async public Task LoadImagesFromArray(String[] fileNames, UploadImageFileList meFots)
        {
            for (int i = 0; i < fileNames.Length; i++)
            {
                UploadImage newImage = new UploadImage();
                await newImage.Setup(fileNames[i]);
                meFots.Add(newImage);
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

            foreach (FileInfo file in imageDirectory.GetFiles().OfType<FileInfo>().Where(r => r.Extension.ToUpper() == ".JPG"))
            {
                UploadImage newImage = new UploadImage();
                await newImage.Setup(file.FullName);
                meFots.Add(newImage);
            }

            /*
            public Window()
            {
                InitializeComponent();

                ThreadPool.QueueUserWorkItem(LoadImage,
                     "http://z.about.com/d/animatedtv/1/0/1/m/simpf.jpg");
            }

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

        /// <summary>
        /// For each entity - Category, Tag, View List
        /// Check local cache for entries and check Walla Hub for updates
        /// Then refresh local data caches.
        /// </summary>
        public async Task RetrieveGeneralUserConfigAsync()
        {
            Task<string> tagListLoadedTask = RefreshTagsListAsync();

            string tagListLoadedOK = await tagListLoadedTask;
            if (tagListLoadedOK == "OK")
            {
                //Call dispatcher thread to run method TagListReloadFromState();
            }
            else
            {
                //Call dispatcher thread to run method TagListUpdateWorkingPane();
            }

            

            //TODO - Categotry and Views.

        }

        public void CreateCategoryFromFolder(DirectoryInfo currentFolder, UploadImageFileList meFots, long parentCategoryId)
        {
            long categoryId = serverHelper.CreateCategory(currentFolder.Name, "", parentCategoryId);
            foreach (UploadImage currentImage in meFots.OfType<UploadImage>().Where(r => r.FolderPath == currentFolder.FullName))
            {
                currentImage.Meta.categoryId = categoryId;
            }

            foreach (DirectoryInfo subFolder in currentFolder.GetDirectories())
            {
                CreateCategoryFromFolder(subFolder, meFots, categoryId);
            }
        }

        async public Task<string> DoUploadAsync(UploadImageFileList meFots, UploadUIState uploadState)
        {
            long rootCategoryId = 1;
            if (uploadState.UploadToNewCategory)
            {
                rootCategoryId = serverHelper.CreateCategory(uploadState.CategoryName, uploadState.CategoryDesc, uploadState.CategoryId);
            }
            else
            {
                rootCategoryId = uploadState.CategoryId;
            }

            if (uploadState.MapToSubFolders)
            {
                if (uploadState.UploadToNewCategory)
                {
                    //Assumption being that all sub categories will now need to be created.
                    DirectoryInfo rootFolder = new DirectoryInfo(uploadState.RootFolder);
                    CreateCategoryFromFolder(rootFolder, meFots, rootCategoryId);
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
                    currentImage.Meta.categoryId = rootCategoryId;
                }

                //Check for each chkAll box set to true, then replace respective values.
            }

            while (meFots.Count > 0)
            {
                string response = await serverHelper.UploadImageAsync(meFots[0]);
                if (response == null)
                {
                    //meFots.RemoveAt(0);
                        //Dispatcher.Invoke(DispatcherPriority.Send,

                    //theLabel.Invoke(new Action(() => theLabel.Text = "hello world from worker thread!"));

                    currentMain.uploadFots.Remove(meFots[0]);

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
                    currentMain.uploadFots[0].State = UploadImage.UploadState.Error;
                    currentMain.uploadFots[0].UploadError = response;
                }
            }
            return null;
        }

        async public Task<string> RefreshUploadStatusListAsync()
        {
            try
            {
                UploadStatusList uploadStatusList = await serverHelper.GetUploadStatusListAsync();
                state.uploadStatusList = uploadStatusList;
                state.uploadStatusListState = GlobalState.DataLoadState.Loaded;
                return "OK";
            }
            catch (Exception ex)
            {
                if (state.tagList != null)
                {
                    state.uploadStatusListState = GlobalState.DataLoadState.LocalCache;
                }
                else
                {
                    state.uploadStatusListState = GlobalState.DataLoadState.Unavailable;
                }
                return ex.Message;
            }
        }

        async public Task ResetMeFotsMeta(UploadImageFileList metFots)
        {
            foreach (UploadImage currentImage in metFots)
            {
                await currentImage.ResetMeta();
            }
        }

        async public Task<string> RefreshTagsListAsync()
        {
            try
            {
                TagList tagList = await serverHelper.GetTagsAvailableAsync();
                state.tagList = tagList;
                state.tagLoadState = GlobalState.DataLoadState.Loaded;
                return "OK";
            }
            catch (Exception ex)
            {
                if (state.tagList != null)
                {
                    state.tagLoadState = GlobalState.DataLoadState.LocalCache;
                }
                else
                {
                    state.tagLoadState = GlobalState.DataLoadState.Unavailable;
                }
                return ex.Message;
            }
        }

        public string UpdateTag(Tag newTag, string oldTagName)
        {
            return serverHelper.UpdateTag(newTag, oldTagName);
        }

        public string SaveNewTag(Tag tag)
        {
            return serverHelper.SaveNewTag(tag);
        }

        public Tag GetTagMeta(TagListTagRef tagRef)
        {
            return serverHelper.GetTagMeta(tagRef);
        }

        public string DeleteTag(Tag tag)
        {
            return serverHelper.DeleteTag(tag);
        }

        public void Dispose()
        {
            state.SaveState();
        }
    }
}
