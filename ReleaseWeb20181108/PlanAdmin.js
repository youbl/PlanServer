
// 修改行默认颜色，醒目一点，避免点错
var __colorOver = '#c25066'; //鼠标进入时行的颜色
var __colorOut = '#ffffff'; //鼠标离开时行的颜色
var __colorClick = '#fd8d89'; //鼠标单击时行的颜色


$().ready(function () {
    // 初始化tab标签
    (new UI_TAB()).init("container-1");

    // 初始化弹出的对话框
    $('#dialog').dialog({
        autoOpen: false,
        modal: true
    });

    $(document).keyup(function (event) {
        if (event.which === 27)// 按下esc
            hideDialog();
    });

    addAdminDel("divAdminIp");
    // 列出所有服务器，允许勾选
    refreshServerIP();
});

var typeoption = "<select>" +
            "<option value='0'>不启动</option>" +
            "<option value='8'>停止进程</option>" +
            "<option value='1'>只运行一次</option>" +
            "<option value='2'>一直运行</option>" +
            "<option value='3'>重启</option>" +
            "<option value='4'>等1分钟重启</option>" +
            "<option value='5'>每天定时运行</option>" +
            "<option value='6'>每周定时运行</option>" +
            "<option value='7'>每月定时运行</option>" +
            "</select>";

function logout() {
    var para = {};
    ajaxSend(para, 2048, function () {
        var url = location.href;
        var idx = url.indexOf("#");
        if (idx > 0)
            url = url.substring(0, idx);
        location.href = url;
    });
}

function checkAdminServer() {
    if ($("#spnAdminServers input:checked").length > 0) {
        $("#spnAdminServers input:checkbox").prop("checked", false);
    } else {
        $("#spnAdminServers input:checkbox").prop("checked", true);
    }
}

function refreshServerIP() {
    var para = {};
    ajaxSend(para, 2053, function (backdata) {
        backdata = "var arr = " + backdata;
        eval(backdata);
        var len = arr.length;
        if (len <= 0) {
            $("#spnAdminServers").html("没有数据");
            return;
        }
        var html = "<table border='0'>";
        var lastDesc = "";
        for (var i = 0, j = len; i < j; i++) {
            var ip = arr[i][0];
            var desc = arr[i][1];
            if (lastDesc != desc) {
                if (lastDesc != "") {
                    html += "</td></tr>";
                }
                lastDesc = desc;
                html += "<tr onmouseover='onRowOver(this);' onmouseout='onRowOut(this);' onclick='onRowClick(this);'><th><label><input type='checkbox' onclick='checkProjectAll(this);'/>" + desc + ":</label></th><td>";
            }
            html += "<label><input type='checkbox' value='" + ip + "' onclick='serverClick(this);'/>" + ip + "</label><a href='http://" + ip + "/' target='_blank'>…</a>　";
        }
        html += "</table>";
        $("#spnAdminServers").html(html);
    });
}

function refreshServerList() {
    var para = {};
    ajaxSend(para, 2054, function (backdata) {
        var id = "divAdminServers";
        $("#" + id).html(backdata);
        refreshServerIP();
    });
}

function addAdminServer() {
    var para = {};
    para.sip = $("#txtAdminServer").val();
    para.desc = $("#txtAdminServerDesc").val();
    if (para.sip == null || para.sip.length == 0) {
        alert("请输入IP");
        return;
    }
    if (para.desc == null || para.desc.length == 0) {
        alert("请输入说明");
        return;
    }
    ajaxSend(para, 2051, function (backdata) {
        var id = "divAdminServers";
        $("#" + id).html(backdata);
        refreshServerIP();
    });
}

function delAdminServer() {
    var sip = "";
    var num = 0;
    $("#spnAdminServers input:checked").each(function () {
        sip += $(this).val() + ",";
        num++;
    });
    if (num == 0) {
        alert("请选择服务器IP");
        return;
    }
    if (!confirm("您确实要删除这" + num + "条服务器记录吗？相应的权限记录也会被删除\n" + sip)) {
        return;
    }
    var para = {};
    para.id = sip;
    ajaxSend(para, 2052, function (backdata) {
        var id = "divAdminServers";
        $("#" + id).html(backdata);
        refreshServerIP();
    });
}

