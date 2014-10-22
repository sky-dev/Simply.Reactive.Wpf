using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Simply.Reactive.Wpf.Xaml
{
    public sealed class DataContextBinding : DependencyObject, IDisposable
    {
        public static readonly DependencyProperty DataContextProperty = DependencyProperty.Register(
            "DataContext", typeof(object), typeof(DataContextBinding), new UIPropertyMetadata(null, OnDataContextChanged));

        private readonly object _lock = new object();
        private readonly Subject<ChangedEventArgs> _dataContexts = new Subject<ChangedEventArgs>();
        private DependencyObject _uiElementTarget;
        private DependencyProperty _uiElementDependencyProperty;
        private BindingExpressionBase _dataContextBinding;
        private bool _isDisposed;
        private UIElement _uiElement;

        public DataContextBinding(DependencyObject uiElementTarget, DependencyProperty uiElementDependencyProperty, Action<ChangedEventArgs> subscriber)
        {
            _uiElementTarget = uiElementTarget;
            _uiElementDependencyProperty = uiElementDependencyProperty;
            _dataContexts.ObserveOn(DispatcherScheduler.Current).Subscribe(subscriber);


            if (uiElementDependencyProperty.Name == "DataContext")
            {
                // For DataContext we must bind to the parent DataContext to avoid an infinite binding situation.
                // Binding must be delayed for DataContext so that VisualTreeHelper.GetParent will not return null
                DelayBinding(uiElementTarget, uiElementDependencyProperty);
            }
            else
            {
                Bind(uiElementTarget);
            }
        }

        private void DelayBinding(DependencyObject target, DependencyProperty dependencyProperty)
        {
            var uiElement = target as UIElement;
            if (uiElement == null)
                throw new Exception(string.Format("Could not bind ReactiveProperty to DependencyProperty '{0}' on DependencyObject '{1}'.  Could not cast  DependencyObject '{1}' to a UIElement to delay the DataContext binding.", dependencyProperty.Name, target.GetType().Name));
            uiElement.LayoutUpdated += OnLayoutUpdated;
            _uiElement = uiElement;
        }

        private void OnLayoutUpdated(object sender, EventArgs eventArgs)
        {
            var parent = GetDataContextParent(_uiElementTarget, _uiElementDependencyProperty);
            Bind(parent);
            _uiElement.LayoutUpdated -= OnLayoutUpdated;
        }

        private void Bind(DependencyObject target)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;
                var binding = new Binding
                {
                    Source = target,
                    Path = new PropertyPath("DataContext"),
                    Mode = BindingMode.OneWay
                };
                _dataContextBinding = BindingOperations.SetBinding(this, DataContextProperty, binding);
            }
        }

        private static DependencyObject GetDataContextParent(DependencyObject target, DependencyProperty dependencyProperty)
        {
            var parent = VisualTreeHelper.GetParent(target);
            if (parent == null)
                throw new Exception(string.Format("Could not bind ReactiveProperty to DependencyProperty '{0}' on DependencyObject '{1}'.  It had no parent, and DataContext bindings must have a parent.", dependencyProperty.Name, target.GetType().Name));
            return parent;
        }

        private static void OnDataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (DataContextBinding)d;
            target.OnValueChanged(e);
        }

        private void OnValueChanged(DependencyPropertyChangedEventArgs e)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;
                _dataContexts.OnNext(new ChangedEventArgs(e.OldValue, e.NewValue));
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_dataContextBinding != null)
                {
                    if (Thread.CurrentThread.ManagedThreadId == Dispatcher.Thread.ManagedThreadId)
                    {
                        BindingOperations.ClearBinding(this, DataContextProperty);
                    }
                }
                _dataContexts.Dispose();
                if (_uiElement != null)
                {
                    _uiElement.LayoutUpdated -= OnLayoutUpdated;
                }
                _uiElementTarget = null;
                _uiElementDependencyProperty = null;
                _uiElement = null;
                _isDisposed = true;
            }
        }
    }
}
