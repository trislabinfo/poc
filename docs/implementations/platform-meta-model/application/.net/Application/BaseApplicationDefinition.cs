using PlatformMetaModel.Common;
using PlatformMetaModel.Entity;
using PlatformMetaModel.Navigation;
using PlatformMetaModel.Page;

namespace PlatformMetaModel.Application
{
    public class BaseApplicationDefinition : AuditDefinition
    {
        public required int Version { get; set; }
        public IList<EntityDefinition>? Entities { get; set; }
        public IList<PageDefinition>? Pages { get; set; }
        public IList<NavigationDefinition>? Navigation { get; set; }
    }
}
