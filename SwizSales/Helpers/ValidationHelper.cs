using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SwizSales.Helpers
{
    public class DataError
    {
        public string PropertyName { get; set; }
        public string Error { get; set; }

        public DataError(string propName, string error)
        {
            this.PropertyName = propName;
            this.Error = error;
        }
    }

    public static class ValidationHelper
    {
        public static DataError[] Validate(this IDataErrorInfo model)
        {
            if (model != null)
            {
                var errorList = new List<DataError>();
                foreach (var prop in model.GetType().GetProperties())
                {
                    var error = model[prop.Name];
                    if (!string.IsNullOrEmpty(error))
                    {
                        errorList.Add(new DataError(prop.Name, error));
                    }
                }

                return errorList.Count > 0 ? errorList.ToArray() : null;
            }

            return null;
        }
    }
}
