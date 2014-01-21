using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FarmServer
{
    public enum SendOPCodes
    {
        LOGIN = 1,
        KEEPALIVE = 2,
        GETMAPDATA = 3,
        MOVEMENT = 4,
        NEWPLAYERLOGIN = 5,
        PLAYERDISCONNECTED = 6,
        LOADINVENTORY = 7,
        PLANTDATA = 8,
        USEITEM = 10
    }

    public enum PlantSubOPCode
    {
        LoadAll = 0,
        Add = 1,
        Remove = 2,
        Update = 3,
        UpdateAll = 4
    }
}
