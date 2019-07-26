using System;
using System.Reflection;
using System.Threading;

namespace PlanServerService.Hook
{
    /// <summary>
    /// 发送通知的帮助类
    /// </summary>
    public static class HookHelper
    {
        /// <summary>
        /// 根据操作类型，发送通知
        /// </summary>
        /// <param name="type"></param>
        /// <param name="msg"></param>
        public static void DoHook(Enum type, string msg)
        {
            var hook = GetAttribute<BaseHook>(type);
            if (hook != null)
            {
                ThreadPool.UnsafeQueueUserWorkItem(state => hook.Hook(msg), null);
            }
        }

        /// <summary>
        /// 从枚举对象上获取指定的Attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumObj"></param>
        /// <returns></returns>
        static T GetAttribute<T>(Enum enumObj) where T : Attribute, IHook
        {
            Type type = enumObj.GetType();
            Attribute attr = null;
            try
            {
                // 获取对应的枚举名
                FieldInfo field = type.GetField(Enum.GetName(type, enumObj));
                var arr = field.GetCustomAttributes(typeof(T), false);
                if (arr.Length > 0)
                    attr = (Attribute) arr[0];
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }

            return (T) attr;
        }
    }
}
