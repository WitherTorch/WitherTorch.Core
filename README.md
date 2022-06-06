![WitherTorch Core Icon](withertorch_core.png)
# WitherTorch 的核心程式庫

WitherTorch 是一個 Minecraft 伺服器的開服軟體

其核心(也就是此存放庫的內容)可供第三方開發者使用

[Discord](https://discord.gg/F7YNJ5m) | [License](LICENSE)

## 適用框架和相依的套件
此核心有 .NET Framework 4.7.2 和 .NET 5.0 兩種版本。<br/>
<br/>
相依的套件有:
<ul>
  <li><a href="https://github.com/JamesNK/Newtonsoft.Json">Newtonsoft.Json</a> (>= 13.0.1)</li>
  <li><a href="https://github.com/aaubry/YamlDotNet">YamlDotNet</a> (>= 11.2.1)</li>
</ul>

## 使用方式

### (非必要) 引入命名空間
WitherTorch 的命名空間很長，建議在撰寫前先把相關的命名空間引入進來
```csharp
using WitherTorch.Core;
using WitherTorch.Core.Server;
```

### 註冊伺服器軟體
要建立或使用一個伺服器前，你需要先註冊需要的伺服器軟體:<br/>
以 Java版的香草(Vanilla)伺服器為例
(其類別為 `WitherTorch.Core.Servers.JavaDedicated`)
```csharp
  SoftwareRegister.RegisterServerSoftware(typeof(JavaDedicated));
```

### 建立伺服器
註冊完後，就可以開始建立伺服器了
一樣以 Java版的香草(Vanilla)伺服器為例
```csharp
  string path = @"C:\path\to\create\server"; //你要建立伺服器的位置
  Server server = Server.CreateServer<WitherTorch.Core.Servers.JavaDedicated>(path);
```

### 安裝伺服器
建立好伺服器後，就要開始下載及安裝伺服器了

#### 取得版本資料
安裝伺服器前，需要先取得版本資料，以確定我們要安裝的軟體版本是多少 (像是 1.18.2、1.12.2 或 1.8.9 之類的)
```csharp
  string[] versions = server.GetSoftwareVersions();
```

#### 註冊事件並開始安裝
確認好版本後就能開始安裝了
伺服器安裝是一個非同步過程，所以需要註冊相關事件才能知道安裝的進度
```csharp
  string version = "1.18.2"; //目標軟體版本
  server.ServerInstalling += Server_ServerInstalling;
  server.ChangeVersion(Array.IndexOf(versions, version));
  
  void Server_ServerInstalling(InstallTask task){
    server.ServerInstalling -= Server_ServerInstalling; //取得安裝工作 (InstallTask) 物件後就不需要這個事件了
    task.InstallFinished += InstallTask_InstallFinished; //觸發於安裝完成時
    task.InstallFailed += InstallTask_InstallFailed; //觸發於安裝失敗時
    task.PercentageChanged += InstallTask_PercentageChanged; //觸發於進度數值變化時
    task.StatusChanged += InstallTask_StatusChanged; //觸發於安裝狀態物件變更時
  }
```

### 伺服器的儲存和讀取
#### 儲存伺服器
```csharp
  server.SaveServer();
```
#### 讀取伺服器
```csharp
  Server server_that_you_load = GetServerFromDirectory(@"C:\path\to\load\server");
```

### 啟動伺服器
安裝完成之後就能啟動了，啟動前記得先取得伺服器的 AbstractProcess 物件
```csharp
  AbstractProcess serverProcess = server.GetProcess(); //取得伺服器的 AbstractProcess 物件
  
  serverProcess.ProcessStarted += ServerProcess_ProcessStarted; //觸發於伺服器啟動時
  serverProcess.ProcessEnded += ServerProcess_ProcessEnded; //觸發於伺服器停止時
  serverProcess.MessageRecived += ServerProcess_MessageRecived; //觸發於伺服器後台輸出訊息時
  
  server.RunServer(server.GetRuntimeEnvironment());
  serverProcess.InputCommand("/help"); //在伺服器後台輸入特定訊息
```
