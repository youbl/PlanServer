<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PlanAdmin.aspx.cs" Inherits="PlanServerTaskManager.Web.PlanAdmin" %>
<%@ Import Namespace="PlanServerService" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>计划任务管理-<%=Request.ServerVariables["LOCAL_ADDR"]%></title>
    <style type="text/css">
        body{ font-size:14px;}
        .server{ font-weight: bold; background-color:aquamarine}
        #fragment1 th, #fragment1 td{ padding-left:5px;border: black 1px solid;}
        table{ border-collapse:collapse}
        .a1 a{color:Green;}
        .a1 a:hover{color:Green;}
        .a1 a:active{color:Green;}
        #spnAdminServers th{ background-color: #96d9f9; text-align: left}
        
        .input-1
        {
            background-color: transparent;
            border: none;
            overflow: hidden;
            color: #FFFFFF;
            width: 150px;
        }
        .input-2
        {
            background-color: transparent;
            border: none;
            overflow: hidden;
            color: #FFFFFF;
            width: 300px;
        }
        .input-3
        {
            background-color: transparent;
            border: none;
            overflow: hidden;
            color: #FFFFFF;
            width: 100px;
        }
    </style>
    <style type="text/css">
        .jqmWindow {
            display: none;    
            position: fixed;
            top: 17%;
            left: 50%;    
            margin-left: -300px;
            width: 600px;    
            background-color: #EEE;
            color: #333;
            border: 1px solid black;
            padding: 12px;
            z-index: 500;
        }
        .jqmOverlay { background-color: #000; }
    </style>
	<link rel="stylesheet" href="http://bcs.91rb.com/rbpiczy/client91_cache/jquery/jqModal.css" />

    <script type="text/javascript" src="http://bcs.91rb.com/rbpiczy/client91_cache/jquery/jquery-1.7.2.min.js"></script>
    <script type="text/javascript" src="http://bcs.91rb.com/rbpiczy/client91_cache/jquery/jqModal.js"></script>

    <script type="text/javascript" src="http://bcs.91rb.com/rbpiczy/client91_cache/ui.tabs/ui.tabs.js"></script>
    <link rel="stylesheet" href="http://bcs.91rb.com/rbpiczy/client91_cache/ui.tabs/ui.tabs.css" type="text/css" media="print, projection, screen" />

    <script type="text/javascript" src="http://bcs.91rb.com/rbpiczy/client91_cache/other/rowColor.js"></script>

    <script type="text/javascript" src="PlanAdmin.js"></script>
    <script type="text/javascript" src="PlanFileAdmin.js"></script>
    <script type="text/javascript" src="PlanAdminOther.js"></script>
    <script type="text/javascript">
        // 修改行默认颜色，醒目一点，避免点错
        var __colorOver = '#c25066'; //鼠标进入时行的颜色
        var __colorOut = '#ffffff'; //鼠标离开时行的颜色
        var __colorClick = '#fd8d89'; //鼠标单击时行的颜色

        $().ready(function () {
            //先在页面创建一个层
            var jqtip = $("<div id='jqtip20130719'" +
            "style='padding: 10px; " +
            "border: 1px solid red;" +
            "background-color: white; " +
            "position: absolute; " +
            "z-index:10001;" +
            "display: none; " +
            "font-family: 宋体; " +
            "font-size: 12px'>fafsdfsa" +
            "</div>");
            $(document.body).append(jqtip);

            // 初始化标签
            (new UI_TAB()).init("container-1");

            // 初始化对话框
            $('#dialog').jqm({ modal: true });
            
            $(document).keyup(function(event) {
                if(event.which == 27)// 按下esc
                    hideDialog();
            });
            
            addAdminDel("divAdminServers");
            addAdminDel("divAdminIp");
            refreshServerIP();
        });
        (function (window, undefined) {
            var tips = {
                init:function(callback){
                    $("[title]").css({
                        'cursor': 'help'
                    }).bind("focus", function () {
                        var tit = this.getAttribute('title');
                        this.setAttribute('msg', tit);
                        this.setAttribute('title', '');
                        $("#jqtip20130719").html(tit).css({
                            'left': $(this).position().left,
                            'top': $(this).position().top + 20
                        }).show();
                    });
                    $("[title]").bind("blur", function () {
                        this.setAttribute('title', this.getAttribute('msg'));
                        $("#jqtip20130719").hide();
                        if (callback)
                            callback();
                    });
                }
            };
            window.tip = tips;
        })(window);
    </script>
</head>
<body style='font-size:13px;'>
    <asp:Label runat="server" ID="labCommon" Text="657541DB7DDE258FE2C905B1B361A039"></asp:Label>
    <asp:Label runat="server" ID="labCommonOther" Text="82cc60100fa1557bf1f2b6eb0acea502"></asp:Label>
    <asp:Label runat="server" ID="labMain" Text="8f13267a0d8a9ea0b964e7b5e7b36eb5"></asp:Label>
<div>
    <div style="background-color:greenyellow">
        <label><input type="checkbox" onclick="chkAll(this);" />全选服务器</label>
        <label><input type="checkbox" id="chkOne" onclick="doCheckOne(this);" checked="checked"/>只允许单选</label>
　　      |Web服务器ip：<%=m_localIp%>
          |<a href="javascript:void(0);" onclick="logout();">重新登录</a>
    </div>
    <div id="spnAdminServers"></div>
    IP列表：<input type="text" id="txtIp" style="width:90%;" value="127.0.0.1" />
    <hr style="border-color: greenyellow"/>
</div>
<div id="container-1">
    <ul class="ui-tabs-nav">
        <li class="ui-tabs-selected"><a href="#fragment1"><span>计划任务管理</span></a></li>
        <li class=""><a href="#fragment2"><span>文件管理</span></a></li>
        <%if (m_isAdmin){%>
        <li class=""><a href="#fragment3"><span>服务器列表管理</span></a></li>
        <li class=""><a href="#fragment4"><span>权限管理</span></a></li>
        <%} %>
        <li class=""><a href="#fragment5"><span>进程查看</span></a></li>
    </ul>
    <!-- 计划任务管理 -->
    <div style="display: block;" class="ui-tabs-panel ui-tabs-hide" id="fragment1">
        <input type="button" value="读取所有任务" onclick="readDb(<%=(int)OperationType.GetAllTasks%>);" />　
        <div id="divmsg" style="color:Red;"></div>
        <div id="divret" style="width:1500px;"></div>
        <hr />    
        <pre>
计划任务程序使用说明（<a href="http://searchadmin.sj.91.com/planService.rar">程序下载</a>）：
    1、拷贝程序到服务器上，然后执行目录下的installService.bat，安装服务，安装完成后服务会自动启动；
    2、开通这台服务器的23244端口入站权限，开给web管理机ip：10.1.240.188 
       外网服务器要开给web管理机的外网ip：121.207.240.188
    3、把服务器的ip和你的个人电脑ip提交给管理员，让管理员添加个人ip对服务器的管理权限
    4、设置HOST
       10.1.240.188 searchadmin.sj.91.com 
    5、管理计划任务，进入页面：http://searchadmin.sj.91.com/planadmin.aspx
       在“读取任务”按钮左侧的文本框内输入服务器ip，点击“读取任务”即可
    注意：请参考程序目录下的exe.Config文件里的注释说明，以开通或关闭文件管理等功能

计划任务配置说明：(默认情况下，计划任务每10秒轮询一次数据库，并按运行类型处理所有任务)
    id：任务序号，唯一标识符，主键
    说明：任务简要说明
    exe路径：任务对应的可执行文件，如:e:\abc\def.exe <span style="color:Red;">注意：路径必须唯一，即任意2个任务的exe路径不能相同</span>
    exe参数：可执行文件所需的参数
    运行类型：
            不启动：不启动任务，如果前次启动了该任务且运行中，<span style="color:Red;font-weight:bold;">不处理</span>
            停止进程：不启动任务，如果前次启动了该任务且运行中，则<span style="color:Red;font-weight:bold;">强制退出进程</span>
            只运行一次：如果任务未启动，则启动任务，任务自然退出后，不再启动（启动任务后，会自己更新数据库里的运行类型为停止）
            一直运行：保持任务运行，如果任务自然退出或意外关闭，会自动重新启动任务
            重启：立即启动任务，如果任务已经在运行中，则强行终止任务再启动任务
            等1分钟重启：等候1分钟再启动任务，如果任务已经在运行中，则强行终止任务再等1分钟（通常用于程序发布）
            每天定时运行：根据任务参数配置，在每天的指定时间点启动，并运行指定时长
            每周定时运行：根据任务参数配置，在每周的指定时间点启动，并运行指定时长
            每月定时运行：根据任务参数配置，在每月的指定时间点启动，并运行指定时长
    任务参数：对应于运行类型的每天、每周、每月,参数格式：1-12:34|10;2-12:34|0;
            多个参数以分号分隔；每个参数-前面是表示周几或几号，后面是启动时间|运行时长(分钟)
    运行次数：任务从创建到现在为止，一共启动了多少次
    pid：最近一次启动任务时，对应的进程id
    pid时间：最近一次启动任务的时间
    创建时间：任务插入数据库的时间

    单次操作下的“<span style="color:blue;font-weight:bold;">启|停|重</span>”：
        用于单次操作任务
            “启”，立即启动任务，然后继续按“运行类型”进行工作
            “停”，立即停止任务，然后继续按“运行类型”进行工作
            “重”，立即重启任务，然后继续按“运行类型”进行工作        
</pre>
    </div>

    <!-- 文件管理 -->
    <div class="ui-tabs-panel ui-tabs-hide" id="fragment2">
        <table style="table-layout:fixed;">
            <tr>
                <td colspan="2" style="text-align:center; font-weight:bold; color:Green;">
                    <a href="javascript:void(0);" onclick="fileUpload();">上传..</a>
                    ｜
                    <a href="javascript:void(0);" onclick="fileZipDown();" title="打包下面选中的下载目录或文件">ZIP打包下载</a>
                    ｜
                    <a href="javascript:void(0);" onclick="fileMove();" title="把选中的目录和文件移动到指定的目录">移动选中..</a>
                    ｜
                    <a href="javascript:void(0);" onclick="fileDelSelect();" title="删除下面选中的目录和文件">删除选中</a>
                    ｜
                    <a href="javascript:void(0);" onclick="fileCreateDir();">创建目录..</a>
                    ｜
                    <a href="javascript:void(0);" onclick="fileUnZip();">解压ZIP</a>
                    <hr />
                </td>
            </tr>
            <tr>
                <td>网站根目录</td>
                <td>
                    <input type="text" style="width:600px" disabled="disabled" value="<%=Server.MapPath("") %>"/>
                </td>
            </tr>
            <tr>
                <td>显示的目录</td>
                <td>
                    <input type="text" id="fileDir" style="width:600px" value="E:\WebLogs"/>
                </td>
            </tr>
            <tr>
                <td colspan="2">
                    <span style="color:royalblue;font-weight: bold;">目录或文件排序:</span>
                    <select id="lstFileSort">
	                    <option value="<%=(int)SortType.Name %>">文件名</option>
	                    <option value="<%=(int)SortType.NameDesc %>">文件名倒序</option>
	                    <option value="<%=(int)SortType.Extention %>">扩展名</option>
	                    <option value="<%=(int)SortType.ExtentionDesc %>">扩展名倒序</option>
	                    <option value="<%=(int)SortType.Size %>">文件大小</option>
	                    <option value="<%=(int)SortType.SizeDesc %>">文件大小倒序</option>
	                    <option value="<%=(int)SortType.ModifyTime %>">修改日期</option>
	                    <option value="<%=(int)SortType.ModifyTimeDesc %>">修改日期倒序</option>
                    </select>
                    　<label style="color: royalblue;font-weight: bold"><input type="checkbox" id="chkShowFileMd5" />显示MD5</label>
                    　<input type="button" id="fileBtnGetDir" onclick="fileManager();" value="获取" style="width:100px; font-weight: bold"/>
                </td>
            </tr>
        </table>
        <hr/>
        <div id="divFileRet"></div>
    </div>

    <%if (m_isAdmin){%>
    <!-- 服务器列表管理 -->
    <div class="ui-tabs-panel ui-tabs-hide" id="fragment3">
        说明:<input type="text" id="txtAdminServerDesc" style="width:200px;" value="" size="50"/>　
        服务器IP:<input type="text" id="txtAdminServer" style="width:300px;" value="10.1.240.1"/><br/>
        <input type="button" value="添加" onclick="addAdminServer()"/>　
        多个服务器ip以分号分隔
        <hr/>
        <div id="divAdminServers">
        <%=AdminDal.GetAdminServers() %>
        </div>
    </div>

    <!-- 权限管理 -->
    <div class="ui-tabs-panel ui-tabs-hide" id="fragment4">
        说明:<input type="text" id="txtAdminDesc" style="width:200px;" value="" size="50"/>　
        客户端IP:<input type="text" id="txtAdminIp" style="width:300px;" value="192.168.254.58"/><br/>
        <input type="button" value="添加" onclick="addAdminIP()"/> 多个客户端IP以分号分隔，请在最上方选择该客户端有权限的服务器IP
        <hr/>
        <div id="divAdminIp">
        <%=AdminDal.GetAllRightTable() %>
        </div>
    </div>
    <%} %>

    <!-- 进程查看 -->
    <div class="ui-tabs-panel ui-tabs-hide" id="fragment5">
        <input type="button" value="获取进程列表" onclick="getProcesses()"/>
        <hr/>
        <div id="divProcess"></div>
    </div>

</div>
    
<div class="jqmWindow" id="dialog">
    <a href="javascript:void(0);" class="jqmClose" id="dialogCancel">取消</a>　<a href="javascript:void(0);" id="dialogConfirm">确定</a>
    <hr />
    <div id="dialogContent"></div>
</div>

<iframe name="hiddenIFrame" width="100%" height="100" frameborder="0" scrolling="no"></iframe>
</body>
</html>
