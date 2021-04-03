//Source: https://stackoverflow.com/questions/31176949/binding-selecteditems-of-listview-to-viewmodel/31178953

using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DriverTool.UI
{
    public class ListViewExtensions
    {

        private static SelectedItemsBinder GetSelectedValueBinder(DependencyObject obj)
        {
            return (SelectedItemsBinder)obj.GetValue(SelectedValueBinderProperty);
        }

        private static void SetSelectedValueBinder(DependencyObject obj, SelectedItemsBinder items)
        {
            obj.SetValue(SelectedValueBinderProperty, items);
        }

        private static readonly DependencyProperty SelectedValueBinderProperty = DependencyProperty.RegisterAttached("SelectedValueBinder", typeof(SelectedItemsBinder), typeof(ListViewExtensions));


        public static readonly DependencyProperty SelectedValuesProperty = DependencyProperty.RegisterAttached("SelectedValues", typeof(IList), typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(null, OnSelectedValuesChanged));


        private static void OnSelectedValuesChanged(DependencyObject o, DependencyPropertyChangedEventArgs value)
        {
            var oldBinder = GetSelectedValueBinder(o);
            oldBinder?.UnBind();
            SetSelectedValueBinder(o, new SelectedItemsBinder((ListView)o, (IList)value.NewValue));
            GetSelectedValueBinder(o).Bind();
        }

        public static void SetSelectedValues(Selector elementName, IEnumerable value)
        {
            elementName.SetValue(SelectedValuesProperty, value);
        }

        public static IEnumerable GetSelectedValues(Selector elementName)
        {
            return (IEnumerable)elementName.GetValue(SelectedValuesProperty);
        }
    }
}