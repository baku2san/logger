* 注意点

- Offline Installの場合、「必須コンポーネント(prerequisites)」は、http://go.microsoft.com/fwlink から、DLして同梱させる必要あり
-- ex.)NDP462-KB3151800-x86-x64-AllOS-ENU.exe 

- Visual Studio 2017 の場合、Installerを保存するFolderが 2015 から大きく変更されている
|vs|folder|h
|2015以前|C:\Program Files (x86)\Microsoft Visual Studio 14.0\SDK\Bootstrapper\Packages|
|2017以降？|C:\Program Files (x86)\Microsoft SDKs\ClickOnce Bootstrapper\Packages|

- 多言語版を入れる場合は、各言語フォルダに、対象言語用のファイルも入れておく必要あり
-- ex.) packages/ja/-- ex.)NDP462-KB3151800-x86-x64-AllOS-JPN.exe 


- そもそも、Version Check のVersionが不具合？により修正された過去があり、そのせいで/ja/package.xml内の、比較Versionの修正が必要な可能性もある
|概要|URL|h
|Net Framework のDL一覧（英語版のみ？）|https://msdn.microsoft.com/en-us/library/ee942965%28v=vs.110%29.aspx、https://docs.microsoft.com/en-us/dotnet/framework/deployment/deployment-guide-for-developers|
|各言語用|/ja/package.xml内のこんな感じのとこにあるURL <String Name="DotNetFX461FullLanguagePackBootstrapper">http://go.microsoft.com/fwlink/?linkid=671731&clcid=0x411 </String> |
|Framework versionとVersion記述|https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/versions-and-dependencies|
|元の情報|https://developercommunity.visualstudio.com/content/problem/10716/clickonce-can-not-find-462-prerequisite.html|

◆補足
・<install root>と言いながら、Bootstrapperまで含めて、VS2017だと以下
	C:\Program Files (x86)\Microsoft SDKs\ClickOnce Bootstrapper
・ClickOnce用に見えるが、実際は Visual Studio Installer 2017 もここの情報を利用している。（動作上、そのように見える）
	
◆VC++runtime Offline Install の方法
１．<install root>\Bootstrapper\Packages\vcredist_x86\
　　この配下にあるpackages.xml 内のDL-Link（上の.net 関連と同様に）からOfflineInstallerを落としておく
　　保存場所は、EUは、このPath。各言語版は、その言語フォルダへ。

２．Setupの Prerequisites(必須コンポーネント）の二つ目「アプリケーションと同じ場所から必須コンポーネントをダウンロードする（D)」を選択しておくことで、Offline Install となる

問題点）
・下記にあるように、ファイル名やら、Product名やらが間違っているのでその修正が必要。
https://blogs.msdn.microsoft.com/jpvsblog/2017/06/22/vs2017-vc14-installer/


実施例）
・product.xml のファイル名のうち一か所（何故二か所共でないのかが不思議だが・・）
・PublicKey: sh1 公開KeyのSpace除去


★この後で、No.(2)もあるので注意：Install必要か判断するVersion間違い（OKにならないんだと思われ）
https://blogs.msdn.microsoft.com/jpvsblog/2017/07/11/vs2017-vc14-installer-2/




★このLinkのTextCopy：Link先変わったら困るので
https://blogs.msdn.microsoft.com/jpvsblog/2017/06/22/vs2017-vc14-installer/
★★★★★★★★★★★★★★★
Visual Studio 2017 で Visual C++ “14” ランタイム ライブラリをインストーラーに含めた場合に発生する問題について
★★★★★★★★★★★★★★★
avatar of visual-studio-support-team-in-japanVisual Studio Support Team in Japan2017-06-22 
6
0
こんにちは、Visual Studio サポート チームです。

今回は Microsoft Visual Studio 2017 に含まれる Visual C++ "14" ランタイム ライブラリを必須コンポーネントに含めてインストーラーを作成する場合に発生する可能性がある問題と対処方法をご案内いたします。


<2017 年 7 月 4 日追記>
Product.xml に指定する Product 属性の内容につきましては、Visual Studio 2017 RTMに対する値でのご案内となっております。
ダウンロード ページから入手していただける最新の Visual C++ 2017 Redistributable のバージョンに対する Product 属性の値につきましては、現在正しい値を確認中です。
確認の結果が得られ次第この記事にてご案内いたしますので、大変恐れ入りますが今しばらくお待ちください。

