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
        private AuthUtil Auth;

        public RequestWrapper()
        {

        }

        private async Task<bool> PrepareAsync()
        {
            if(Auth != null)
            {
                try
                {
                    HttpWebRequest request = Auth.CreateRequest("/riotclient/ux-state");
                    HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                    string empty = ReadStream(response.GetResponseStream()).Trim('"');
                    return true;
                }
                catch (Exception)
                {
                    Auth = null;
                }
            }
            AuthUtil authUtil = GetAuth();
            if (authUtil != null)
            {
                Auth = authUtil;
                return true;
            }
            return false;
        }

        private AuthUtil GetAuth()
        {
            string lolPath = ClientLocator.GetLolPath();
            if (lolPath == null)
                return null;
            Dictionary<string, string> argsDict = null;
            try
            {
                argsDict = GetRemotingArgs(lolPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
            AuthUtil authRequest = new AuthUtil(argsDict["app-port"], argsDict["remoting-auth-token"]);
            return authRequest;
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

        public class NoClientException : Exception { }

        public async Task<string> GetGameflowPhaseAsync()
        {
            if (!await PrepareAsync())
                throw new NoClientException();
            HttpWebRequest request = Auth.CreateRequest("/lol-gameflow/v1/gameflow-phase");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string phase = ReadStream(response.GetResponseStream()).Trim('"');
            return phase;
        }

        public async Task<Json.Value> GetRunePagesAsync()
        {
            if (!await PrepareAsync())
                throw new NoClientException();
            HttpWebRequest request = Auth.CreateRequest("/lol-perks/v1/pages");
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            string result = ReadStream(response.GetResponseStream());
            Json.Value value = Json.Parser.Parse(result);
            return value;
        }

        public async Task<Json.Value> GetCurrentRunePageAsync()
        {
            if (!await PrepareAsync())
                throw new NoClientException();

            HttpWebRequest request = Auth.CreateRequest("/lol-perks/v1/currentpage");
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
            if (!await PrepareAsync())
                throw new NoClientException();

            HttpWebRequest request = Auth.CreateRequest(string.Format("/lol-perks/v1/pages/{0}", id));
            request.Method = "DELETE";
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            if(response.StatusCode == HttpStatusCode.NoContent)
                return true;
            return false;
        }

        public async Task<bool> AddRunePageAsync(Json.Value pageJson)
        {
            if (!await PrepareAsync())
                throw new NoClientException();

            HttpWebRequest request = Auth.CreateRequest("/lol-perks/v1/pages");
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

    }
}
