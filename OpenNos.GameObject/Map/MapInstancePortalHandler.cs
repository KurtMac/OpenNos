﻿/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Domain;
using System;
using System.Collections.Generic;

namespace OpenNos.GameObject
{
    public class MapInstancePortalHandler
    {
        public static List<Portal> GetMapInstanceExitPortals(short MapId, Guid SourceMap)
        {
            List<Portal> list = new List<Portal>();
            switch (MapId)
            {
                case 20001:
                    list.Add(new Portal
                    {
                        SourceX = 3,
                        SourceY = 8,
                        Type = (short)PortalType.MapPortal,
                        SourceMapInstanceId = SourceMap,
                    });
                    break;
                case 150:
                    list.Add(new Portal
                    {
                        SourceX = 172,
                        SourceY = 170,
                        Type = (short)PortalType.MapPortal,
                        SourceMapInstanceId = SourceMap,
                    });
                    break;
            }
            return list;
        }

        public static List<Portal> GenerateMinilandEntryPortals(short EntryMap, Guid ExitMapinstanceId)
        {
            List<Portal> list = new List<Portal>();

            switch (EntryMap)
            {
                case 1:
                    list.Add(new Portal
                    {
                        SourceX = 48,
                        SourceY = 132,
                        DestinationX = 5,
                        DestinationY = 8,
                        Type = (short)PortalType.Miniland,
                        SourceMapId = 1,
                        DestinationMapInstanceId = ExitMapinstanceId,
                    });
                    break;
                case 145:
                    list.Add(new Portal
                    {
                        SourceX = 9,
                        SourceY = 171,
                        DestinationX = 5,
                        DestinationY = 8,
                        Type = (short)PortalType.Miniland,
                        SourceMapId = 145,
                        DestinationMapInstanceId = ExitMapinstanceId,
                    });
                    break;
            }



            return list;
        }
    }
}