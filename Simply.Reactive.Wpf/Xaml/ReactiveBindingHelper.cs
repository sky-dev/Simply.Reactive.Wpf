using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xaml;
using Simply.Reactive.Wpf.Monads;
using XamlParseException = System.Windows.Markup.XamlParseException;

namespace Simply.Reactive.Wpf.Xaml
{
    internal static class ReactiveBindingHelper
    {
        public static Maybe<UiElementBindingInfo> TryGetUiElementBindingInfo(MarkupExtension markupExtension, IServiceProvider serviceProvider)
        {
            var valueProvider = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (valueProvider == null)
                return Maybe<UiElementBindingInfo>.Nothing;

            var targetObject = valueProvider.TargetObject;
            var targetProperty = valueProvider.TargetProperty;

            if (targetObject == null)
                return Maybe<UiElementBindingInfo>.Nothing;
            if (targetProperty == null)
            {
                CheckIsBindingCollection(markupExtension, targetObject);
                return Maybe<UiElementBindingInfo>.Nothing;
            }

            if (!(targetProperty is DependencyProperty))
            {
                ValidateNonDependencyPropertyTargetProperty(markupExtension, targetObject, targetProperty, serviceProvider, valueProvider);
                return Maybe<UiElementBindingInfo>.Nothing;
            }

            if (!(targetObject is DependencyObject))
                return Maybe<UiElementBindingInfo>.Nothing;

            return new UiElementBindingInfo((DependencyObject)targetObject, (DependencyProperty)targetProperty).ToMaybe();
        }

        private static void ValidateNonDependencyPropertyTargetProperty(MarkupExtension markupExtension, object targetObject, object targetProperty, IServiceProvider serviceProvider, IProvideValueTarget valueProvider)
        {
            var memberInfo = targetProperty as MemberInfo;
            if (memberInfo == null)
            {
                CheckIsBindingCollection(markupExtension, targetProperty);
                return;
            }

            var isEventRaised = TryRaiseEventToMarkupExtensionHandler(markupExtension, memberInfo, targetObject, serviceProvider, valueProvider);
            if (isEventRaised)
                return;

            CheckType(markupExtension, memberInfo, targetObject.GetType());
        }

        private static void CheckIsBindingCollection(MarkupExtension markupExtension, object obj)
        {
            if (!(obj is Collection<BindingBase>))
            {
                throw new XamlParseException(string.Format("A '{0}' cannot be used within a '{1}' collection. A '{0}' can only be set on a DependencyProperty of a DependencyObject.", markupExtension.GetType().Name, obj.GetType().Name));
            }
        }

        private static bool TryRaiseEventToMarkupExtensionHandler(MarkupExtension markupExtension, MemberInfo memberInfo, object targetObject, IServiceProvider serviceProvider, IProvideValueTarget valueProvider)
        {
            var propertyInfo = memberInfo as PropertyInfo;
            var eventHandler = LookupSetMarkupExtensionHandler(targetObject.GetType());
            if (eventHandler == null || propertyInfo == null)
                return false;
            var xamlSchemaContextProvider = serviceProvider.GetService(typeof(IXamlSchemaContextProvider)) as IXamlSchemaContextProvider;
            if (xamlSchemaContextProvider == null)
                return false;
            var schemaContext = xamlSchemaContextProvider.SchemaContext;
            var xamlType = schemaContext.GetXamlType(targetObject.GetType());
            if (xamlType == null)
                return false;
            var member = xamlType.GetMember(propertyInfo.Name);
            if (member == null)
                return false;
            var xamlSetMarkupExtensionEventArgs = new XamlSetMarkupExtensionEventArgs(member, markupExtension, serviceProvider);
            eventHandler(valueProvider.TargetObject, xamlSetMarkupExtensionEventArgs);
            if (!xamlSetMarkupExtensionEventArgs.Handled)
                return false;
            return true;
        }

        private static EventHandler<XamlSetMarkupExtensionEventArgs> LookupSetMarkupExtensionHandler(Type type)
        {
            if (typeof(Setter).IsAssignableFrom(type))
                return Setter.ReceiveMarkupExtension;
            if (typeof(DataTrigger).IsAssignableFrom(type))
                return DataTrigger.ReceiveMarkupExtension;
            if (typeof(Condition).IsAssignableFrom(type))
                return Condition.ReceiveMarkupExtension;
            return null;
        }

        private static void CheckType(MarkupExtension markupExtension, MemberInfo memberInfo, Type targetObjectType)
        {
            var memberType = GetMemberType(memberInfo);
            if (!typeof(MarkupExtension).IsAssignableFrom(memberType) || !memberType.IsInstanceOfType(markupExtension))
            {
                throw new XamlParseException(string.Format("A '{0}' cannot be set on the '{1}' property of type '{2}'. A '{0}' can only be set on a DependencyProperty of a DependencyObject.", markupExtension.GetType().Name, memberInfo.Name, targetObjectType.Name));
            }
        }

        private static Type GetMemberType(MemberInfo memberInfo)
        {
            var propertyInfo = memberInfo as PropertyInfo;
            if (propertyInfo != null)
                return propertyInfo.PropertyType;

            var methodInfo = (MethodInfo)memberInfo;
            var parameters = methodInfo.GetParameters();
            return parameters[1].ParameterType;
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
