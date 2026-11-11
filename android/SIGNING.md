# Android 签名说明

`android/signing/` 下的签名密钥和密码**只保存在本地，不提交到 Git**：

```text
android/signing/salarymonitor.jks
android/signing/keystore.properties
```

`keystore.properties` 格式：

```properties
storePassword=你的签名密码
```

`build.ps1` 按以下顺序读取密码：

1. 环境变量 `SALARYMONITOR_KEYSTORE_PASS`；
2. `android/signing/keystore.properties` 中的 `storePassword`；
3. 如果密钥不存在且以上都没有，生成随机密码并写入本地
   `keystore.properties`。

后续发布必须继续使用同一把 `salarymonitor.jks`，否则 Android 无法覆盖安装。
仓库克隆后第一次构建可以生成新的本地密钥，但只能作为新的签名身份使用。

不要把 `salarymonitor.jks`、`keystore.properties` 或真实密码提交到 GitHub。
