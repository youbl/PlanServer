var fileGeting = false;

function getIP() {
    var ip = $.trim($("#txtIp").val());
    var arr = ip.split(";");
    if (arr[0] == "") {
        alert("请选择一个ip进行文件管理");
        return null;
    }
    if (arr.length > 1 && arr[1] != "") {
        alert("只能选择一个ip");
        return null;
    }
    return ip;
}

// 显示目录列表
function fileManager() {
    if (fileGeting) {
        alert("加载中，请稍候……");
        return;
    }

    var ip = getIP();
    if (ip == null)
        return;
    var txt = $("#fileDir").val();
    if (txt.length == 0) {
        alert("目录不能为空");
        return;
    }

    $("#fileBtnGetDir").attr("disabled", "disabled").val("处理中……");

    var para = {};
    para.tip = ip;
    para.dir = txt;
    para.sort = $("#lstFileSort").val();
    if ($("#chkShowFileMd5").is(":checked"))
        para.md5=1; // 显示md5

    fileGeting = true;
    try {
        ajaxSend(para, 100, function(backdata) {
            fileGeting = false;
            $("#fileBtnGetDir").removeAttr("disabled").val("获取");

            if (backdata.indexOf("<table") <= 0) {
                backdata = backdata.replace(/[\r\n]+/g, "<br/>");
                $("#divFileRet").html(backdata);
                return;
            }

            $("#divFileRet").html(backdata);
        });
    }
    catch (ee1) {
        fileGeting = false;
        $("#fileBtnGetDir").removeAttr("disabled").val("获取");
        alert("出错：" + ee1);
    }
}


// 上传文件
function fileUpload() {
    var ip = getIP();
    if (ip == null)
        return;

    var txt = $("#fileDir").val();
    var htm = "<form id='formUpload' action='?flg=111&tip=" + ip +
        "' enctype='multipart/form-data' target='hiddenIFrame' method='post'><p>" +
        "&nbsp;文件选择：<input type='file' name='file1' id='file1' style='width:400px;' /><br />" +
        "&nbsp;上传目录：<input type='text' name='fileUploadDir' value='" + txt +
        "' readonly='readonly' style='width:500px;background-color:gray'/></p></form>";

    showDialog(htm, function () {
        var fname = $("#file1").val();
        if (fname.length == 0) {
            alert("请选择要上传的文件");
            return;
        }
        $("#divFileRet").html("上传中，请稍候……");
        $("#formUpload").submit();
    });
}
// 打包下载
function fileZipDown() {
    var file = $("input:checkbox[name='chkFileListBeinet']:checked");
    var dir = $("input:checkbox[name='chkDirListBeinet']:checked");

    if (file.length == 0 && dir.length == 0) {
        alert("没有选中任何文件或目录");
        return;
    }
    var msg = "你确定要打包并下载选中的 ";
    if (file.length > 0)
        msg += file.length + "个文件 ";
    if (dir.length > 0)
        msg += dir.length + "个目录 ";
    if (!confirm(msg + "吗?"))
        return;

    var filestr = "";
    var dirstr = "";
    file.each(function () {
        filestr += $(this).val() + ",";
    });
    dir.each(function () {
        dirstr += $(this).val() + ",";
    });
    doZipDown(filestr, dirstr);
}
// zip下载单个目录
function dirZipDown(dir) {
    var msg = "你确定要打包并下载目录 " + dir + " 吗?";
    if (!confirm(msg))
        return;

    var filestr = "";
    var dirstr = dir;
    doZipDown(filestr, dirstr);
}
function doZipDown(filestr, dirstr) {
    var ip = getIP();
    if (ip == null)
        return;
    var txt = $("#fileDir").val();
    if (txt.length == 0) {
        alert("目录不能为空");
        return;
    }
    var para = {};
    para.tip = ip;
    para.dir = txt;
    para.files = filestr;
    para.dirs = dirstr;
    ajaxSend(para, 105, function (backdata) {
        if (backdata.substring(0, 2) != "ok") {
            alert(backdata);
            return;
        }
        var localfile = backdata.substring(2);
        if (localfile.length <= 0) {
            alert("获取成功，但是返回数据有误，请重试");
            return;
        }
        var html = "文件已成功打包并下载到web服务器，请下载<br/>" +
            "<a href='?flg=119&file=" + encodeURIComponent(localfile) + "&name=tmp.zip'>下载文件</a>";
        showDialog(html, null);
    });
}

