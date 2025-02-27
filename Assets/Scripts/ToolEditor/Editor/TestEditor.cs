using System;
using System.Security.Cryptography;
using System.Text;
using Game.Common;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using XC.RSAUtil;

namespace ToolEditor.Editor
{
    public class TestEditor : OdinEditorWindow
    {
        private const string PrivateKey = "MIIEpQIBAAKCAQEAr2R4XRoE30TcpgaNos6Mh7kQBO2X0hFOd1VOJzyhdflhwYVxfHC/sftE+mryxn42L8SMxQtRgUD8pbAdRbqM4tktcyn26uVeQpduIsudGq8CZVV+liq/X5w8XnmCknBR96i2/24gptnCExELB8aY+owTsKHwmH/W1/8uEzuS83ApwXQjqXiZod13aI0lE8De9UYFI2Y1wWJ/yywf5JixlCSbuoyWOPL2rQWtoUtoeoTKZFnMLY1oCERDxWbj/fVxiOrIR/ltri3aFB2LwhZa6Jb7wmOBe66WtWoVLhL4D85wJyx3sJYFghu2uJe195dFHQV72FD9iLLw2WNTmn9HgQIDAQABAoIBAAK3gd0gqeJS0j9+ICkynYmoAQqv1Xy1s5X0oSfJcCTVg+v3/F3gRI/mDleksCBeqcnhTQmWVyEIl9ach5o3aAxG2fCL6LJoctEsQFKoqG/WiykfrbzMv7ckQpPMumGN/Nm4aCaabEYOtHYQJbAX+vGn7tF7HgYhYNDADVTTROpMFBF0AWYV56rwJ9Xl7WsPZ01l3oLnBp6FDwxFv/EPaB12waXuJ7eLhK6nxSVYSC+FHfr+2ii6WV2qGdTYsiOGCQYhGht04k3ezIos01u/OaGG1iuxKry6t0deQ1akjytfdnlY9jTR4dTYdEK9wTipea+JrGxmdyRkc1umbbXOV+ECgYEA5JSFrfU0ycu6PYT4Mw4iz3o0/Q1sbtVy6Gmm9vgitAKKxPqSaAECwggfdBP3enlNBzQghisZeIPQw4IdtPUF46UNO10Vd0NYu4ibicJs/BYVIJCrVthZzksIjZYyh4J+V+d04t019Jziqb0YmzA5jrkEL2ccOnmxG+PpuzFaizkCgYEAxG6axmCXmKZPlP6qmEAqxFcsHwyixWWmUTwUwwI6btP/Wmwi9CrvGPy123YwHPWVXIZMvG1/zmUbEpRv+v1KtCMhMyueINhAJf2b7m/RCOShSU21tGTYdVg2u9v9ZzJUCsNpLur5Lq3BnnA1L0SbB4u3VkIpUb3eCzw40k9S9okCgYEAzn0JAevH4x/CP37WRDDZ63mnUo4EzV7PLfr7VJE1sE23lFgTWBbJqgRyfYboAmB/4CkL1IgsuzzAo0zwjx7lUg8xE9Zz5MgW6Vpvv5O+pj9AmKl3zr0k64HG2Ti8rsOIZNp3MdXJvw7Wh6WGC/MVm8Oxby0DSCPUhbBu3aao96kCgYEAv1oHiCxcQSXx7HDQ+pO3laBdqLmEbssA462lg/pNtdzqqckhm72fQTYcafeOEwfhQkrJwdzhXcv8PXNASr4n4ac/FjvtRI2kn92X4wQmG4Ws4F3FHkAG5PjUCNja14adfAa0FjJsH17AeHlSNgOdChK+vFajLa/J+CPoLmd7qBECgYEAtbdBdLRnte02XemTDMTr9VrgxXdlPJRiL9cJq7EDpxTmaoID58u6i9H6kB62kvdydSs2DLxJdSEVgp/tPBE6Tl3oR/+jX7llEmhljeVj3+K18N0CqZyLk5cu7amyr4gpVpHW2uc7UTOGPD+FPg/L+a5bgCkmHxF5mgg9i/+bJB8=";
        private const string PublicKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAr2R4XRoE30TcpgaNos6Mh7kQBO2X0hFOd1VOJzyhdflhwYVxfHC/sftE+mryxn42L8SMxQtRgUD8pbAdRbqM4tktcyn26uVeQpduIsudGq8CZVV+liq/X5w8XnmCknBR96i2/24gptnCExELB8aY+owTsKHwmH/W1/8uEzuS83ApwXQjqXiZod13aI0lE8De9UYFI2Y1wWJ/yywf5JixlCSbuoyWOPL2rQWtoUtoeoTKZFnMLY1oCERDxWbj/fVxiOrIR/ltri3aFB2LwhZa6Jb7wmOBe66WtWoVLhL4D85wJyx3sJYFghu2uJe195dFHQV72FD9iLLw2WNTmn9HgQIDAQAB";
        //显示窗口
        [MenuItem("Tools/TestEditor")]
        private static void OpenWindow()
        {
            GetWindow<TestEditor>();
        }
        
        [Button("测试Rsa")]
        private void TestKnnSplit(string deviceUniqueIdentifier,int minute)
        {
            var unixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var expireTime = unixTimeMilliseconds + minute * 60 * 1000;
            var rsaPublic = new RsaPkcs1Util(Encoding.UTF8,PublicKey);
            var rsaPrivate = new RsaPkcs1Util(Encoding.UTF8,null,PrivateKey);
            var encrypt = rsaPublic.Encrypt($"{deviceUniqueIdentifier}@{expireTime}", RSAEncryptionPadding.Pkcs1);
            Debug.LogError(encrypt);
            var decrypt = rsaPrivate.Decrypt(encrypt, RSAEncryptionPadding.Pkcs1);
            Debug.LogError(decrypt);
        }
        [Button("清理许可证")]
        private void ClearLicense()
        {
            PlayerPrefs.DeleteKey("ZombieKillerGameLicense");
            PlayerPrefs.Save();
        }
    }
}