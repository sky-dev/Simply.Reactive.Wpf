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
        private FrameworkElement _frameworkElement;

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
            if (serviceProvider == null)
                return this;
            var bindingInfo = ReactiveBindingHelper.TryGetUiElementBindingInfo(this, serviceProvider);
            if (!bindingInfo.HasValue)
                return this;

            AnchorReactiveBindingToControl(bindingInfo.Value.Target);

            _proxy = new ReactiveBindingProxy(bindingInfo.Value.Target, bindingInfo.Value.DependencyProperty, GetBindingInfo());

            return _proxy.Value;
        }

        private void AnchorReactiveBindingToControl(DependencyObject target)
        {
            _frameworkElement = (FrameworkElement)target;
            _frameworkElement.Unloaded += RemoveAnchor;
        }

        private void RemoveAnchor(object sender, RoutedEventArgs e)
        {
            // This subscription only exists to keep ReactiveBinding from being Finalized by the GarbageCollector
            // Remove the subscription now that the control has been unloaded, and allow the ReactiveBinding to Finalize
            _frameworkElement.Unloaded -= RemoveAnchor;
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
    }
}