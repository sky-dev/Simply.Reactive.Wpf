namespace Simply.Reactive.Wpf.Xaml
{
    public class ChangedEventArgs
    {
        public object OldValue { get; private set; }
        public object NewValue { get; private set; }

        public ChangedEventArgs(object old, object @new)
        {
            OldValue = old;
            NewValue = @new;
        }
    }
}
