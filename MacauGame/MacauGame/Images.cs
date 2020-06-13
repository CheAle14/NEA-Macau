using MacauEngine.Models.Enums;
using MacauGame.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauGame
{
    public static class Images
    {
        static Dictionary<string, Bitmap> cache;
        static Images()
        {
            cache = new Dictionary<string, Bitmap>();
            imagePath = Path.Combine(Program.AppDataPath, "images");
            if (!Directory.Exists(imagePath))
                Directory.CreateDirectory(imagePath);
        }

        static string imagePath;
        public static Bitmap GetImage(string name)
        {
            if (cache.TryGetValue(name, out var img))
                return img;
            Bitmap image = null;
            try
            {
                string fileName = Path.Combine(imagePath, name + ".png");
                if(File.Exists(fileName)) 
                    image = new Bitmap(fileName);
            } catch(Exception ex)
            {
                Log.Error($"GetImage-{name}", ex);
            }
            if(image == null)
            {
                var obj = Resources.ResourceManager.GetObject(name, System.Globalization.CultureInfo.CurrentCulture);
                image = (Bitmap)obj;
            }
            cache[name] = image;
            return image;
        }
        public static Bitmap GetImage(Suit house, Number value) => GetImage($"{house.ToString()[0]}_{(int)value}");

        public static void ClearCache()
        {
            cache = new Dictionary<string, Bitmap>();
        }

        #region Images
        public static Bitmap BACK => GetImage("BACK");

        #endregion
    }
}
