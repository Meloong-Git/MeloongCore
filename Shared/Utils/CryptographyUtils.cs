using System.Security;
using System.Security.Cryptography;

namespace MeloongCore;
public static class CryptographyUtils {

    #region Hash

    public enum HashMethod {
        /// <summary>
        /// 使用 <see cref="MD5"/> 算法。在 16 进制下哈希为 32 长度字符串。
        /// </summary>
        Md5,
        /// <summary>
        /// 使用 <see cref="SHA1"/> 算法。在 16 进制下哈希为 40 长度字符串。
        /// </summary>
        Sha1,
        /// <summary>
        /// 使用 <see cref="SHA256"/> 算法。在 16 进制下哈希为 64 长度字符串。
        /// </summary>
        Sha256,
        /// <summary>
        /// 使用 <see cref="SHA512"/> 算法。在 16 进制下哈希为 128 长度字符串。
        /// </summary>
        Sha512
    }
    private static HashAlgorithm GetHashAlgorithm(HashMethod method) => method switch {
        HashMethod.Md5 => MD5.Create(),
        HashMethod.Sha1 => SHA1.Create(),
        HashMethod.Sha256 => SHA256.Create(),
        HashMethod.Sha512 => SHA512.Create(),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    /// <summary>
    /// 计算文件的 Hash。返回 16 进制小写字符串。
    /// </summary>
    public static string ComputeFileHash(string filePath, HashMethod method = HashMethod.Md5) {
        using HashAlgorithm hashImpl = GetHashAlgorithm(method);
        Logger.Trace($"计算文件 {method}：{filePath}");
        using var file = FileUtils.ReadAsStream(filePath);
        return BitConverter.ToString(hashImpl.ComputeHash(file)).Replace("-", "").Lower();
    }

    /// <summary>
    /// 计算字节数组的 Hash。返回 16 进制小写字符串。
    /// </summary>
    public static string ComputeHash(byte[] input, HashMethod method = HashMethod.Md5) {
        using HashAlgorithm hashImpl = GetHashAlgorithm(method);
        return BitConverter.ToString(hashImpl.ComputeHash(input)).Replace("-", "").Lower();
    }
    /// <summary>
    /// 计算字符串的 Hash。返回 16 进制小写字符串。
    /// <para/> 使用 UTF-8 将字符串转换为字节数组。
    /// </summary>
    public static string ComputeHash(string input, HashMethod method = HashMethod.Md5) 
        => ComputeHash(Encoding.UTF8.GetBytes(input), method);

    #endregion

    #region AES 对称加解密

    private const byte AesDataVersion = 1;

    /// <summary>
    /// 使用 AES-256-CBC 对称加密字符串。
    /// </summary>
    /// <returns>加密后的 Base64 字符串。若输入为空字符串或 null，则原样返回空字符串或 null。</returns>
    public static string? AesEncrypt(string? sourceString, string key = "EncryptKey") {
        if (string.IsNullOrEmpty(sourceString)) return sourceString; // 原样返回空字符串或 null
        byte[] iv = new byte[AesIvSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv);
        byte[] cipherBytes;
        using var sha256 = SHA256.Create();
        byte[] aesKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        byte[] hmacKey = sha256.ComputeHash(Encoding.UTF8.GetBytes("AES-HMAC|" + key));
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using ICryptoTransform encryptor = aes.CreateEncryptor(aesKey, iv);
        byte[] sourceBytes = Encoding.UTF8.GetBytes(sourceString);
        cipherBytes = encryptor.TransformFinalBlock(sourceBytes, 0, sourceBytes.Length);
        // 格式：版本 [1 字节] | IV [16 字节] | 密文 [N 字节] | 校验 [4 字节]
        byte[] encryptedBytes = new byte[1 + iv.Length + cipherBytes.Length + AesCheckSize];
        encryptedBytes[0] = AesDataVersion;
        Array.Copy(iv, 0, encryptedBytes, 1, iv.Length);
        Array.Copy(cipherBytes, 0, encryptedBytes, 1 + iv.Length, cipherBytes.Length);
        using var hmac = new HMACSHA256(hmacKey);
        byte[] checkBytes = hmac.ComputeHash(encryptedBytes, 0, 1 + iv.Length + cipherBytes.Length);
        Array.Copy(checkBytes, 0, encryptedBytes, 1 + iv.Length + cipherBytes.Length, AesCheckSize);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// 使用 AES-256-CBC 对称解密字符串。
    /// 如果输入数据有误，则确保会抛出 <see cref="CryptographicException"/>。
    /// </summary>
    /// <returns>解密后的字符串。若输入为空字符串或 null，则原样返回空字符串或 null。</returns>
    public static string? AesDecrypt(string? encryptedString, string key = "EncryptKey") {
        if (string.IsNullOrEmpty(encryptedString)) return encryptedString; // 原样返回空字符串或 null
        byte[] encryptedBytes;
        try {
            encryptedBytes = Convert.FromBase64String(encryptedString);
        } catch (FormatException ex) {
            throw new CryptographicException($"数据不是一个有效的 Base64 字符串（{encryptedString}）", ex);
        }
        if (encryptedBytes.Length == 0) throw new CryptographicException($"对称加密数据长度有误（{encryptedString}）");
        int cipherSize = encryptedBytes.Length - 1 - AesIvSize - AesCheckSize;
        if (encryptedBytes[0] != AesDataVersion || cipherSize < 16 || cipherSize % 16 != 0)
            throw new CryptographicException($"对称加密数据长度有误（{encryptedString}）");
        using var sha256 = SHA256.Create();
        byte[] hmacKey = sha256.ComputeHash(Encoding.UTF8.GetBytes("AES-HMAC|" + key));
        using var hmac = new HMACSHA256(hmacKey);
        byte[] checkBytes = hmac.ComputeHash(encryptedBytes, 0, 1 + AesIvSize + cipherSize);
        int checkOffset = 1 + AesIvSize + cipherSize;
        int checkDiff = 0;
        for (int i = 0; i < AesCheckSize; i++)
            checkDiff |= encryptedBytes[checkOffset + i] ^ checkBytes[i];
        if (checkDiff != 0) throw new CryptographicException($"对称加密数据校验失败（{encryptedString}）");
        byte[] aesKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        byte[] iv = new byte[AesIvSize];
        Array.Copy(encryptedBytes, 1, iv, 0, iv.Length);
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using ICryptoTransform decryptor = aes.CreateDecryptor(aesKey, iv);
        byte[] output = decryptor.TransformFinalBlock(encryptedBytes, 1 + iv.Length, cipherSize);
        return Encoding.UTF8.GetString(output);
    }

    private const int AesIvSize = 16;
    private const int AesCheckSize = 4;

    #endregion

    #region ECDSA 非对称加密签名

    /// <summary>
    /// 使用 ECDSA 算法，验证一个字符串的签名是否使用私钥生成。
    /// 如果验证失败则抛出 <see cref="SecurityException"/>。
    /// </summary>
    public static void EcdsaVerify(string sourceString, string sign, string publicKey) {
        using var sha256 = GetHashAlgorithm(HashMethod.Sha256);
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sourceString));
        byte[] publicKeyBytes = Convert.FromBase64String(publicKey);
        byte[] signature = Convert.FromBase64String(sign);
        IntPtr algorithmHandle = IntPtr.Zero;
        IntPtr keyHandle = IntPtr.Zero;
        // 直接调用 DLL，以避免 .NET API 依赖的系统服务存在问题
        try {
            int status = BCryptOpenAlgorithmProvider(out algorithmHandle, "ECDSA_P256", null, 0);
            if (status < 0) throw new CryptographicException($"{nameof(BCryptOpenAlgorithmProvider)} 失败，错误码 {status}");
            status = BCryptImportKeyPair(algorithmHandle, IntPtr.Zero, "ECCPUBLICBLOB", out keyHandle, publicKeyBytes, publicKeyBytes.Length, 0);
            if (status < 0) throw new CryptographicException($"{nameof(BCryptImportKeyPair)} 失败，错误码 {status}");
            status = BCryptVerifySignature(keyHandle, IntPtr.Zero, hash, hash.Length, signature, signature.Length, 0);
            if (status == unchecked((int) 0xC000A000)) throw new SecurityException("签名验证失败");
            if (status < 0) throw new CryptographicException($"{nameof(BCryptVerifySignature)} 失败，错误码 {status}");
        } finally {
            if (keyHandle != IntPtr.Zero) BCryptDestroyKey(keyHandle);
            if (algorithmHandle != IntPtr.Zero) BCryptCloseAlgorithmProvider(algorithmHandle, 0);
        }
    }

