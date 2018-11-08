PlanServerWinService项目：  
- 计划任务服务端程序，用于调度和管理服务器上的job运行情况；  
- 前端客户端使用web界面，参看 PlanServerTaskManager.Web项目里，PlanAdmin.aspx页面；  

使用前注意  
- 修改Config配置文件里的通讯密钥：PlanSocketKey；  
- 按Release模式编译WinService后，复制到服务器上，然后右击installService.bat文件，以管理员模式运行，就会自动安装服务并启动了。  
