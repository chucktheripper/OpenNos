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
using OpenNos.DAL.EF.MySQL.DB;
using OpenNos.DAL.EF.MySQL.Helpers;
using OpenNos.DAL.Interface;
using OpenNos.Data;
using OpenNos.Data.Enums;
using OpenNos.Domain;
using System;
using System.Linq;

namespace OpenNos.DAL.EF.MySQL
{
    public class AccountDAO : IAccountDAO
    {
        #region Members

        private IMapper _mapper;

        #endregion

        #region Instantiation

        public AccountDAO()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Account, AccountDTO>();
                cfg.CreateMap<AccountDTO, Account>();
            });

            _mapper = config.CreateMapper();
        }

        #endregion

        #region Methods

        public DeleteResult Delete(long accountId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    Account Account = context.Account.FirstOrDefault(c => c.AccountId.Equals(accountId));

                    if (Account != null)
                    {
                        context.Account.Remove(Account);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Log.Error(String.Format(Language.Instance.GetMessageFromKey("DELETE_Account_ERROR"), accountId, e.Message), e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref AccountDTO account)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long AccountId = account.AccountId;
                    Account entity = context.Account.FirstOrDefault(c => c.AccountId.Equals(AccountId));

                    if (entity == null) //new entity
                    {
                        account = Insert(account, context);
                        return SaveResult.Inserted;
                    }
                    else //existing entity
                    {
                        account = Update(entity, account, context);
                        return SaveResult.Updated;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log.Error(String.Format(Language.Instance.GetMessageFromKey("UPDATE_Account_ERROR"), account.AccountId, e.Message),e);
                return SaveResult.Error;
            }
        }

        public AccountDTO LoadById(long accountId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                Account Account = context.Account.FirstOrDefault(a => a.AccountId.Equals(accountId));

                if (Account != null)
                {
                    return _mapper.Map<AccountDTO>(Account);
                }
            }

            return null;
        }

        public AccountDTO LoadByName(string name)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                Account Account = context.Account.FirstOrDefault(a => a.Name.Equals(name));

                if (Account != null)
                {
                    return _mapper.Map<AccountDTO>(Account);
                }
            }

            return null;
        }

        public AccountDTO LoadBySessionId(int sessionId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                Account Account = context.Account.FirstOrDefault(a => a.LastSession.Equals(sessionId));

                if (Account != null)
                {
                    return _mapper.Map<AccountDTO>(Account);
                }
            }

            return null;
        }

        public void LogIn(string name)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                Account Account = context.Account.FirstOrDefault(a => a.Name.Equals(name));
                context.SaveChanges();
            }
        }

        public void ToggleBan(long id)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                Account Account = context.Account.FirstOrDefault(a => a.AccountId.Equals(id));
                Account.Authority = Account.Authority == AuthorityType.User ? AuthorityType.Banned : AuthorityType.User;
                context.SaveChanges();
            }
        }

        public void UpdateLastSessionAndIp(string name, int session, string ip)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                Account Account = context.Account.FirstOrDefault(a => a.Name.Equals(name));
                Account.LastSession = session;
                context.SaveChanges();
            }
        }

        public void WriteGeneralLog(long accountId, string ipAddress, long? CharacterId, string logType, string logData)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                GeneralLog log = new GeneralLog()
                {
                    AccountId = accountId,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.Now,
                    LogType = logType,
                    LogData = logData,
                    CharacterId = CharacterId
                };

                context.GeneralLog.Add(log);
                context.SaveChanges();
            }
        }

        private AccountDTO Insert(AccountDTO account, OpenNosContext context)
        {
            Account entity = _mapper.Map<Account>(account);
            context.Account.Add(entity);
            context.SaveChanges();
            return _mapper.Map<AccountDTO>(entity);
        }

        private AccountDTO Update(Account entity, AccountDTO account, OpenNosContext context)
        {
            entity = _mapper.Map<Account>(account);
            context.SaveChanges();
            return _mapper.Map<AccountDTO>(entity);
        }

        #endregion
    }
}