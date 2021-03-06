﻿using System.Threading.Tasks;
using OSS.Common.BasicMos.Resp;
using OSS.Core.RepDapper.Basic.Portal.Mos;

namespace OSS.Core.Services.Basic.Portal.IProxies
{
  public interface IUserServiceProxy
  {
      Task<Resp<UserBasicMo>> GetUserById(string userId);
  }
}
