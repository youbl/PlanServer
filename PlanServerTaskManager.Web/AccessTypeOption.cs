
namespace PlanServerTaskManager.Web
{
    /// <summary>
    /// 访问统计的通用枚举类型
    /// </summary>
    public enum AccessTypeOption
    {
        /// <summary>
        /// 未知访问类型
        /// </summary>
        UnKnown = 0,
        /// <summary>
        /// 正常用户访问
        /// </summary>
        User = 88881,
        /// <summary>
        /// 监控程序访问
        /// </summary>
        Monitor = 88882,
        /// <summary>
        /// 其它类型访问（含测试）
        /// </summary>
        Other = 88883,
        /// <summary>
        /// 用于接收访问统计的页面 专用
        /// </summary>
        AccessTotal = 88884
    }
}
/*
DELETE FROM TotalProject WHERE project='appupdate.91.com'
DELETE FROM TotalProjServer WHERE project='appupdate.91.com'
DROP TABLE dbo.[Totalappupdate.91.com13]
 */