function addAdminIP() {
    var sip = "";
    $("#spnAdminServers input:checked").each(function () {
        sip += $(this).val() + ",";
    });
    if (sip == null || sip.length == 0) {
        alert("请选择服务器IP");
        return;
    }
    var para = {};
    para.cip = $("#txtAdminIp").val();
    para.sip = sip;
    para.desc = $("#txtAdminDesc").val();
    if (para.cip == null || para.cip.length == 0) {
        alert("请输入客户端IP");
        return;
    }
    if (para.desc == null || para.desc.length == 0) {
        alert("请输入说明");
        return;
    }
    ajaxSend(para, 2049, function (backdata) {
        var id = "divAdminIp";
        $("#" + id).html(backdata);
        addAdminDel(id);
    });
}

function addAdminDel(id) {
    var isServer;
    if (id == "divAdminServers") {
        return;// 服务器不做处理
    } else {//if (id == "divAdminIp") {
        isServer = false;
    }
    var adminHtml = "<a href='javascript:void(0);' onclick='copyIp(this, " + (isServer ? "1" : "0") + ");'>复制到文本框</a>" +
        "　<a href='javascript:void(0);' onclick='delAdminIp(this);'>删除</a>";
    $("#" + id + " tr:gt(0)").each(function () {
        $(this).bind("mouseover", function () { onRowOver(this); });
        $(this).bind("mouseout", function () { onRowOut(this); });
        $(this).bind("click", function () { onRowClick(this); });
        $(this).find("td:last").html(adminHtml);
    });
}

function copyIp(obj, isServer) {
    obj = $(obj);
    var ip, desc;
    if (isServer == 1) {
        ip = obj.parents("tr").find("td:eq(1)").text();
        desc = obj.parents("tr").find("td:eq(2)").text();
        $("#txtAdminServerDesc").val(desc);
        $("#txtAdminServer").val(ip);
    } else {
        ip = obj.parents("tr").find("td:eq(1)").text();
        desc = obj.parents("tr").find("td:eq(3)").text();
        $("#txtAdminDesc").val(desc);
        $("#txtAdminIp").val(ip);
    }
}

function delAdminIp(obj) {
    obj = $(obj);
    var id = obj.parents("tr").find("td:first").text();
    id = $.trim(id);
    if (id.length <= 0) {
        alert("未找到id");
        return;
    }
    var para = {};
    para.id = id;

    var flg;
    var div = obj.parents("div[id^='divAdmin']:first");
    var id2 = div.attr("id");
    if (id2 == "divAdminIp")
        flg = 2050;
    else
        return;
    ajaxSend(para, flg, function (backdata) {
        div.html(backdata);
        addAdminDel(id2);
    });
}

function doCheckOne(obj) {
    if ($(obj).is(":checked")) {
        $("#spnAdminServers th input:checkbox").attr("checked", false);
        
        var firstCheckFind = false;
        // 只允许单选时，只保留第一个选项
        $("#spnAdminServers td input:checkbox").each(function () {
            if (firstCheckFind)
                $(this).attr("checked", false);
            else if ($(this).is(":checked"))
                firstCheckFind = true;
        });
        serverClick();
    }
}
function chkAll(obj) {
    var chkOne = $("#chkOne").is(":checked");
    var checked = $(obj).is(":checked");
    if (chkOne && checked) {
        alert("你选择了只能单选, 无法全选");
        return;
    }
    $('#spnAdminServers input:checkbox').prop('checked', checked);
    serverClick();
}

function serverClick(obj) {
    obj = $(obj);
    var chkOne = $("#chkOne").is(":checked");
    if (chkOne) {
        $("#spnAdminServers td input:checkbox").attr("checked", false);
        obj.prop("checked", true);
    }
    var ips = "";
    $("#spnAdminServers td input:checkbox:checked").each(function () {
        ips += $(this).parent().text() + ";";
    });
    if (ips.length > 0)
        ips = ips.substring(0, ips.length - 1);
    $("#txtIp").val(ips);
}

function checkProjectAll(obj) {
    obj = $(obj);
    var isCheck = obj.is(":checked");
    var chkOne = $("#chkOne").is(":checked");
    if (chkOne && isCheck) {
        obj.prop("checked", false);
        alert("你选择了只能单选, 无法多选");
        return;
    }
    obj.parents("tr:first").find("td input:checkbox").attr("checked", isCheck);
    serverClick();
}

