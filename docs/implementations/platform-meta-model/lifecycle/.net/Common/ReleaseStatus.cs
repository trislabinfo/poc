using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PlatformMetaModel.Lifecycle.Common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReleaseStatus
    {
        Pending,
        Valid,
        Invalid
    }
}
