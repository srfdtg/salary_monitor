# 薪资显示器 Android 版

原生 Android（Java）版本，与 Windows 版共享同一套薪资计算规则和默认参数。

- 包名：`com.salarymonitor.app`
- 版本：`1.2.0`（versionCode 4）
- 最低系统：Android 8.0（API 26）
- 目标系统：Android 14（API 34）
- 产物：`../dist/薪资显示器-Android.apk`

## 功能

- 今日已赚、日薪、时薪、分钟薪
- 距离上班/下班倒计时，每秒刷新
- 今日进度、已完成/总有效分钟
- 午休冻结、下班封顶、休息日不计薪
- 设置页修改月薪、月计薪天数、上下班和午休时间
- “按当月实际工作日计薪”开关：
  - 关闭：按 21.75 天平均，仅周末休息；
  - 开启：按当月实际工作日计算日薪，自动识别 2026—2027 年法定节假日和调休；
  - 法定节假日显示“节假日休息”，调休周末正常计薪；
  - 2028 年及以后暂无官方表，回退为周一至周五。
- 设置校验、错误提示、恢复默认
- 屏幕常亮开关（默认开启）
- 首页“今日已赚”右侧小眼睛按钮：一键隐藏/显示主金额与日薪、时薪、分钟薪；隐藏状态写入私有配置，重启后保持
- v1.1.1 顶部品牌图标使用独立的透明圆角位图，不再直接复用自适应启动图标，避免图标四角出现方形深色底/黑框

Android 没有普通应用窗口置顶能力，因此没有实现 Windows 版的“窗口置顶”；全屏显示 + 屏幕常亮是这一版的替代方案。

## 构建环境

当前工作区使用便携工具链，不要求系统安装 Android Studio：

```text
D:\AI\.android-toolchain\
  jdk\                         Microsoft OpenJDK 17
  sdk\
    platforms\android-34\
    build-tools\34.0.0\
    platform-tools\
    cmdline-tools\latest\
```

如果工具链不存在，需要重新准备 JDK 17、Android SDK Platform 34、Build-Tools 34.0.0。

## 构建

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\android\build.ps1
```

或跳过逻辑测试：

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\android\build.ps1 -SkipTests
```

脚本执行顺序：

1. 编译并运行 70 个纯 JVM 核心测试。
2. 编译资源、链接 Manifest。
3. 编译 Java、生成 dex。
4. 对齐、签名、校验签名。
5. 复制 APK 到 `dist/薪资显示器-Android.apk`，输出 SHA-256。

签名密钥：`android/signing/salarymonitor.jks`。后续升级必须保留同一把密钥；删除后无法覆盖安装旧版本。

签名密钥和密码**不提交到 Git**。构建脚本从环境变量
`SALARYMONITOR_KEYSTORE_PASS` 或本地 `android/signing/keystore.properties`
读取密码，详见 [签名说明](SIGNING.md)。

## 安装

1. 把 `薪资显示器-Android.apk` 传到手机；
2. 允许文件管理器/浏览器“安装未知应用”；
3. 安装并打开；
4. 首次使用可按默认参数，或点右上角设置按钮修改。

APK 为本地自签名，未上架应用商店；部分手机会有“未知来源/Play Protect”提示。

## 目录

```text
android/
  app/
    AndroidManifest.xml
    res/                       # 颜色、布局、drawable、图标
      drawable-*dpi/header_icon.png  # 顶部透明圆角图标（5 个密度）
    src/com/salarymonitor/app/
      MainActivity.java
      SettingsActivity.java
      core/                    # 可在 JVM 上独立测试的计算层
        WorkdayCalendar.java   # 2026—2027 节假日/调休表
      data/SettingsRepository.java
      ui/                      # 渐变大金额、进度条
  tests/CoreTests.java         # 70 个测试
  build.ps1
  signing/salarymonitor.jks
```

## 版本记录

- v1.2.0：首页增加小眼睛金额隐私开关，隐藏状态持久化；版本号 4，计算与默认参数不变。
- v1.1.1：修复顶部品牌图标的自适应图标方形底色/四角黑框；版本号 3，计算与配置不变。
- v1.1.0：增加“按当月实际工作日计薪”、2026—2027 年节假日/调休表。
- v1.0.x：基础仪表盘、设置、屏幕常亮。

设计、测试用例和结果：

- [需求与方案设计](../docs/android/01-需求与方案设计.md)
- [测试用例](../docs/android/02-测试用例.md)
- [测试记录](../docs/android/03-测试记录.md)
- [2026—2027 日历与工作日](../docs/android/04-2026-2027日历与工作日.md)