// 读取数据库全部行显示
function readDb(flg) {
    $("#divmsg").html("");
    //$(obj).attr("disabled", "disabled");

    var para = {};
    para.tip = $.trim($("#txtIp").val());
    ajaxSend(para, flg, function (backdata) {
        //$(obj).removeAttr("disabled");
        
        if (backdata.indexOf("<table") <= 0) {
            backdata = backdata.replace(/[\r\n]+/g, "<br/>");
            $("#divret").html(backdata);
            return;
        }

        $("#divret").html(backdata);
        var trs = $("#tbData").find("tr");
        //$("#divmsg").html((trs.length - 1) + "条记录");
        trs.each(function () {
            // 填充并设置运行类型值
            var td = $(this).find("td:eq(4)");
            td.html(typeoption);
            td.find("select").val(td.attr("v"));
        });
    });
}

function showTaskLog(obj, flg) {
    var tr = $(obj).parent();
    var para = {};
    para.exepath = $.trim(tr.find("input:eq(1)").val());
    para.server = $.trim(tr.find("td:eq(0)").text());
    if (para.exepath.length === 0) {
        alert("请输入可执行文件路径");
        return;
    }
    ajaxSend(para, flg, function (backdata) {

        showDialog(backdata, null, 1000, 500);
    });
}

// 保存行
function saverow(id, obj, flg) {
    var tr = $(obj).parent().parent();

    var para = {};
    para.desc = $.trim(tr.find("input:eq(0)").val());
    para.exepath = $.trim(tr.find("input:eq(1)").val());
    para.exepara = $.trim(tr.find("input:eq(2)").val());
    para.server = $.trim(tr.find("td:eq(0)").text());
    if (para.desc.length === 0) {
        alert("请输入说明");
        return false;
    }
    if (para.exepath.length === 0) {
        alert("请输入可执行文件路径");
        return false;
    }
    
    //if (id != -1)
        para.id = id;

    para.runtype = tr.find("select").val();
    para.taskpara = $.trim(tr.find("input:eq(3)").val());
    ajaxSend(para, flg, function (backdata) {
        if (backdata.indexOf("<table") <= 0) {
            if (backdata.indexOf("column exepath is not unique") >= 0) {
                backdata = "exe路径必须唯一，每个任务的exe路径必须跟其它任务不同";
            } else {
                backdata = backdata.replace(/[\r\n]+/g, "<br/>");
            }
            $("#divmsg").html(backdata);
            return;
        }
        var trServer = tr;
        while (!trServer.hasClass("server")) {
            var old = trServer;
            trServer = trServer.prev();
            
            old.remove();
            if (trServer[0].tagName != "TR") {
                trServer = null;
                break;
            }
        }
        if (trServer == null)
            $("#divret").html(backdata);
        else {
            var row = clearServerRow(tr);
            $(backdata).find("tr:gt(0)").each(function () {
                row.after(this);
                row = row.next();
            });
        }

        initTable();

        //$("#divmsg").html((trs.length - 1) + "条记录");
        alert("保存成功");
    });
    return true;
}

function clearServerRow(tr) {
    var ip = $.trim(tr.find("td:eq(0)").text());
    var prevRow = $("#tbData tr:eq(0)"); // 默认插入第1行后
    var findIp = false;
    // 移除所有这个ip的行
    $("#tbData tr").each(function () {
        var obj = $(this);
        if ($.trim(obj.find("td:eq(0)").text()) == ip) {
            obj.remove();
            findIp = true;
        }else if (!findIp)
            prevRow = obj;
    });
    return prevRow;
}

function initTable() {
    var trs = $("#tbData").find("tr");
    trs.each(function () {
        // 填充并设置运行类型值
        var td = $(this).find("td:eq(4)");
        td.html(typeoption);
        td.find("select").val(td.attr("v"));
    });
}

// 添加新行
function addrow(obj) {
    var tr = $(obj).parent().parent();
    var newrow = "<td>" + tr.find("td:eq(0)").text() + "</td>" +
                "<td><input type='text' style='width:97%;' /></td>" +   // 说明
                "<td><input type='text' style='width:97%;' /></td>" +   // exe路径
                "<td><input type='text' style='width:92%;' /></td>" +   // exe参数
                "<td>" + typeoption + "</td>" +   // 运行类型
                "<td><input type='text' style='width:97%;' onclick='setPara(this)' readonly='readonly' /></td>" +   // 任务参数
                "<td><a href='#-1' onclick='saverow(-1,this,1);'>保存</a>&nbsp;<a href='javascript:void(0);' onclick='delrow(-1,this,2);'>删除</a>";
    
    tr.after("<tr>" + newrow + "</tr>");
}

