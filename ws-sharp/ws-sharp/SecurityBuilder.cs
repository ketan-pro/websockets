using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ws_sharp
{
    public class SecurityBuilder
    {
        public const string DefaultIssuer = "ApraLabs Root CA";

        public void build()
        {
            string fullyQualifiedName = "main_server.apralabs.com";

            List<string> subjectAlternateNames = new List<string>();
            /* you can either add by wildcard */
            //subjectAlternateNames.Add(“*.fullyqualified.domainname.com”); /* see https://www.digicert.com/subject-alternative-name.htm */

            /* or you can add by machine names on the farm */
            subjectAlternateNames.Add("localhost");
            subjectAlternateNames.Add("Machine1.apralabs.com");
            subjectAlternateNames.Add("Machine2.apralabs.com");
            subjectAlternateNames.Add("Machine3.apralabs.com");

            string privateKeyFilePassword = "@prA!a6sD3vTe@m";

            string rootFolder = "self_signed_certs";

            string certFileName = Path.Combine(rootFolder, fullyQualifiedName + ".pfx");
            string rootsigningCertFileName = Path.Combine(rootFolder, DefaultIssuer + ".pfx");

            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
            }

            if (System.IO.File.Exists(certFileName))
            {
                System.IO.File.Delete(certFileName);
            }

            if (System.IO.File.Exists(rootsigningCertFileName))
            {
                System.IO.File.Delete(rootsigningCertFileName);
            }

            MakeItSo(fullyQualifiedName, subjectAlternateNames, certFileName, privateKeyFilePassword, rootsigningCertFileName);
        }
        public void MakeItSo(string certificateName, List<string> subjectAlternateNames, string certificateFileName, string privateKeyFilePassword, string rootsigningCertFileName)
        {
            string issuerCnName = string.Format("CN={0}", DefaultIssuer);
            AsymmetricKeyParameter caPrivKey = GenerateCACertificate(issuerCnName, privateKeyFilePassword, rootsigningCertFileName);
            System.Security.Cryptography.X509Certificates.X509Certificate2 cert = GenerateSelfSignedCertificate(certificateName, subjectAlternateNames, certificateFileName, privateKeyFilePassword, issuerCnName, caPrivKey);
            AddCertToStore(cert, System.Security.Cryptography.X509Certificates.StoreName.My, System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine);
        }

        public System.Security.Cryptography.X509Certificates.X509Certificate2 GenerateSelfSignedCertificate(string certificateName, List<string> subjectAlternateNames, string certificateFileName, string privateKeyFilePassword, string issuerName, AsymmetricKeyParameter issuerPrivKey)
        {
            return GenerateSelfSignedCertificate(certificateName, subjectAlternateNames, certificateFileName, privateKeyFilePassword, issuerName, issuerPrivKey, 2048);
        }

        public System.Security.Cryptography.X509Certificates.X509Certificate2 GenerateSelfSignedCertificate(string certificateName, List<string> subjectAlternateNames, string certificateFileName, string privateKeyFilePassword, string issuerName, AsymmetricKeyParameter issuerPrivKey, int keyStrength)
        {
            string subjectName = string.Format("CN={0}", certificateName);

            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            X509V3CertificateGenerator certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Issuer and Subject Name
            var subjectDN = new X509Name(subjectName);

            // original code var issuerDN = issuerName;
            var issuerDN = new X509Name(issuerName);
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            // Valid For
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            KeyUsage keyUsage = new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment);
            certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, keyUsage);

            // Add the “Extended Key Usage” attribute, specifying “server authentication”.
            var usages = new[] { KeyPurposeID.IdKPServerAuth };
            certificateGenerator.AddExtension(
            X509Extensions.ExtendedKeyUsage.Id,
            false,
            new ExtendedKeyUsage(usages));

            /* DNS Name=*.fullyqualified.domainname.com */
            if (subjectAlternateNames.Count <= 1)
            {
                /* the <=1 is for the simple reason of showing an alternate syntax .. */
                foreach (string subjectAlternateName in subjectAlternateNames)
                {
                    GeneralName altName = new GeneralName(GeneralName.DnsName, subjectAlternateName);
                    GeneralNames subjectAltName = new GeneralNames(altName);
                    certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, subjectAltName);
                }
            }
            else
            {
                //Asn1Encodable[] ansiEncodeSubjectAlternativeNames = new Asn1Encodable[]
                // {
                // //new GeneralName(GeneralName.DnsName, “*.fullyqualified.domainname.com”),
                // new GeneralName(GeneralName.DnsName, “*.fullyqualified.domainname.com”)
                // };

                List<Asn1Encodable> asn1EncodableList = new List<Asn1Encodable>();
                foreach (string subjectAlternateName in subjectAlternateNames)
                {
                    asn1EncodableList.Add(new GeneralName(GeneralName.DnsName, subjectAlternateName));
                }

                DerSequence subjectAlternativeNamesExtension = new DerSequence(asn1EncodableList.ToArray());
                certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName.Id, false, subjectAlternativeNamesExtension);
            }

            // Subject Public Key
            AsymmetricCipherKeyPair subjectKeyPair;
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // Generating the Certificate
            var issuerKeyPair = subjectKeyPair;

            // selfsign certificate
            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", issuerKeyPair.Private, random);
            Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(signatureFactory);

            // correcponding private key
            PrivateKeyInfo pinfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);

            // merge into X509Certificate2
            var x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(certificate.GetEncoded());

            var seq = (Asn1Sequence)Asn1Object.FromByteArray(pinfo.ParsePrivateKey().GetDerEncoded());
            if (seq.Count != 9)
            {
                throw new PemException("malformed sequence in RSA private key");
            }

            RsaPrivateKeyStructure rsa = RsaPrivateKeyStructure.GetInstance(seq);
            RsaPrivateCrtKeyParameters rsaparams = new RsaPrivateCrtKeyParameters(
            rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, rsa.Coefficient);

            x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);

            File.WriteAllBytes(certificateFileName.Replace(".pfx", ".cer"), x509.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert));

            // Export Certificate with private key
            File.WriteAllBytes(certificateFileName, x509.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, privateKeyFilePassword));

            return x509;
        }

        public AsymmetricKeyParameter GenerateCACertificate(string subjectName, string privateKeyFilePassword, string rootsigningCertFileName, int keyStrength = 2048)
        {
            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Issuer and Subject Name
            var subjectDN = new X509Name(subjectName);
            var issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            // Valid For
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            KeyUsage keyUsage = new KeyUsage(KeyUsage.KeyCertSign | KeyUsage.CrlSign);
            certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, keyUsage);

            // Subject Public Key
            AsymmetricCipherKeyPair subjectKeyPair;
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // Generating the Certificate
            var issuerKeyPair = subjectKeyPair;

            // selfsign certificate
            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", issuerKeyPair.Private, random);
            Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(signatureFactory);
            System.Security.Cryptography.X509Certificates.X509Certificate2 x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(certificate.GetEncoded());

            #region Private Key

            // correcponding private key
            PrivateKeyInfo pinfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);

            var seq = (Asn1Sequence)Asn1Object.FromByteArray(pinfo.ParsePrivateKey().GetDerEncoded());
            if (seq.Count != 9)
            {
                throw new PemException("malformed sequence in RSA private key");
            }

            RsaPrivateKeyStructure rsa = RsaPrivateKeyStructure.GetInstance(seq);
            RsaPrivateCrtKeyParameters rsaparams = new RsaPrivateCrtKeyParameters(
            rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, rsa.Coefficient);

            x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);
            #endregion

            // Add CA certificate to Root store
            AddCertToStore(x509, System.Security.Cryptography.X509Certificates.StoreName.Root, System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine);

            File.WriteAllBytes(rootsigningCertFileName.Replace(".pfx", ".cer"), x509.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert));

            // Export Certificate with private key
            File.WriteAllBytes(rootsigningCertFileName, x509.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, privateKeyFilePassword));

            return issuerKeyPair.Private;
        }

        public bool AddCertToStore(System.Security.Cryptography.X509Certificates.X509Certificate2 cert, System.Security.Cryptography.X509Certificates.StoreName st, System.Security.Cryptography.X509Certificates.StoreLocation sl)
        {
            bool bRet = false;

            try
            {
                System.Security.Cryptography.X509Certificates.X509Store store = new System.Security.Cryptography.X509Certificates.X509Store(st, sl);
                store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadWrite);
                store.Add(cert);

                store.Close();
            }
            catch
            {
                throw;
            }

            return bRet;
        }
    }
}

