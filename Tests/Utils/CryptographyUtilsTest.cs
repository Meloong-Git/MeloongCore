using System.Security;
using System.Security.Cryptography;

namespace MeloongCore.Tests;
public class CryptographyUtilsTest : TestBase {

    #region 对称加密

    [Test]
    [Arguments("")]
    [Arguments("PCL")]
    [Arguments("中文内容 Test 123 !@#$%^&*()")]
    public async Task 对称加密_往返(string sourceString) {
        string encryptedString = CryptographyUtils.AesEncrypt(sourceString, "PCL-Key-中文");
        if (!string.IsNullOrEmpty(sourceString))
            await Assert.That(encryptedString != sourceString).IsTrue();
        await Assert.That(CryptographyUtils.AesDecrypt(encryptedString, "PCL-Key-中文")).IsEqualTo(sourceString);
    }

    [Test]
    public async Task 对称加密_随机化()
        => await Assert.That(CryptographyUtils.AesEncrypt("same source", "same key") != CryptographyUtils.AesEncrypt("same source", "same key")).IsTrue();

    [Test]
    public void 对称加密_篡改失败() {
        byte[] encryptedBytes = Convert.FromBase64String(CryptographyUtils.AesEncrypt("source", "key"));
        encryptedBytes[encryptedBytes.Length - 1] ^= 1;
        Assert.Throws<CryptographicException>(() => CryptographyUtils.AesDecrypt(Convert.ToBase64String(encryptedBytes), "key"));
    }

    [Test]
    public void AesDecrypt_TamperedIvFails() {
        byte[] encryptedBytes = Convert.FromBase64String(CryptographyUtils.AesEncrypt("source", "key"));
        encryptedBytes[1] ^= 1;
        Assert.Throws<CryptographicException>(() => CryptographyUtils.AesDecrypt(Convert.ToBase64String(encryptedBytes), "key"));
    }

    [Test]
    public void 对称加密_错误密钥失败() {
        Assert.Throws<CryptographicException>(() => CryptographyUtils.AesDecrypt(CryptographyUtils.AesEncrypt("source", "key"), "bad key"));
    }

    [Test]
    public void 对称加密_格式错误失败()
        => Assert.Throws<CryptographicException>(() => CryptographyUtils.AesDecrypt("not base64", "key"));

    #endregion

    #region ECDSA 签名

    [Test]
    public async Task ECDSA_字符串与标准API互通() {
        using CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256, null, new CngKeyCreationParameters {ExportPolicy = CngExportPolicies.AllowPlaintextExport});
        string publicKey = Convert.ToBase64String(key.Export(CngKeyBlobFormat.EccPublicBlob));
        string privateKey = Convert.ToBase64String(key.Export(CngKeyBlobFormat.EccPrivateBlob));
        const string text = "土豆码 / 配置签名 中文\r\n";
        byte[] data = Encoding.UTF8.GetBytes(text);
        string signature = CryptographyUtils.EcdsaSign(text, privateKey);
        using ECDsaCng ecdsa = new(key) {HashAlgorithm = CngAlgorithm.Sha256};
        await Assert.That(Convert.FromBase64String(signature).Length).IsEqualTo(64);
        await Assert.That(ecdsa.VerifyData(data, Convert.FromBase64String(signature))).IsTrue();
        CryptographyUtils.EcdsaVerify(text, Convert.ToBase64String(ecdsa.SignData(data)), publicKey);
        Assert.Throws<SecurityException>(() => CryptographyUtils.EcdsaVerify(text.Replace("\r\n", "\n"), signature, publicKey));
    }

    [Test]
    [Arguments(0)]
    [Arguments(63)]
    [Arguments(65)]
    public async Task ECDSA_错误签名长度被拒绝(int length) {
        using CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256);
        string publicKey = Convert.ToBase64String(key.Export(CngKeyBlobFormat.EccPublicBlob));
        Exception? error = null;
        try {
            CryptographyUtils.EcdsaVerify("test", Convert.ToBase64String(new byte[length]), publicKey);
        } catch (Exception ex) {
            error = ex;
        }
        await Assert.That(error).IsNotNull();
    }

    #endregion

}
