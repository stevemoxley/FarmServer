using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FarmServer
{
    public enum ReadOPCodes
    {
        LOGIN = 1,
        KEEPALIVE = 2,
        GETMAPDATA = 3,
        MOVEMENT = 4,
        PLAYERDISCONNECTED = 6,
        LOADINVENTORY = 7,
        PLANTDATA = 8,
        INVENTORYMOVE = 9,
        USEITEM = 10
    }
}
