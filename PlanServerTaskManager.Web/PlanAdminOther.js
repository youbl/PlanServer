function getProcesses() {
    var ip = getIP();
    if (ip == null)
        return;
    var para = {};
    para.tip = ip;
    ajaxSend(para, 3000, function (backdata) {
        var html = '<table border="1" cellspacing="0" cellpadding="1">' +
            '<tr style="background-color:#96d9f9"><th>pid</th><th>name</th><th>memory</th><th>memoryVirtual</th><th>memoryPage</th><th>CreateDate</th><th>CommandLine</th></tr>';
        var arr = backdata.split("|/|/|/");
        var colNum = 7;
        for (var i = 0, j = arr.length; i < j; i++) {
            var row = $.trim(arr[i]);
            if (row.length == 0)
                continue;
            var arrCol = row.split("|||");
            var realNum = arrCol.length;
            html += "<tr onmouseover='onRowOver(this);' onmouseout='onRowOut(this);' onclick='onRowClick(this);'>";
            for (var k = 0; k < colNum; k++) {
                html += "<td>";
                if (realNum > k) {
                    html += arrCol[k];
                }
                html += "</td>";
            }
            html += "</tr>";
        }
        html += '</table>';
        $("#divProcess").html(html);
    });
}