<2017 年 7 月11 日修正>
Visual C++ "14" ランタイム ライブラリのインストーラーの入手先に関する記述を修正しました。

 

現象

a) [アプリケーションと同じ場所から必須コンポーネントをダウンロードする] を指定して Visual C++ "14" ランタイム ライブラリを必須コンポーネントに指定した場合、発行時にエラーが発生する。

b) Visual C++ "14" ランタイム ライブラリが既にインストールされているにも関わらず、インストーラーの実行時にランタイム ライブラリのインストールが求められる。

これらの現象は、ClickOnce アプリケーションの発行や、Microsoft Visual Studio 2017 Installer Projects 拡張機能を使用してセットアップ プロジェクトのビルドを行い setup.exe を生成する場合に発生します。

 

原因

この現象は、Visual Studio 2017 のインストールで同時にインストールされる Visual C++ "14" ランタイム ライブラリのブートストラップ パッケージに含まれる Product.xml ならびに Package.xml に誤りがあるために発生します。

現象 a) は、Product.xml の PackageFile 要素に指定されている Name 属性と PublicKey 属性の値に誤りがあり、Visual C++ "14" ランタイム ライブラリのインストーラーと異なっていることが原因です。

現象 b) は、Product.xml の MsiProductCheck 要素に指定されている Product 属性の値に誤りがあり、Visual C++ "14" ランタイム ライブラリのインストーラーのプロダクト コードと異なっていることが原因です。

 

対処策

Product.xml ならびに Package.xml の各設定を修正してください。

変更手順 :

1. Product.xml、Package.xml のバックアップを作成しておきます。各ファイルは既定で以下の場所に配置されます。

x86 版
Product.xml : <インストール ルート>\Bootstrapper\Packages\vcredist_x86\

x64 版
Product.xml : <インストール ルート>\Bootstrapper\Packages\vcredist_x64\

2. メモ帳などのエディタで各 xml ファイルを開き、以下の箇所を変更してファイルを保存します。 "※ 1" と記載している箇所については後述します。

 

Product.xml (x86 版)

変更前

<!-- Defines list of files to be copied on build -->
  <PackageFiles CopyAllPackageFiles="false">
    <PackageFile Name="vcredist_x86.exe" HomeSite="VCRedistExe" PublicKey="3082010a0282010100ee5bbe7d1124e38606e066ff48b517bd02e4b40c32f0723e7d2e87d74ea1b1a7432ff7659e31e1323145aed7c1248421d72eb5847efa35d3531cd7b6511e4fce66b9ebb70c02fd295cada887f6ca22b4d5bf0875f58a708f63d7ef8a1ee98f4324645ad3877d906d3bac76cd57367de8bc1056ac98f0895d2e64c6af26095e1e6315f13dbf168f998802c330b7c10b601f0f72ccd6b7a83512869ba10b0ae6935b8efa549cc1f3195f428d129f1d3f90b72713831932821df3d987d421b23ca2b6074fd724aaee8df5b3d9faf9394fa7e9f2af5952f4dc419b2f117063ddeadeaaf16d2104105333bbb24fc5e153b24165476e37f6bce99b1641916b2e5b30c30203010001" />
  </PackageFiles>
  <InstallChecks>
    <MsiProductCheck Property="VCRedistInstalled" Product="{7AACE5DC-CD5D-3856-A333-DCC0872AA88C}"/>
  </InstallChecks>
  <!-- Defines how to invoke the setup for the Visual C++ 14.0 redist -->
  <Commands Reboot="Defer">
    <Command PackageFile="vcredist_x86.exe" Arguments=' /q:a '>
変更後

  <!-- Defines list of files to be copied on build -->
  <PackageFiles CopyAllPackageFiles="false">
    <PackageFile Name="vc_redist.x86.exe" HomeSite="VCRedistExe" PublicKey="※ 1" />
  </PackageFiles>
  <InstallChecks>
    <MsiProductCheck Property="VCRedistInstalled" Product="{C6CDA568-CD91-3CA0-9EDE-DAD98A13D6E1}"/>
  </InstallChecks>
  <!-- Defines how to invoke the setup for the Visual C++ 14.0 redist -->
  <Commands Reboot="Defer">
    <Command PackageFile="vc_redist.x86.exe" Arguments=' /q:a '>
 

