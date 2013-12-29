﻿using System.Collections.ObjectModel;

namespace ManageWalla
{
    public sealed class GalleryCategoryModel
    {
        public ObservableCollection<CategoryItem> CategoryItems { get; set; }

        public GalleryCategoryModel()
        {
            /*
            var redMeat = new CategoryItem { name = "notsimon" };
            redMeat.Add(new CategoryItem { name = "Beef" });
            redMeat.Add(new CategoryItem { name = "Buffalo" });
            redMeat.Add(new CategoryItem { name = "Lamb" });

            var whiteMeat = new CategoryItem { name = "Whites" };
            whiteMeat.Add(new CategoryItem { name = "Chicken" });
            whiteMeat.Add(new CategoryItem { name = "Duck" });
            whiteMeat.Add(new CategoryItem { name = "Pork" });
            var meats = new CategoryItem { name = "Meats", Children = { redMeat, whiteMeat } };

            var veggies = new CategoryItem { name = "Vegetables" };
            veggies.Add(new CategoryItem { name = "Potato" });
            veggies.Add(new CategoryItem { name = "Corn" });
            veggies.Add(new CategoryItem { name = "Spinach" });

            var fruits = new CategoryItem { name = "Fruits" };
            fruits.Add(new CategoryItem { name = "Apple" });
            fruits.Add(new CategoryItem { name = "Orange" });
            fruits.Add(new CategoryItem { name = "Pear" });
            */

            CategoryItems = new ObservableCollection<CategoryItem> { };
        }

        public GalleryCategoryModel(string simon)
        {

            var redMeat = new CategoryItem { name = simon };
            redMeat.Add(new CategoryItem { name = "Beef" });
            redMeat.Add(new CategoryItem { name = "Buffalo" });
            redMeat.Add(new CategoryItem { name = "Lamb" });

            var whiteMeat = new CategoryItem { name = "Whites" };
            whiteMeat.Add(new CategoryItem { name = "Chicken" });
            whiteMeat.Add(new CategoryItem { name = "Duck" });
            whiteMeat.Add(new CategoryItem { name = "Pork" });
            var meats = new CategoryItem { name = "Meats", CategoryItems = { redMeat, whiteMeat } };

            var veggies = new CategoryItem { name = "Vegetables" };
            veggies.Add(new CategoryItem { name = "Potato" });
            veggies.Add(new CategoryItem { name = "Corn" });
            veggies.Add(new CategoryItem { name = "Spinach" });

            var fruits = new CategoryItem { name = "Fruits" };
            fruits.Add(new CategoryItem { name = "Apple" });
            fruits.Add(new CategoryItem { name = "Orange" });
            fruits.Add(new CategoryItem { name = "Pear" });


            CategoryItems = new ObservableCollection<CategoryItem> { fruits, veggies, meats };
        }
    }

    public sealed class CategoryItem
    {
        public long id { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public int selectionIndex { get; set; } //0 - not selected, 1 - selected, 2 - recursive.
        public bool enabled { get; set; }
        public int imageCount { get; set; }
        public long parentId { get; set; }

        public string tooltip
        {
            get 
            {
                return ((desc != null) ? desc + ".  " : "") + "Foto count: " + imageCount.ToString() + " isenabled=" + enabled.ToString();
            }
        }

        public ObservableCollection<CategoryItem> CategoryItems { get; set; }

        public CategoryItem()
        {
            CategoryItems = new ObservableCollection<CategoryItem>();
        }
        public void Add(CategoryItem item)
        {
            CategoryItems.Add(item);
        }
    }
}