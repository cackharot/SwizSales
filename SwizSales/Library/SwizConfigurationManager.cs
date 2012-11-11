using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Reflection;
using SwizSales.Properties;

namespace SwizSales.Library
{
    public static class SwizConfigurationManager
    {
        public static void Init()
        {
            InitializeCultures();            
        }

        private static void InitializeCultures()
        {
            var enInCulture = CultureInfo.CreateSpecificCulture(string.IsNullOrEmpty(Settings.Default.Culture) ? "en-IN" : Settings.Default.Culture);

            enInCulture.NumberFormat.CurrencyDecimalDigits = Settings.Default.CurrencyDecimalDigits;
            enInCulture.NumberFormat.CurrencySymbol = string.IsNullOrEmpty(Settings.Default.CurrencySymbol) ? "`" : Settings.Default.CurrencySymbol;

            Thread.CurrentThread.CurrentCulture = enInCulture;
            Thread.CurrentThread.CurrentUICulture = enInCulture;

            LanguageManipulator.SetXmlLanguage();
        }
    }

    public static class LanguageManipulator
    {
        public static void SetXmlLanguage()
        {
            var curr = CultureInfo.CurrentCulture;
            var lang = XmlLanguage.GetLanguage(curr.Name);

            SetCulture(lang, curr);

            var meteadata = new FrameworkPropertyMetadata(lang);

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement), meteadata);
        }

        private static void SetCulture(
            XmlLanguage lang, CultureInfo cult)
        {
            var propertynames = new[]
                            {
                                "_equivalentCulture", 
                                "_specificCulture", 
                                "_compatibleCulture"
                            };

            const BindingFlags flags = BindingFlags.ExactBinding |
                BindingFlags.SetField | BindingFlags.Instance |
                BindingFlags.NonPublic;

            foreach (var name in propertynames)
            {
                var field = typeof(XmlLanguage).GetField(name, flags);
                field.SetValue(lang, cult);
            }
        }
    }
}