Product.xml (x64 版)

変更前

  <!-- Defines list of files to be copied on build -->
  <PackageFiles CopyAllPackageFiles="false">
    <PackageFile Name="vcredist_x64.exe" HomeSite="VCRedistExe" PublicKey="3082010a0282010100ee5bbe7d1124e38606e066ff48b517bd02e4b40c32f0723e7d2e87d74ea1b1a7432ff7659e31e1323145aed7c1248421d72eb5847efa35d3531cd7b6511e4fce66b9ebb70c02fd295cada887f6ca22b4d5bf0875f58a708f63d7ef8a1ee98f4324645ad3877d906d3bac76cd57367de8bc1056ac98f0895d2e64c6af26095e1e6315f13dbf168f998802c330b7c10b601f0f72ccd6b7a83512869ba10b0ae6935b8efa549cc1f3195f428d129f1d3f90b72713831932821df3d987d421b23ca2b6074fd724aaee8df5b3d9faf9394fa7e9f2af5952f4dc419b2f117063ddeadeaaf16d2104105333bbb24fc5e153b24165476e37f6bce99b1641916b2e5b30c30203010001" />
  </PackageFiles>
  <InstallChecks>
    <MsiProductCheck Property="VCRedistInstalled" Product="{83BAF6AE-E65F-3FA7-8DE5-BF65A60FA3C2}"/>
  </InstallChecks>
  <!-- Defines how to invoke the setup for the Visual C++ 14.0 redist -->
  <Commands Reboot="Defer">
    <Command PackageFile="vcredist_x64.exe" Arguments=' /q:a '>
変更後

  <!-- Defines list of files to be copied on build -->
  <PackageFiles CopyAllPackageFiles="false">
    <PackageFile Name=" vc_redist.x64.exe " HomeSite="VCRedistExe" PublicKey="※ 1" />
  </PackageFiles>
  <InstallChecks>
    <MsiProductCheck Property="VCRedistInstalled" Product="{8D50D8C6-1E3D-3BAB-B2B7-A5399EA1EBD1}"/>
  </InstallChecks>
  <!-- Defines how to invoke the setup for the Visual C++ 14.0 redist -->
  <Commands Reboot="Defer">
    <Command PackageFile=" vc_redist.x64.exe " Arguments=' /q:a '>
 

弊社製品の不具合でご迷惑をおかけし、誠に申し訳ありません。Visual C++ "14" ランタイム ライブラリを必須コンポーネントに含めてインストーラーを作成する場合には、上記の手順での対処をお願いいたします。

 

※ 1 PulicKey の値には、最新の Visual C++ "14" ランタイム ライブラリのインストーラーをダウンロードし、実行可能ファイルのデジタル署名に含まれる証明書の公開キーの値を指定します。

1. ダウンロードした [vc_redist.x86.exe] または [vc_redist.x64.exe] を右クリックし、プロパティを表示します。
2. プロパティ画面上部タブ [デジタル署名] に移動します。
3. [署名の一覧] で下記の証明書を選択し [詳細] をクリックします。

署名者名 : Microsoft Corporation
ダイジェスト アルゴリズム : sha1


4. デジタル署名の詳細画面では、画面中央 [証明書の表示] をクリックします。
5. 証明書が表示されましたら上部タブ [詳細] に移動いただき [公開キー] を選択します。
6. 表示された公開キーをメモ帳などにコピーして、置換で半角スペースを削除してください。



なお、お客様のインストーラーに含めて配布していただくための Visual C++ “14” ランタイム ライブラリのインストーラーは、以下のドキュメントでご案内しておりますようにPackage.xml に指定されている URL からダウンロードしてください。

How to: Include Prerequisites with a ClickOnce Application
https://docs.microsoft.com/en-us/visualstudio/deployment/how-to-include-prerequisites-with-a-clickonce-application

 