    /// <summary>
    /// 使用 ECDSA 算法，为一个双方均知晓的字符串生成签名。
    /// </summary>
    public static string EcdsaSign(string sourceString, string privateKey) {
        using var sha256 = GetHashAlgorithm(HashMethod.Sha256);
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sourceString));
        byte[] privateKeyBytes = Convert.FromBase64String(privateKey);
        IntPtr algorithmHandle = IntPtr.Zero;
        IntPtr keyHandle = IntPtr.Zero;
        // 直接调用 DLL，避免 .NET API 依赖的系统服务存在问题
        try {
            int status = BCryptOpenAlgorithmProvider(out algorithmHandle, "ECDSA_P256", null, 0);
            if (status < 0) throw new CryptographicException($"{nameof(BCryptOpenAlgorithmProvider)} 失败，错误码 {status}");
            status = BCryptImportKeyPair(algorithmHandle, IntPtr.Zero, "ECCPRIVATEBLOB", out keyHandle, privateKeyBytes, privateKeyBytes.Length, 0);
            if (status < 0) throw new CryptographicException($"{nameof(BCryptImportKeyPair)} 失败，错误码 {status}");
            status = BCryptSignHash(keyHandle, IntPtr.Zero, hash, hash.Length, null, 0, out int signSize, 0);
            if (status < 0) throw new CryptographicException($"{nameof(BCryptSignHash)} 失败，错误码 {status}");
            byte[] sign = new byte[signSize];
            status = BCryptSignHash(keyHandle, IntPtr.Zero, hash, hash.Length, sign, sign.Length, out int actualSignSize, 0);
            if (status < 0) throw new CryptographicException($"{nameof(BCryptSignHash)} 失败，错误码 {status}");
            if (actualSignSize != sign.Length) Array.Resize(ref sign, actualSignSize);
            return Convert.ToBase64String(sign);
        } finally {
            if (keyHandle != IntPtr.Zero) BCryptDestroyKey(keyHandle);
            if (algorithmHandle != IntPtr.Zero) BCryptCloseAlgorithmProvider(algorithmHandle, 0);
        }
    }

    [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)] private static extern int BCryptOpenAlgorithmProvider(out IntPtr algorithmHandle, string algorithmId, string? implementation, int flags);
    [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)] private static extern int BCryptImportKeyPair(IntPtr algorithmHandle, IntPtr importKey, string blobType, out IntPtr keyHandle, byte[] input, int inputSize, int flags);
    [DllImport("bcrypt.dll")] private static extern int BCryptVerifySignature(IntPtr keyHandle, IntPtr paddingInfo, byte[] hash, int hashSize, byte[] signature, int signatureSize, int flags);
    [DllImport("bcrypt.dll")] private static extern int BCryptDestroyKey(IntPtr keyHandle);
    [DllImport("bcrypt.dll")] private static extern int BCryptCloseAlgorithmProvider(IntPtr algorithmHandle, int flags);
    [DllImport("bcrypt.dll")] private static extern int BCryptSignHash(IntPtr keyHandle, IntPtr paddingInfo, byte[] hash, int hashSize, byte[]? signature, int signatureSize, out int resultSize, int flags);

    #endregion

}