// 删除行
function delrow(id, obj, flg) {
    var tr = $(obj).parent().parent();
    if (id == -1) {
        // 新增的行，直接前端删除，无须请求服务器
        tr.remove();
        return;
    }
    var taskName = tr.find("td:eq(1)").find("input").val();
    var server = $.trim(tr.find("td:eq(0)").text());
    var status = $.trim(tr.find("td:eq(8)").text());
    if (status.toLowerCase() == "running") {
        alert(server + "上的任务：" + taskName + " 正运行中，请先停止进程");
        return;
    }
    if (!confirm("确实要删除" + server + "上的任务：" + taskName + " 吗?")) {
        return;
    }

    $(obj).attr("disabled", "disabled");
    var para = {};
    para.id = id;
    para.server = server;
    ajaxSend(para, flg, function (backdata) {
        alert(backdata);
        $("#divmsg").html(backdata);
        tr.remove();
    });
}

// 立即操作
function operateImm(obj, imtype, flg) {
    var tr = $(obj).parent().parent();
    var para = { };
    para.exepath = $.trim(tr.find("input:eq(1)").val());
    para.exepara = $.trim(tr.find("input:eq(2)").val());
    para.imtype = imtype;
    para.server = $.trim(tr.find("td:eq(0)").text());

    ajaxSend(para, flg, function (backdata) {
        alert(backdata);
        $("#divmsg").html(backdata);
    });
}

// 运行方法
function runMethodOpen(obj, flg) {
    var tr = $(obj).parent().parent();
    var row = "方法全名:<input type='text' style='width:500px;' id='txtMethod' " +
        "value='PlanServerExtend.ExtendClass.GetServerIpList,p.dll' /><br/>" +
        "<div id='divMethodRet' style='color:blue;'></div>";
    showDialog(row ,function () {
        var m = $.trim($("#txtMethod").val());

        if (m.length === 0) {
            alert("没有设置方法名");
            return;
        }
        var para = {};
        para.method = m;
        para.server = $.trim(tr.find("td:eq(0)").text());
        ajaxSend(para, flg, function (backdata) {
            //alert(backdata);
            $("#divMethodRet").html(backdata);
        });
        //hideDialog();
    });
}

function ajaxSend(para, flg, callback, dataType) {
    para.flg = flg;
    showDialog("<span style='color:blue;font-size:20px;'>请稍候……</span>");
    if (dataType == undefined)
        dataType = "text";
    
    //var url = location.href;
    $.ajax({
        //url: "",
        dataType: dataType,
        cache: false,
        type: "POST",
        data: para,
        success: function (backdata) {
            hideDialog();
            if (callback != null)
                callback(backdata);
        },
        error: ajaxError
    });
}


function ajaxSendJson(para, flg, callback) {
    ajaxSend(para, flg, callback, "json");
}

// ajax失败时的回调函数
function ajaxError(httpRequest, textStatus, errorThrown) {
    // 通常 textStatus 和 errorThrown 之中, 只有一个会包含信息
    //this; // 调用本次AJAX请求时传递的options参数
    hideDialog();
    alert(textStatus + errorThrown);
}
function showDialog(html, confirmClick, width, height) {
    $("#dialogContent").html(html);

    if (!width) {
        if (confirmClick) {
            width = 600;
            height = 300;
        } else {
            width = 200;
            height = 100;
        }
    }
    if (confirmClick) {
        $('#dialog').dialog('option', 'width', width);
        $('#dialog').dialog('option', 'height', height);
        $('#dialog').dialog('option', 'buttons', {
            "确认按钮": confirmClick,
            Cancel: function() {
                hideDialog();
            }
        });
    } else {
        $('#dialog').dialog('option', 'buttons', []);
        $('#dialog').dialog('option', 'width', width);
        $('#dialog').dialog('option', 'height', height);
    }
    $("#dialog").dialog('open');
}
function hideDialog() {
    $("#dialog").dialog('close');
}






