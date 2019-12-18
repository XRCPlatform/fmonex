using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace CertGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest("cn=fm.one", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.MaxValue);

            // Create PFX (PKCS #12) with private key
            File.WriteAllBytes("fm.one.pfx", cert.Export(X509ContentType.Pfx, ConfigurationManager.AppSettings["CertificatePassword"]));

            // Create Base 64 encoded CER (public key only)
            File.WriteAllText("fm.one.cer",
                "-----BEGIN CERTIFICATE-----\r\n"
                + Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
                + "\r\n-----END CERTIFICATE-----");
        }
    }
}
