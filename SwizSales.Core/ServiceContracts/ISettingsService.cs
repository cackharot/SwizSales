using System;
using SwizSales.Core.Model;
using System.Collections.Generic;

namespace SwizSales.Core.ServiceContracts
{
    public interface ISettingsService
    {
        Guid Add(Setting entity);

        void Delete(Guid id);

        Setting GetSettingById(Guid id);

        ICollection<Setting> GetSettings();

        ICollection<Setting> GetSettingsByCategory(string category);

        void Update(Setting entity);
    }
}