///**********************************************************************/
/// 开始：任务参数配置的js代码
///**********************************************************************/
// 设置任务参数 弹窗所需的函数
// 设置任务参数
function setPara(obj) {
    var row = '<table border="1" cellspacing="0" cellpadding="1">';
    var tr = $(obj).parent().parent().parent();

    var oldVal = $(obj).val();
    var runtype = tr.find("select").val();
    switch (runtype) {
        case "5": //每天定时运行
            row += '<tr>';
            break;
        case "6": //每周定时运行
            row += '<tr><th>周几运行(0为周日)</th>';
            break;
        case "7": //每月定时运行
            row += '<tr><th>几号运行</th>';
            break;
        default:
            return;
    }
    row += '<th>启动时间</th><th>运行时长(分钟)</th><th>结束时间</th><th>操作</th></tr>';

    var tmp = oldVal.split(';');
    for (var i = 0; i < tmp.length; i++) {
        if (runtype == "5")
            tmp[i] = "1-" + tmp[i];
        var itemtmp = tmp[i].split('-');
        if (itemtmp.length != 2)
            continue;
        var weekOrDay = parseInt(itemtmp[0], 10); //周几 或 每月几号
        if (isNaN(weekOrDay) ||
                    (runtype == "6" && (weekOrDay < 0 || weekOrDay > 6)) || // 每周运行，必须是周日到周六之间
                    (runtype == "7" && (weekOrDay < 1 || weekOrDay > 31)))  // 每月运行，必须是1到31号之间
            continue;
        var valuetmp = itemtmp[1].split(',');
        for (var j = 0; j < valuetmp.length; j++) {
            var startEnd = valuetmp[j].split("|");
            var timetmp = startEnd[0].split(':');
            // 必须配置启动时间
            if (timetmp.length != 2)
                continue;
            var hourStart = parseInt(timetmp[0], 10);
            var minStart = parseInt(timetmp[1], 10);
            if (isNaN(hourStart) || isNaN(minStart))
                continue;
            var runtime = parseInt(startEnd[1], 10);
            if (isNaN(runtime))
                runtime = 0;

            row += '<tr>';
            if (runtype != "5")
                row += "<td>" + weekOrDay + "</td>";
            var endtime = countEndTime(hourStart, minStart, runtime);
            row += '<td>' + formatHM(hourStart, minStart) + '</td><td>' + runtime + '</td>' +
                        '<td>' + endtime + '</td><td><a href="javascript:void(0);" onclick="delPara(this);">删除</a></td></tr>';
        }
    }
    row += "</table>";
    var opn = "每天";
    if (runtype == "6")
        opn = "<select id='para0'>" +
                    "<option value='0'>每周日</option>" +
                    "<option value='1'>每周一</option>" +
                    "<option value='2'>每周二</option>" +
                    "<option value='3'>每周三</option>" +
                    "<option value='4'>每周四</option>" +
                    "<option value='5'>每周五</option>" +
                    "<option value='6'>每周六</option>" +
                    "</select>";
    else if (runtype == "7") {
        opn = "每月几号：<select id='para0'>";
        for (var ii = 1; ii < 32; ii++)
            opn += "<option value='" + ii + "'>" + ii + "号</option>";
        opn += "</select>";
    }
    //$("#valEnd").val(oldVal);

    row = "<div>" + opn +
                "几点:<input id='para1' type='text' value='12:34' style='width:40px' />" +
                "时长:<input id='para2' type='text' value='600' style='width:40px' />分钟(0表示不停止)" +
                "<input type='button' value='添加' onclick='addPara();' />" +
                "</div><br />" +
                row + "<br />最终参数:<input id='valEnd' type='text' value='" + oldVal + "' style='width:500px;'/><hr />" +
        '<div style="color: blue;">注1:未判断时间交叉情况，请自行判断<br/>' +
        '注2:定时运行时，每一项都会进行启动判断，比如配置为<br />' +
        '　　<span style="font-weight: bold">每天8点启动且运行120分钟；每天9点启动且不结束</span><br />' +
        '　　那么9点前程序被退出时，在9点又会被启动，且10点会结束程序(8点加120分钟等于10点)<br/>' +
        '注3:定时运行时，每一项都会进行结束判断，比如配置为<br />' +
        '　　<span style="font-weight: bold">每天8点启动且不结束；每天9点启动且运行10分钟</span><br />' +
        '　　那么9点10程序会被结束，即使程序并不是9点启动的<br/></div>';
    showDialog(row, function () {
        var para = $("#valEnd").val();

        if (para.length === 0) {
            alert("没有设置运行参数，参数将配置为空");
        }
        $(obj).val(para);
        hideDialog();
    });

}

