using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PropertyUtil.Test
{
    public static class PropertyUtil
    {
        public static string GetName<T>(Expression<Func<T>> propertyExpression)
        {
            return null;
        }

        public static string GetName<T, TResult>(Expression<Func<T, TResult>> propertyExpression)
        {
            return null;
        }
        public static string GetName<T>(Expression<Func<T, object>> propertyExpression)
        {
            return null;
        }
    }

    class Testclass
    {
        public Testclass()
        {
            //var value = PropertyUtil.GetName<Person>(x => x.Name);
            //PropertyUtil.GetName<Person>(x => x.Name);

        }
    }

    public class SampleModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public SampleModel()
        { }

        public string Forename { get; set; }
    }

    class SomeViewModel
    {
        SampleModel model = new SampleModel();

        public SomeViewModel()
        {
            model.PropertyChanged += model_PropertyChanged;
        }
        private void model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
#pragma warning disable PropertyExpression // Use nameof()



            if (e.PropertyName == PropertyUtil.GetName<SampleModel>(x => x.Forename))


#pragma warning restore PropertyExpression // Use nameof()
            {
                // Perform some logic
            }
        }
    }

    class Person
    {
        public string Name { get; set; }
    }
}