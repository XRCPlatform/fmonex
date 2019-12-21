using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Pluralsight.Crypto;

namespace CertGen
{
    class Program
    {
        static void Main(string[] args)
        {
            using (CryptContext ctx = new CryptContext())
            {
                ctx.Open();

                X509Certificate2 certNew = ctx.CreateSelfSignedCertificate(
                    new SelfSignedCertProperties
                    {
                        IsPrivateKeyExportable = true,
                        KeyBitLength = 4096,
                        Name = new X500DistinguishedName("cn=fm.one"),
                        ValidFrom = DateTime.Today.AddDays(-1),
                        ValidTo = DateTime.MaxValue,
                    });

                File.WriteAllBytes("fm.one.pfx", certNew.Export(X509ContentType.Pfx, (string)null));

                var test = X509Certificate2.CreateFromCertFile("fm.one.pfx");

                File.WriteAllText("fm.one.cer",
                    "-----BEGIN CERTIFICATE-----\r\n"
                    + Convert.ToBase64String(certNew.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
                    + "\r\n-----END CERTIFICATE-----");
            }
        }
    }
}
