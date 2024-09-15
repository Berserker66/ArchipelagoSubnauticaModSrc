using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Archipelago
{
    public class APLastConnectInfo
    {
        public string host_name;
        public string game_name;
        public string slot_name;
        public string password;

        public bool Valid
        {
            get => host_name != "" && slot_name != "";
        }
        
        public static APLastConnectInfo LoadFromFile(string path)
        {
            if (File.Exists(path))
            {
                var reader = File.OpenText(path);
                var content = reader.ReadToEnd();
                reader.Close();
                return JsonConvert.DeserializeObject<APLastConnectInfo>(content);
            }
            return null;
        }

        public void WriteToFile(string path)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
            Platform.IO.File.WriteAllBytes(path, bytes);
        }
    }
}
