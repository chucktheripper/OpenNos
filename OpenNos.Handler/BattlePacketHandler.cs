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
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenNos.Handler
{
    public class BattlePacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession _session;

        #endregion

        #region Instantiation

        public BattlePacketHandler(ClientSession session)
        {
            _session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get { return _session; } }

        #endregion

        #region Methods

        [Packet("mtlist")]
        public void SpecialZoneHit(string packet)
        {
            Logger.Debug(packet, Session.SessionId);
            string[] packetsplit = packet.Split(' ');
            ushort damage = 0;
            int hitmode = 0;

            if (packetsplit.Length > 3)
                for (int i = 3; i < packetsplit.Length - 1; i += 2)
                {
                    List<CharacterSkill> skills = Session.Character.UseSp ? Session.Character.SkillsSp : Session.Character.Skills;

                    if (skills != null)
                    {
                        Skill skill = null;
                        CharacterSkill ski = skills.FirstOrDefault(s => (skill = ServerManager.GetSkill(s.SkillVNum)) != null && skill.CastId == short.Parse(packetsplit[i]));

                        MapMonster mon = Session.CurrentMap.Monsters.FirstOrDefault(s => s.MapMonsterId == short.Parse(packetsplit[i + 1]));
                        if (mon != null && skill != null)
                        {
                            damage = GenerateDamage(mon.MapMonsterId, skill, ref hitmode);
                            Session.CurrentMap?.Broadcast($"su {1} {Session.Character.CharacterId} {3} {mon.MapMonsterId} {skill.SkillVNum} {skill.Cooldown} {skill.AttackAnimation} {skill.Effect} {Session.Character.MapX} {Session.Character.MapY} {(mon.Alive ? 1 : 0)} {(int)(((float)mon.CurrentHp / (float)ServerManager.GetNpc(mon.MonsterVNum).MaxHP) * 100)} {damage} {5} {skill.SkillType - 1}");
                        }
                    }
                }
        }

        public void TargetHit(int castingId, int targetObject, int targetId)
        {
            List<CharacterSkill> skills = Session.Character.UseSp ? Session.Character.SkillsSp : Session.Character.Skills;
            bool notcancel = false;

             if (skills != null)
            {
                ushort damage = 0; ;
                int hitmode = 0;
                Skill skill = null;

                CharacterSkill ski = skills.FirstOrDefault(s => (skill = ServerManager.GetSkill(s.SkillVNum)) != null && skill?.CastId == castingId);
                if (ski != null && !ski.Used)
                {
                    if (skill != null && skill.TargetType == 1 && skill.HitType == 1)
                    {
                        Task t = Task.Factory.StartNew((Func<Task>)(async () =>
                        {
                            Session.CurrentMap?.Broadcast($"ct 1 {Session.Character.CharacterId} 1 {Session.Character.CharacterId} {skill.CastAnimation} -1 {skill.SkillVNum}");
                            ski.Used = true;

                            ski.LastUse = DateTime.Now;
                            if (skill.CastEffect != 0)
                            {
                                Session.CurrentMap?.Broadcast(Session.Character.GenerateEff(skill.CastEffect));
                                await Task.Delay(skill.CastTime * 100);
                            }
                            notcancel = true;
                            Session.CurrentMap?.Broadcast($"su {1} {Session.Character.CharacterId} {1} {Session.Character.CharacterId} {skill.SkillVNum} {skill.Cooldown} {skill.AttackAnimation} {skill.Effect} {Session.Character.MapX} {Session.Character.MapY} 1 {(((double)Session.Character.Hp / Session.Character.HPLoad()) * 100)} {0} -2 {skill.SkillType}");
                            MapMonster mmon;
                            if (skill.TargetRange != 0)
                                foreach (MapMonster mon in ServerManager.GetMap(Session.Character.MapId).GetListMonsterInRange(Session.Character.MapX, Session.Character.MapY, skill.TargetRange))
                                {
                                    damage = GenerateDamage(mon.MapMonsterId, skill, ref hitmode);
                                    mmon = ServerManager.GetMap(Session.Character.MapId).Monsters.FirstOrDefault(s => s.MapMonsterId == mon.MapMonsterId);
                                    Session.CurrentMap?.Broadcast($"su {1} {Session.Character.CharacterId} {3} {mmon.MapMonsterId} {skill.SkillVNum} {skill.Cooldown} {skill.AttackAnimation} {skill.Effect} {Session.Character.MapX} {Session.Character.MapY} {(mmon.Alive ? 1 : 0)} {(int)(((float)mmon.CurrentHp / (float)ServerManager.GetNpc(mon.MonsterVNum).MaxHP) * 100)} {damage} {5} {skill.SkillType - 1}");
                                }

                            await Task.Delay((skill.Cooldown) * 100);
                            ski.Used = false;
                            Session.Client.SendPacket($"sr {castingId}");
                        }));
                    }
                    else if (skill != null && skill.TargetType == 0)//if monster target
                    {
                        MapMonster mmon = Session.CurrentMap.Monsters.FirstOrDefault(s => s.MapMonsterId == targetId);
                        if (mmon != null && mmon.Alive)
                        {
                            NpcMonster monsterinfo = ServerManager.GetNpc(mmon.MonsterVNum);
                            if (ski != null && monsterinfo != null && skill != null && !ski.Used)
                            {
                                Task t = Task.Factory.StartNew((Func<Task>)(async () =>
                                {
                                    short dX = (short)(Session.Character.MapX - mmon.MapX);
                                    short dY = (short)(Session.Character.MapY - mmon.MapY);

                                    if (Map.GetDistance(new MapCell() { X = Session.Character.MapX, Y = Session.Character.MapY }, new MapCell() { X = mmon.MapX, Y = mmon.MapY }) <= skill.Range + 1 || skill.TargetRange != 0)
                                    {
                                        Session.CurrentMap?.Broadcast($"ct 1 {Session.Character.CharacterId} 3 {mmon.MapMonsterId} {skill.CastAnimation} -1 {skill.SkillVNum}");
                                        damage = GenerateDamage(mmon.MapMonsterId, skill, ref hitmode);
                                        ski.Used = true;
                                        notcancel = true;
                                        ski.LastUse = DateTime.Now;
                                        if (damage == 0 || (DateTime.Now - ski.LastUse).TotalSeconds > 3)
                                            ski.Hit = 0;
                                        else
                                            ski.Hit++;

                                        if (skill.CastEffect != 0)
                                        {
                                            Session.CurrentMap?.Broadcast(Session.Character.GenerateEff(skill.CastEffect));
                                            await Task.Delay(skill.CastTime * 100);
                                        }
                                        Combo comb = skill.Combos.FirstOrDefault(s => ski.Hit == s.Hit);
                                        if (comb != null)
                                        {
                                            if (skill.Combos.OrderByDescending(s => s.Hit).ElementAt(0).Hit == ski.Hit)
                                                ski.Hit = 0;
                                            Session.CurrentMap?.Broadcast($"su {1} {Session.Character.CharacterId} {3} {mmon.MapMonsterId} {skill.SkillVNum} {skill.Cooldown} {comb.Animation} {comb.Effect} {Session.Character.MapX} {Session.Character.MapY} {(mmon.Alive ? 1 : 0)} {(int)(((float)mmon.CurrentHp / (float)monsterinfo.MaxHP) * 100)} {damage} {hitmode} {skill.SkillType - 1}");
                                        }
                                        else
                                        {
                                            Session.CurrentMap?.Broadcast($"su {1} {Session.Character.CharacterId} {3} {mmon.MapMonsterId} {skill.SkillVNum} {skill.Cooldown} {skill.AttackAnimation} {skill.Effect} {Session.Character.MapX} {Session.Character.MapY} {(mmon.Alive ? 1 : 0)} {(int)(((float)mmon.CurrentHp / (float)monsterinfo.MaxHP) * 100)} {damage} {hitmode} {skill.SkillType - 1}");
                                        }

                                        if (skill.TargetRange != 0)
                                            foreach (MapMonster mon in ServerManager.GetMap(Session.Character.MapId).GetListMonsterInRange(mmon.MapX, mmon.MapY, skill.TargetRange))
                                            {
                                                damage = GenerateDamage(mon.MapMonsterId, skill, ref hitmode);
                                                Session.CurrentMap?.Broadcast($"su {1} {Session.Character.CharacterId} {3} {mon.MapMonsterId} {skill.SkillVNum} {skill.Cooldown} {skill.AttackAnimation} {skill.Effect} {Session.Character.MapX} {Session.Character.MapY} {(mon.Alive ? 1 : 0)} {(int)(((float)mon.CurrentHp / (float)ServerManager.GetNpc(mon.MonsterVNum).MaxHP) * 100)} {damage} {5} {skill.SkillType - 1}");
                                            }

                                        await Task.Delay((skill.Cooldown) * 100);
                                        ski.Used = false;
                                        Session.Client.SendPacket($"sr {castingId}");
                                    }
                                }));
                            }
                        }
                    }
                }
            }

            if (!notcancel)
                Session.Client.SendPacket("cancel 0 0");
        }

        [Packet("u_s")]
        public void UseSkill(string packet)
        {
            Logger.Debug(packet, Session.SessionId);
            Session.Character.InterruptCharChange();

            if (Session.Character.CanFight)
            {
                string[] packetsplit = packet.Split(' ');
                if (packetsplit.Length > 6)
                {
                    Session.Character.MapX = Convert.ToInt16(packetsplit[5]);
                    Session.Character.MapY = Convert.ToInt16(packetsplit[6]);
                }
                if (packetsplit.Length > 4)
                    if (Session.Character.Hp > 0)
                        Task.Factory.StartNew(() => TargetHit(Convert.ToInt32(packetsplit[2]), Convert.ToInt32(packetsplit[3]), Convert.ToInt32(packetsplit[4])));
            }
        }

        [Packet("u_as")]
        public void UseZonesSkill(string packet)
        {
            Logger.Debug(packet, Session.SessionId);
            Session.Character.InterruptCharChange();
            if (Session.Character.CanFight)
            {
                string[] packetsplit = packet.Split(' ');
                if (packetsplit.Length > 4)
                    if (Session.Character.Hp > 0)
                        Task.Factory.StartNew(() => ZoneHit(Convert.ToInt32(packetsplit[2]), Convert.ToInt16(packetsplit[3]), Convert.ToInt16(packetsplit[4])));
            }
        }

        private ushort GenerateDamage(int monsterid, Skill skill, ref int hitmode)
        {
            #region Definitions

            MapMonster mmon = ServerManager.GetMap(Session.Character.MapId).Monsters.FirstOrDefault(s => s.MapMonsterId == monsterid);
            short dX = (short)(Session.Character.MapX - mmon.MapX);
            short dY = (short)(Session.Character.MapY - mmon.MapY);
            NpcMonster monsterinfo = ServerManager.GetNpc(mmon.MonsterVNum);
            Random random = new Random();

            int generated = random.Next(0, 100);
            int miss_chance = 20;
            int MonsterDefense = 0;

            byte MainUpgrade = 0;
            int MainCritChance = 0;
            int MainCritHit = 0;
            int MainMinDmg = 0;
            int MainMaxDmg = 0;
            int MainHitRate = 0;

            byte SecUpgrade = 0;
            int SecCritChance = 0;
            int SecCritHit = 0;
            int SecMinDmg = 0;
            int SecMaxDmg = 0;
            int SecHitRate = 0;

            int CritChance = 0;
            int CritHit = 0;
            int MinDmg = 0;
            int MaxDmg = 0;
            int HitRate = 0;
            sbyte Upgrade = 0;

            #endregion

            #region Get Weapon Stats

            WearableInstance weapon = Session.Character.EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.MainWeapon, (byte)InventoryType.Equipment);
            if (weapon != null)
            {
                MainUpgrade = weapon.Upgrade;
                MainMinDmg += weapon.DamageMinimum + weapon.Item.DamageMinimum;
                MainMaxDmg += weapon.DamageMaximum + weapon.Item.DamageMaximum;
                MainHitRate += weapon.HitRate + weapon.Item.HitRate;
                MainCritChance += weapon.CriticalLuckRate + weapon.Item.CriticalLuckRate;
                MainCritHit += weapon.CriticalRate + weapon.Item.CriticalRate;
            }

            WearableInstance weapon2 = Session.Character.EquipmentList.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.SecondaryWeapon, (byte)InventoryType.Equipment);
            if (weapon2 != null)
            {
                SecUpgrade = weapon2.Upgrade;
                SecMinDmg += weapon2.DamageMinimum + weapon2.Item.DamageMinimum;
                SecMaxDmg += weapon2.DamageMaximum + weapon2.Item.DamageMaximum;
                SecHitRate += weapon2.HitRate + weapon2.Item.HitRate;
                SecCritChance += weapon2.CriticalLuckRate + weapon2.Item.CriticalLuckRate;
                SecCritHit += weapon2.CriticalRate + weapon2.Item.CriticalRate;
            }

            #endregion

            #region Switch skill.Type

            switch (skill.Type)
            {
                case 0:
                    MonsterDefense = monsterinfo.CloseDefence;
                    if (Session.Character.Class == (byte)ClassType.Archer)
                    {
                        Upgrade = Convert.ToSByte(SecUpgrade - monsterinfo.DefenceUpgrade);
                        MinDmg = ServersData.MinHit(Session.Character.Class, Session.Character.Level) + SecMinDmg;
                        MaxDmg = ServersData.MaxHit(Session.Character.Class, Session.Character.Level) + SecMaxDmg;

                        #region Upgrade Boost Calculation

                        switch (Upgrade)
                        {
                            case -10:
                                MonsterDefense += (int)(MonsterDefense * 2);
                                break;

                            case -9:
                                MonsterDefense += (int)(MonsterDefense * 1.2);
                                break;

                            case -8:
                                MonsterDefense += (int)(MonsterDefense * 0.9);
                                break;

                            case -7:
                                MonsterDefense += (int)(MonsterDefense * 0.65);
                                break;

                            case -6:
                                MonsterDefense += (int)(MonsterDefense * 0.54);
                                break;

                            case -5:
                                MonsterDefense += (int)(MonsterDefense * 0.43);
                                break;

                            case -4:
                                MonsterDefense += (int)(MonsterDefense * 0.32);
                                break;

                            case -3:
                                MonsterDefense += (int)(MonsterDefense * 0.22);
                                break;

                            case -2:
                                MonsterDefense += (int)(MonsterDefense * 0.15);
                                break;

                            case -1:
                                MonsterDefense += (int)(MonsterDefense * 0.1);
                                break;

                            case 0:
                                break;

                            case 1:
                                MinDmg += (int)(MinDmg * 0.1);
                                MaxDmg += (int)(MaxDmg * 0.1);
                                break;

                            case 2:
                                MinDmg += (int)(MinDmg * 0.15);
                                MaxDmg += (int)(MaxDmg * 0.15);
                                break;

                            case 3:
                                MinDmg += (int)(MinDmg * 0.22);
                                MaxDmg += (int)(MaxDmg * 0.22);
                                break;

                            case 4:
                                MinDmg += (int)(MinDmg * 0.32);
                                MaxDmg += (int)(MaxDmg * 0.32);
                                break;

                            case 5:
                                MinDmg += (int)(MinDmg * 0.43);
                                MaxDmg += (int)(MaxDmg * 0.43);
                                break;

                            case 6:
                                MinDmg += (int)(MinDmg * 0.54);
                                MaxDmg += (int)(MaxDmg * 0.54);
                                break;

                            case 7:
                                MinDmg += (int)(MinDmg * 0.65);
                                MaxDmg += (int)(MaxDmg * 0.65);
                                break;

                            case 8:
                                MinDmg += (int)(MinDmg * 0.9);
                                MaxDmg += (int)(MaxDmg * 0.9);
                                break;

                            case 9:
                                MinDmg += (int)(MinDmg * 1.2);
                                MaxDmg += (int)(MaxDmg * 1.2);
                                break;

                            case 10:
                                MinDmg += (int)(MinDmg * 2);
                                MaxDmg += (int)(MaxDmg * 2);
                                break;
                        }

                        #endregion

                        MinDmg -= MonsterDefense;
                        MaxDmg -= MonsterDefense;

                        HitRate = SecHitRate;
                        CritChance = SecCritChance;
                        CritHit = SecCritHit;
                    }
                    else
                    {
                        Upgrade = Convert.ToSByte(MainUpgrade - monsterinfo.DefenceUpgrade);
                        MinDmg = ServersData.MinHit(Session.Character.Class, Session.Character.Level) + MainMinDmg;
                        MaxDmg = ServersData.MaxHit(Session.Character.Class, Session.Character.Level) + MainMaxDmg;

                        #region Upgrade Boost Calculation

                        switch (Upgrade)
                        {
                            case -10:
                                MonsterDefense += (int)(MonsterDefense * 2);
                                break;

                            case -9:
                                MonsterDefense += (int)(MonsterDefense * 1.2);
                                break;

                            case -8:
                                MonsterDefense += (int)(MonsterDefense * 0.9);
                                break;

                            case -7:
                                MonsterDefense += (int)(MonsterDefense * 0.65);
                                break;

                            case -6:
                                MonsterDefense += (int)(MonsterDefense * 0.54);
                                break;

                            case -5:
                                MonsterDefense += (int)(MonsterDefense * 0.43);
                                break;

                            case -4:
                                MonsterDefense += (int)(MonsterDefense * 0.32);
                                break;

                            case -3:
                                MonsterDefense += (int)(MonsterDefense * 0.22);
                                break;

                            case -2:
                                MonsterDefense += (int)(MonsterDefense * 0.15);
                                break;

                            case -1:
                                MonsterDefense += (int)(MonsterDefense * 0.1);
                                break;

                            case 0:
                                break;

                            case 1:
                                MinDmg += (int)(MinDmg * 0.1);
                                MaxDmg += (int)(MaxDmg * 0.1);
                                break;

                            case 2:
                                MinDmg += (int)(MinDmg * 0.15);
                                MaxDmg += (int)(MaxDmg * 0.15);
                                break;

                            case 3:
                                MinDmg += (int)(MinDmg * 0.22);
                                MaxDmg += (int)(MaxDmg * 0.22);
                                break;

                            case 4:
                                MinDmg += (int)(MinDmg * 0.32);
                                MaxDmg += (int)(MaxDmg * 0.32);
                                break;

                            case 5:
                                MinDmg += (int)(MinDmg * 0.43);
                                MaxDmg += (int)(MaxDmg * 0.43);
                                break;

                            case 6:
                                MinDmg += (int)(MinDmg * 0.54);
                                MaxDmg += (int)(MaxDmg * 0.54);
                                break;

                            case 7:
                                MinDmg += (int)(MinDmg * 0.65);
                                MaxDmg += (int)(MaxDmg * 0.65);
                                break;

                            case 8:
                                MinDmg += (int)(MinDmg * 0.9);
                                MaxDmg += (int)(MaxDmg * 0.9);
                                break;

                            case 9:
                                MinDmg += (int)(MinDmg * 1.2);
                                MaxDmg += (int)(MaxDmg * 1.2);
                                break;

                            case 10:
                                MinDmg += (int)(MinDmg * 2);
                                MaxDmg += (int)(MaxDmg * 2);
                                break;
                        }

                        #endregion

                        MinDmg -= MonsterDefense;
                        MaxDmg -= MonsterDefense;

                        HitRate = MainHitRate;
                        CritChance = MainCritChance;
                        CritHit = MainCritHit;
                    }
                    miss_chance /= (int)(1 + HitRate / 100.0);//unsure
                    break;

                case 1:
                    MonsterDefense = monsterinfo.CloseDefence;
                    if (Session.Character.Class != (byte)ClassType.Archer)
                    {
                        Upgrade = Convert.ToSByte(SecUpgrade - monsterinfo.DefenceUpgrade);
                        MinDmg = ServersData.MinHit(Session.Character.Class, Session.Character.Level) + SecMinDmg;
                        MaxDmg = ServersData.MaxHit(Session.Character.Class, Session.Character.Level) + SecMaxDmg;

                        #region Upgrade Boost Calculation

                        switch (Upgrade)
                        {
                            case -10:
                                MonsterDefense += (int)(MonsterDefense * 2);
                                break;

                            case -9:
                                MonsterDefense += (int)(MonsterDefense * 1.2);
                                break;

                            case -8:
                                MonsterDefense += (int)(MonsterDefense * 0.9);
                                break;

                            case -7:
                                MonsterDefense += (int)(MonsterDefense * 0.65);
                                break;

                            case -6:
                                MonsterDefense += (int)(MonsterDefense * 0.54);
                                break;

                            case -5:
                                MonsterDefense += (int)(MonsterDefense * 0.43);
                                break;

                            case -4:
                                MonsterDefense += (int)(MonsterDefense * 0.32);
                                break;

                            case -3:
                                MonsterDefense += (int)(MonsterDefense * 0.22);
                                break;

                            case -2:
                                MonsterDefense += (int)(MonsterDefense * 0.15);
                                break;

                            case -1:
                                MonsterDefense += (int)(MonsterDefense * 0.1);
                                break;

                            case 0:
                                break;

                            case 1:
                                MinDmg += (int)(MinDmg * 0.1);
                                MaxDmg += (int)(MaxDmg * 0.1);
                                break;

                            case 2:
                                MinDmg += (int)(MinDmg * 0.15);
                                MaxDmg += (int)(MaxDmg * 0.15);
                                break;

                            case 3:
                                MinDmg += (int)(MinDmg * 0.22);
                                MaxDmg += (int)(MaxDmg * 0.22);
                                break;

                            case 4:
                                MinDmg += (int)(MinDmg * 0.32);
                                MaxDmg += (int)(MaxDmg * 0.32);
                                break;

                            case 5:
                                MinDmg += (int)(MinDmg * 0.43);
                                MaxDmg += (int)(MaxDmg * 0.43);
                                break;

                            case 6:
                                MinDmg += (int)(MinDmg * 0.54);
                                MaxDmg += (int)(MaxDmg * 0.54);
                                break;

                            case 7:
                                MinDmg += (int)(MinDmg * 0.65);
                                MaxDmg += (int)(MaxDmg * 0.65);
                                break;

                            case 8:
                                MinDmg += (int)(MinDmg * 0.9);
                                MaxDmg += (int)(MaxDmg * 0.9);
                                break;

                            case 9:
                                MinDmg += (int)(MinDmg * 1.2);
                                MaxDmg += (int)(MaxDmg * 1.2);
                                break;

                            case 10:
                                MinDmg += (int)(MinDmg * 2);
                                MaxDmg += (int)(MaxDmg * 2);
                                break;
                        }

                        #endregion

                        MinDmg -= MonsterDefense;
                        MaxDmg -= MonsterDefense;

                        HitRate = SecHitRate;
                        CritChance = SecCritChance;
                        CritHit = SecCritHit;
                    }
                    else
                    {
                        Upgrade = Convert.ToSByte(MainUpgrade - monsterinfo.DefenceUpgrade);
                        MinDmg = ServersData.MinHit(Session.Character.Class, Session.Character.Level) + MainMinDmg;
                        MaxDmg = ServersData.MaxHit(Session.Character.Class, Session.Character.Level) + MainMaxDmg;

                        #region Upgrade Boost Calculation

                        switch (Upgrade)
                        {
                            case -10:
                                MonsterDefense += (int)(MonsterDefense * 2);
                                break;

                            case -9:
                                MonsterDefense += (int)(MonsterDefense * 1.2);
                                break;

                            case -8:
                                MonsterDefense += (int)(MonsterDefense * 0.9);
                                break;

                            case -7:
                                MonsterDefense += (int)(MonsterDefense * 0.65);
                                break;

                            case -6:
                                MonsterDefense += (int)(MonsterDefense * 0.54);
                                break;

                            case -5:
                                MonsterDefense += (int)(MonsterDefense * 0.43);
                                break;

                            case -4:
                                MonsterDefense += (int)(MonsterDefense * 0.32);
                                break;

                            case -3:
                                MonsterDefense += (int)(MonsterDefense * 0.22);
                                break;

                            case -2:
                                MonsterDefense += (int)(MonsterDefense * 0.15);
                                break;

                            case -1:
                                MonsterDefense += (int)(MonsterDefense * 0.1);
                                break;

                            case 0:
                                break;

                            case 1:
                                MinDmg += (int)(MinDmg * 0.1);
                                MaxDmg += (int)(MaxDmg * 0.1);
                                break;

                            case 2:
                                MinDmg += (int)(MinDmg * 0.15);
                                MaxDmg += (int)(MaxDmg * 0.15);
                                break;

                            case 3:
                                MinDmg += (int)(MinDmg * 0.22);
                                MaxDmg += (int)(MaxDmg * 0.22);
                                break;

                            case 4:
                                MinDmg += (int)(MinDmg * 0.32);
                                MaxDmg += (int)(MaxDmg * 0.32);
                                break;

                            case 5:
                                MinDmg += (int)(MinDmg * 0.43);
                                MaxDmg += (int)(MaxDmg * 0.43);
                                break;

                            case 6:
                                MinDmg += (int)(MinDmg * 0.54);
                                MaxDmg += (int)(MaxDmg * 0.54);
                                break;

                            case 7:
                                MinDmg += (int)(MinDmg * 0.65);
                                MaxDmg += (int)(MaxDmg * 0.65);
                                break;

                            case 8:
                                MinDmg += (int)(MinDmg * 0.9);
                                MaxDmg += (int)(MaxDmg * 0.9);
                                break;

                            case 9:
                                MinDmg += (int)(MinDmg * 1.2);
                                MaxDmg += (int)(MaxDmg * 1.2);
                                break;

                            case 10:
                                MinDmg += (int)(MinDmg * 2);
                                MaxDmg += (int)(MaxDmg * 2);
                                break;
                        }

                        #endregion

                        MinDmg -= MonsterDefense;
                        MaxDmg -= MonsterDefense;

                        HitRate = MainHitRate;
                        CritChance = MainCritChance;
                        CritHit = MainCritHit;
                    }
                    miss_chance /= (int)(1 + HitRate / 100.0);//unsure
                    break;

                case 2:
                    Upgrade = Convert.ToSByte(MainUpgrade - monsterinfo.MagicDefence);
                    MinDmg = ServersData.MinHit(Session.Character.Class, Session.Character.Level) + MainMinDmg;
                    MaxDmg = ServersData.MaxHit(Session.Character.Class, Session.Character.Level) + MainMaxDmg;

                    #region Upgrade Boost Calculation

                    switch (Upgrade)
                    {
                        case -10:
                            MonsterDefense += (int)(MonsterDefense * 2);
                            break;

                        case -9:
                            MonsterDefense += (int)(MonsterDefense * 1.2);
                            break;

                        case -8:
                            MonsterDefense += (int)(MonsterDefense * 0.9);
                            break;

                        case -7:
                            MonsterDefense += (int)(MonsterDefense * 0.65);
                            break;

                        case -6:
                            MonsterDefense += (int)(MonsterDefense * 0.54);
                            break;

                        case -5:
                            MonsterDefense += (int)(MonsterDefense * 0.43);
                            break;

                        case -4:
                            MonsterDefense += (int)(MonsterDefense * 0.32);
                            break;

                        case -3:
                            MonsterDefense += (int)(MonsterDefense * 0.22);
                            break;

                        case -2:
                            MonsterDefense += (int)(MonsterDefense * 0.15);
                            break;

                        case -1:
                            MonsterDefense += (int)(MonsterDefense * 0.1);
                            break;

                        case 0:
                            break;

                        case 1:
                            MinDmg += (int)(MinDmg * 0.1);
                            MaxDmg += (int)(MaxDmg * 0.1);
                            break;

                        case 2:
                            MinDmg += (int)(MinDmg * 0.15);
                            MaxDmg += (int)(MaxDmg * 0.15);
                            break;

                        case 3:
                            MinDmg += (int)(MinDmg * 0.22);
                            MaxDmg += (int)(MaxDmg * 0.22);
                            break;

                        case 4:
                            MinDmg += (int)(MinDmg * 0.32);
                            MaxDmg += (int)(MaxDmg * 0.32);
                            break;

                        case 5:
                            MinDmg += (int)(MinDmg * 0.43);
                            MaxDmg += (int)(MaxDmg * 0.43);
                            break;

                        case 6:
                            MinDmg += (int)(MinDmg * 0.54);
                            MaxDmg += (int)(MaxDmg * 0.54);
                            break;

                        case 7:
                            MinDmg += (int)(MinDmg * 0.65);
                            MaxDmg += (int)(MaxDmg * 0.65);
                            break;

                        case 8:
                            MinDmg += (int)(MinDmg * 0.9);
                            MaxDmg += (int)(MaxDmg * 0.9);
                            break;

                        case 9:
                            MinDmg += (int)(MinDmg * 1.2);
                            MaxDmg += (int)(MaxDmg * 1.2);
                            break;

                        case 10:
                            MinDmg += (int)(MinDmg * 2);
                            MaxDmg += (int)(MaxDmg * 2);
                            break;
                    }

                    #endregion

                    MinDmg -= MonsterDefense;
                    MaxDmg -= MonsterDefense;

                    HitRate = MainHitRate;
                    CritChance = 0;
                    CritHit = 0;
                    miss_chance = 0;
                    break;
            }

            #endregion

            int intdamage = random.Next(MinDmg, MaxDmg + 1);
            //unchanged from here on
            if (generated < CritChance)
            {
                hitmode = 3;
                intdamage *= 2;
            }
            if (generated > 100 - miss_chance)
            {
                hitmode = 1;
                intdamage = 0;
            }

            if (mmon.CurrentHp <= intdamage)
            {
                mmon.Alive = false;
                mmon.CurrentHp = 0;
                mmon.CurrentMp = 0;
                mmon.Death = DateTime.Now;
                Random rnd;
                int i = 1;
                foreach (DropDTO drop in monsterinfo.Drops)
                {
                    i++;
                    rnd = new Random(i * (int)DateTime.Now.Ticks & 0x0000FFFF);
                    double rndamount = rnd.Next(0, 100) * rnd.NextDouble();
                    if (rndamount <= (double)drop.DropChance / 5000.000)
                    {
                        Session.CurrentMap.DropItemByMonster(drop, mmon.MapX, mmon.MapY);
                    }
                }

                rnd = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
                short gold = Convert.ToInt16((rnd.Next(1, 8) >= 7 ? 1 : 0) * rnd.Next(6 * monsterinfo.Level, 12 * monsterinfo.Level));
                if (gold != 0)
                {
                    DropDTO drop2 = new DropDTO()
                    {
                        Amount = gold,
                        ItemVNum = 1046
                    };
                    Session.CurrentMap.DropItemByMonster(drop2, mmon.MapX, mmon.MapY);
                }
                if (Session.Character.Hp > 0)
                    GenerateXp(monsterinfo);
            }
            else
            {
                mmon.CurrentHp -= intdamage;
            }
            ushort damage = 0;

            while (intdamage > ushort.MaxValue)
            {
                intdamage -= ushort.MaxValue;
            }

            damage = Convert.ToUInt16(intdamage);

            mmon.Target = Session.Character.CharacterId;
            return damage;
        }

        private void GenerateXp(NpcMonster monsterinfo)
        {
            if ((int)(Session.Character.LevelXp / (Session.Character.XPLoad() / 10)) < (int)((Session.Character.LevelXp + monsterinfo.XP) / (Session.Character.XPLoad() / 10)))
            {
                Session.Character.Hp = (int)Session.Character.HPLoad();
                Session.Character.Mp = (int)Session.Character.MPLoad();
                Session.Client.SendPacket(Session.Character.GenerateStat());
                Session.Client.SendPacket(Session.Character.GenerateEff(5));
            }

            SpecialistInstance specialist = Session.Character.EquipmentList.LoadBySlotAndType<SpecialistInstance>((short)EquipmentType.Sp, (byte)InventoryType.Equipment);
            if (Session.Character.Level < 99)
                Session.Character.LevelXp += monsterinfo.XP;
            if ((Session.Character.Class == 0 && Session.Character.JobLevel < 20) || (Session.Character.Class != 0 && Session.Character.JobLevel < 80))
            {
                if (specialist != null && Session.Character.UseSp && specialist.SpLevel < 99)
                    Session.Character.JobLevelXp += (int)((double)monsterinfo.JobXP / (double)100 * specialist.SpLevel);
                else
                    Session.Character.JobLevelXp += monsterinfo.JobXP;
            }
            if (specialist != null && Session.Character.UseSp && specialist.SpLevel < 99)
                specialist.SpXp += monsterinfo.JobXP * (100 - specialist.SpLevel);
            double t = Session.Character.XPLoad();
            while (Session.Character.LevelXp >= t)
            {
                Session.Character.LevelXp -= (long)t;
                Session.Character.Level++;
                t = Session.Character.XPLoad();
                Session.Character.Hp = (int)Session.Character.HPLoad();
                Session.Character.Mp = (int)Session.Character.MPLoad();
                Session.Client.SendPacket(Session.Character.GenerateStat());
                Session.Client.SendPacket($"levelup {Session.Character.CharacterId}");
                Session.CurrentMap?.Broadcast(Session.Character.GenerateEff(6));
                Session.CurrentMap?.Broadcast(Session.Character.GenerateEff(198));
                ServerManager.Instance.UpdateGroup(Session.Character.CharacterId);
            }
            t = Session.Character.JobXPLoad();
            while (Session.Character.JobLevelXp >= t)
            {
                Session.Character.JobLevelXp -= (long)t;
                Session.Character.JobLevel++;
                t = Session.Character.JobXPLoad();
                Session.Character.Hp = (int)Session.Character.HPLoad();
                Session.Character.Mp = (int)Session.Character.MPLoad();
                Session.Client.SendPacket(Session.Character.GenerateStat());
                Session.Client.SendPacket($"levelup {Session.Character.CharacterId}");

                if (Session.Character.Class == 0)
                {
                    byte NewSkill = 0;
                    for (int i = 200; i <= 210; i++)
                    {
                        if (i == 209)
                            i++;

                        Skill skinfo = ServerManager.GetSkill((short)i);
                        if (skinfo.Class == 0 && Session.Character.JobLevel >= skinfo.LevelMinimum)
                        {
                            byte NewSkillVNum = (byte)i;
                            for (int ii = Session.Character.Skills.Count - 1; ii >= 0; ii--)
                            {
                                Skill myskinfo = ServerManager.GetSkill(Session.Character.Skills[ii].SkillVNum);
                                if (skinfo.SkillVNum == myskinfo.SkillVNum)
                                {
                                    NewSkillVNum = 0;
                                    break;
                                }
                            }
                            if (NewSkillVNum > 0)
                            {
                                NewSkill = 1;
                                Session.Character.Skills.Add(new CharacterSkill() { SkillVNum = NewSkillVNum, CharacterId = Session.Character.CharacterId });
                            }
                        }
                    }
                    if (NewSkill > 0)
                    {
                        Session.Client.SendPacket(Session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("SKILL_LEARNED"), 0));
                        Session.Client.SendPacket(Session.Character.GenerateSki());
                        string[] quicklistpackets = Session.Character.GenerateQuicklist();
                        foreach (string quicklist in quicklistpackets)
                            Session.Client.SendPacket(quicklist);
                    }
                }
                Session.CurrentMap?.Broadcast(Session.Character.GenerateEff(6));
                Session.CurrentMap?.Broadcast(Session.Character.GenerateEff(198));
            }
            if (specialist != null)
                t = Session.Character.SPXPLoad();
            while (specialist != null && specialist.SpXp >= t)
            {
                specialist.SpXp -= (long)t;
                specialist.SpLevel++;
                t = Session.Character.SPXPLoad();
                Session.Client.SendPacket(Session.Character.GenerateStat());
                Session.Client.SendPacket($"levelup {Session.Character.CharacterId}");

                byte SkillSpCount = (byte)Session.Character.SkillsSp.Count;
                foreach (Skill ski in ServerManager.GetAllSkill())
                {
                    if (ski.Class == Session.Character.Morph + 31 && specialist.SpLevel >= ski.LevelMinimum && SkillSpCount <= ski.CastId)
                        Session.Character.SkillsSp.Add(new CharacterSkill() { SkillVNum = ski.SkillVNum, CharacterId = Session.Character.CharacterId });
                }
                if (Session.Character.SkillsSp.Count > SkillSpCount)
                {
                    Session.Client.SendPacket(Session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("SKILL_LEARNED"), 0));
                    Session.Client.SendPacket(Session.Character.GenerateSki());
                    string[] quicklistpackets = Session.Character.GenerateQuicklist();
                    foreach (string quicklist in quicklistpackets)
                        Session.Client.SendPacket(quicklist);
                }
                Session.CurrentMap?.Broadcast(Session.Character.GenerateEff(6));
                Session.CurrentMap?.Broadcast(Session.Character.GenerateEff(198));
            }
            Session.Client.SendPacket(Session.Character.GenerateLev());
        }

        private void ZoneHit(int Castingid, short x, short y)
        {
            List<CharacterSkill> skills = Session.Character.UseSp ? Session.Character.SkillsSp : Session.Character.Skills;
            ushort damage = 0;
            int hitmode = 0;
            Skill skill = null;
            CharacterSkill ski = skills.FirstOrDefault(s => (skill = ServerManager.GetSkill(s.SkillVNum)) != null && skill.CastId == Castingid);

            if (skill != null)
            {
                Task t = Task.Factory.StartNew((Func<Task>)(async () =>
                {
                    Session.CurrentMap?.Broadcast($"ct_n 1 {Session.Character.CharacterId} 3 -1 {skill.CastAnimation} {skill.CastEffect} {skill.SkillVNum}");
                    ski.Used = true;
                    ski.LastUse = DateTime.Now;
                    await Task.Delay(skill.CastTime * 100);

                    Session.CurrentMap?.Broadcast($"bs 1 {Session.Character.CharacterId} {x} {y} {skill.SkillVNum} {skill.Cooldown} {skill.AttackAnimation} {skill.Effect} 0 0 1 1 0 0 0");

                    foreach (MapMonster mon in ServerManager.GetMap(Session.Character.MapId).GetListMonsterInRange(x, y, skill.TargetRange))
                    {
                        damage = GenerateDamage(mon.MapMonsterId, skill, ref hitmode);
                        Session.CurrentMap?.Broadcast($"su {1} {Session.Character.CharacterId} {3} {mon.MapMonsterId} {skill.SkillVNum} {skill.Cooldown} {skill.AttackAnimation} {skill.Effect} {x} {y} {(mon.Alive ? 1 : 0)} {(int)(((float)mon.CurrentHp / (float)ServerManager.GetNpc(mon.MonsterVNum).MaxHP) * 100)} {damage} {5} {skill.SkillType - 1}");
                    }

                    await Task.Delay((skill.Cooldown) * 100);
                    ski.Used = false;
                    Session.Client.SendPacket($"sr {Castingid}");
                }));
            }

            Session.Client.SendPacket("cancel 0 0");
        }

        #endregion
    }
}
