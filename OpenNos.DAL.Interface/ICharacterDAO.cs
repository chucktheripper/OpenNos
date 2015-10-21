﻿using OpenNos.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.DAL.Interface
{
    public interface ICharacterDAO
    {
        IEnumerable<CharacterDTO> LoadByAccount(long accountId);
    }
}