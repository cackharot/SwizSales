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
            var enInCulture = GetCulture();

            Thread.CurrentThread.CurrentCulture = enInCulture;
            Thread.CurrentThread.CurrentUICulture = enInCulture;

            LanguageManipulator.SetXmlLanguage();
        }

        public static CultureInfo GetCulture()
        {
            var enInCulture = CultureInfo.CreateSpecificCulture(string.IsNullOrEmpty(Settings.Default.Culture) ? "en-IN" : Settings.Default.Culture);
            enInCulture.NumberFormat.CurrencyDecimalDigits = Settings.Default.CurrencyDecimalDigits;
            enInCulture.NumberFormat.CurrencySymbol = string.IsNullOrEmpty(Settings.Default.CurrencySymbol) ? "`" : Settings.Default.CurrencySymbol;
            return enInCulture;
        }
    }

    public static class LanguageManipulator
    {
        private static bool isInit = false;
        public static void SetXmlLanguage(CultureInfo culture)
        {
            var lang = XmlLanguage.GetLanguage(culture.Name);
            SetCulture(lang, culture);
            var meteadata = new FrameworkPropertyMetadata(lang);

            if (!isInit)
            {
                FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), meteadata);
                isInit = true;
            }
        }

        public static void SetXmlLanguage()
        {
            SetXmlLanguage(CultureInfo.CurrentCulture); 
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
