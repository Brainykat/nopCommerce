﻿using System;
using Nop.Core;
using Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Models;
using Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Services;

namespace Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Factories
{
    public class AuthenticationModelFactory
    {
        #region Fields

        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly GoogleAuthenticatorService _googleAuthenticatorService;
        private readonly GoogleAuthenticatorSettings _googleAuthenticatorSettings;

        #endregion

        #region Ctor

        public AuthenticationModelFactory(IStoreContext storeContext,
            GoogleAuthenticatorService googleAuthenticatorService,
            IWorkContext workContext,
            GoogleAuthenticatorSettings googleAuthenticatorSettings)
        {
            _storeContext = storeContext;
            _workContext = workContext;
            _googleAuthenticatorService = googleAuthenticatorService;
            _googleAuthenticatorSettings = googleAuthenticatorSettings;
        }

        #endregion
        
        #region Methods

        public AuthModel PrepareAuthModel(AuthModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var secretkey = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10);
            var setupInfo = _googleAuthenticatorService.GenerateSetupCode(secretkey);

            model.SecretKey = secretkey;
            model.Account = $"{_googleAuthenticatorSettings.BusinessPrefix} ({_workContext.CurrentCustomer.Email})";
            model.ManualEntryQrCode = setupInfo.ManualEntryKey;
            model.QrCodeImageUrl = setupInfo.QrCodeSetupImageUrl;

            return model;
        }

        #endregion
    }
}