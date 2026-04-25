# nlcDownloaderNeo
新一代国图下载器，直接通过WebView2解析拦截请求  

解决传统脚本无法修改Referer导致失效的问题，真正意义上的克隆请求级下载  

但微软我操你妈WebResourceResponseReceived在2026年还不支持Worker的请求（[https://github.com/MicrosoftEdge/WebView2Feedback/issues/4926](https://github.com/MicrosoftEdge/WebView2Feedback/issues/4926)）  

