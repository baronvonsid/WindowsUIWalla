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

namespace ManageWalla
{
    public class MainController : IDisposable
    {
        private MainWindow currentMain;
        private GlobalState state = null;
        private ServerHelper serverHelper = null;
        private const string userName = "simo1n";

        public MainController(MainWindow currentMainParam)
        {
            currentMain = currentMainParam;

            state = new GlobalState();
            state = GlobalState.GetState(userName);

            serverHelper = new ServerHelper(state);
        }

        public void LoadImagesFromArray(String[] fileNames, UploadImageFileList meFots)
        {
            for (int i = 0; i < fileNames.Length; i++)
            {
                meFots.Add(new UploadImage(fileNames[i]));
            }

        }

        public void LoadImagesFromFolder(DirectoryInfo imageDirectory, bool recursive, UploadImageFileList meFots)
        {
            if (recursive)
            {
                foreach (DirectoryInfo folder in imageDirectory.GetDirectories())
                {
                    LoadImagesFromFolder(folder, recursive, meFots);
                }
            }

            foreach (FileInfo file in imageDirectory.GetFiles().OfType<FileInfo>().Where(r => r.Extension.ToUpper() == ".JPG"))
            {
                meFots.Add(new UploadImage(file.FullName));
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
        /// For each entity - Category, Tag, View List, Account Settings
        /// Check local cache for entries and check Walla Hub for updates
        /// Then refresh local data caches.
        /// </summary>
        public void RetrieveGeneralUserConfig()
        {
            //GetCategoryTree();
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

        public void DoUpload(UploadImageFileList meFots, UploadUIState uploadState)
        {
            long rootCategoryId = 0;
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

            foreach (UploadImage currentImage in meFots)
            {
                serverHelper.UploadImage(currentImage.Meta, currentImage.FilePath);
            }
        }

        public void ResetMeFotsMeta(UploadImageFileList metFots)
        {
            foreach (UploadImage currentImage in metFots)
            {
                currentImage.ResetMeta();
            }
        }

        public TagList GetTagsAvailable()
        {
            return serverHelper.GetTagsAvailable();
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
