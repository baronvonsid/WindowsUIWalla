using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace ManageWalla
{
    [System.SerializableAttribute()]
    public class GalleryPresentationList : ObservableCollection<GalleryPresentationItem>
    {
        public GalleryPresentationList() { }
    }
}
