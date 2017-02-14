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

using OpenNos.Core;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.WebApi.Reference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OpenNos.Handler
{
    public class MinilandPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession _session;

        #endregion

        #region Instantiation

        public MinilandPacketHandler(ClientSession session)
        {
            _session = session;
        }

        #endregion

        #region Properties

        private ClientSession Session => _session;

        #endregion

        #region Methods
        /// <summary>
        /// mJoinPacket packet
        /// </summary>
        /// <param name="mJoinPacket"></param>
        public void JoinMiniland(MJoinPacket mJoinPacket)
        {
            ClientSession sess = ServerManager.Instance.GetSessionByCharacterId(mJoinPacket.CharacterId);
            if (sess?.Character != null)
            {
                if (sess?.Character.MinilandState == MinilandState.OPEN)
                {
                    ServerManager.Instance.JoinMiniland(Session, sess);
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateInfo(Language.Instance.GetMessageFromKey("MINILAND_CLOSED_BY_FRIEND")));
                }
            }
        }
        public void MinilandRemoveObject(RmvobjPacket packet)
        {
            ItemInstance minilandobject = Session.Character.Inventory.LoadBySlotAndType<ItemInstance>(packet.Slot, InventoryType.Miniland);
            if (minilandobject != null)
            {
                if (Session.Character.MinilandState == MinilandState.LOCK)
                {
                    MinilandObject mo = Session.Character.MinilandObjects.FirstOrDefault(s => s.ItemInstanceId == minilandobject.Id);
                    if (mo != null)
                    {
                        if (minilandobject.Item.IsMinilandObject)
                        {
                            Session.Character.WareHouseSize = 0;
                        }
                        Session.Character.MinilandObjects.Remove(mo);
                        Session.SendPacket(Session.Character.GenerateMinilandEffect(mo, true));
                        Session.SendPacket(Session.Character.GenerateMinilandPoint());
                        Session.SendPacket(Session.Character.GenerateMinilandObject(mo, packet.Slot, true));
                    }
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("MINILAND_NEED_LOCK"), 0));
                }
            }
        }
        public void MinilandAddObject(AddobjPacket packet)
        {
            ItemInstance minilandobject = Session.Character.Inventory.LoadBySlotAndType<ItemInstance>(packet.Slot, InventoryType.Miniland);
            if (minilandobject != null)
            {
                if (!Session.Character.MinilandObjects.Any(s => s.ItemInstanceId == minilandobject.Id))
                {
                    if (Session.Character.MinilandState == MinilandState.LOCK)
                    {
                        MinilandObject mo = new MinilandObject()
                        {
                            CharacterId = Session.Character.CharacterId,
                            ItemInstance = minilandobject,
                            ItemInstanceId = minilandobject.Id,
                            MapX = packet.PositionX,
                            MapY = packet.PositionY,
                            Level1BoxAmount = 0,
                            Level2BoxAmount = 0,
                            Level3BoxAmount = 0,
                            Level4BoxAmount = 0,
                            Level5BoxAmount = 0,
                        };

                        if (minilandobject.Item.ItemType == ItemType.House)
                        {
                            switch (minilandobject.Item.ItemSubType)
                            {
                                case 2:
                                    mo.MapX = 31;
                                    mo.MapY = 3;
                                    break;
                                case 0:
                                    mo.MapX = 24;
                                    mo.MapY = 7;
                                    break;
                                case 1:
                                    mo.MapX = 21;
                                    mo.MapY = 4;
                                    break;
                            }

                            MinilandObject min = Session.Character.MinilandObjects.FirstOrDefault(s => s.ItemInstance.Item.ItemType == ItemType.House && s.ItemInstance.Item.ItemSubType == minilandobject.Item.ItemSubType);
                            if (min != null)
                            {
                                MinilandRemoveObject(new RmvobjPacket() { Slot = min.ItemInstance.Slot });
                            }

                        }

                        if (minilandobject.Item.IsMinilandObject)
                        {
                            Session.Character.WareHouseSize = minilandobject.Item.MinilandObjectPoint;
                        }
                        Session.Character.MinilandObjects.Add(mo);
                        Session.SendPacket(Session.Character.GenerateMinilandEffect(mo, false));
                        Session.SendPacket(Session.Character.GenerateMinilandPoint());
                        Session.SendPacket(Session.Character.GenerateMinilandObject(mo, packet.Slot, false));


                    }
                    else
                    {
                        Session.SendPacket(Session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("MINILAND_NEED_LOCK"), 0));
                    }
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("ALREADY_THIS_MINILANDOBJECT"), 0));
                }
            }
        }

        public void UseMinilandObject(UseobjPacket packet)
        {
            ClientSession client = ServerManager.Instance.Sessions.FirstOrDefault(s => s.Character?.Miniland == Session.Character.MapInstance);
            if (client != null)
            {
                ItemInstance minilandobject = client.Character.Inventory.LoadBySlotAndType<ItemInstance>(packet.Slot, InventoryType.Miniland);
                if (minilandobject != null)
                {
                    MinilandObject mlobj = client.Character.MinilandObjects.FirstOrDefault(s => s.ItemInstanceId == minilandobject.Id);
                    if (mlobj != null)
                    {
                        if (!minilandobject.Item.IsMinilandObject)
                        {
                            byte game = (byte)((mlobj.ItemInstance.Item.EquipmentSlot == 0) ? 4 + mlobj.ItemInstance.ItemVNum % 10 : ((int)mlobj.ItemInstance.Item.EquipmentSlot / 3));
                            bool full = false;
                            Session.SendPacket($"mlo_info {(client == Session ? 1 : 0)} {minilandobject.ItemVNum} {packet.Slot} {Session.Character.MinilandPoint} {(minilandobject.DurabilityPoint < 1000 ? 1 : 0)} {(full ? 1 : 0)} 0 {GetMinilandMaxPoint(game)[0]} {GetMinilandMaxPoint(game)[0] + 1} {GetMinilandMaxPoint(game)[1]} {GetMinilandMaxPoint(game)[1] + 1} {GetMinilandMaxPoint(game)[2]} {GetMinilandMaxPoint(game)[2] + 2} {GetMinilandMaxPoint(game)[3]} {GetMinilandMaxPoint(game)[3] + 1} {GetMinilandMaxPoint(game)[4]} {GetMinilandMaxPoint(game)[4] + 1} {GetMinilandMaxPoint(game)[5]}");
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateStashAll());
                        }
                    }
                }
            }
        }

        [Packet("#mg")]
        public void SpecialUseItem(string packet)
        {
            string[] packetsplit = packet.Split(' ', '^');
            byte Type;
            int Point;
            short MinigameVnum;
            byte Id;
            if (packetsplit.Length > 4
                && byte.TryParse(packetsplit[2], out Id)
                && short.TryParse(packetsplit[3], out MinigameVnum)
                 && int.TryParse(packetsplit[4], out Point)
                  && byte.TryParse(packetsplit[1], out Type)
                  )
                MinigamePlay(new MinigamePacket() { Id = Id, MinigameVNum = MinigameVnum, Point = Point, Type = Type });
        }
        public void MinigamePlay(MinigamePacket packet)
        {
            ClientSession client = ServerManager.Instance.Sessions.FirstOrDefault(s => s.Character?.Miniland == Session.Character.MapInstance);
            if (client != null)
            {
                MinilandObject mlobj = client.Character.MinilandObjects.FirstOrDefault(s => s.ItemInstance.ItemVNum == packet.MinigameVNum);
                if (mlobj != null)
                {
                    bool full = false;
                    byte game = (byte)((mlobj.ItemInstance.Item.EquipmentSlot == 0) ? 4 + mlobj.ItemInstance.ItemVNum % 10 : ((int)mlobj.ItemInstance.Item.EquipmentSlot / 3));
                    switch (packet.Type)
                    {
                        case 1://play
                            if (mlobj.ItemInstance.DurabilityPoint <= 0)
                            {
                                Session.SendPacket(Session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_DURABILITY_POINT"),0));
                                return;
                            }
                            if (Session.Character.MinilandPoint <= 0)
                            {
                                Session.SendPacket($"qna #mg^1^7^3125^1^1 {Language.Instance.GetMessageFromKey("NOT_ENOUGH_MINILAND_POINT")}");
                            }
                            Session.Character.MapInstance.Broadcast(Session.Character.GenerateGuri(2, 1));
                            Session.Character.CurrentMinigame = (short)(game == 0 ? 5102 : game == 1 ? 5103 : game == 2 ? 5105 : game == 3 ? 5104 : game == 4 ? 5113 : 5112);
                            Session.SendPacket($"mlo_st {game}");
                            break;
                        case 2://stop
                            Session.Character.CurrentMinigame = 0;
                            Session.Character.MapInstance.Broadcast(Session.Character.GenerateGuri(6, 1));
                            break;
                        case 3:
                            Session.Character.CurrentMinigame = 0;
                            Session.Character.MapInstance.Broadcast(Session.Character.GenerateGuri(6, 1));
                            int Level = -1;
                            for (short i = 0; i < GetMinilandMaxPoint(game).Count(); i++)
                            {
                                if (packet.Point > GetMinilandMaxPoint(game)[i])
                                {
                                    Level = i;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (Level != -1)
                            {
                                Session.SendPacket($"mlo_lv {Level}");
                            }
                            else
                            {
                                Session.SendPacket($"mg 3 {game} {packet.MinigameVNum} 0 0");
                            }
                            break;
                        case 4: // select gift
                            if (Session.Character.MinilandPoint >= 100)
                            {
                                Gift obj = GetMinilandGift(game, (int)packet.Point);
                                if (obj != null)
                                {
                                    Session.SendPacket($"mlo_rw {obj.VNum} {packet.Point}");
                                    Session.SendPacket(Session.Character.GenerateMinilandPoint());
                                    List<ItemInstance> inv = Session.Character.Inventory.AddNewToInventory(obj.VNum, obj.Amount);
                                    Session.Character.MinilandPoint -= 100;
                                    if (!inv.Any())
                                    {
                                        Session.Character.SendGift(Session.Character.CharacterId, obj.VNum, obj.Amount, 0, 0, false);
                                    }

                                    if (client != Session)
                                    {
                                        switch (packet.Point)
                                        {
                                            case 0:
                                                mlobj.Level1BoxAmount++;
                                                break;
                                            case 1:
                                                mlobj.Level2BoxAmount++;
                                                break;
                                            case 2:
                                                mlobj.Level3BoxAmount++;
                                                break;
                                            case 3:
                                                mlobj.Level4BoxAmount++;
                                                break;
                                            case 4:
                                                mlobj.Level5BoxAmount++;
                                                break;
                                        }

                                    }
                                }
                            }
                            break;
                        case 5:
                            Session.SendPacket(Session.Character.GenerateMloMg(mlobj, packet));
                            break;
                        case 6://refill
                            if (packet.Point == null)
                                return;
                            if (Session.Character.Gold > packet.Point)
                            {
                                Session.Character.Gold -= (int)packet.Point;
                                Session.SendPacket(Session.Character.GenerateGold());
                                mlobj.ItemInstance.DurabilityPoint += (int)(packet.Point / 100);
                                Session.SendPacket(Session.Character.GenerateInfo(Language.Instance.GetMessageFromKey(String.Format("REFILL_MINIGAME", ((int)((int)packet.Point / 100)).ToString()))));
                                Session.SendPacket(Session.Character.GenerateMloMg(mlobj, packet));
                            }
                            break;
                        case 7://gift
                            Session.SendPacket($"mlo_pmg {packet.MinigameVNum} {Session.Character.MinilandPoint} {(mlobj.ItemInstance.DurabilityPoint < 1000 ? 1 : 0)} {(full ? 1 : 0)} {(mlobj.Level1BoxAmount > 0 ? $"392 {mlobj.Level1BoxAmount}" : "0 0")} {(mlobj.Level2BoxAmount > 0 ? $"393 {mlobj.Level2BoxAmount}" : "0 0")} {(mlobj.Level3BoxAmount > 0 ? $"394 {mlobj.Level3BoxAmount}" : "0 0")} {(mlobj.Level4BoxAmount > 0 ? $"395 {mlobj.Level4BoxAmount}" : "0 0")} {(mlobj.Level5BoxAmount > 0 ? $"396 {mlobj.Level5BoxAmount}" : "0 0")} 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0");
                            break;
                        case 8://get gift
                            int amount = 0;
                            switch (packet.Point)
                            {
                                case 0:
                                    amount = mlobj.Level1BoxAmount;
                                    break;
                                case 1:
                                    amount = mlobj.Level2BoxAmount;
                                    break;
                                case 2:
                                    amount = mlobj.Level3BoxAmount;
                                    break;
                                case 3:
                                    amount = mlobj.Level4BoxAmount;
                                    break;
                                case 4:
                                    amount = mlobj.Level5BoxAmount;
                                    break;
                            }
                            List<Gift> objlst = new List<Gift>();
                            for (int i = 0; i < amount; i++)
                            {
                                Gift s = GetMinilandGift(game, (int)packet.Point);
                                if (s != null)
                                {
                                    if (objlst.Any(o => o.VNum == s.VNum))
                                    {
                                        objlst.First(o => o.Amount == s.Amount).Amount += s.Amount;
                                    }
                                    else
                                    {
                                        objlst.Add(s);
                                    }

                                }
                            }
                            string str = string.Empty;
                            for (int i=0;i< 9;i++)
                            {
                                if(objlst.Count > i)
                                {
                                    List<ItemInstance> inv = Session.Character.Inventory.AddNewToInventory(objlst.ElementAt(i).VNum, objlst.ElementAt(i).Amount);
                                    if (inv.Any())
                                    {
                                       Session.SendPacket(Session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {ServerManager.GetItem(objlst.ElementAt(i).VNum).Name} x {objlst.ElementAt(i).Amount}", 12));
                                    }
                                    else
                                    {
                                        Session.Character.SendGift(Session.Character.CharacterId, objlst.ElementAt(i).VNum, objlst.ElementAt(i).Amount, 0, 0, false);
                                    }
                                    str += $" {objlst.ElementAt(i).VNum} {objlst.ElementAt(i).Amount}";
                                }
                                else
                                {
                                    str += " 0 0";
                                }
                            }
                            Session.SendPacket($"mlo_pmg {packet.MinigameVNum} {Session.Character.MinilandPoint} {(mlobj.ItemInstance.DurabilityPoint < 1000 ? 1 : 0)} {(full ? 1 : 0)} {(mlobj.Level1BoxAmount > 0 ? $"392 {mlobj.Level1BoxAmount}" : "0 0")} {(mlobj.Level2BoxAmount > 0 ? $"393 {mlobj.Level2BoxAmount}" : "0 0")} {(mlobj.Level3BoxAmount > 0 ? $"394 {mlobj.Level3BoxAmount}" : "0 0")} {(mlobj.Level4BoxAmount > 0 ? $"395 {mlobj.Level4BoxAmount}" : "0 0")} {(mlobj.Level5BoxAmount > 0 ? $"396 {mlobj.Level5BoxAmount}" : "0 0")}{str}");
                            break;
                        case 9://coupon
                            List<ItemInstance> items = Session.Character.Inventory.GetAllItems().Where(s => s.ItemVNum == 1269 || s.ItemVNum == 1271).OrderBy(s => s.Slot).ToList();
                            if (items.Count > 0)
                            {
                                Session.Character.Inventory.RemoveItemAmount(items.ElementAt(0).ItemVNum);
                                int point = items.ElementAt(0).ItemVNum == 1269 ? 300 : 500;
                                mlobj.ItemInstance.DurabilityPoint += point;
                                Session.SendPacket(Session.Character.GenerateInfo(Language.Instance.GetMessageFromKey(String.Format("REFILL_MINIGAME", point))));
                                Session.SendPacket(Session.Character.GenerateMloMg(mlobj, packet));
                            }
                            break;
                    }
                }
            }
        }

        private Gift GetMinilandGift(byte game, int point)
        {
            List<Gift> lst = new List<Gift>();
            Random rand = new Random();
            switch (game)
            {
                default:
                    lst.Add(new Gift(1012, 2));
                    break;
            }
            return lst.OrderBy(s => rand.Next()).FirstOrDefault();
        }

        private int[] GetMinilandMaxPoint(byte game)
        {
            int[] arr;
            switch (game)
            {
                case 0:
                    arr = new int[] { 999, 4999, 7999, 11999, 15999, 1000000 };
                    break;
                case 1:
                    arr = new int[] { 999, 4999, 7999, 11999, 15999, 1000000 };
                    break;
                case 2:
                    arr = new int[] { 999, 4999, 7999, 11999, 15999, 1000000 };
                    break;
                case 3:
                    arr = new int[] { 999, 4999, 7999, 11999, 15999, 1000000 };
                    break;
                case 4:
                    arr = new int[] { 999, 4999, 7999, 11999, 15999, 1000000 };
                    break;
                case 5:
                    arr = new int[] { 999, 4999, 7999, 11999, 15999, 1000000 };
                    break;
                default:
                    arr = new int[] { 999, 4999, 7999, 11999, 15999, 1000000 };
                    break;
            }
            return arr;
        }
        public void MinilandEdit(MLeditPacket packet)
        {
            switch (packet.Type)
            {
                case 1:
                    Session.SendPacket($"mlintro {packet.Parameters.Replace(' ', '^')}");
                    Session.SendPacket(Session.Character.GenerateInfo(Language.Instance.GetMessageFromKey("MINILAND_INFO_CHANGED")));
                    break;
                case 2:
                    MinilandState state;
                    Enum.TryParse(packet.Parameters, out state);

                    switch (state)
                    {
                        case MinilandState.PRIVATE:
                            Session.SendPacket(Session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("MINILAND_PRIVATE"), 0));
                            //Need to be review to permit one friend limit on the miniland
                            Session.Character.Miniland.Sessions.Where(s => s.Character != Session.Character).ToList().ForEach(s => ServerManager.Instance.ChangeMap(s.Character.CharacterId, s.Character.MapId, s.Character.MapX, s.Character.MapY));
                            break;
                        case MinilandState.LOCK:
                            Session.SendPacket(Session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("MINILAND_LOCK"), 0));
                            Session.Character.Miniland.Sessions.Where(s => s.Character != Session.Character).ToList().ForEach(s => ServerManager.Instance.ChangeMap(s.Character.CharacterId, s.Character.MapId, s.Character.MapX, s.Character.MapY));
                            break;
                        case MinilandState.OPEN:
                            Session.SendPacket(Session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("MINILAND_PUBLIC"), 0));
                            break;
                    }

                    Session.Character.MinilandState = state;
                    break;
            }


        }
        #endregion
    }
}