また、最新の Visual C++ “14” ランタイム ライブラリのインストーラーは Visual Studio のダウンロード – ページ下部 [Other Tools and Frameworks] 内で、[Visual Studio 2017 の Microsoft Visual C++ 再頒布可能パッケージ] の欄で x86 もしくは x64 にチェックを付けてダウンロードしていただけます。
以下のページから最新のインストーラーをご利用いただく際には Product Code が異なる可能性がございますので、お手数をおかけいたしますが予め Product Code をご確認いただいた上で Product.xml の変更をお願いいたします。

Visual Studio のダウンロード
https://www.visualstudio.com/ja/downloads

 

Product Code につきましては、以下のドキュメントでご案内しております Uninstall レジストリ キーのサブ キーからご確認ください。DisplayName 値として、” Microsoft Visual C++ 2017 xxx Minimum Runtime - <バージョン番号>” を含むサブ キーが、Visual C++ “14” ランタイム ライブラリの Product Code となります。(xxx は、x86 もしくは x64 となります。)

Uninstall Registry Key
https://msdn.microsoft.com/ja-jp/library/windows/desktop/aa372105(v=vs.85).aspx
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall

 

 

重要
お客様からのご報告により、現状の Product.xml で行われている Product Code による製品検出については、別の問題が発生する可能性を確認しております。
この点につきましては以下の記事でご案内しておりますので、あわせてご確認ください。

Visual Studio 2017 で Visual C++ “14” ランタイム ライブラリをインストーラーに含めた場合に発生する問題について (2)
https://blogs.msdn.microsoft.com/jpvsblog/2017/07/11/vs2017-vc14-installer-2/

★★
Visual Studio 2017 で Visual C++ ”14” ランタイム ライブラリをインストーラーに含めた場合に発生する問題について (2)
★★★★★★★★★★★★★★★
avatar of visual-studio-support-team-in-japanVisual Studio Support Team in Japan2017-07-11 
4
0
こんにちは、Visual Studio サポート チームです。

今回は、先日ご案内した以下の記事に関連して、新しいバージョンのVisual C++ ランタイム ライブラリをご利用される際に発生する可能性がある問題とその対処方法をご案内いたします。

 

Visual Studio 2017 で Visual C++ "14" ランタイム ライブラリをインストーラーに含めた場合に発生する問題について
https://blogs.msdn.microsoft.com/jpvsblog/2017/06/22/vs2017-vc14-installer/

 

現象
作成したパッケージに含まれる Visual C++ "14" ランタイム ライブラリよりも、さらに新しいバージョンの Visual C++ "14" ランタイム ライブラリが既にインストールされているにもかかわらず、インストーラーの実行時にランタイム ライブラリのインストールが求められる。

 

原因
Visual C++ ランタイム ライブラリ用の Product.xml では、対象の製品がインストールされているかどうかの検証に Product Code を使用します。

この検証では、対象の Product Code の製品がインストールされているかどうかを調べますが、Visual C++ ランタイム ライブラリはアップデートごとに新しい Product Code を採番しているため、インストール対象のランタイム ライブラリよりも新しいバージョンのライブラリが既にインストールされていたとしても、それらは検出されないため、ライブラリのインストールが必要と判断される動作となります。

 

対処策
以下のように Product.xml の設定を変更することで、より新しいバージョンのライブラリが既にインストールされていた場合には、ライブラリのインストールをスキップさせることが可能です。

変更手順 :

1. Product.xml のバックアップを作成しておきます。各ファイルは既定で以下の場所に配置されます。

x86 版
Product.xml : <インストール ルート>\Bootstrapper\Packages\vcredist_x86\

x64 版
Product.xml : <インストール ルート>\Bootstrapper\Packages\vcredist_x64\

2. メモ帳などのエディタで各 xml ファイルを開き、以下の箇所を変更してファイルを保存します。

Product.xml (x86 版)

変更前

  <InstallChecks>
    <MsiProductCheck Property="VCRedistInstalled" Product="{C6CDA568-CD91-3CA0-9EDE-DAD98A13D6E1}"/>
  </InstallChecks>
  <!-- Defines how to invoke the setup for the Visual C++ 14.0 redist -->
  <Commands Reboot="Defer">
    <Command PackageFile="vcredist_x86.exe" Arguments=' /q:a '>

      <!-- These checks determine whether the package is to be installed -->

      <InstallConditions>

        <BypassIf Property="VCRedistInstalled" Compare="ValueGreaterThanOrEqualTo" Value="3"/>

        <!-- Block install if user does not have admin privileges -->

        <FailIf Property="AdminUser" Compare="ValueEqualTo" Value="false" String="AdminRequired"/>

        <!-- Block install on Win95 -->

        <FailIf Property="Version9X" Compare="VersionLessThan" Value="4.10" String="InvalidPlatformWin9x"/>

        <!-- Block install on Vista or below -->

        <FailIf Property="VersionNT" Compare="VersionLessThan" Value="6.00" String="InvalidPlatformWinNT"/>

      </InstallConditions>

