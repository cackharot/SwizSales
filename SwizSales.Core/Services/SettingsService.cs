using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwizSales.Core.Model;
using System.Data.Objects;
using SwizSales.Core.Library;

namespace SwizSales.Core.Services
{
    public class SettingsService : SwizSales.Core.ServiceContracts.ISettingsService
    {
        public ICollection<Setting> GetSettings()
        {
            using (var ctx = new OpenPOSDbEntities())
            {
                ctx.ContextOptions.LazyLoadingEnabled = false;
                ctx.Settings.MergeOption = MergeOption.NoTracking;

                return ctx.Settings.ToList();
            }
        }

        public ICollection<Setting> GetSettingsByCategory(string category)
        {
            using (var ctx = new OpenPOSDbEntities())
            {
                ctx.ContextOptions.LazyLoadingEnabled = false;
                ctx.Settings.MergeOption = MergeOption.NoTracking;

                return ctx.Settings.Where(x => x.Category == category).ToList();
            }
        }

        public Setting GetSettingById(Guid id)
        {
            using (var ctx = new OpenPOSDbEntities())
            {
                ctx.ContextOptions.LazyLoadingEnabled = false;
                ctx.Settings.MergeOption = MergeOption.NoTracking;

                return ctx.Settings.Where(x => x.Id == id).FirstOrDefault();
            }
        }

        public Guid Add(Setting entity)
        {
            using (var ctx = new OpenPOSDbEntities())
            {
                ctx.Settings.AddObject(entity);
                ctx.SaveChanges();
                return entity.Id;
            }
        }

        public void Update(Setting entity)
        {
            using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
            {
                try
                {

                    ctx.Settings.Attach(entity);
                    ctx.ObjectStateManager.ChangeObjectState(entity, System.Data.EntityState.Modified);

                    ctx.Settings.ApplyCurrentValues(entity);
                    ctx.SaveChanges();
                }
                catch (Exception ex)
                {
                    LogService.Error("Error while updating settings", ex);
                    throw ex;
                }
            }
        }

        public void Delete(Guid id)
        {
            if (id.Equals(Guid.Empty))
                throw new ArgumentException("Setting id cannot be empty!");

            try
            {
                using (OpenPOSDbEntities ctx = new OpenPOSDbEntities())
                {
                    var entity = GetSettingById(id);

                    if (entity != null)
                    {
                        ctx.Settings.Attach(entity);
                        ctx.Settings.DeleteObject(entity);
                        ctx.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error while deleting Settings", ex);
            }
        }
    }
}