// 移动选中
function fileMove() {
    var ip = getIP();
    if (ip == null)
        return;
    var txt = $("#fileDir").val();
    if (txt.length == 0) {
        alert("目录不能为空");
        return;
    }

    var file = $("input:checkbox[name='chkFileListBeinet']:checked");
    var dir = $("input:checkbox[name='chkDirListBeinet']:checked");

    if (file.length == 0 && dir.length == 0) {
        alert("没有选中任何文件或目录");
        return;
    }
    var htm = "移动到的目录：<input type='text' id='fileToDir' style='width:400px;' value='" + txt + "\\'/>";

    showDialog(htm, function () {
        var todir = $("#fileToDir").val();
        if (todir.length == 0) {
            alert("请输入移动到哪个目录");
            return;
        }
        var msg = "你确定要移动选中的";
        if (file.length > 0)
            msg += file.length + "个文件 ";
        if (dir.length > 0)
            msg += dir.length + "个目录 ";
        if (!confirm(msg + "吗?"))
            return;

        $("#divFileRet").html("处理中，请稍候……");
        var filestr = "";
        var dirstr = "";
        file.each(function () {
            filestr += $(this).val() + ",";
        });
        dir.each(function () {
            dirstr += $(this).val() + ",";
        });
        var para = {};
        para.tip = ip;
        para.dir = txt;
        para.files = filestr;
        para.dirs = dirstr;
        para.to = todir;
        ajaxSend(para, 110, function (backdata) {
            fileManager();
            alert(backdata);
        });
    });
}
// 删除选中
function fileDelSelect() {
    var file = $("input:checkbox[name='chkFileListBeinet']:checked");
    var dir = $("input:checkbox[name='chkDirListBeinet']:checked");

    if (file.length == 0 && dir.length == 0) {
        alert("没有选中任何文件或目录");
        return;
    }
    var msg = "你确定要删除选中的";
    if (file.length > 0)
        msg += file.length + "个文件 ";
    if (dir.length > 0)
        msg += dir.length + "个目录 ";
    if (!confirm(msg + "吗?"))
        return;

    var filestr = "";
    var dirstr = "";
    file.each(function () {
        filestr += $(this).val() + ",";
    });
    dir.each(function () {
        dirstr += $(this).val() + ",";
    });
    doDel(filestr, dirstr);
}

// 单个目录或文件删除
function fileDel(str, flg) {

    var filestr = "";
    var dirstr = "";
    var msg = "你确认要删除 " + str;
    if (flg == 0) {
        msg += "目录及下面的全部子目录和子文件吗";
        dirstr = str;
    } else {
        filestr = str;
    }
    if (!confirm(msg + "？"))
        return;
    doDel(filestr, dirstr);
}
function doDel(filestr, dirstr) {
    var ip = getIP();
    if (ip == null)
        return;
    var txt = $("#fileDir").val();
    if (txt.length == 0) {
        alert("目录不能为空");
        return;
    }

    var para = {};
    para.tip = ip;
    para.dir = txt;
    para.files = filestr;
    para.dirs = dirstr;
    ajaxSend(para, 103, function (backdata) {
        fileManager();
        alert(backdata);
    });
}

// 创建目录
function fileCreateDir() {
    var ip = getIP();
    if (ip == null)
        return;
    var txt = $("#fileDir").val();
    if (txt.length == 0) {
        alert("目录不能为空");
        return;
    }
    var htm = "要创建的目录：<input type='text' id='fileNewDir' style='width:400px;' value='" + txt + "\\'/>";

    showDialog(htm, function () {
        txt = $("#fileNewDir").val();
        if (txt.length == 0) {
            alert("请输入要创建的目录");
            return;
        }

        var para = {};
        para.tip = ip;
        para.dir = txt;
        ajaxSend(para, 108, function (backdata) {
            fileManager();
            alert(backdata);
        });
    });
}

