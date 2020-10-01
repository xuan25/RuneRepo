using System;
using System.Net;
using System.Text;

namespace RuneRepo.ClientUx
{
    class AuthUtil
    {
        private readonly string AppHost = "127.0.0.1";
        private readonly string AppPort;

        private readonly string Username = "riot";
        private readonly string RemotingAuthToken;

        private readonly string Authorization;

        private readonly System.Net.Security.RemoteCertificateValidationCallback RemoteCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CertificateValidation);

        public AuthUtil(string appPort, string remotingAuthToken)
        {
            AppPort = appPort;
            RemotingAuthToken = remotingAuthToken;
            Authorization = string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", Username, RemotingAuthToken))));
        }

        public HttpWebRequest CreateRequest(string target)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format("https://{0}:{1}{2}", AppHost, AppPort, target));
            httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, Authorization);
            httpWebRequest.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;
            return httpWebRequest;
        }

        private static bool CertificateValidation(object httpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Security.Cryptography.X509Certificates.X509Chain certChain, System.Net.Security.SslPolicyErrors policyErrors)
        {
            return true;
        }
    }
}
