using JsonUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RuneRepo.ClientUx
{
    class RequestWrapper
    {
        public bool IsAvaliable
        {
            get
            {
                try
                {
                    GetAuthTokenAsync().Wait();
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
        }

        private readonly AuthRequestUtil AuthRequest;

        public RequestWrapper(string lolPath)
        {
            Dictionary<string, string> argsDict = GetRemotingArgs(lolPath);
            AuthRequest = new AuthRequestUtil(argsDict["app-port"], argsDict["remoting-auth-token"]);
        }

        public async Task<bool> CheckAvaliableAsync()
        {
            try
            {
                await GetAuthTokenAsync();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }



        public async Task<string> GetAuthTokenAsync()
        {
            HttpWebRequest request = AuthRequest.CreateRequest("/riotclient/auth-token");
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            string token = ReadStream(response.GetResponseStream()).Trim('"');
            return token;
        }

        public async Task<Json.Value> GetRunePages()
        {
            HttpWebRequest request = AuthRequest.CreateRequest("/lol-perks/v1/pages");
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            string result = ReadStream(response.GetResponseStream());
            Json.Value value = Json.Parser.Parse(result);
            return value;
        }

        public async Task<Json.Value> GetCurrentRunePageAsync()
        {
            HttpWebRequest request = AuthRequest.CreateRequest("/lol-perks/v1/currentpage");
            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                string result = ReadStream(response.GetResponseStream());
                Json.Value value = Json.Parser.Parse(result);
                return value;
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public async Task<bool> DeleteRunePageAsync(ulong id)
        {
            HttpWebRequest request = AuthRequest.CreateRequest(string.Format("/lol-perks/v1/pages/{0}", id));
            request.Method = "DELETE";
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            if(response.StatusCode == HttpStatusCode.NoContent)
                return true;
            return false;
        }

        public async Task<bool> AddRunePageAsync(Json.Value pageJson)
        {
            HttpWebRequest request = AuthRequest.CreateRequest("/lol-perks/v1/pages");
            request.Method = "POST";

            byte[] data = Encoding.UTF8.GetBytes(pageJson.ToString());
            Stream newStream = request.GetRequestStream();
            newStream.Write(data, 0, data.Length);
            newStream.Close();

            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                    return true;
                return false;
            }
            catch (WebException ex)
            {
                return false;
            }
            
        }

        private string ReadStream(Stream stream)
        {
            using (StreamReader streamReader = new StreamReader(stream))
            {
                string result = streamReader.ReadToEnd();
                return result;
            }
        }

        private Dictionary<string, string> GetRemotingArgs(string lolPath)
        {
            DirectoryInfo lolDirectoryInfo = new DirectoryInfo(lolPath);

            if (!lolDirectoryInfo.Exists)
                throw new Exception("LoL directory not found.");

            string lockfilePath = Path.Combine(lolDirectoryInfo.FullName, "lockfile");
            if (!File.Exists(lockfilePath))
                throw new Exception("Lockfile not found.");

            string lockfileContent = null;
            using (FileStream fileStream = new FileStream(lockfilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    lockfileContent = streamReader.ReadToEnd();
                }
            }
            string[] paramArr = lockfileContent.Split(':');

            Dictionary<string, string> argsDict = new Dictionary<string, string>
            {
                { "app-name", paramArr[0] },
                { "app-pid", paramArr[1] },
                { "app-port", paramArr[2] },
                { "remoting-auth-token", paramArr[3] },
                { "remoting-protocal", paramArr[4] }
            };

            return argsDict;
        }
    }
}
