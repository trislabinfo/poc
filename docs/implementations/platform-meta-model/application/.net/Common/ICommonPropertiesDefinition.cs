using PlatformMetaModel.Entity;
using PlatformMetaModel.Navigation;
using PlatformMetaModel.Page;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformMetaModel.Common
{
    public interface ICommonPropertiesDefinition
    {
        public IList<EntityDefinition>? Entities { get; set; }

        /// <summary>Page definitions (list, edit, custom).</summary>
        public IList<PageDefinition>? Pages { get; set; }

        /// <summary>Navigation definitions (each defines a navigation type and its item tree).</summary>
        public IList<NavigationDefinition>? Navigation { get; set; }
    }
}
