using System;

namespace InGameEditorPlugin
{
    /// <summary>
    /// 编辑字段类型
    /// </summary>
    public enum EditorFieldType
    {
        Text,       // 文本输入
        Number,     // 数值输入
        Boolean,    // 布尔选择
        Dropdown,   // 下拉列表
        ReadOnly    // 只读显示
    }

    /// <summary>
    /// 编辑器字段 - 代表一个可编辑的属性
    /// </summary>
    public class EditorField
    {
        /// <summary>
        /// 显示名称 (中文)
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 属性名称
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// 字段类型
        /// </summary>
        public EditorFieldType FieldType { get; set; }

        /// <summary>
        /// 最小值 (用于数值类型)
        /// </summary>
        public int MinValue { get; set; }

        /// <summary>
        /// 最大值 (用于数值类型)
        /// </summary>
        public int MaxValue { get; set; }

        /// <summary>
        /// 获取值的委托
        /// </summary>
        private Func<object> getter;

        /// <summary>
        /// 设置值的委托
        /// </summary>
        private Action<object> setter;

        /// <summary>
        /// 下拉选项 (用于Dropdown类型)
        /// </summary>
        public string[] DropdownOptions { get; set; }

        /// <summary>
        /// 帮助文本
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public EditorField(
            string displayName,
            string propertyName,
            Func<object> getter,
            Action<object> setter,
            EditorFieldType fieldType,
            int minValue = 0,
            int maxValue = 100)
        {
            DisplayName = displayName;
            PropertyName = propertyName;
            FieldType = fieldType;
            MinValue = minValue;
            MaxValue = maxValue;
            this.getter = getter;
            this.setter = setter;
        }

        /// <summary>
        /// 获取当前值
        /// </summary>
        public object GetValue()
        {
            try
            {
                return getter?.Invoke();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 设置新值
        /// </summary>
        public void SetValue(object value)
        {
            try
            {
                if (FieldType == EditorFieldType.ReadOnly) return;

                // Type conversion
                if (FieldType == EditorFieldType.Number)
                {
                    int numValue = Convert.ToInt32(value);
                    numValue = Math.Max(MinValue, Math.Min(MaxValue, numValue));
                    setter?.Invoke(numValue);
                }
                else if (FieldType == EditorFieldType.Boolean)
                {
                    bool boolValue = Convert.ToBoolean(value);
                    setter?.Invoke(boolValue);
                }
                else
                {
                    setter?.Invoke(value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditorField] SetValue error: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建只读字段
        /// </summary>
        public static EditorField ReadOnly(string displayName, string propertyName, Func<object> getter)
        {
            return new EditorField(displayName, propertyName, getter, null, EditorFieldType.ReadOnly);
        }

        /// <summary>
        /// 创建数值字段
        /// </summary>
        public static EditorField Numeric(string displayName, string propertyName, Func<int> getter, Action<int> setter, int min = 0, int max = 100)
        {
            return new EditorField(
                displayName,
                propertyName,
                () => getter(),
                v => setter((int)v),
                EditorFieldType.Number,
                min,
                max
            );
        }

        /// <summary>
        /// 创建文本字段
        /// </summary>
        public static EditorField Text(string displayName, string propertyName, Func<string> getter, Action<string> setter)
        {
            return new EditorField(
                displayName,
                propertyName,
                () => getter(),
                v => setter((string)v),
                EditorFieldType.Text
            );
        }

        /// <summary>
        /// 创建布尔字段
        /// </summary>
        public static EditorField Boolean(string displayName, string propertyName, Func<bool> getter, Action<bool> setter)
        {
            return new EditorField(
                displayName,
                propertyName,
                () => getter(),
                v => setter((bool)v),
                EditorFieldType.Boolean
            );
        }
    }
}
