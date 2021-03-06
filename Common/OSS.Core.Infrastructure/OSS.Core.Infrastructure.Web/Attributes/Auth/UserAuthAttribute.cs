﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using OSS.Common.BasicMos.Resp;
using OSS.Core.Context;
using OSS.Core.Context.Mos;
using OSS.Core.Infrastructure.Const;
using OSS.Core.Infrastructure.Web.Attributes.Auth.Interface;

namespace OSS.Core.Infrastructure.Web.Attributes.Auth
{
    /// <summary>
    ///  服务接口用户校验
    /// </summary>
    public class UserAuthAttribute : BaseOrderAuthAttribute
    {
        private readonly UserAuthOption _userOption;

        public UserAuthAttribute(UserAuthOption userOption)
        {
            if (userOption.UserProvider == null)
                throw new Exception("UserAuthOption 中 UserProvider 接口对象必须提供！");

            p_Order     = -10;
            _userOption = userOption;
            p_IsWebSite = userOption.IsWebSite;
        }

        public override async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (UserContext.IsAuthenticated)
                return;

            var appInfo = AppReqContext.Identity;

            var res =await FormatUserIdentity(context, appInfo, _userOption);
            if (!res.IsSuccess())
            {
                ResponseEnd(context, res);
                return;
            }

            res = await CheckFunc(context.HttpContext, appInfo, _userOption);
            if (!res.IsSuccess())
            {
                ResponseEnd(context, res);
            }
        }


        private static async Task<Resp> FormatUserIdentity(AuthorizationFilterContext context,AppIdentity appInfo,UserAuthOption opt)
        {
            if (context.ActionDescriptor.EndpointMetadata.Any(filter => filter is IAllowAnonymous))
                return new Resp();

            if (opt.IsWebSite && string.IsNullOrEmpty(appInfo.token))
                appInfo.token = context.HttpContext.Request.Cookies[CookieKeys.UserCookieName];

            if (string.IsNullOrEmpty(appInfo.token))
            {
                return new Resp().WithResp(RespTypes.UnLogin, "请先登录！");
            }

            var identityRes = await opt.UserProvider.InitialAuthUserIdentity(context.HttpContext, appInfo);
            if (!identityRes.IsSuccess())
                return identityRes;

            UserContext.SetIdentity(identityRes.data);
            return identityRes;
        }

        private static Task<Resp> CheckFunc(HttpContext context, AppIdentity appInfo, UserAuthOption opt)
        {
            var userInfo = UserContext.Identity;
            if (userInfo == null // 非需授权认证请求
                || opt.FuncProvider == null 
                || userInfo.auth_type == PortalAuthorizeType.SuperAdmin)
                return Task.FromResult(new Resp());

            return opt.FuncProvider.CheckFuncPermission(context, userInfo, appInfo.func);
        }

    }
    public class UserAuthOption : BaseAuthOption
    {
        /// <summary>
        ///  功能方法权限判断接口
        /// </summary>
        public IFuncAuthProvider FuncProvider { get; set; }

        /// <summary>
        ///  用户授权登录判断接口
        /// </summary>
        public IUserAuthProvider UserProvider { get; set; }

    }
}