function addPara() {
    var row = '<tr>';
    if ($("#para0").length == 1) {
        var wd = $.trim($("#para0").val());
        if (!(/^\d+$/.test(wd))) {
            alert("日期设置错误");
            return;
        }
        row += "<td>" + wd + "</td>";
    }
    var tm = $.trim($("#para1").val());
    if (!(/^\d{1,2}:\d{1,2}$/.test(tm))) {
        alert("时间设置错误");
        return;
    }
    var tmpHm = tm.split(':');
    var hourStart = tmpHm[0], minStart = tmpHm[1];
    row += "<td>" + formatHM(hourStart, minStart) + "</td>";

    var rtm = $.trim($("#para2").val());
    if (!(/^\d+$/.test(rtm))) {
        alert("运行时长设置错误");
        return;
    }
    row += "<td>" + rtm + "</td>";
    var endtime = countEndTime(parseInt(hourStart, 10), parseInt(minStart, 10), parseInt(rtm, 10));
    row += "<td>" + endtime + "</td>";
    row += '<td><a href="javascript:void(0);" onclick="delPara(this);">删除</a></td></tr>';

    $(".jqmWindow table").append(row);
    combinePara();
}
function delPara(obj) {
    $(obj).parents("tr").remove();
    combinePara();
}
function combinePara() {
    // 收集
    var arrpara = [];
    $(".jqmWindow table tr:gt(0)").each(function () {
        var p = { day: -1, tm: "", min: "" };
        arrpara.push(p);
        var tdi = 0;
        if ($("#para0").length == 1) {
            p.day = $(this).find("td:eq(0)").text();
            tdi = 1;
        }
        p.tm = $(this).find("td:eq(" + tdi + ")").text();
        p.min = $(this).find("td:eq(" + (tdi + 1) + ")").text();
    });
    // 排序
    QuickSort(arrpara);

    // 组合
    var para = "";
    for (var i = 0; i < arrpara.length; i++) {
        var ip = arrpara[i];
        if (i > 0 && ip.day == arrpara[i - 1].day && ip.tm == arrpara[i - 1].tm) {
            alert("存在重复时间配置");
            $("#valEnd").val("");
            return false;
        }
        if (ip.day != -1)
            para += ip.day + "-";
        para += ip.tm + "|";
        para += ip.min + ";";
    }
    $("#valEnd").val(para);
    return true;
}
function countEndTime(startHour, startMin, runMinute) {
    var endtime;
    if (runMinute <= 0)
        endtime = "不自动终止";
    else {
        // 计算运行时长
        var day = Math.floor(runMinute / 1440);   // 几天 每天是1440分钟=24*60
        var tmpMin = runMinute - day * 1440;
        var hour = Math.floor(tmpMin / 60);     // 几小时
        var min = tmpMin - hour * 60;           // 几分

        endtime = "运行";
        if (day > 0)
            endtime += day + "天";
        if (hour > 0)
            endtime += hour + "小时";
        if (min > 0)
            endtime += min + "分钟";

        // 计算具体结束时间
        min += startMin;
        if (min >= 60) {
            min -= 60;
            hour += 1;
        }
        hour += startHour;
        if (hour >= 24) {
            hour -= 24;
            day += 1;
        }
        endtime += ";将于";
        if (day == 0)
            endtime += "当天";
        else if (day == 1)
            endtime += "次日";
        else if (day > 1)
            endtime += "第" + (day) + "天的";
        endtime += formatHM(hour, min) + "结束";
    }
    return endtime;
}
function formatHM(hour, min) {
    hour = hour.toString();
    hour = hour.length < 2 ? "0" + hour : hour;
    min = min.toString();
    min = min.length < 2 ? "0" + min : min;
    return hour + ":" + min;
}
//交换排序->快速排序
function QuickSort(arr) {
    var low, high;
    if (arguments.length > 1) {
        low = arguments[1];
        high = arguments[2];
    } else {
        low = 0;
        high = arr.length - 1;
    }
    if (low < high) {
        // function Partition
        var i = low;
        var j = high;
        var pivot = arr[i];
        while (i < j) {
            while (i < j && compareTo(arr[j], pivot) <= 0)//
                j--;
            if (i < j)
                arr[i++] = arr[j];
            while (i < j && compareTo(arr[j], pivot) >= 0)//
                i++;
            if (i < j)
                arr[j--] = arr[i];
        } //endwhile
        arr[i] = pivot;
        // end function
        var pivotpos = i; //Partition(arr，low，high);
        QuickSort(arr, low, pivotpos - 1);
        QuickSort(arr, pivotpos + 1, high);
    }
}
function compareTo(a, b) {
    var d1 = parseInt(a.day);
    var d2 = parseInt(b.day);
    if (d1 > d2)
        return -1;
    else if (d1 < d2)
        return 1;
    if (a.tm > b.tm)
        return -1;
    else if (a.tm < b.tm)
        return 1;
    return 0;
}
///**********************************************************************/
/// 结束：任务参数配置的js代码
///**********************************************************************/