変更後

  <InstallChecks>
    <RegistryCheck Property="VCRedistInstalledVersion" Key="HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86" Value="Version" />
  </InstallChecks>
  <!-- Defines how to invoke the setup for the Visual C++ 14.0 redist -->
  <Commands Reboot="Defer">
    <Command PackageFile="vcredist_x86.exe" Arguments=' /q:a '>

      <!-- These checks determine whether the package is to be installed -->

      <InstallConditions>

        <BypassIf Property="VCRedistInstalledVersion" Compare="ValueGreaterThanOrEqualTo" Value="v14.10.25008.00" />

        <!-- Block install if user does not have admin privileges -->

        <FailIf Property="AdminUser" Compare="ValueEqualTo" Value="false" String="AdminRequired"/>

        <!-- Block install on Win95 -->

        <FailIf Property="Version9X" Compare="VersionLessThan" Value="4.10" String="InvalidPlatformWin9x"/>

        <!-- Block install on Vista or below -->

        <FailIf Property="VersionNT" Compare="VersionLessThan" Value="6.00" String="InvalidPlatformWinNT"/>

      </InstallConditions>

 

Product.xml (x64 版)

変更前

<InstallChecks>
    <MsiProductCheck Property="VCRedistInstalled" Product="…"/>
  </InstallChecks>
  <!-- Defines how to invoke the setup for the Visual C++ 14.0 redist -->
  <Commands Reboot="Defer">
    <Command PackageFile="vc_redist.x64.exe" Arguments=' /q:a '>

      <!-- These checks determine whether the package is to be installed -->

      <InstallConditions>

        <BypassIf Property="VCRedistInstalled" Compare="ValueGreaterThanOrEqualTo" Value="3"/>

        <!-- Block install if user does not have admin privileges -->

        <FailIf Property="AdminUser" Compare="ValueEqualTo" Value="false" String="AdminRequired"/>

        <!-- Block install on any platform other than x64 -->

        <FailIf Property="ProcessorArchitecture" Compare="ValueNotEqualTo" Value="AMD64" String="InvalidOS"/>

        <!-- Block install on Vista or below -->

        <FailIf Property="VersionNT" Compare="VersionLessThan" Value="6.00" String="InvalidPlatformWinNT"/>

      </InstallConditions>

変更後

  <InstallChecks>
    <RegistryCheck Property="VCRedistInstalledVersion" Key="HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" Value="Version" />
  </InstallChecks>
  <!-- Defines how to invoke the setup for the Visual C++ 14.0 redist -->
  <Commands Reboot="Defer">
    <Command PackageFile="vc_redist.x64.exe" Arguments=' /q:a '>

      <!-- These checks determine whether the package is to be installed -->

      <InstallConditions>

        <BypassIf Property="VCRedistInstalledVersion" Compare="ValueGreaterThanOrEqualTo" Value="v14.10.25008.00" />

        <!-- Block install if user does not have admin privileges -->

        <FailIf Property="AdminUser" Compare="ValueEqualTo" Value="false" String="AdminRequired"/>

        <!-- Block install on any platform other than x64 -->

        <FailIf Property="ProcessorArchitecture" Compare="ValueNotEqualTo" Value="AMD64" String="InvalidOS"/>

        <!-- Block install on Vista or below -->

        <FailIf Property="VersionNT" Compare="VersionLessThan" Value="6.00" String="InvalidPlatformWinNT"/>

      </InstallConditions>

 

RegistryCheck 要素で指定しているレジストリ キーに関する情報につきましては、下記のドキュメントでもご案内しておりますのであわせてご確認ください。

 

Redistributing Visual C++ Files
https://docs.microsoft.com/en-us/cpp/ide/redistributing-visual-cpp-files