// 解压ZIP
function fileUnZip() {
    var ip = getIP();
    if (ip == null)
        return;
    var txt = $("#fileDir").val();
    if (txt.length == 0) {
        alert("目录不能为空");
        return;
    }

    var obj = $("input:checkbox[name='chkFileListBeinet']:checked");
    if (obj.length == 0) {
        alert("没有选中任何文件");
        return;
    }
    if (obj.length != 1) {
        alert("一次只能解压一个文件");
        return;
    }
    if (!confirm("你确认要解压选中的文件吗？\r\n注1：文件必须是ZIP格式\r\n注2：解压会直接覆盖当前目录下文件，请确认清楚"))
        return;
    var para = {};
    para.tip = ip;
    if (txt.charAt(txt.length - 1) != "\\")
        txt += "\\";
    para.dir = txt + obj.val();
    ajaxSend(para, 109, function (backdata) {
        fileManager();
        alert(backdata);
    });
}

        
// 打开目录，dir为要打开的目录
function fileOpenDir(dir) {
    if (dir.length <= 0)
        return;
    var txt = $("#fileDir");
    //if (dir.constructor == Number){ //typeof(dir) == "number" 也可以
    txt.val(dir);
    fileManager();
}
            
// type=0目录改名，type=1文件改名
function fileReName(str, type) {
    var ip = getIP();
    if (ip == null)
        return;
    var txt = $("#fileDir").val();
    if (txt.length == 0) {
        alert("目录不能为空");
        return;
    }

    var html = ("<p>&nbsp;原　名：" + str + "<br />&nbsp;更改为：<input type='text' id='txtNewName' value='" +
        str + "' style='width:400px;'/></p>");
    showDialog(html, function () {
        var newName = $("#txtNewName").val();
        if (newName.length <= 0 || str == newName) {
            alert("请输入新的名字");
            return;
        }
        var para = {};
        para.tip = ip;
        if (txt.charAt(txt.length - 1) != "\\")
            txt += "\\";
        para.dir = txt + str;
        para.newname = newName;
        if (type == 0)
            type = 101;
        else
            type = 102;
        ajaxSend(para, type, function (backdata) {
            fileManager();
            alert(backdata);
        });
    });
}


// 获取目录大小
function countDirSize(dir) {
    var ip = getIP();
    if (ip == null)
        return;
    var txt = $("#fileDir").val();
    if (txt.length == 0) {
        alert("目录不能为空");
        return;
    }

    var para = {};
    para.tip = ip;
    if (txt.charAt(txt.length - 1) != "\\")
        txt += "\\";
    para.dir = txt + dir;
    ajaxSend(para, 104, function (backdata) {
        alert(backdata);
    });
}

// 下载或打开文件
function fileDownOpen(file, open) {
    var ip = getIP();
    if (ip == null)
        return;
    var txt = $("#fileDir").val();
    if (txt.length == 0) {
        alert("目录不能为空");
        return;
    }
    if (txt.charAt(txt.length - 1) != "\\")
        txt += "\\";
    var para = {};
    para.tip = ip;
    para.dir = txt + file;
    ajaxSend(para, 106, function (backdata) {
        if (backdata.substring(0,2)!="ok") {
            alert(backdata);
            return;
        }
        var localfile = backdata.substring(2);
        if (localfile.length <= 0) {
            alert("获取成功，但是返回数据有误，请重试");
            return;
        }
        var enFile = encodeURIComponent(localfile);
        var html = "文件 <span style='color:blue;font-weight:bold;'>" + file + "</span><br/>已成功获取，请选择操作方式：<hr/>" +
            "<a href='?flg=119&file=" + enFile + "&name=" + encodeURIComponent(file) + "'>下载文件</a> | " +
            "<a href='?flg=120&file=" + enFile + "' target='_blank'>直接打开</a><hr/>" +
            "注：如果不是文本文件，建议选择下载";
        showDialog(html, null);
        if (open == 1) {
            window.open("?flg=120&file=" + enFile);
        }
    });
}


// 选中或取消全部文件
function CheckAllFile(obj) {
    chgChkColor($("input[name='chkFileListBeinet']"), obj.checked);
}
// 选中或取消全部目录
function CheckAllDir(obj) {
    chgChkColor($("input[name='chkDirListBeinet']"), obj.checked);
}
function chgChkColor(objchk, check) {
    if (check) {
        $(objchk).attr("checked", true);
        //$(objchk).css("background-color", "green");
    } else {
        $(objchk).attr("checked", false);
        //$(objchk).css("background-color", "white");
    }
}
