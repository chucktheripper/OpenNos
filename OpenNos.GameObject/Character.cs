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

using AutoMapper;
using OpenNos.Core;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Data.Enums;
using OpenNos.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OpenNos.GameObject
{
    public class Character : CharacterDTO
    {
        #region Members

        private readonly ClientSession _session;
        private AuthorityType _authority;
        private int _backpack;
        private byte _cmapcount = 0;
        private int _direction;
        private InventoryList _equipmentlist;
        private InventoryList _inventorylist;
        private bool _invisible;
        private int _isDancing;
        private bool _issitting;
        private double _lastPortal;
        private int _lastPulse;
        private int _morph;
        private int _morphUpgrade;
        private int _morphUpgrade2;
        private int _size = 10;
        private byte _speed;

        #endregion

        #region Instantiation

        public Character(ClientSession Session)
        {
            SpCooldown = 30;
            SaveX = 0;
            SaveY = 0;
            LastDefence = DateTime.Now;
            _session = Session;
        }

        #endregion

        #region Properties

        public AuthorityType Authority { get { return _authority; } set { _authority = value; } }

        public int BackPack { get { return _backpack; } set { _backpack = value; } }

        public bool CanFight
        {
            get
            {
                return !IsSitting && ExchangeInfo == null;
            }
        }

        public int DarkResistance { get; set; }
        public int Defence { get; set; }
        public int DefenceRate { get; set; }
        public int Direction { get { return _direction; } set { _direction = value; } }
        public int DistanceCritical { get; set; }
        public int DistanceCriticalRate { get; set; }
        public int DistanceDefence { get; set; }
        public int DistanceDefenceRate { get; set; }
        public int DistanceRate { get; set; }
        public int Element { get; set; }
        public int ElementRate { get; set; }
        public InventoryList EquipmentList { get { return _equipmentlist; } set { _equipmentlist = value; } }
        public ExchangeInfo ExchangeInfo { get; set; }
        public int FireResistance { get; set; }
        public Group Group { get; set; }
        public bool HasShopOpened { get; set; }
        public int HitCritical { get; set; }

        public int HitCriticalRate { get; set; }

        public int HitRate { get; set; }

        public InventoryList InventoryList { get { return _inventorylist; } set { _inventorylist = value; } }

        public bool Invisible { get { return _invisible; } set { _invisible = value; } }

        public bool InvisibleGm { get; set; }

        public int IsDancing { get { return _isDancing; } set { _isDancing = value; } }

        public bool IsSitting { get { return _issitting; } set { _issitting = value; } }

        public bool IsVehicled { get; set; }

        public DateTime LastDefence { get; set; }

        public DateTime LastLogin { get; set; }

        public short LastNRunId { get; set; }

        public double LastPortal { get { return _lastPortal; } set { _lastPortal = value; } }

        public int LastPulse { get { return _lastPulse; } set { _lastPulse = value; } }

        public double LastSp { get; set; }

        public byte LastSpeed { get; set; }

        public int LightResistance { get; set; }

        public int MagicalDefence { get; set; }

        public Map Map { get; set; }

        public int MaxDistance { get; set; }

        public int MaxHit { get; set; }

        public int MaxSnack { get; set; }

        public int MinDistance { get; set; }

        public int MinHit { get; set; }

        public int Morph { get { return _morph; } set { _morph = value; } }

        public int MorphUpgrade { get { return _morphUpgrade; } set { _morphUpgrade = value; } }

        public int MorphUpgrade2 { get { return _morphUpgrade2; } set { _morphUpgrade2 = value; } }

        public List<QuicklistEntry> QuicklistEntries { get; set; }

        public short SaveX { get; set; }

        public short SaveY { get; set; }

        public ClientSession Session { get { return _session; } }

        public int Size { get { return _size; } set { _size = value; } }

        public List<CharacterSkill> Skills { get; set; }

        public List<CharacterSkill> SkillsSp { get; set; }

        public int SnackAmount { get; set; }

        public int SnackHp { get; set; }

        public int SnackMp { get; set; }

        public int SpCooldown { get; set; }

        public byte Speed { get { return _speed; } set { if (value > 59) { _speed = 59; } else { _speed = value; } } }

        public Thread ThreadCharChange { get; set; }

        public bool UseSp { get; set; }

        public int WaterResistance { get; set; }

        #endregion

        #region Methods

        public void ChangeClass(long id, byte characterClass)
        {
            if (characterClass < 4)
            {
                ClientSession session = ServerManager.Instance.Sessions.SingleOrDefault(s => s.Character != null && s.Character.CharacterId.Equals(id));
                session.Character.JobLevel = 1;
                session.Client.SendPacket("npinfo 0");
                session.Client.SendPacket("p_clear");

                session.Character.Class = characterClass;
                if (ServersData.SpeedData.Contains(characterClass))
                    session.Character.Speed = ServersData.SpeedData[session.Character.Class];

                session.Client.SendPacket(session.Character.GenerateCond());
                session.Character.Hp = (int)session.Character.HPLoad();
                session.Character.Mp = (int)session.Character.MPLoad();
                session.Client.SendPacket(session.Character.GenerateTit());

                //eq 37 0 1 0 9 3 -1.120.46.86.-1.-1.-1.-1 0 0
                Session.CurrentMap?.Broadcast(session.Character.GenerateEq());

                //equip 0 0 0.46.0.0.0 1.120.0.0.0 5.86.0.0.
                session.Client.SendPacket(session.Character.GenerateLev());
                session.Client.SendPacket(session.Character.GenerateStat());
                Session.CurrentMap?.Broadcast(session, session.Character.GenerateEff(8), ReceiverType.All);
                session.Client.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("JOB_CHANGED"), 0));
                Session.CurrentMap?.Broadcast(session, session.Character.GenerateEff(196), ReceiverType.All);
                Random rand = new Random();
                int faction = 1 + (int)rand.Next(0, 2);
                session.Character.Faction = faction;
                session.Client.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey($"GET_PROTECTION_POWER_{faction}"), 0));
                session.Client.SendPacket("scr 0 0 0 0 0 0");

                session.Client.SendPacket(session.Character.GenerateFaction());
                session.Client.SendPacket(session.Character.GenerateStatChar());

                session.Client.SendPacket(session.Character.GenerateEff(4799 + faction));
                session.Client.SendPacket(session.Character.GenerateLev());
                Session.CurrentMap?.Broadcast(session, session.Character.GenerateIn(), ReceiverType.AllExceptMe);
                Session.CurrentMap?.Broadcast(session, session.Character.GenerateEff(6), ReceiverType.All);
                Session.CurrentMap?.Broadcast(session, session.Character.GenerateEff(198), ReceiverType.All);

                session.Character.Skills = new List<CharacterSkill>();
                session.Character.Skills.Add(new CharacterSkill { SkillVNum = (short)(200 + 20 * session.Character.Class), CharacterId = session.Character.CharacterId });
                session.Character.Skills.Add(new CharacterSkill { SkillVNum = (short)(201 + 20 * session.Character.Class), CharacterId = session.Character.CharacterId });

                session.Client.SendPacket(session.Character.GenerateSki());

                // TODO Reset Quicklist (just add Rest-on-T Item)
                foreach (QuicklistEntryDTO quicklists in DAOFactory.QuicklistEntryDAO.Load(session.Character.CharacterId).Where(quicklists => session.Character.QuicklistEntries.Any(qle => qle.EntryId == quicklists.EntryId)))
                    DAOFactory.QuicklistEntryDAO.Delete(session.Character.CharacterId, quicklists.EntryId);
                session.Character.QuicklistEntries = new List<QuicklistEntry>
                {
                    new QuicklistEntry
                    {
                        CharacterId = session.Character.CharacterId,
                        Q1 = 0,
                        Q2 = 9,
                        Type = 1,
                        Slot = 3,
                        Pos = 1
                    }
                };

                if (ServerManager.Instance.Groups.FirstOrDefault(s => s.IsMemberOfGroup(session)) != null)
                    ServerManager.Instance.Broadcast(session, $"pidx 1 1.{session.Character.CharacterId}", ReceiverType.AllExceptMe);
            }
        }

        public void CloseShop()
        {
            if (HasShopOpened)
            {
                KeyValuePair<long, MapShop> shop = this.Session.CurrentMap.UserShops.FirstOrDefault(mapshop => mapshop.Value.OwnerId.Equals(this.CharacterId));
                if (!shop.Equals(default(KeyValuePair<long, MapShop>)))
                {
                    this.Session.CurrentMap.UserShops.Remove(shop.Key);
                    this.Session.CurrentMap?.Broadcast(GenerateShopEnd());
                    this.Session.CurrentMap?.Broadcast(Session, GeneratePlayerFlag(0), ReceiverType.AllExceptMe);
                    Speed = Session.Character.LastSpeed != 0 ? Session.Character.LastSpeed : Session.Character.Speed;
                    IsSitting = false;
                    Session.Client.SendPacket(Session.Character.GenerateCond());
                    Session.CurrentMap?.Broadcast(Session.Character.GenerateRest());
                    Session.Client.SendPacket("shop_end 0");
                }

                HasShopOpened = false;
            }
        }

        public string Dance()
        {
            IsDancing = IsDancing == 0 ? 1 : 0;
            return string.Empty;
        }

        public void DeleteItem(byte type, short slot)
        {
            InventoryList.DeleteFromSlotAndType(slot, type);
            Session.Client.SendPacket(GenerateInventoryAdd(-1, 0, type, slot, 0, 0, 0));
        }

        public void DeleteItemByItemInstanceId(long itemInstanceId)
        {
            Tuple<short, byte> result = InventoryList.DeleteByInventoryItemId(itemInstanceId);
            Session.Client.SendPacket(GenerateInventoryAdd(-1, 0, result.Item2, result.Item1, 0, 0, 0));
        }

        public void DeleteTimeout()
        {
            for (int i = Session.Character.InventoryList.Inventory.Count() - 1; i >= 0; i--)
            {
                Inventory item = Session.Character.InventoryList.Inventory[i];
                if (item != null)
                {
                    if (item.ItemInstance.IsUsed && item.ItemInstance.ItemDeleteTime != null && item.ItemInstance.ItemDeleteTime < DateTime.Now)
                    {
                        Session.Character.InventoryList.DeleteByInventoryItemId(item.ItemInstance.ItemInstanceId);
                        Session.Client.SendPacket(Session.Character.GenerateInventoryAdd(-1, 0, item.Type, item.Slot, 0, 0, 0));
                        Session.Client.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ITEM_TIMEOUT"), 10));
                    }
                }
            }
            for (int i = Session.Character.EquipmentList.Inventory.Count() - 1; i >= 0; i--)
            {
                Inventory item = Session.Character.EquipmentList.Inventory[i];
                if (item != null)
                {
                    if (item.ItemInstance.IsUsed && item.ItemInstance.ItemDeleteTime != null && item.ItemInstance.ItemDeleteTime < DateTime.Now)
                    {
                        Session.Character.EquipmentList.DeleteByInventoryItemId(item.ItemInstance.ItemInstanceId);
                        Session.Client.SendPacket(Session.Character.GenerateEquipment());
                        Session.Client.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ITEM_TIMEOUT"), 10));
                    }
                }
            }
        }

        public string GenerateAt()
        {
            return $"at {CharacterId} {MapId} {MapX} {MapY} 2 0 {ServerManager.GetMap(MapId).Music} 1";
        }

        public string GenerateCInfo()
        {
            return $"c_info {Name} - -1 -1 - {CharacterId} {Authority} {Gender} {HairStyle} {HairColor} {Class} {GetReputIco()} {Compliment} {(UseSp || IsVehicled ? Morph : 0)} {(Invisible ? 1 : 0)} 0 {(UseSp ? MorphUpgrade : 0)} {ArenaWinner}";
        }

        public string GenerateCMap()
        {
            _cmapcount = _cmapcount == 1 ? (byte)0 : (byte)1;
            return $"c_map 0 {MapId} {_cmapcount}";
        }

        public string GenerateCMode()
        {
            return $"c_mode 1 {CharacterId} {(UseSp || IsVehicled ? Morph : 0)} {(UseSp ? MorphUpgrade : 0)} {(UseSp ? MorphUpgrade2 : 0)} {ArenaWinner}";
        }

        public string GenerateCond()
        {
            return $"cond 1 {CharacterId} 0 0 {Speed}";
        }

        public string GenerateDialog(string dialog)
        {
            return $"dlg {dialog}";
        }

        public string GenerateDir()
        {
            return $"dir 1 {CharacterId} {Direction}";
        }

        public List<string> GenerateDroppedItem()
        {
            return ServerManager.GetMap(MapId).DroppedList.Select(item => $"in 9 {item.Value.ItemInstance.ItemVNum} {item.Key} {item.Value.PositionX} {item.Value.PositionY} {item.Value.ItemInstance.Amount} 0 0 -1").ToList();
        }

        public string GenerateEff(int effectid)
        {
            return $"eff 1 {CharacterId} {effectid}";
        }

        public string GenerateEInfo(WearableInstance item)
        {
            Item iteminfo = item.Item;
            byte equipmentslot = iteminfo.EquipmentSlot;
            byte itemType = iteminfo.ItemType;
            byte classe = iteminfo.Class;
            byte subtype = iteminfo.ItemSubType;
            DateTime test = item.ItemDeleteTime != null ? (DateTime)item.ItemDeleteTime : DateTime.Now;
            long time = item.ItemDeleteTime != null ? (long)test.Subtract(DateTime.Now).TotalSeconds : 0;
            long seconds = item.IsUsed ? time : iteminfo.ItemValidTime;
            if (seconds < 0)
                seconds = 0;
            switch (itemType)
            {
                case (byte)ItemType.Weapon:
                    switch (equipmentslot)
                    {
                        case (byte)EquipmentType.MainWeapon:
                            switch (classe)
                            {
                                case 4:
                                    return $"e_info 1 {item.ItemVNum} {item.Rare} {item.Upgrade} {(item.IsFixed ? 1 : 0)} {iteminfo.LevelMinimum} {iteminfo.DamageMinimum + item.DamageMinimum} {iteminfo.DamageMaximum + item.DamageMaximum} {iteminfo.HitRate + item.HitRate} {iteminfo.CriticalLuckRate + item.CriticalLuckRate} {iteminfo.CriticalRate + item.CriticalRate} {item.Ammo} {iteminfo.MaximumAmmo} {iteminfo.Price} -1 0 0 0"; // -1 = {item.ShellEffectValue} {item.FirstShell}...
                                case 8:
                                    return $"e_info 5 {item.ItemVNum} {item.Rare} {item.Upgrade} {(item.IsFixed ? 1 : 0)} {iteminfo.LevelMinimum} {iteminfo.DamageMinimum + item.DamageMinimum} {iteminfo.DamageMaximum + item.DamageMaximum} {iteminfo.HitRate + item.HitRate} {iteminfo.CriticalLuckRate + item.CriticalLuckRate} {iteminfo.CriticalRate + item.CriticalRate} {item.Ammo} {iteminfo.MaximumAmmo} {iteminfo.Price} -1 0 0 0";

                                default:
                                    return $"e_info 0 {item.ItemVNum} {item.Rare} {item.Upgrade} {(item.IsFixed ? 1 : 0)} {iteminfo.LevelMinimum} {iteminfo.DamageMinimum + item.DamageMinimum} {iteminfo.DamageMaximum + item.DamageMaximum} {iteminfo.HitRate + item.HitRate} {iteminfo.CriticalLuckRate + item.CriticalLuckRate} {iteminfo.CriticalRate + item.CriticalRate} {item.Ammo} {iteminfo.MaximumAmmo} {iteminfo.Price} -1 0 0 0";
                            }
                        case (byte)EquipmentType.SecondaryWeapon:
                            switch (classe)
                            {
                                case 4:
                                    return $"e_info 1 {item.ItemVNum} {item.Rare} {item.Upgrade} {(item.IsFixed ? 1 : 0)} {iteminfo.LevelMinimum} {iteminfo.DamageMinimum + item.DamageMinimum} {iteminfo.DamageMaximum + item.DamageMaximum} {iteminfo.HitRate + item.HitRate} {iteminfo.CriticalLuckRate + item.CriticalLuckRate} {iteminfo.CriticalRate + item.CriticalRate} {item.Ammo} {iteminfo.MaximumAmmo} {iteminfo.Price} -1 0 0 0";

                                default:
                                    return $"e_info 0 {item.ItemVNum} {item.Rare} {item.Upgrade} {(item.IsFixed ? 1 : 0)} {iteminfo.LevelMinimum} {iteminfo.DamageMinimum + item.DamageMinimum} {iteminfo.DamageMaximum + item.DamageMaximum} {iteminfo.HitRate + item.HitRate} {iteminfo.CriticalLuckRate + item.CriticalLuckRate} {iteminfo.CriticalRate + item.CriticalRate} {item.Ammo} {iteminfo.MaximumAmmo} {iteminfo.Price} -1 0 0 0";
                            }
                    }
                    break;

                case (byte)ItemType.Armor:
                    return $"e_info 2 {item.ItemVNum} {item.Rare} {item.Upgrade} {(item.IsFixed ? 1 : 0)} {iteminfo.LevelMinimum} {iteminfo.CloseDefence + item.CloseDefence} {iteminfo.DistanceDefence + item.DistanceDefence} {iteminfo.MagicDefence + item.MagicDefence} {iteminfo.DefenceDodge + item.DefenceDodge} {iteminfo.Price} -1 0 0 0";

                case (byte)ItemType.Fashion:
                    switch (equipmentslot)
                    {
                        case (byte)EquipmentType.CostumeHat:
                            return $"e_info 3 {item.ItemVNum} {iteminfo.LevelMinimum} {iteminfo.CloseDefence + item.CloseDefence} {iteminfo.DistanceDefence + item.DistanceDefence} {iteminfo.MagicDefence + item.MagicDefence} {iteminfo.DefenceDodge + item.DefenceDodge} {iteminfo.FireResistance + item.FireResistance} {iteminfo.WaterResistance + item.WaterResistance} {iteminfo.LightResistance + item.LightResistance} {iteminfo.DarkResistance + item.DarkResistance} {iteminfo.Price} 0 1 {(seconds / (3600))}";

                        case (byte)EquipmentType.CostumeSuit:
                            return $"e_info 2 {item.ItemVNum} {item.Rare} {item.Upgrade} {(item.IsFixed ? 1 : 0)} {iteminfo.LevelMinimum} {iteminfo.CloseDefence + item.CloseDefence} {iteminfo.DistanceDefence + item.DistanceDefence} {iteminfo.MagicDefence + item.MagicDefence} {iteminfo.DefenceDodge + item.DefenceDodge} {iteminfo.Price} -1 1 {(seconds / (3600))}"; // 1 = IsCosmetic -1 = no shells
                        default:
                            return $"e_info 3 {item.ItemVNum} {iteminfo.LevelMinimum} {iteminfo.CloseDefence + item.CloseDefence} {iteminfo.DistanceDefence + item.DistanceDefence} {iteminfo.MagicDefence + item.MagicDefence} {iteminfo.DefenceDodge + item.DefenceDodge} {iteminfo.FireResistance + item.FireResistance} {iteminfo.WaterResistance + item.WaterResistance} {iteminfo.LightResistance + item.LightResistance} {iteminfo.DarkResistance + item.DarkResistance} {iteminfo.Price} 0 0 -1"; // after iteminfo.Price theres TimesConnected {(iteminfo.ItemValidTime == 0 ? -1 : iteminfo.ItemValidTime / (3600))}
                    }
                case (byte)ItemType.Jewelery:
                    switch (equipmentslot)
                    {
                        case (byte)EquipmentType.Amulet:
                            return $"e_info 4 {item.ItemVNum} {iteminfo.LevelMinimum} {seconds * 10} 0 0 {iteminfo.Price}";

                        case (byte)EquipmentType.Fairy:
                            return $"e_info 4 {item.ItemVNum} {iteminfo.Element} {item.ElementRate + iteminfo.ElementRate} 0 0 0 0 0"; // last IsNosmall
                        default:
                            return $"e_info 4 {item.ItemVNum} {iteminfo.LevelMinimum} {iteminfo.MaxCellonLvl} {iteminfo.MaxCellon} {item.Cellon} {iteminfo.Price}";
                    }
                case (byte)ItemType.Box:
                //int freepoint = ServersData.SpPoint(item.SpLevel, item.Upgrade) - item.SlDamage - item.SlHP - item.SlElement - item.SlDefence;
                //switch (subtype) //0 = NOSMATE pearl 1= npc pearl 2 = sp box 3 = raid box 4= VEHICLE pearl 5=fairy pearl
                //{
                //    case 2:
                //        return $"e_info 7 {item.ItemVNum} {(item.IsEmpty ? 1 : 0)} {item.Design} {item.SpLevel} {item.SpXp} {ServersData.SpXPData[JobLevel - 1]} {item.Upgrade} {item.SlDamage} {item.SlDefence} {item.SlElement} {item.SlHP} {freepoint} {item.FireResistance} {item.WaterResistance} {item.LightResistance} {item.DarkResistance} {item.SpStoneUpgrade} {item.SpDamage} {item.SpDefence} {item.SpElement} {item.SpHP} {item.SpFire} {item.SpWater} {item.SpLight} {item.SpDark}";

                //    default:
                //        return $"e_info 8 {item.ItemVNum} {item.Design} {item.Rare}";
                //}
                case (byte)ItemType.Shell:
                    return $"e_info 4 {item.ItemVNum} {iteminfo.LevelMinimum} {item.Rare} {iteminfo.Price} 0"; //0 = Number of effects
            }
            return string.Empty;
        }

        public string GenerateEq()
        {
            int color = HairColor;
            WearableInstance head = EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.Hat, (byte)InventoryType.Equipment);

            if (head != null && head.Item.IsColored)
                color = head.Design;

            return $"eq {CharacterId} {(Authority == AuthorityType.Admin ? 2 : 0)} {Gender} {HairStyle} {color} {Class} {GenerateEqListForPacket()} {GenerateEqRareUpgradeForPacket()}";
        }

        public string GenerateEqListForPacket()
        {
            string[] invarray = new string[15];
            for (short i = 0; i < 15; i++)
            {
                Inventory inv = EquipmentList.LoadInventoryBySlotAndType(i, (byte)InventoryType.Equipment);
                if (inv != null)
                {
                    invarray[i] = inv.ItemInstance.ItemVNum.ToString();
                }
                else invarray[i] = "-1";
            }

            return $"{invarray[(byte)EquipmentType.Hat]}.{invarray[(byte)EquipmentType.Armor]}.{invarray[(byte)EquipmentType.MainWeapon]}.{invarray[(byte)EquipmentType.SecondaryWeapon]}.{invarray[(byte)EquipmentType.Mask]}.{invarray[(byte)EquipmentType.Fairy]}.{invarray[(byte)EquipmentType.CostumeSuit]}.{invarray[(byte)EquipmentType.CostumeHat]}";
        }

        public string GenerateEqRareUpgradeForPacket()
        {
            byte weaponRare = 0;
            byte weaponUpgrade = 0;
            byte armorRare = 0;
            byte armorUpgrade = 0;
            for (short i = 0; i < 15; i++)
            {
                WearableInstance wearable = EquipmentList.LoadBySlotAndType<WearableInstance>(i, (byte)InventoryType.Equipment);
                if (wearable != null)
                {
                    if (wearable.Item.EquipmentSlot == (byte)EquipmentType.Armor)
                    {
                        armorRare = wearable.Rare;
                        armorUpgrade = wearable.Upgrade;
                    }
                    else if (wearable.Item.EquipmentSlot == (byte)EquipmentType.MainWeapon)
                    {
                        weaponRare = wearable.Rare;
                        weaponUpgrade = wearable.Upgrade;
                    }
                }
            }
            return $"{weaponUpgrade}{weaponRare} {armorUpgrade}{armorRare}";
        }

        public string GenerateEquipment()
        {
            //equip 86 0 0.4903.6.8.0 2.340.0.0.0 3.4931.0.5.0 4.4845.3.5.0 5.4912.7.9.0 6.4848.1.0.0 7.4849.3.0.0 8.4850.2.0.0 9.227.0.0.0 10.281.0.0.0 11.347.0.0.0 13.4150.0.0.0 14.4076.0.0.0
            string eqlist = string.Empty;
            byte weaponRare = 0;
            byte weaponUpgrade = 0;
            byte armorRare = 0;
            byte armorUpgrade = 0;

            for (short i = 0; i < 15; i++)
            {
                ItemInstance wearable = EquipmentList.LoadBySlotAndType<WearableInstance>(i, (byte)InventoryType.Equipment);
                if (wearable == null)
                    wearable = EquipmentList.LoadBySlotAndType<SpecialistInstance>(i, (byte)InventoryType.Equipment);
                if (wearable != null)
                {
                    if (wearable.Item.EquipmentSlot == (byte)EquipmentType.Armor)
                    {
                        armorRare = wearable.Rare;
                        armorUpgrade = wearable.Upgrade;
                    }
                    else if (wearable.Item.EquipmentSlot == (byte)EquipmentType.MainWeapon)
                    {
                        weaponRare = wearable.Rare;
                        weaponUpgrade = wearable.Upgrade;
                    }
                    eqlist += $" {i}.{wearable.Item.VNum}.{wearable.Rare}.{(wearable.Item.IsColored ? wearable.Design : wearable.Upgrade)}.0";
                }
            }
            return $"equip {weaponUpgrade}{weaponRare} {armorUpgrade}{armorRare}{eqlist}";
        }

        public string GenerateExts()
        {
            return $"exts 0 {48 + BackPack * 12} {48 + BackPack * 12} {48 + BackPack * 12}";
        }

        public string GenerateFaction()
        {
            return $"fs {Faction}";
        }

        public string GenerateFd()
        {
            return $"fd {Reput} {GetReputIco()} {Dignite} {Math.Abs(GetDigniteIco())}";
        }

        public string GenerateGender()
        {
            return $"p_sex {Gender}";
        }

        public string GenerateGet(long id)
        {
            return $"get 1 {CharacterId} {id} 0";
        }

        public string GenerateGold()
        {
            return $"gold {Gold}";
        }

        public List<string> GenerateGp()
        {
            List<string> gpList = new List<string>();
            int i = 0;
            foreach (Portal portal in ServerManager.GetMap(MapId).Portals)
            {
                gpList.Add($"gp {portal.SourceX} {portal.SourceY} {portal.DestinationMapId} {portal.Type} {i} {(portal.IsDisabled ? 1 : 0)}");
                i++;
            }

            return gpList;
        }

        public string GenerateGp(Portal portal)
        {
            return $"gp {portal.SourceX} {portal.SourceY} {portal.DestinationMapId} {portal.Type} {ServerManager.GetMap(MapId).Portals.Count} {(portal.IsDisabled ? 1 : 0)}";
        }

        public string GenerateIn()
        {
            int color = HairColor;
            WearableInstance headWearable = EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.Hat, (byte)InventoryType.Equipment);
            if (headWearable != null && ServerManager.GetItem(headWearable.ItemVNum).IsColored)
                color = headWearable.Design;
            Inventory fairy = EquipmentList.LoadInventoryBySlotAndType((byte)EquipmentType.Fairy, (byte)InventoryType.Equipment);

            return $"in 1 {Name} - {CharacterId} {MapX} {MapY} {Direction} {(Authority == AuthorityType.Admin ? 2 : 0)} {Gender} {HairStyle} {color} {Class} {GenerateEqListForPacket()} {(int)(Hp / HPLoad() * 100)} {(int)(Mp / MPLoad() * 100)} {(IsSitting ? 1 : 0)} -1 {(fairy != null ? 2 : 0)} {(fairy != null ? ServerManager.GetItem(fairy.ItemInstance.ItemVNum).Element : 0)} 0 {(fairy != null ? ServerManager.GetItem(fairy.ItemInstance.ItemVNum).Morph : 0)} 0 {(UseSp ? Morph : 0)} {GenerateEqRareUpgradeForPacket()} -1 - {((GetDigniteIco() == 1) ? GetReputIco() : -GetDigniteIco())} {(_invisible ? 1 : 0)} {(UseSp ? MorphUpgrade : 0)} 0 {(UseSp ? MorphUpgrade2 : 0)} {Level} 0 {ArenaWinner} {Compliment} {Size} {HeroLevel}";
        }

        public List<string> GenerateIn2()
        {
            return ServerManager.GetMap(MapId).Npcs.Select(npc => $"in 2 {npc.NpcVNum} {npc.MapNpcId} {npc.MapX} {npc.MapY} {npc.Position} 100 100 {npc.Dialog} 0 0 - {(npc.IsSitting ? 0 : 1)} 0 0 - 1 - 0 - 1 0 0 0 0 0 0 0 0").ToList();
        }

        public List<string> GenerateIn3()
        {
            return ServerManager.GetMap(MapId).Monsters.Select(monster => monster.GenerateIn3()).ToList();
        }

        public string GenerateInfo(string message)
        {
            return $"info {message}";
        }

        public string GenerateInventoryAdd(short vnum, int amount, byte type, short slot, byte rare, short color, byte upgrade)
        {
            Item item = ServerManager.GetItem(vnum);
            switch (type)
            {
                case (byte)InventoryType.Costume:
                    return $"ivn 7 {slot}.{vnum}.{rare}.{upgrade}";

                case (byte)InventoryType.Wear:
                    return $"ivn 0 {slot}.{vnum}.{rare}.{(item != null ? (item.IsColored ? color : upgrade) : upgrade)}";

                case (byte)InventoryType.Main:
                    return $"ivn 1 {slot}.{vnum}.{amount}";

                case (byte)InventoryType.Etc:
                    return $"ivn 2 {slot}.{vnum}.{amount}";

                case (byte)InventoryType.Sp:
                    return $"ivn 6 {slot}.{vnum}.{rare}.{upgrade}";
            }
            return string.Empty;
        }

        public string GenerateInvisible()
        {
            return $"cl {CharacterId} {(Invisible ? 1 : 0)} 0";
        }

        public string GenerateLev()
        {
            SpecialistInstance specialist = EquipmentList.LoadBySlotAndType<SpecialistInstance>((byte)EquipmentType.Sp, (byte)InventoryType.Equipment);

            return $"lev {Level} {LevelXp} {(!UseSp || specialist == null ? JobLevel : specialist.SpLevel)} {(!UseSp || specialist == null ? JobLevelXp : specialist.SpXp)} {XPLoad()} {(!UseSp || specialist == null ? JobXPLoad() : SPXPLoad())} {Reput} {GetCP()} {HeroXp} {HeroLevel} {HeroXPLoad()}";
        }

        public string GenerateMapOut()
        {
            return "mapout";
        }

        public string GenerateModal(string message, int type)
        {
            return $"modal {type} {message}";
        }

        public string GenerateMsg(string message, int type)
        {
            return $"msg {type} {message}";
        }

        public string GenerateMv()
        {
            return $"mv 1 {CharacterId} {MapX} {MapY} {Speed}";
        }

        public List<string> GenerateNPCShopOnMap()
        {
            return (from npc in ServerManager.GetMap(MapId).Npcs where npc.Shop != null select $"shop 2 {npc.MapNpcId} {npc.Shop.ShopId} {npc.Shop.MenuType} {npc.Shop.ShopType} {npc.Shop.Name}").ToList();
        }

        public string GenerateOut()
        {
            return $"out 1 {CharacterId}";
        }

        public string GeneratePairy()
        {
            WearableInstance fairy = EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.Fairy, (byte)InventoryType.Equipment);
            Item iteminfo = null;
            ElementRate = 0;
            Element = 0;
            if (fairy != null)
            {
                iteminfo = ServerManager.GetItem(fairy.ItemVNum);
                ElementRate += fairy.ElementRate + iteminfo.ElementRate;
                Element = iteminfo.Element;
            }

            return fairy != null
                ? $"pairy 1 {CharacterId} 4 {iteminfo.Element} {fairy.ElementRate + iteminfo.ElementRate} {iteminfo.Morph}"
                : $"pairy 1 {CharacterId} 0 0 0 40";
        }

        public string GeneratePlayerFlag(long pflag)
        {
            return $"pflag 1 {CharacterId} {pflag}";
        }

        public List<string> GeneratePlayerShopOnMap()
        {
            return ServerManager.GetMap(MapId).UserShops.Select(shop => $"pflag 1 {shop.Value.OwnerId} {shop.Key + 1}").ToList();
        }

        public string[] GenerateQuicklist()
        {
            string[] pktQs = new[] { "qslot 0", "qslot 1", "qslot 2" };

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    QuicklistEntry qi = QuicklistEntries.FirstOrDefault(n => n.Q1 == j && n.Q2 == i && n.Morph == (UseSp ? Morph : 0));
                    pktQs[j] += string.Format(" {0}.{1}.{2}", qi?.Type != null ? qi?.Type : 7, qi?.Slot != null ? qi?.Slot : 7, qi != null ? qi.Pos.ToString() : "-1");
                }
            }

            return pktQs;
        }

        public string GenerateRc(int v)
        {
            return $"rc 1 {CharacterId} {v} 0";
        }

        public string GenerateReqInfo()
        {
            WearableInstance fairy = EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.Fairy, (byte)InventoryType.Equipment);
            WearableInstance armor = EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.Armor, (byte)InventoryType.Equipment);
            WearableInstance weapon2 = EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.SecondaryWeapon, (byte)InventoryType.Equipment);
            WearableInstance weapon = EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.MainWeapon, (byte)InventoryType.Equipment);
            //tc_info 0  name   0 0  0 0 -1 - 0  0 0 0 0 0 0 0 0 0 0 wins deaths reput 0 0 0 morph talentwin talentlose capitul rankingpoints arenapoints 0 0 ispvpprimary ispvpsecondary ispvparmor herolvl desc

            return $"tc_info {Level} {Name} {(fairy != null ? ServerManager.GetItem(fairy.ItemVNum).Element : 0)} {(Element != 0 ? ElementRate : 0)} {Class} {Gender} -1 - {GetReputIco()} {GetDigniteIco()} {(weapon != null ? 1 : 0)} {weapon?.Rare ?? 0} {weapon?.Upgrade ?? 0} {(weapon2 != null ? 1 : 0)} {weapon2?.Rare ?? 0} {weapon2?.Upgrade ?? 0} {(armor != null ? 1 : 0)} {armor?.Rare ?? 0} {armor?.Upgrade ?? 0} 0 0 {Reput} 0 0 0 {(UseSp ? Morph : 0)} 0 0 0 0 0 {Compliment} 0 0 0 0 {HeroLevel} {Language.Instance.GetMessageFromKey("NO_PREZ_MESSAGE")}";
        }

        public string GenerateRest()
        {
            return $"rest 1 {CharacterId} {(IsSitting ? 1 : 0)}";
        }

        public string GenerateRevive()
        {
            return $"revive 1 {CharacterId} 0";
        }

        public string GenerateSay(string message, int type)
        {
            return $"say 1 {CharacterId} {type} {message}";
        }

        public string GenerateScal()
        {
            return $"char_sc 1 {CharacterId} {Size}";
        }

        public string GenerateShop(string shopname)
        {
            return $"shop 1 {CharacterId} 1 3 0 {shopname}";
        }

        public string GenerateShopEnd()
        {
            return $"shop 1 {CharacterId} 0 0";
        }

        public string GenerateShopMemo(int type, string message)
        {
            return $"s_memo {type} {message}";
        }

        public List<string> GenerateShopOnMap()
        {
            return ServerManager.GetMap(MapId).UserShops.Select(shop => $"shop 1 {shop.Key + 1} 1 3 0 {shop.Value.Name}").ToList();
        }

        public string GenerateSki()
        {
            List<CharacterSkill> skill = UseSp ? SkillsSp : Skills;
            string skibase = "";
            if (!UseSp)
                skibase = $"{200 + 20 * Class} {201 + 20 * Class}";
            else
                skibase = $"{skill.ElementAt(0).SkillVNum} {skill.ElementAt(0).SkillVNum}";
            string skills = "";
            foreach (CharacterSkill ski in skill)
            {
                skills += $" {ski.SkillVNum}";
            }

            return $"ski {skibase}{skills}";
        }

        public string GenerateSlInfo(SpecialistInstance inventoryItem, int type)
        {
            Item iteminfo = ServerManager.GetItem(inventoryItem.ItemVNum);
            int freepoint = ServersData.SpPoint(inventoryItem.SpLevel, inventoryItem.Upgrade) - inventoryItem.SlDamage - inventoryItem.SlHP - inventoryItem.SlElement - inventoryItem.SlDefence;

            int slElement = ServersData.SlPoint(inventoryItem.SlElement, 2);
            int slHp = ServersData.SlPoint(inventoryItem.SlHP, 3);
            int slDefence = ServersData.SlPoint(inventoryItem.SlDefence, 1);
            int slHit = ServersData.SlPoint(inventoryItem.SlDamage, 0);

            string skill = ""; //sk.sk.sk.sk.sk...
            List<CharacterSkill> skillsSp = new List<CharacterSkill>();
            foreach (Skill ski in ServerManager.GetAllSkill())
            {
                if (ski.Class == iteminfo.Morph + 31)
                    skillsSp.Add(new CharacterSkill() { SkillVNum = ski.SkillVNum, CharacterId = CharacterId });
            }

            if (skillsSp.Count == 0)
                skill = "-1";
            for (int i = 1; i < 11; i++)
            {
                if (skillsSp.Count >= i + 1)
                    skill += $"{skillsSp[i].SkillVNum}.";
            }
            //0 0 0 0 0 0 0 '2' <- PS - 'number' after reputationminimum
            //10 9 8 '0 0 0 0'<- bonusdamage bonusarmor bonuselement bonushpmp its after upgrade and 3 first values are not important
            skill = skill.TrimEnd('.');
            return $"slinfo {type} {inventoryItem.ItemVNum} {iteminfo.Morph} {inventoryItem.SpLevel} {iteminfo.LevelJobMinimum} {iteminfo.ReputationMinimum + 1} 0 0 0 0 0 0 0 2 {iteminfo.FireResistance} {iteminfo.WaterResistance} {iteminfo.LightResistance} {iteminfo.DarkResistance} {inventoryItem.SpXp} {ServersData.SpXPData[inventoryItem.SpLevel - 1]} {skill} {inventoryItem.ItemInstanceId} {freepoint} {slHit} {slDefence} {slElement} {slHp} {inventoryItem.Upgrade} 0 0 0 0 0 0 0 {inventoryItem.SpStoneUpgrade} {inventoryItem.SpDamage} {inventoryItem.SpDefence} {inventoryItem.SpElement} {inventoryItem.SpHP} {inventoryItem.SpFire} {inventoryItem.SpWater} {inventoryItem.SpLight} {inventoryItem.SpDark}";
        }

        public string GenerateSpk(object message, int v)
        {
            return $"spk 1 {CharacterId} {v} {Name} {message}";
        }

        public string GenerateSpPoint()
        {
            return $"sp {SpAdditionPoint} 1000000 {SpPoint} 10000";
        }

        public void GenerateStartupInventory()
        {
            string inv0 = "inv 0", inv1 = "inv 1", inv2 = "inv 2", inv6 = "inv 6", inv7 = "inv 7";

            foreach (Inventory inv in InventoryList.Inventory)
            {
                Item item = ServerManager.GetItem(inv.ItemInstance.ItemVNum);
                switch (inv.Type)
                {
                    case (byte)InventoryType.Costume:
                        var costumeInstance = inv.ItemInstance as WearableInstance;
                        inv7 += $" {inv.Slot}.{inv.ItemInstance.ItemVNum}.{costumeInstance.Rare}.{costumeInstance.Upgrade}";
                        break;

                    case (byte)InventoryType.Wear:
                        var wearableInstance = inv.ItemInstance as WearableInstance;
                        inv0 += $" {inv.Slot}.{inv.ItemInstance.ItemVNum}.{wearableInstance.Rare}.{(item.IsColored ? wearableInstance.Design : wearableInstance.Upgrade)}";
                        break;

                    case (byte)InventoryType.Main:
                        inv1 += $" {inv.Slot}.{inv.ItemInstance.ItemVNum}.{inv.ItemInstance.Amount}.0";
                        break;

                    case (byte)InventoryType.Etc:
                        inv2 += $" {inv.Slot}.{inv.ItemInstance.ItemVNum}.{inv.ItemInstance.Amount}.0";
                        break;

                    case (byte)InventoryType.Sp:
                        var specialist = inv.ItemInstance as SpecialistInstance;
                        inv6 += $" {inv.Slot}.{inv.ItemInstance.ItemVNum}.{specialist.Rare}.{specialist.Upgrade}";
                        break;

                    case (byte)InventoryType.Equipment:
                        break;
                }
            }

            Session.Client.SendPacket(inv0);
            Session.Client.SendPacket(inv1);
            Session.Client.SendPacket(inv2);
            Session.Client.SendPacket(inv6);
            Session.Client.SendPacket(inv7);
        }

        public string GenerateStat()
        {
            double option =
                (WhisperBlocked ? Math.Pow(2, (int)ConfigType.WhisperBlocked - 1) : 0)
                + (FamilyRequestBlocked ? Math.Pow(2, (int)ConfigType.FamilyRequestBlocked - 1) : 0)
                + (!MouseAimLock ? Math.Pow(2, (int)ConfigType.MouseAimLock - 1) : 0)
                + (MinilandInviteBlocked ? Math.Pow(2, (int)ConfigType.MinilandInviteBlocked - 1) : 0)
                + (ExchangeBlocked ? Math.Pow(2, (int)ConfigType.ExchangeBlocked - 1) : 0)
                + (FriendRequestBlocked ? Math.Pow(2, (int)ConfigType.FriendRequestBlocked - 1) : 0)
                + (EmoticonsBlocked ? Math.Pow(2, (int)ConfigType.EmoticonsBlocked - 1) : 0)
                + (HpBlocked ? Math.Pow(2, (int)ConfigType.HpBlocked - 1) : 0)
                + (BuffBlocked ? Math.Pow(2, (int)ConfigType.BuffBlocked - 1) : 0)
                + (GroupRequestBlocked ? Math.Pow(2, (int)ConfigType.GroupRequestBlocked - 1) : 0)
                + (HeroChatBlocked ? Math.Pow(2, (int)ConfigType.HeroChatBlocked - 1) : 0)
                + (QuickGetUp ? Math.Pow(2, (int)ConfigType.QuickGetUp - 1) : 0);
            ;
            return $"stat {Hp} {HPLoad()} {Mp} {MPLoad()} 0 {option}";
        }

        public string GenerateStatChar()
        {
            int type = 0;
            int type2 = 0;
            switch (Class)
            {
                case (byte)ClassType.Adventurer:
                    type = 0;
                    type2 = 1;
                    break;

                case (byte)ClassType.Magician:
                    type = 2;
                    type2 = 1;
                    break;

                case (byte)ClassType.Swordman:
                    type = 0;
                    type2 = 1;
                    break;

                case (byte)ClassType.Archer:
                    type = 1;
                    type2 = 0;
                    break;
            }

            int weaponUpgrade = 0;
            int secondaryUpgrade = 0;
            int armorUpgrade = 0;
            MinHit = ServersData.MinHit(Class, Level);
            MaxHit = ServersData.MaxHit(Class, Level);
            HitRate = ServersData.HitRate(Class, Level);
            HitCriticalRate = ServersData.HitCriticalRate(Class, Level);
            HitCritical = ServersData.HitCritical(Class, Level);
            MinDistance = ServersData.MinDistance(Class, Level);
            MaxDistance = ServersData.MaxDistance(Class, Level);
            DistanceRate = ServersData.DistanceRate(Class, Level);
            DistanceCriticalRate = ServersData.DistCriticalRate(Class, Level);
            DistanceCritical = ServersData.DistCritical(Class, Level);
            FireResistance = ServersData.FireResistance(Class, Level);
            LightResistance = ServersData.LightResistance(Class, Level);
            WaterResistance = ServersData.WaterResistance(Class, Level);
            DarkResistance = ServersData.DarkResistance(Class, Level);
            Defence = ServersData.Defence(Class, Level);
            DefenceRate = ServersData.DefenceRate(Class, Level);
            Element = ServersData.Element(Class, Level);
            DistanceDefence = ServersData.DistanceDefence(Class, Level);
            DistanceDefenceRate = ServersData.DistanceDefenceRate(Class, Level);
            MagicalDefence = ServersData.MagicalDefence(Class, Level);
            if (UseSp)
            {
                SpecialistInstance specialist = EquipmentList.LoadBySlotAndType<SpecialistInstance>((byte)EquipmentType.Sp, (byte)InventoryType.Equipment);
                if (specialist != null)
                {
                    int point = ServersData.SlPoint(specialist.SlDamage, 0);
                    int p = 0;
                    if (point <= 10)
                        p = point * 5;
                    else if (point <= 20)
                        p = 50 + (point - 10) * 6;
                    else if (point <= 30)
                        p = 110 + (point - 20) * 7;
                    else if (point <= 40)
                        p = 180 + (point - 30) * 8;
                    else if (point <= 50)
                        p = 260 + (point - 40) * 9;
                    else if (point <= 60)
                        p = 350 + (point - 50) * 10;
                    else if (point <= 70)
                        p = 450 + (point - 60) * 11;
                    else if (point <= 80)
                        p = 560 + (point - 70) * 13;
                    else if (point <= 90)
                        p = 690 + (point - 80) * 14;
                    else if (point <= 94)
                        p = 830 + (point - 90) * 15;
                    else if (point <= 95)
                        p = 890 + 16;
                    else if (point <= 97)
                        p = 906 + (point - 95) * 17;
                    else if (point <= 100)
                        p = 940 + (point - 97) * 20;
                    MaxHit += p;
                    MinHit += p;

                    p = 0;
                    if (point <= 10)
                        p = point;
                    else if (point <= 20)
                        p = 10 + (point - 10) * 2;
                    else if (point <= 30)
                        p = 30 + (point - 10) * 3;
                    else if (point <= 40)
                        p = 60 + (point - 10) * 4;
                    else if (point <= 50)
                        p = 100 + (point - 10) * 5;
                    else if (point <= 60)
                        p = 150 + (point - 10) * 6;
                    else if (point <= 70)
                        p = 210 + (point - 10) * 7;
                    else if (point <= 80)
                        p = 280 + (point - 10) * 8;
                    else if (point <= 90)
                        p = 360 + (point - 10) * 9;
                    else if (point <= 100)
                        p = 450 + (point - 10) * 10;
                    MinDistance += p;
                    MaxDistance += p;

                    point = ServersData.SlPoint(specialist.SlDefence, 1);
                    p = 0;
                    if (point <= 50)
                        p = point;
                    else
                        p = 50 + (point - 50) * 2;
                    Defence += p;
                    MagicalDefence += p;
                    DistanceDefence += p;

                    point = ServersData.SlPoint(specialist.SlElement, 2);
                    p = 0;
                    if (point <= 50)
                        p = point;
                    else
                        p = 50 + (point - 50) * 2;
                    Element += p;
                }
            }
            //TODO: add base stats
            WearableInstance weapon = EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.MainWeapon, (byte)InventoryType.Equipment);
            if (weapon != null)
            {
                Item iteminfo = ServerManager.GetItem(weapon.ItemVNum);
                weaponUpgrade = weapon.Upgrade;
                MinHit += weapon.DamageMinimum + iteminfo.DamageMinimum;
                MaxHit += weapon.DamageMaximum + iteminfo.DamageMaximum;
                HitRate += weapon.HitRate + iteminfo.HitRate;
                HitCriticalRate += weapon.CriticalLuckRate + iteminfo.CriticalLuckRate;
                HitCritical += weapon.CriticalRate + iteminfo.CriticalRate;
                //maxhp-mp
            }

            WearableInstance weapon2 = EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.SecondaryWeapon, (byte)InventoryType.Equipment);
            if (weapon2 != null)
            {
                Item iteminfo = ServerManager.GetItem(weapon2.ItemVNum);
                secondaryUpgrade = weapon2.Upgrade;
                MinDistance += weapon2.DamageMinimum + iteminfo.DamageMinimum;
                MaxDistance += weapon2.DamageMaximum + iteminfo.DamageMaximum;
                DistanceRate += weapon2.HitRate + iteminfo.HitRate;
                DistanceCriticalRate += weapon2.CriticalLuckRate + iteminfo.CriticalLuckRate;
                DistanceCritical += weapon2.CriticalRate + iteminfo.CriticalRate;
                //maxhp-mp
            }

            WearableInstance armor = EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.Armor, (byte)InventoryType.Equipment);
            if (armor != null)
            {
                Item iteminfo = ServerManager.GetItem(armor.ItemVNum); // unused variable
                armorUpgrade = armor.Upgrade;
                Defence += armor.CloseDefence + iteminfo.CloseDefence;
                DistanceDefence += armor.DistanceDefence + iteminfo.DistanceDefence;
                MagicalDefence += armor.MagicDefence + iteminfo.MagicDefence;
                DefenceRate += armor.DefenceDodge + iteminfo.DefenceDodge;
                DistanceDefenceRate += armor.DistanceDefenceDodge + iteminfo.DistanceDefenceDodge;
            }

            //handle specialist
            if (UseSp)
            {
                SpecialistInstance specialist = EquipmentList.LoadBySlotAndType<SpecialistInstance>((byte)EquipmentType.Sp, (byte)InventoryType.Equipment);
                if (specialist != null)
                {
                    FireResistance += specialist.SpFire;
                    LightResistance += specialist.SpLight;
                    WaterResistance += specialist.SpWater;
                    DarkResistance += specialist.SpWater;
                    Defence += specialist.SpDefence * 10;
                    DistanceDefence += specialist.SpDefence * 10;

                    MinHit += specialist.SpDamage * 10;
                    MaxHit += specialist.SpDamage * 10;
                }
            }

            WearableInstance item = null;
            for (short i = 1; i < 14; i++)
            {
                item = EquipmentList.LoadBySlotAndType<WearableInstance>(i, (byte)InventoryType.Equipment);

                if (item != null)
                {
                    Item iteminfo = ServerManager.GetItem(item.ItemVNum);
                    if (((iteminfo.EquipmentSlot != (byte)EquipmentType.MainWeapon)
                        && (iteminfo.EquipmentSlot != (byte)EquipmentType.SecondaryWeapon)
                        && iteminfo.EquipmentSlot != (byte)EquipmentType.Armor
                        && iteminfo.EquipmentSlot != (byte)EquipmentType.Sp))
                    {
                        FireResistance += item.FireResistance + iteminfo.FireResistance;
                        LightResistance += item.LightResistance + iteminfo.LightResistance;
                        WaterResistance += item.WaterResistance + iteminfo.WaterResistance;
                        DarkResistance += item.DarkResistance + iteminfo.DarkResistance;
                        Defence += item.CloseDefence + iteminfo.CloseDefence;
                        DefenceRate += item.DefenceDodge + iteminfo.DefenceDodge;
                        DistanceDefence += item.DistanceDefence + iteminfo.DistanceDefence;
                        DistanceDefenceRate += item.DistanceDefenceDodge + iteminfo.DistanceDefenceDodge;
                    }
                }
            }
            return $"sc {type} {weaponUpgrade} {MinHit} {MaxHit} {HitRate} {HitCriticalRate} {HitCritical} {type2} {secondaryUpgrade} {MinDistance} {MaxDistance} {DistanceRate} {DistanceCriticalRate} {DistanceCritical} {armorUpgrade} {Defence} {DefenceRate} {DistanceDefence} {DistanceDefenceRate} {MagicalDefence} {FireResistance} {WaterResistance} {LightResistance} {DarkResistance}";
        }

        public string GenerateStatInfo()
        {
            return $"st 1 {CharacterId} {Level} {HeroLevel} {(int)(Hp / (float)HPLoad() * 100)} {(int)(Mp / (float)MPLoad() * 100)} {Hp} {Mp}";
        }

        public string GenerateTit()
        {
            return $"tit {Language.Instance.GetMessageFromKey(Class == (byte)ClassType.Adventurer ? ClassType.Adventurer.ToString().ToUpper() : Class == (byte)ClassType.Swordman ? ClassType.Swordman.ToString().ToUpper() : Class == (byte)ClassType.Archer ? ClassType.Archer.ToString().ToUpper() : ClassType.Magician.ToString().ToUpper())} {Name}";
        }

        public string GenerateTp()
        {
            return $"tp 1 {CharacterId} {MapX} {MapY} 0";
        }

        public int GetCP()
        {
            int cpused = 0;
            foreach (CharacterSkill ski in Skills)
            {
                Skill skillinfo = ServerManager.GetSkill(ski.SkillVNum);
                if (skillinfo != null)
                    cpused += skillinfo.CPCost;
            }
            return (JobLevel - 1) * 2 - cpused;
        }

        public int GetDamage(int damage)
        {
            CloseShop();
            if (Hp >= 0)
            {
                Hp -= damage;
            }

            return Hp;
        }

        public int GetDigniteIco()
        {
            int icoDignite = 1;
            if (Dignite <= -100)
                icoDignite = 2;
            if (Dignite <= -200)
                icoDignite = 3;
            if (Dignite <= -400)
                icoDignite = 4;
            if (Dignite <= -600)
                icoDignite = 5;
            if (Dignite <= -800)
                icoDignite = 6;

            return icoDignite;
        }

        public int GetReputIco()
        {
            if (Reput >= 5000001)
            {
                switch (DAOFactory.CharacterDAO.IsReputHero(CharacterId))
                {
                    case 1:
                        return 28;

                    case 2:
                        return 29;

                    case 3:
                        return 30;

                    case 4:
                        return 31;

                    case 5:
                        return 32;
                }
            }
            if (Reput <= 50) return 1;
            if (Reput <= 150) return 2;
            if (Reput <= 250) return 3;
            if (Reput <= 500) return 4;
            if (Reput <= 750) return 5;
            if (Reput <= 1000) return 6;
            if (Reput <= 2250) return 7;
            if (Reput <= 3500) return 8;
            if (Reput <= 5000) return 9;
            if (Reput <= 9500) return 10;
            if (Reput <= 19000) return 11;
            if (Reput <= 25000) return 12;
            if (Reput <= 40000) return 13;
            if (Reput <= 60000) return 14;
            if (Reput <= 85000) return 15;
            if (Reput <= 115000) return 16;
            if (Reput <= 150000) return 17;
            if (Reput <= 190000) return 18;
            if (Reput <= 235000) return 19;
            if (Reput <= 285000) return 20;
            if (Reput <= 350000) return 21;
            if (Reput <= 500000) return 22;
            if (Reput <= 1500000) return 23;
            if (Reput <= 2500000) return 24;
            if (Reput <= 3750000) return 25;
            if (Reput <= 5000000) return 26;
            return 27;
        }

        public int HealthHPLoad()
        {
            if (IsSitting)
                return ServersData.HpHealth[Class];
            else if ((DateTime.Now - LastDefence).TotalSeconds > 2)
                return ServersData.HpHealthStand[Class];
            else
                return 0;
        }

        public int HealthMPLoad()
        {
            if (IsSitting)
                return ServersData.MpHealth[Class];
            else if ((DateTime.Now - LastDefence).TotalSeconds > 2)
                return ServersData.MpHealthStand[Class];
            else
                return 0;
        }

        public double HPLoad()
        {
            double multiplicator = 1.0;
            int hp = 0;
            if (UseSp)
            {
                SpecialistInstance inventory = EquipmentList.LoadBySlotAndType<SpecialistInstance>((byte)EquipmentType.Sp, (byte)InventoryType.Equipment);
                if (inventory != null)
                {
                    int point = ServersData.SlPoint(inventory.SlHP, 3);
                    if (point <= 50)
                        multiplicator += point / 100.0;
                    else
                        multiplicator += 0.5 + (point - 50.00) / 50.00;

                    hp = inventory.HP + inventory.SpHP * 100;
                }
            }
            return (int)((ServersData.HPData[Class, Level] + hp) * multiplicator);
        }

        public void InterruptCharChange()
        {
            if (ThreadCharChange != null && ThreadCharChange.IsAlive)
                ThreadCharChange.Suspend();
        }

        public double JobXPLoad()
        {
            if (Class == (byte)ClassType.Adventurer)
                return ServersData.FirstJobXPData[JobLevel - 1];
            return ServersData.SecondJobXPData[JobLevel - 1];
        }

        public IEnumerable<ItemInstance> LoadBySlotAllowed(short itemVNum, int amount)
        {
            return InventoryList.Inventory.Where(i => i.ItemInstance.ItemVNum.Equals(itemVNum) && i.ItemInstance.Amount + amount < 100).Select(inventoryitemobject => new ItemInstance(inventoryitemobject.ItemInstance));
        }

        public void LoadInventory()
        {
            IEnumerable<InventoryDTO> inventorysDTO = DAOFactory.InventoryDAO.LoadByCharacterId(CharacterId).ToList();

            InventoryList = new InventoryList(Session.Character);
            EquipmentList = new InventoryList(Session.Character);
            foreach (InventoryDTO inventory in inventorysDTO)
            {
                inventory.CharacterId = CharacterId;

                if (inventory.Type != (byte)InventoryType.Equipment)
                    InventoryList.Inventory.Add(new Inventory(inventory));
                else
                    EquipmentList.Inventory.Add(new Inventory(inventory));
            }
        }

        public void LoadQuicklists()
        {
            QuicklistEntries = new List<QuicklistEntry>();
            IEnumerable<QuicklistEntryDTO> quicklistDTO = DAOFactory.QuicklistEntryDAO.Load(CharacterId);
            foreach (QuicklistEntryDTO qle in quicklistDTO)
            {
                QuicklistEntries.Add(Mapper.DynamicMap<QuicklistEntry>(qle));
            }
        }

        public void LoadSkills()
        {
            Skills = new List<CharacterSkill>();
            IEnumerable<CharacterSkillDTO> characterskillDTO = DAOFactory.CharacterSkillDAO.LoadByCharacterId(CharacterId);
            foreach (CharacterSkillDTO characterskill in characterskillDTO.OrderBy(s => s.SkillVNum))
            {
                Skills.Add(Mapper.DynamicMap<CharacterSkill>(characterskill));
            }
        }

        public double MPLoad()
        {
            int mp = 0;
            double multiplicator = 1.0;
            if (UseSp)
            {
                SpecialistInstance inventory = EquipmentList.LoadBySlotAndType<SpecialistInstance>((byte)EquipmentType.Sp, (byte)InventoryType.Equipment);
                if (inventory != null)
                {
                    int point = ServersData.SlPoint(inventory.SlHP, 3);
                    if (point <= 50)
                        multiplicator += point / 100.0;
                    else
                        multiplicator += 0.5 + (point - 50.00) / 50.00; ;

                    mp = inventory.MP + inventory.SpHP * 100;
                }
            }
            return (int)((ServersData.MPData[Class, Level] + mp) * multiplicator);
        }

        public void NotiyRarifyResult(byte rare)
        {
            Session.Client.SendPacket(GenerateSay(String.Format(Language.Instance.GetMessageFromKey("RARIFY_SUCCESS"), rare), 12));
            Session.Client.SendPacket(GenerateMsg(String.Format(Language.Instance.GetMessageFromKey("RARIFY_SUCCESS"), rare), 0));
            Session.Client.SendPacket(GenerateEff(3005));
        }

        public void Save()
        {
            try
            {
                CharacterDTO tempsave = this;
                SaveResult insertResult = DAOFactory.CharacterDAO.InsertOrUpdate(ref tempsave); // unused variable, check for success?

                // First remove the old...

                // Character's Inventories
                foreach (InventoryDTO inv in DAOFactory.InventoryDAO.LoadByCharacterId(CharacterId))
                {
                    if (inv.Type == (byte)InventoryType.Equipment)
                    {
                        if (EquipmentList.LoadInventoryBySlotAndType(inv.Slot, inv.Type) == null)
                            DAOFactory.InventoryDAO.DeleteFromSlotAndType(CharacterId, inv.Slot, inv.Type);
                    }
                    else
                    {
                        if (InventoryList.LoadInventoryBySlotAndType(inv.Slot, inv.Type) == null)
                            DAOFactory.InventoryDAO.DeleteFromSlotAndType(CharacterId, inv.Slot, inv.Type);
                    }
                }

                // Character's Skills
                if (Skills != null)
                {
                    foreach (CharacterSkillDTO skill in DAOFactory.CharacterSkillDAO.LoadByCharacterId(CharacterId))
                        if (Skills.FirstOrDefault(s => s.SkillVNum == skill.SkillVNum) == null)
                            DAOFactory.CharacterSkillDAO.Delete(CharacterId, skill.SkillVNum);
                }

                // Character's QuicklistEntries
                if (QuicklistEntries != null)
                {
                    foreach (QuicklistEntryDTO quicklists in DAOFactory.QuicklistEntryDAO.Load(CharacterId))
                        if (QuicklistEntries.FirstOrDefault(s => s.EntryId == quicklists.EntryId) == null)
                            DAOFactory.QuicklistEntryDAO.Delete(CharacterId, quicklists.EntryId);
                }

                // ... then save the new
                InventoryList.Save();
                EquipmentList.Save();

                if (Skills != null)
                {
                    Skills = DAOFactory.CharacterSkillDAO.InsertOrUpdate(Skills).Select(cs => new CharacterSkill(cs)).ToList();
                }

                if (QuicklistEntries != null)
                    for (int i = QuicklistEntries.Count() - 1; i >= 0; i--)
                        QuicklistEntries.ElementAt(i).Save();
            }
            catch (Exception e)
            {
                Logger.Log.Error("Save Character failed.",e);
            }
        }

        public double SPXPLoad()
        {
            SpecialistInstance sp2 = EquipmentList.LoadBySlotAndType<SpecialistInstance>((short)EquipmentType.Sp, (byte)InventoryType.Equipment);

            return ServersData.SpXPData[sp2.SpLevel - 1];
        }

        public bool Update()
        {
            try
            {
                CharacterDTO characterToUpdate = Mapper.DynamicMap<CharacterDTO>(this);
                DAOFactory.CharacterDAO.InsertOrUpdate(ref characterToUpdate);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public double XPLoad()
        {
            return ServersData.XPData[Level - 1];
        }

        private object HeroXPLoad()
        {
            return 949560;//need to load true algoritm
        }

        #endregion
    }
}