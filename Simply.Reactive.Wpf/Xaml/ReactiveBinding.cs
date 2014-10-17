using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Simply.Reactive.Wpf.Xaml
{
    [MarkupExtensionReturnType(typeof(object))]
    public class ReactiveBinding : MarkupExtension
    {
        private ReactiveBindingProxy _proxy;
        private BindingExpressionBase _bindingInfo;

        public object Source { get; set; }
        [ConstructorArgument("path")]
        public PropertyPath Path { get; set; }

        public IValueConverter Converter { get; set; }
        public object ConverterParamter { get; set; }
        public bool ValidatesOnDataErrors { get; set; }
        public bool ValidatesOnExceptions { get; set; }
        public UpdateSourceTrigger UpdateSourceTrigger { get; set; }
        public string StringFormat { get; set; }

        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
        public CultureInfo ConverterCulture { get; set; }

        public ReactiveBinding()
        {
            // Required for InitializeComponent
        }

        public ReactiveBinding(PropertyPath path)
            : this()
        {
            Path = path;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var obj = GetUiElementBindingInfo(serviceProvider);
            var uiElementBindingInfo = obj as UiElementBindingInfo;
            if (uiElementBindingInfo == null)
                return obj;
            _proxy = new ReactiveBindingProxy(uiElementBindingInfo.Target, uiElementBindingInfo.DependencyProperty, GetBindingInfo());
            var source = GetSource(uiElementBindingInfo); // TODO - assumes source is DataContext, not always the case
            _bindingInfo = _proxy.BindTo(source, Path);
            return _proxy.Value;
        }

        private Binding GetBindingInfo()
        {
            return new Binding
            {
                Source = Source,
                Path = Path,
                Converter = Converter,
                ConverterCulture = ConverterCulture,
                ConverterParameter = ConverterParamter,
                ValidatesOnDataErrors = ValidatesOnDataErrors,
                ValidatesOnExceptions = ValidatesOnExceptions,
                UpdateSourceTrigger = UpdateSourceTrigger,
                StringFormat = StringFormat
            };
        }

        private static object GetSource(UiElementBindingInfo uiElementBindingInfo)
        {
            return ((FrameworkElement)uiElementBindingInfo.Target).DataContext;
        }

        private object GetUiElementBindingInfo(IServiceProvider serviceProvider)
        {
            var valueProvider = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (valueProvider == null)
                return null;
            if (valueProvider.TargetObject.GetType().FullName == "System.Windows.SharedDp")
                return this;
            var bindingTarget = valueProvider.TargetObject as DependencyObject;
            var bindingTargetProperty = valueProvider.TargetProperty as DependencyProperty;
            if (bindingTargetProperty == null || bindingTarget == null)
            {
                throw new NotSupportedException(string.Format(
                    "The property '{0}' on target '{1}' is not valid for a {2}. The {2} target must be a DependencyObject, and the target property must be a DependencyProperty.",
                    valueProvider.TargetProperty,
                    valueProvider.TargetObject,
                    GetType().Name));
            }
            return new UiElementBindingInfo(bindingTarget, bindingTargetProperty);
        }

        public class UiElementBindingInfo
        {
            public DependencyObject Target { get; private set; }
            public DependencyProperty DependencyProperty { get; private set; }

            public UiElementBindingInfo(DependencyObject bindingTarget, DependencyProperty dependencyProperty)
            {
                Target = bindingTarget;
                DependencyProperty = dependencyProperty;
            }
        }
    }
}
