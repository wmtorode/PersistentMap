﻿using Castle.DynamicProxy;
using PersistentMapAPI;
using PersistentMapServer.Attribute;
using System;
using System.Net;
using System.ServiceModel.Web;

namespace PersistentMapServer.Interceptor {

    /* Castle.DynamicProxy Interceptor that looks for methods decorated with the UserQuotaAttribute. Invocations of these methods are checked 
     *   to ensure a user isn't trying to send too many of them at once. 
     *   
     *   At this point in time, all requests are constrained by Settings.minMinutesBetweenPost. This value may need to be flexible to support
     *     POST methods other than MissionResults. The user's likely to send multiple shop purchase orders during that time, for instance, 
     *     and if we want quotas there this needs to be refined.
     */
    class UserQuotaInterceptor : IInterceptor {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public void Intercept(IInvocation invocation) {

            string requestIP = Helper.mapRequestIP();
            string obfuscatedIP = Helper.HashAndTruncate(requestIP);

            bool preventMethodInvocation = false;
            foreach (System.Attribute attribute in invocation.GetConcreteMethod().GetCustomAttributes(false)) {
                if (attribute.GetType() == typeof(UserQuotaAttribute)) {
                    // Method is decorated with UserQuotaAttribute

                    if (Holder.connectionStore.ContainsKey(requestIP) && Holder.connectionStore[requestIP].LastDataSend != null) {
                        // We have seen this IP before
                        UserInfo info = Holder.connectionStore[requestIP];
                        PersistentMapAPI.Settings settings = Helper.LoadSettings();

                        string lastDateSendISO = info.LastDataSend.ToString("u");
                        DateTime now = DateTime.UtcNow;
                        DateTime blockedUntil = info.LastDataSend.AddMinutes(settings.minMinutesBetweenPost);
                        TimeSpan delta = now.Subtract(info.LastDataSend);
                        string deltaS = $"{(int)delta.TotalMinutes}:{delta.Seconds:00}";
                        if (now >= blockedUntil) {
                            // The user hasn't sent a message within the time limit, so just note it when tracing is enabled
                            logger.Trace($"IP:{(settings.Debug ? requestIP : obfuscatedIP)} last send a request {deltaS} ago.");
                        } else {
                            // User is flooding. We should send back a 429 (Too Many Requests) but WCF isn't there yet. Send back a 403 for now.
                            // TODO: Verify this breaks the client as expected - with an error (cannot upload)
                            // TOOD: Add a better error message on the client for this case
                            string floodingMsg = $"IP: Flooding from IP:({(settings.Debug ? requestIP : obfuscatedIP)}) - last successful request was ({lastDateSendISO}) which was {deltaS} ago.";
                            if (((UserQuotaAttribute)attribute).enforcementPolicy == UserQuotaAttribute.EnforcementEnum.Block) {
                                WebOperationContext context = WebOperationContext.Current;
                                context.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                                context.OutgoingResponse.StatusDescription = $"Too many requests - try again later.";
                                logger.Info(floodingMsg);
                                preventMethodInvocation = true;
                            } else {
                                // The attribute is marked as log only, so log a warning
                                logger.Warn(floodingMsg);
                            }
                        }
                    } else {
                        // We haven't seen this IP before, so go ahead and let it through
                        logger.Trace($"IP: Unrecognized IP, so allowing request.");
                    }                    
                }
            }
            if (preventMethodInvocation) {
                // Prevent the method from executing
                invocation.ReturnValue = null;
            } else {
                // Add or set the userInfo
                UserInfo info;
                if (!Holder.connectionStore.ContainsKey(requestIP)) {
                    info = new UserInfo();
                    // Trust that any request beyond this adds the company name and lastSystemFoughtAt attributes. 
                    // TODO: Improve this somehow, to identify users?
                    info.lastSystemFoughtAt = "";
                    info.companyName = "";                
                    info.LastDataSend = DateTime.UtcNow;
                    Holder.connectionStore.Add(requestIP, info);
                } else {
                    info = Holder.connectionStore[requestIP];
                    info.LastDataSend = DateTime.UtcNow;
                    Holder.connectionStore[requestIP] = info;
                }
                
                // Allow the method to execute normally
                invocation.Proceed();
            }
        }

    }
}
